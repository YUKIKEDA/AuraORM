English | [日本語](https://github.com/YUKIKEDA/AuraORM/blob/main/README.ja.md)

# AuraORM

[![CI](https://github.com/YUKIKEDA/AuraORM/actions/workflows/ci.yml/badge.svg)](https://github.com/YUKIKEDA/AuraORM/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://github.com/YUKIKEDA/AuraORM/blob/main/LICENSE)

A type-safe SQL query builder for .NET: you write SQL clause by clause in C#, the compiler rejects the queries that are wrong, and you can read the SQL before it runs.

- **SQL stays SQL.** One builder call is one SQL clause. AuraORM adds no query, join, subquery, `DISTINCT`, `ORDER BY`, `LIMIT`, or round trip that you did not write, and the same chain gives the same SQL text for any value. What the API cannot express, a raw SQL fragment can, with its values still parameters.
- **Types reject only invalid queries.** A column of a table that is not in the query, operands of different types, a clause in the wrong order, a missing required column in `INSERT`, an `UPDATE` or `DELETE` without `WHERE`, an aggregate mixed with row values, a feature the database lacks: these do not compile.
- **Values are parameters.** No value reaches the SQL text, and identifiers are always quoted.
- **Safe for code written by AI agents.** A mistake surfaces as a compile error with a stable id and a message that names the fix, not as a wrong result at run time.
- **NativeAOT and trimming.** No reflection, no expression trees, no dynamic code. The code that reads rows is generated at compile time.
- **Small.** The runtime library depends only on the .NET base library and `System.Data.Common`. Bring any ADO.NET provider.

```shell
dotnet add package AuraORM
dotnet add package Microsoft.Data.Sqlite
```

## Before the first query

- **The dialect is a type.** `Database<SqliteDialect>`, `Database<PostgreSqlDialect>`, `Database<MySqlDialect>`, `Database<MariaDbDialect>`, and `Database<SqlServerDialect>`. A feature that a database lacks is a compile error on that dialect, not a run-time error.
- **Functions come from `using static AuraORM.Sql;`**: `Eq`, `Gt`, `And`, `Count`, `Asc`, `Set`, and so on. Comparisons are functions, not C# operators, so the types of both sides are checked.
- **Columns come only from the lambda's parameters.** `From(Book.Table).Where(b => ...)` gives `b`, the columns of that occurrence of the table. A column of a table that is not in the statement cannot be written.
- **A statement is a value.** Every builder call returns a new immutable stage; nothing runs until `ToListAsync`, `ExecuteAsync`, or another execution method. `ToSql()` returns the SQL and the parameters without a connection.
- **Every execution is asynchronous** and takes a `CancellationToken`. There is no synchronous API.
- **A 0.x minor version may break the public API.** The changes of each version are in [`CHANGELOG.md`](https://github.com/YUKIKEDA/AuraORM/blob/main/CHANGELOG.md).
- **Turn on nullable reference types** (see [Nullable reference types](#nullable-reference-types)). AuraORM relies on them for `string` and `byte[]`.

## A first example

Declare a table as a `partial` record. The source generator in the package writes its typed columns, the model of an `INSERT`, and the reading of its rows:

```csharp
using AuraORM;

[Table("authors")]
public sealed partial record Author([PrimaryKey] long Id, [MaxLength(100)] string Name, string? Country);

[Table("books")]
[Index("ix_books_author", nameof(AuthorId))]
public sealed partial record Book(
    [PrimaryKey, AutoIncrement] long Id,
    [MaxLength(200)] string Title,
    [References(typeof(Author), nameof(Author.Id))] long AuthorId,
    int Pages,
    Genre Genre,
    [Precision(10, 2)] decimal Price,
    [Default(SqlDefault.CurrentTimestamp)] DateTime Added
);

public enum Genre
{
    Fiction,
    Science,
}
```

Open a connection, create the tables, insert rows, and query them:

```csharp
using AuraORM;
using Microsoft.Data.Sqlite;
using static AuraORM.Sql;

await using var connection = new SqliteConnection("Data Source=:memory:");
await connection.OpenAsync();
var db = new Database<SqliteDialect>(connection);

await db.CreateTable(Author.Table).ExecuteAsync();
await db.CreateTable(Book.Table).ExecuteAsync();
await db.CreateIndexes(Book.Table).ExecuteAsync();

await db.Insert(Author.Table)
    .Values(
        [
            new Author.Insert { Id = 1, Name = "Frank Herbert", Country = "US" },
            new Author.Insert { Id = 2, Name = "Carl Sagan", Country = "US" },
            new Author.Insert { Id = 3, Name = "Stanisław Lem", Country = null },
        ]
    )
    .ExecuteAsync();

await db.Insert(Book.Table)
    .Values(
        [
            new Book.Insert { Title = "Dune", AuthorId = 1, Pages = 412, Genre = Genre.Fiction, Price = 9.99m },
            new Book.Insert { Title = "Children of Dune", AuthorId = 1, Pages = 444, Genre = Genre.Fiction, Price = 8.99m },
            new Book.Insert { Title = "Cosmos", AuthorId = 2, Pages = 365, Genre = Genre.Science, Price = 12.50m },
        ]
    )
    .ExecuteAsync();

List<(string Title, int Pages)> longBooks = await db.From(Book.Table)
    .Where(b => Gt(b.Pages, 400))
    .Select(b => (b.Title, b.Pages))
    .OrderBy(b => [Asc(b.Title)])
    .ToListAsync();
```

`Book.Insert` has no `Id`, because the database numbers it (`[AutoIncrement]`), and no required `Added`, because the column has a default. A missing required member, such as `Title`, does not compile (CS9035).

## Usage

The examples below continue the one above: the same `db`, tables, and rows. Every `csharp` block of this README is compiled against the package and run in CI (`tools/readme.ps1`), and the SQL shown in a comment is what the example prints.

### Tables

A table is a `partial` record (or class) with `[Table]`. Each member is a column; its CLR type decides the SQL type on each dialect.

| Attribute | Meaning |
| --- | --- |
| `[Table("name", Schema = "...", Naming = ColumnNaming.SnakeCase)]` | The table, its schema, and how member names become column names |
| `[Column("name")]` | The column name of one member |
| `[PrimaryKey]` | A column of the primary key (several members make a composite key) |
| `[AutoIncrement]` | A key the database numbers; it is not in the `INSERT` model |
| `[MaxLength(n)]`, `[Precision(p, s)]` | The length of a `string` or `byte[]`, the precision and scale of a `decimal` |
| `[Default(value)]`, `[Default(SqlDefault.CurrentTimestamp)]`, `[DefaultSql("...")]` | The default of `CREATE TABLE`; the column becomes optional in `INSERT` |
| `[Unique]`, `[Index("name", ...)]`, `[References(typeof(T), nameof(T.Key))]` | Constraints and indexes of `CREATE TABLE` and `CREATE INDEX` |
| `[NotColumn]` | A property that is not a column |

A column's CLR type decides its SQL type in `CREATE TABLE`. Its nullable form (`int?`, `string?`) makes the column nullable; a type of your own needs a [value converter](#value-converters).

| CLR type | SQLite | PostgreSQL | MySQL, MariaDB | SQL Server |
| --- | --- | --- | --- | --- |
| `bool` | `INTEGER` | `boolean` | `BOOLEAN` | `bit` |
| `byte`, `short`, `int`, `long` | `INTEGER` | `smallint`, `smallint`, `integer`, `bigint` | `TINYINT UNSIGNED`, `SMALLINT`, `INT`, `BIGINT` | `tinyint`, `smallint`, `int`, `bigint` |
| `float`, `double` | `REAL` | `real`, `double precision` | `FLOAT`, `DOUBLE` | `real`, `float` |
| `decimal` with `[Precision(p, s)]` | `TEXT` | `numeric(p,s)` | `DECIMAL(p,s)` | `decimal(p,s)` |
| `string` (with `[MaxLength(n)]`) | `TEXT` | `text` (`varchar(n)`) | `LONGTEXT` (`VARCHAR(n)`) | `nvarchar(max)` (`nvarchar(n)`) |
| `byte[]` (with `[MaxLength(n)]`) | `BLOB` | `bytea` | `LONGBLOB` (`VARBINARY(n)`) | `varbinary(max)` (`varbinary(n)`) |
| `Guid` | `TEXT` | `uuid` | `CHAR(36)` | `uniqueidentifier` |
| `DateTime`, `DateTimeOffset` | `TEXT` | `timestamp`, `timestamp with time zone` | `DATETIME(6)` | `datetime2`, `datetimeoffset` |
| `DateOnly`, `TimeOnly` | `TEXT` | `date`, `time` | `DATE`, `TIME(6)` | `date`, `time` |
| An enum | Its underlying integer type | | | |

A schema mistake is a generator error (`AURA0101` to `AURA0119`): a `decimal` without `[Precision]`, a key or index on a `string` without `[MaxLength]`, two columns with one name, and so on. SQLite stores a `decimal` as text, so on SQLite a `decimal` is compared for equality only: ordering by it, or comparing it by size, throws before the statement is sent.

The generator writes, for `Book`:

- `Book.Table`, the table, and `Book.Table.As("b2")` for another occurrence of it (a self-join);
- `Book.Columns`, the typed columns that the lambdas receive (`Column<string>`, `Column<int>`, ...);
- `Book.Insert`, the model of an `INSERT`, with `required` members for the columns that have no default;
- the reading of a row, with the typed getters of the reader by ordinal, without boxing.

### SELECT

The clauses come in SQL's order, and each stage offers only the clauses that may follow it:

```csharp
// WHERE with several conditions, a projection to a tuple, ORDER BY, LIMIT, and OFFSET.
List<(string Title, decimal Price)> page = await db.From(Book.Table)
    .Where(b => And(Eq(b.Genre, Genre.Fiction), Lt(b.Pages, 450)))
    .Select(b => (b.Title, b.Price))
    .OrderBy(b => [Desc(b.Pages), Asc(b.Id)])
    .Limit(10)
    .Offset(0)
    .ToListAsync();

// A condition that is there only when a filter is set: null leaves it out of the SQL.
string? titleFilter = null;
List<long> ids = await db.From(Book.Table)
    .Where(b => And([titleFilter is null ? null : Like(b.Title, titleFilter), Gt(b.Pages, 100)]))
    .Select(b => b.Id)
    .ToListAsync();

// One row, and the whole row as the record.
Book cosmos = await db.From(Book.Table).Where(b => Eq(b.Title, "Cosmos")).Select(b => b).SingleAsync();
(long count, int? maxPages) = await db.From(Book.Table).Select(b => (Count(), Max(b.Pages))).SingleAsync();

// DISTINCT, and streaming the rows one at a time.
List<Genre> genres = await db.From(Book.Table).SelectDistinct(b => b.Genre).OrderBy(g => [Asc(g)]).ToListAsync();
await foreach (string title in db.From(Book.Table).Select(b => b.Title).OrderBy(b => [Asc(b.Id)]).AsAsyncEnumerable())
{
    Console.WriteLine(title);
}
```

`FirstAsync`, `FirstOrDefaultAsync`, `SingleAsync`, and `SingleOrDefaultAsync` read one row; `SingleAsync` throws when there is more than one. A comparison with `null` is not a comparison: `Eq(a.Country, null)` is an analyzer error (`AURA0201`) that asks for `IsNull(a.Country)`.

### Joins

A join gives the next lambda one more parameter. On the side of an outer join that may have no row, every column becomes nullable, and the whole row too:

```csharp
// INNER JOIN.
List<(string Book, string Author)> written = await db.From(Book.Table)
    .InnerJoin(Author.Table, (b, a) => Eq(a.Id, b.AuthorId))
    .Select((b, a) => (b.Title, a.Name))
    .OrderBy((b, a) => [Asc(b.Id)])
    .ToListAsync();

// LEFT JOIN: the book side may be missing, so b.Title is a string? and b is a Book?.
List<(string Author, string? Title)> withBooks = await db.From(Author.Table)
    .LeftJoin(Book.Table, (a, b) => Eq(b.AuthorId, a.Id))
    .Select((a, b) => (a.Name, b.Title))
    .ToListAsync();

// Two occurrences of one table: As gives the second one its own alias.
long pairs = await db.From(Book.Table)
    .InnerJoin(Book.Table.As("later"), (b, later) => And(Eq(later.AuthorId, b.AuthorId), Gt(later.Id, b.Id)))
    .Select((b, later) => Count())
    .SingleAsync();
```

`RightJoin`, `FullJoin`, and `CrossJoin` work the same way; a dialect without `FULL JOIN` (MySQL, MariaDB) does not have the method.

### GROUP BY, HAVING, and result types

Aggregates are evaluated per group and row values per row, and the types keep them apart: an aggregate in `WHERE`, or a column that is not a key in the select list of a grouped query, does not compile. A `[QueryResult]` type names the columns of a result:

```csharp
using AuraORM;

[QueryResult]
public sealed partial record AuthorStats(string Name, long Books, int? Longest);
```

```csharp
List<AuthorStats> stats = await db.From(Author.Table)
    .InnerJoin(Book.Table, (a, b) => Eq(b.AuthorId, a.Id))
    .GroupBy((a, b) => a.Name)
    .Having((name, a, b) => Ge(Count(), 1L))
    .Select((name, a, b) => new AuthorStats.GroupProjection { Name = name, Books = Count(), Longest = Max(b.Pages) })
    .OrderBy((name, a, b) => [Desc(Count()), Asc(name)])
    .ToListAsync();
```

The first parameter of the lambdas after `GroupBy` is the key. Each member of a result type is set by name, so a reordered or missing member is a compile error, not a column read into the wrong place.

### Subqueries, set operations, and CTEs

A subquery is a statement passed where a value or a set is expected. A correlated subquery reads the outer row from the outer lambda's parameter:

```csharp
// IN, EXISTS, and a scalar subquery in the select list.
List<string> withScience = await db.From(Author.Table)
    .Where(a => In(a.Id, db.From(Book.Table).Where(b => Eq(b.Genre, Genre.Science)).Select(b => b.AuthorId)))
    .Select(a => a.Name)
    .ToListAsync();

List<string> withoutBooks = await db.From(Author.Table)
    .Where(a => Not(Exists(db.From(Book.Table).Where(b => Eq(b.AuthorId, a.Id)).Select(b => b.Id))))
    .Select(a => a.Name)
    .ToListAsync();

List<(string Name, int? Longest)> longest = await db.From(Author.Table)
    .Select(a => (a.Name, Scalar(db.From(Book.Table).Where(b => Eq(b.AuthorId, a.Id)).Select(b => Max(b.Pages)))))
    .ToListAsync();

// UNION ALL of two statements with the same result type.
List<string> names = await db.From(Author.Table)
    .Select(a => a.Name)
    .UnionAll(db.From(Book.Table).Select(b => b.Title))
    .ToListAsync();

// A CTE: a statement named with As, listed in With, and read like a table.
var fiction = db.From(Book.Table).Where(b => Eq(b.Genre, Genre.Fiction)).Select(b => b).As("fiction");
List<string> fictionTitles = await db.With(fiction).From(fiction).Select(f => f.Title).OrderBy(f => [Asc(f.Title)]).ToListAsync();
```

`Union`, `Intersect`, and `Except` combine statements of the same result type. The same named statement in `From` without `With` is a derived table.

### INSERT, UPDATE, DELETE, and RETURNING

`UPDATE` and `DELETE` need a `Where`, or `AllRows()` to say that every row is meant; without one there is no `ExecuteAsync`. `ExecuteAsync` returns the number of rows. On the dialects that have `RETURNING` (SQLite, PostgreSQL, MariaDB for `INSERT` and `DELETE`, and SQL Server as `OUTPUT`), `Returning` reads rows back:

```csharp
// Read the numbered key back with RETURNING.
long newId = await db.Insert(Book.Table)
    .Values(new Book.Insert { Title = "Solaris", AuthorId = 3, Pages = 204, Genre = Genre.Fiction, Price = 11m })
    .Returning(b => b.Id)
    .SingleAsync();

// Set a column to an expression of the row, then delete rows.
int repriced = await db.Update(Book.Table)
    .Set(b => [Set(b.Price, Multiply(b.Price, 0.9m))])
    .Where(b => Eq(b.AuthorId, 1L))
    .ExecuteAsync();

int removed = await db.Delete(Book.Table).Where(b => Lt(b.Pages, 100)).ExecuteAsync();
```

A column the database numbers cannot be set: `Set(b.Id, 5)` is an error that says why (CS0619).

### UPSERT

SQLite and PostgreSQL write `ON CONFLICT`; MySQL and MariaDB write `ON DUPLICATE KEY UPDATE`. Each dialect has only its own form:

```csharp
await db.Insert(Author.Table)
    .Values(new Author.Insert { Id = 3, Name = "Stanisław Lem", Country = "PL" })
    .OnConflict(a => [a.Id])
    .DoUpdate((a, excluded) => [Set(a.Country, excluded.Country)])
    .ExecuteAsync();
```

`DoNothing()` and `OnConflictDoNothing()` skip the row instead. On MySQL and MariaDB the same row is `OnDuplicateKeyUpdate((a, row) => [Set(a.Country, row.Country)])`.

### Dialects

Each dialect type declares what its database has. A method that needs a feature exists only on the dialects that declare it, so the error is the compiler's:

| Feature | SQLite | PostgreSQL | MySQL | MariaDB | SQL Server |
| --- | --- | --- | --- | --- | --- |
| `FullJoin` | ✓ | ✓ | | | ✓ |
| `Returning` after `INSERT` / `UPDATE` / `DELETE` | ✓ / ✓ / ✓ | ✓ / ✓ / ✓ | | ✓ / – / ✓ | ✓ / ✓ / ✓ (`OUTPUT`) |
| `Limit` without `OrderBy` | ✓ | ✓ | ✓ | ✓ | |
| `Offset` without `Limit` | | ✓ | | | ✓ (after `OrderBy`) |
| `Limit` inside an `In` subquery | ✓ | ✓ | | | ✓ |
| `OnConflict` / `OnDuplicateKeyUpdate` | ✓ / – | ✓ / – | – / ✓ | – / ✓ | |
| `CreateTable(...).IfNotExists()` | ✓ | ✓ | ✓ | ✓ | |
| DDL inside a transaction | ✓ | ✓ | | | ✓ |

On SQL Server 2019, `IsDistinctFrom` is written as an expansion that gives the same result, since `IS DISTINCT FROM` came in 2022.

### Transactions

`InTransactionAsync` commits when the delegate returns and rolls back when it throws. `BeginTransactionAsync` gives a `Transaction<TDialect>` to commit yourself:

```csharp
await db.InTransactionAsync(async tx =>
{
    await tx.Update(Author.Table).Set(a => [Set(a.Country, "USA")]).Where(a => Eq(a.Country, "US")).ExecuteAsync();
    await tx.Delete(Book.Table).Where(b => Eq(b.Title, "Solaris")).ExecuteAsync();
});
```

A `Database` opens and closes the connection for each statement when it is given a closed connection or a `DbDataSource`, and leaves an open one open.

### Raw SQL

A fragment is an interpolated string: each hole is a parameter or, for a table, a column, an expression, or `Identifier("name")`, that part of the SQL. A plain `string` is not accepted, so SQL cannot be built by concatenation:

```csharp
// A typed fragment inside a builder query.
List<(string Title, int Length)> lengths = await db.From(Book.Table)
    .Select(b => (b.Title, Raw<int>($"length({b.Title})")))
    .ToListAsync();

// A whole query in SQL, read into a row type or a tuple by column name or position.
int minimum = 400;
List<(string, int)> raw = await db.QueryAsync<string, int>($"SELECT title, pages FROM {Book.Table} WHERE pages > {minimum} ORDER BY title");
```

### The SQL of a statement

`ToSql()` returns the SQL and the parameters without running. `SqlBuilder<TDialect>` builds statements for a dialect without a connection; they have no execution methods (CS1929):

```csharp
SqlStatement statement = db.From(Book.Table).Where(b => Gt(b.Pages, 400)).Select(b => b.Title).ToSql();
Console.WriteLine(statement.Text);
// SELECT "books"."Title" FROM "books" WHERE "books"."Pages" > @p0

Console.WriteLine(new SqlBuilder<PostgreSqlDialect>().From(Book.Table).Select(b => b.Title).OrderBy(b => [Asc(b.Title)]).Limit(5).ToSql().Text);
// SELECT "books"."Title" FROM "books" ORDER BY "books"."Title" ASC LIMIT $1

Console.WriteLine(new SqlBuilder<SqlServerDialect>().From(Book.Table).Select(b => b.Title).OrderBy(b => [Asc(b.Title)]).Limit(5).ToSql().Text);
// SELECT [books].[Title] FROM [books] ORDER BY [books].[Title] ASC OFFSET 0 ROWS FETCH NEXT @p0 ROWS ONLY
```

On SQL Server, `Limit` without `OrderBy` does not compile, because SQL Server needs an `ORDER BY` there.

### Value converters

A type of your own is stored as a column type through a converter, declared on the type or on the assembly:

```csharp
using AuraORM;

[ValueConverter(typeof(IsbnConverter))]
public readonly record struct Isbn(string Value);

public sealed class IsbnConverter : IValueConverter<Isbn, string>
{
    public static string ToProvider(Isbn value) => value.Value;

    public static Isbn FromProvider(string value) => new(value);
}
```

A column of `Isbn` is a `Column<Isbn>`; it compares with `Isbn` values, its parameters hold the `string`, and its rows are read back as `Isbn`. The conversion is generated code, with no reflection.

## Nullable reference types

Whether a `string` or `byte[]` value can be `NULL` is part of its type only when nullable reference types are on. Turn them on, and make their warnings errors, in the project that uses AuraORM:

```xml
<PropertyGroup>
  <Nullable>enable</Nullable>
  <WarningsAsErrors>nullable</WarningsAsErrors>
</PropertyGroup>
```

`string` and `string?` are the same type at run time, so AuraORM cannot reject a query on them by type alone. Without these settings, a `string` column on the nullable side of an outer join, or `null` given to a column of a `string` that does not allow `NULL`, compiles: with the settings it is an error (CS8619 or CS8600 for the join, CS8620 or CS8631 for `Set`). Value types (`int` and `int?`) are checked by type in either case. A table declared where nullable reference types are off is a generator error (`AURA0102`).

## What is promised

- **The SQL is the chain.** Each call is one clause; the same chain gives the same SQL text whatever the values, the culture, the time zone, or the machine. Values are parameters, numbered in the order they appear; an expression written twice in one statement (a `GROUP BY` key in the select list) keeps one parameter.
- **The invalid queries of [the design's list](https://github.com/YUKIKEDA/AuraORM/blob/main/docs/design.ja.md#141-コンパイルできないもの) do not compile.** Each line of that list has a compile test.
- **Every dialect has the same API**, and a feature a database lacks is a compile error on its dialect. The SQL of each statement on each dialect is a golden test, and the integration tests run every statement on every database at its oldest and newest supported version.
- **Reading rows allocates only the result** (the row objects, their strings and arrays), with no boxing. A CI test checks it for every column type.
- **NativeAOT publishes with no warning**, on Linux, Windows, and macOS, for `net8.0` and `net10.0`, from the package (CI).

## What is not

- **The contents of a raw SQL fragment**, beyond the types of its holes.
- **That the database matches the C# schema.** AuraORM does not read the schema at run time.
- **Differences between databases in results**: collation, time zones, numeric precision, and the order of rows without `ORDER BY`.
- **The correctness of a value converter** (that `FromProvider(ToProvider(x))` is `x`).
- **Change tracking, navigation properties, lazy loading, and LINQ.** AuraORM is not an object-relational mapper of that kind and translates nothing.

## Limits

| | |
| --- | --- |
| .NET | `net8.0` or later; NativeAOT and trimming |
| Databases | SQLite 3.39, PostgreSQL 15, MySQL 8.4, MariaDB 10.11, SQL Server 2019, or later |
| Providers | Any ADO.NET provider (`DbConnection` or `DbDataSource`); the tests use Microsoft.Data.Sqlite, Npgsql, MySqlConnector, and Microsoft.Data.SqlClient |
| Parameters per statement | The database's limit, checked before the statement is sent (for example 2,100 on SQL Server) |
| Dependencies | None beyond the .NET base library |

## Performance

Measured with BenchmarkDotNet on Windows 11, an Intel Core i5-13400F, and .NET 10.0.13 (Release, JIT); the method and every figure are in [the design, §16.1](https://github.com/YUKIKEDA/AuraORM/blob/main/docs/design.ja.md#161-目標).

| | Time | Allocated |
| --- | --- | --- |
| Reading a row of six columns from an in-memory reader | 22.7 ns (1.71 × a loop written by hand, 13.3 ns) | The row only |
| Building and rendering a `SELECT` with `WHERE` | 562 ns | 2,488 B |
| Building and rendering two joins with `GROUP BY` | 730 ns | 3,640 B |
| A round trip on SQLite in memory, 100 rows | 1.09 × hand-written ADO.NET | |

Most of the time of building a statement is its allocations: the stage, the alias, and the column objects that tie each column to its statement, and the rendering. A statement that runs many times is built again each time; reusing a built statement is planned after version 0.1.

## Documentation

| Document | Role |
| --- | --- |
| [`docs/design.ja.md`](https://github.com/YUKIKEDA/AuraORM/blob/main/docs/design.ja.md) | The design (Japanese, canonical): the SQL of each API on each dialect, the guarantees, the diagnostics |
| [`docs/design-decisions.md`](https://github.com/YUKIKEDA/AuraORM/blob/main/docs/design-decisions.md) | The decisions and the alternatives that were rejected |
| [`docs/roadmap.md`](https://github.com/YUKIKEDA/AuraORM/blob/main/docs/roadmap.md) | The order of work |
| [`CHANGELOG.md`](https://github.com/YUKIKEDA/AuraORM/blob/main/CHANGELOG.md) | The changes of each version |
| [`CONTRIBUTING.md`](https://github.com/YUKIKEDA/AuraORM/blob/main/CONTRIBUTING.md) | How to build, test, and contribute |
| [`SECURITY.md`](https://github.com/YUKIKEDA/AuraORM/blob/main/SECURITY.md) | Reporting vulnerabilities |

## Verification

CI is the merge gate, on Windows, Linux, and macOS (`.github/workflows/ci.yml`): the format check, Release and Debug builds with warnings as errors, the unit, golden SQL, compile, and generator tests, the integration tests against every database at its oldest and newest supported version, the NativeAOT sample published from the package, the contents of the package, and the examples of this README. Locally, `./build.ps1` runs the same build and tests, and `./tools/db.ps1` starts the databases.

## License

[MIT](https://github.com/YUKIKEDA/AuraORM/blob/main/LICENSE)
