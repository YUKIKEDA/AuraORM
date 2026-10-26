# AuraORM

A type-safe SQL query builder for .NET, in the spirit of [Drizzle ORM](https://orm.drizzle.team/).

> **Status: before the first release.** The API of version 0.1 is implemented, and nothing is published to NuGet yet. Follow the progress in [`docs/roadmap.md`](https://github.com/YUKIKEDA/AuraORM/blob/main/docs/roadmap.md).

[日本語](https://github.com/YUKIKEDA/AuraORM/blob/main/README.ja.md)

## Principles

- **SQL stays SQL.** The API follows SQL clause by clause. Anything valid in SQL that the API can express compiles; raw SQL fragments are available where it cannot, with values still bound as parameters.
- **Types reject only invalid queries.** A column of a table that is not in the query, operands of different types, a clause in the wrong order, a missing required column in `INSERT`, an `UPDATE` or `DELETE` without `WHERE`, a feature the database lacks: these do not compile.
- **Predictable SQL.** One builder call is one SQL clause. No hidden queries, joins, or round trips. Every query shows its SQL and parameters before it runs.
- **Safe for code written by AI agents.** Mistakes surface as compile errors with a stable diagnostic id and a message that names the fix, not as wrong results at run time.
- **NativeAOT and trimming.** No reflection, no expression trees, no dynamic code. Mapping code is generated at compile time.
- **Small.** The runtime library depends only on the .NET base library and `System.Data.Common`. Bring any ADO.NET provider.

## Usage

Add the package and an ADO.NET provider, here SQLite:

```shell
dotnet add package AuraORM
dotnet add package Microsoft.Data.Sqlite
```

Declare a table as a record. The source generator in the package writes its typed columns, the model of an `INSERT`, and the reading of its rows:

```csharp
using AuraORM;

[Table("books")]
public sealed partial record Book([PrimaryKey] long Id, string Title, string? Author, int Pages);
```

Create the table, insert rows, and query them. Each call is one clause of the SQL, and values are parameters:

```csharp
using AuraORM;
using Microsoft.Data.Sqlite;
using static AuraORM.Sql;

await using var connection = new SqliteConnection("Data Source=:memory:");
await connection.OpenAsync();
var db = new Database<SqliteDialect>(connection);

await db.CreateTable(Book.Table).ExecuteAsync();
await db.Insert(Book.Table)
    .Values(
        [
            new Book.Insert { Id = 1, Title = "Dune", Author = "Frank Herbert", Pages = 412 },
            new Book.Insert { Id = 2, Title = "Cosmos", Author = null, Pages = 365 },
        ]
    )
    .ExecuteAsync();

List<(string Title, int Pages)> longBooks = await db.From(Book.Table)
    .Where(b => Gt(b.Pages, 400))
    .Select(b => (b.Title, b.Pages))
    .OrderBy(b => [Asc(b.Title)])
    .ToListAsync();
```

`UPDATE` and `DELETE` need a `WHERE`, or `AllRows()` to say that every row is meant:

```csharp
await db.Update(Book.Table).Set(b => [Set(b.Author, "Carl Sagan")]).Where(b => Eq(b.Id, 2L)).ExecuteAsync();
await db.Delete(Book.Table).Where(b => Lt(b.Pages, 100)).ExecuteAsync();
```

Every statement shows its SQL and parameters without running:

```csharp
SqlStatement statement = db.From(Book.Table).Where(b => Gt(b.Pages, 400)).Select(b => b.Title).ToSql();
Console.WriteLine(statement.Text);
// SELECT "books"."Title" FROM "books" WHERE "books"."Pages" > @p0
```

## Nullable reference types

Whether a `string` or `byte[]` value can be `NULL` is part of its type only when nullable reference types are on. Turn them on, and make their warnings errors, in the project that uses AuraORM:

```xml
<PropertyGroup>
  <Nullable>enable</Nullable>
  <WarningsAsErrors>nullable</WarningsAsErrors>
</PropertyGroup>
```

`string` and `string?` are the same type at run time, so AuraORM cannot reject a query on them by type alone. Without these settings, a `string` column on the nullable side of an outer join, or `null` given to a column of a `string` that does not allow `NULL`, compiles: with the settings it is an error (CS8619 or CS8600 for the join, CS8620 or CS8631 for `Set`). Value types (`int` and `int?`) are checked by type in either case.

## Databases

SQLite, PostgreSQL, MySQL (and MariaDB), and SQL Server.

## Requirements

.NET 8 or later.

## Contributing

See [`CONTRIBUTING.md`](https://github.com/YUKIKEDA/AuraORM/blob/main/CONTRIBUTING.md).

## License

[MIT](https://github.com/YUKIKEDA/AuraORM/blob/main/LICENSE)
