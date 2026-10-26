[English](https://github.com/YUKIKEDA/AuraORM/blob/main/README.md) | 日本語

# AuraORM

[![CI](https://github.com/YUKIKEDA/AuraORM/actions/workflows/ci.yml/badge.svg)](https://github.com/YUKIKEDA/AuraORM/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://github.com/YUKIKEDA/AuraORM/blob/main/LICENSE)

.NET 向けの型安全な SQL クエリビルダー。SQL を句ごとに C# で書き、誤ったクエリはコンパイラーが拒み、実行する前に SQL を確認できる。

- **SQL は SQL のまま。** ビルダーの呼び出し一つが SQL の句一つになる。書いていないクエリ、結合、サブクエリ、`DISTINCT`、`ORDER BY`、`LIMIT`、往復を足さず、同じ連鎖は値によらず同じ SQL の文字列になる。例外は文の形の一部であるもの（`In` の値の数、`INSERT` の行と列、省いた省略可能な条件）である。API で書けないところは生 SQL の断片で書ける。その場合も値はパラメータになる。
- **型が拒むのは不正なクエリだけ。** クエリにないテーブルの列、型の違う比較、順序の誤った句、`INSERT` での必須列の欠落、`WHERE` のない `UPDATE` / `DELETE`、集計と行の値の混在、そのデータベースにない機能は、コンパイルが通らない。
- **値はパラメータになる。** クエリと変更の値が SQL の文字列に入ることはなく、識別子は常に引用符で囲む。値をリテラルとして書くのは、`CREATE TABLE` の列の既定値（`[Default(...)]`、スキーマの定数）だけである。
- **AI エージェントが書いたコードでも安全。** 誤りは実行時の誤った結果ではなく、固定の ID と直し方を示すメッセージを持つコンパイルエラーとして現れる。
- **NativeAOT とトリミングに対応する。** リフレクション、式ツリー、動的コード生成は使わない。行を読むコードはコンパイル時に生成する。
- **小さい。** ランタイムのライブラリは .NET の基本ライブラリと `System.Data.Common` だけに依存する。ADO.NET プロバイダーはどれでも使える。

```shell
dotnet add package AuraORM
dotnet add package Microsoft.Data.Sqlite
```

## 最初のクエリの前に

- **方言は型である。** `Database<SqliteDialect>`、`Database<PostgreSqlDialect>`、`Database<MySqlDialect>`、`Database<MariaDbDialect>`、`Database<SqlServerDialect>`。データベースにない機能は、実行時のエラーではなく、その方言でのコンパイルエラーになる。
- **関数は `using static AuraORM.Sql;` から使う。** `Eq`、`Gt`、`And`、`Count`、`Asc`、`Set` など。比較は C# の演算子ではなく関数なので、両辺の型が確かめられる。
- **列はラムダの引数からしか得られない。** `From(Book.Table).Where(b => ...)` の `b` が、そのテーブルの出現の列である。文にないテーブルの列は書けない。
- **文は値である。** ビルダーの呼び出しはどれも、変更できない新しい段階を返す。`ToListAsync`、`ExecuteAsync` などの実行のメソッドまで何も実行しない。`ToSql()` は、接続なしで SQL とパラメータを返す。
- **実行はすべて非同期で**、`CancellationToken` を受け取る。同期の API はない。
- **0.x のマイナーバージョンは公開の API を壊すことがある。** 各版の変更は [`CHANGELOG.md`](https://github.com/YUKIKEDA/AuraORM/blob/main/CHANGELOG.md) にある。
- **null 許容参照型を有効にする**（[null 許容参照型](#null-許容参照型)）。AuraORM は `string` と `byte[]` の NULL 可否をこれに頼る。

## 最初の例

テーブルを `partial` のレコードで宣言する。パッケージに入っているソースジェネレーターが、型付きの列、`INSERT` のモデル、行の読み取りを書く。

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

接続を開き、テーブルを作り、行を入れて、問い合わせる。

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

`Book.Insert` に `Id` はない（`[AutoIncrement]` でデータベースが採番する）。`Added` も必須ではない（列に既定値がある）。`Title` のような必須のメンバーがないと、コンパイルが通らない（CS9035）。

## 使い方

以下の例は、上の例の続きである（同じ `db`、テーブル、行を使う）。この README の `csharp` のブロックはすべて、CI がパッケージを参照してコンパイルし、実行する（`tools/readme.ps1`）。コメントに示した SQL は、その例が実際に出力するものである。

### テーブル

テーブルは `[Table]` を付けた `partial` のレコード（またはクラス）である。メンバーの一つ一つが列で、その CLR の型が方言ごとの SQL の型を決める。

| 属性 | 意味 |
| --- | --- |
| `[Table("name", Schema = "...", Naming = ColumnNaming.SnakeCase)]` | テーブル、そのスキーマ、メンバー名から列名を作る方法 |
| `[Column("name")]` | 一つのメンバーの列名 |
| `[PrimaryKey]` | 主キーの列（複数のメンバーに付けると複合キー） |
| `[AutoIncrement]` | データベースが採番するキー。`INSERT` のモデルには入らない |
| `[MaxLength(n)]`、`[Precision(p, s)]` | `string` / `byte[]` の長さ、`decimal` の精度と位取り |
| `[Default(value)]`、`[Default(SqlDefault.CurrentTimestamp)]`、`[DefaultSql("...")]` | `CREATE TABLE` の既定値。その列は `INSERT` で省ける |
| `[Unique]`、`[Index("name", ...)]`、`[References(typeof(T), nameof(T.Key))]` | `CREATE TABLE` と `CREATE INDEX` の制約と索引 |
| `[NotColumn]` | 列にしないプロパティ |

列の CLR の型が、`CREATE TABLE` の SQL の型を決める。NULL を許す形（`int?`、`string?`）にすると、NULL を許す列になる。自分の型を使うには[値コンバーター](#値コンバーター)が要る。

| CLR の型 | SQLite | PostgreSQL | MySQL、MariaDB | SQL Server |
| --- | --- | --- | --- | --- |
| `bool` | `INTEGER` | `boolean` | `BOOLEAN` | `bit` |
| `byte`、`short`、`int`、`long` | `INTEGER` | `smallint`、`smallint`、`integer`、`bigint` | `TINYINT UNSIGNED`、`SMALLINT`、`INT`、`BIGINT` | `tinyint`、`smallint`、`int`、`bigint` |
| `float`、`double` | `REAL` | `real`、`double precision` | `FLOAT`、`DOUBLE` | `real`、`float` |
| `[Precision(p, s)]` を付けた `decimal` | `TEXT` | `numeric(p,s)` | `DECIMAL(p,s)` | `decimal(p,s)` |
| `string`（`[MaxLength(n)]` 付き） | `TEXT` | `text`（`varchar(n)`） | `LONGTEXT`（`VARCHAR(n)`） | `nvarchar(max)`（`nvarchar(n)`） |
| `byte[]`（`[MaxLength(n)]` 付き） | `BLOB` | `bytea` | `LONGBLOB`（`VARBINARY(n)`） | `varbinary(max)`（`varbinary(n)`） |
| `Guid` | `TEXT` | `uuid` | `CHAR(36)` | `uniqueidentifier` |
| `DateTime`、`DateTimeOffset` | `TEXT` | `timestamp`、`timestamp with time zone` | `DATETIME(6)` | `datetime2`、`datetimeoffset` |
| `DateOnly`、`TimeOnly` | `TEXT` | `date`、`time` | `DATE`、`TIME(6)` | `date`、`time` |
| 列挙型 | 基になる整数型と同じ | | | |

スキーマの誤りはジェネレーターのエラー（`AURA0101`〜`AURA0119`）になる。`[Precision]` のない `decimal`、`[MaxLength]` のない `string` のキーや索引、同じ名前の二つの列などである。SQLite は `decimal` を文字列で保存するので、SQLite では `decimal` を等しいかどうかでしか比べられない。それで並べ替えたり大小を比べたりすると、文を送る前に例外になる。

ジェネレーターは `Book` について次を書く。

- `Book.Table`（テーブル）と、そのもう一つの出現を表す `Book.Table.As("b2")`（自己結合）
- `Book.Columns`（ラムダが受け取る型付きの列。`Column<string>`、`Column<int>` など）
- `Book.Insert`（`INSERT` のモデル。既定値のない列は `required` のメンバー）
- 行の読み取り（リーダーの型付きの取得メソッドを位置で呼び、ボックス化しない）

### SELECT

句は SQL の順に書く。どの段階も、その後に書ける句だけを持つ。

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

`FirstAsync`、`FirstOrDefaultAsync`、`SingleAsync`、`SingleOrDefaultAsync` は一行を読む。`SingleAsync` は二行以上あると例外を投げる。`null` との比較は比較にならないので、`Eq(a.Country, null)` はアナライザーのエラー（`AURA0201`）になり、`IsNull(a.Country)` を書くよう示す。

### 結合

結合すると、次のラムダの引数が一つ増える。外部結合で行がないかもしれない側は、どの列も NULL を許す型になり、行全体も NULL を許す型になる。

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

`RightJoin`、`FullJoin`、`CrossJoin` も同じように使える。`FULL JOIN` のない方言（MySQL、MariaDB）には、そのメソッドがない。

### GROUP BY、HAVING、結果の型

集計はグループごとに、行の値は行ごとに評価され、型がこの二つを分ける。`WHERE` の集計や、グループ化したクエリの選択リストにあるキーでない列は、コンパイルが通らない。`[QueryResult]` の型は結果の列に名前を付ける。

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

`GroupBy` の後のラムダの最初の引数がキーである。結果の型のメンバーは名前で設定するので、順序を入れ替えたりメンバーが欠けたりするとコンパイルエラーになり、列が別の場所に読まれることはない。

### サブクエリ、集合演算、CTE

サブクエリは、値や集合を書く場所に渡す文である。相関サブクエリは、外側のラムダの引数から外側の行を読む。

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

`Union`、`Intersect`、`Except` は、結果の型が同じ文どうしを結ぶ。名前を付けた同じ文を `With` なしで `From` に書くと、派生テーブルになる。

### INSERT、UPDATE、DELETE、RETURNING

`UPDATE` と `DELETE` には `Where` が要る。すべての行が対象なら `AllRows()` と書く。どちらもなければ `ExecuteAsync` がない。`ExecuteAsync` は行数を返す。`RETURNING` を持つ方言（SQLite、PostgreSQL、`INSERT` と `DELETE` の MariaDB、`OUTPUT` として SQL Server）では、`Returning` で行を読み返せる。

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

データベースが採番する列には代入できない。`Set(b.Id, 5)` は、理由を示すエラーになる（CS0619）。

### UPSERT

SQLite と PostgreSQL は `ON CONFLICT` を、MySQL と MariaDB は `ON DUPLICATE KEY UPDATE` を書く。どの方言も、自分の形だけを持つ。

```csharp
await db.Insert(Author.Table)
    .Values(new Author.Insert { Id = 3, Name = "Stanisław Lem", Country = "PL" })
    .OnConflict(a => [a.Id])
    .DoUpdate((a, excluded) => [Set(a.Country, excluded.Country)])
    .ExecuteAsync();
```

行を飛ばすには `DoNothing()` か `OnConflictDoNothing()` を使う。MySQL と MariaDB では、同じ行を `OnDuplicateKeyUpdate((a, row) => [Set(a.Country, row.Country)])` と書く。

### 方言

方言の型は、そのデータベースが持つ機能を宣言する。機能が要るメソッドは、それを宣言した方言にだけあるので、誤りはコンパイラーが示す。

| 機能 | SQLite | PostgreSQL | MySQL | MariaDB | SQL Server |
| --- | --- | --- | --- | --- | --- |
| `FullJoin` | ✓ | ✓ | | | ✓ |
| `INSERT` / `UPDATE` / `DELETE` の後の `Returning` | ✓ / ✓ / ✓ | ✓ / ✓ / ✓ | | ✓ / – / ✓ | ✓ / ✓ / ✓（`OUTPUT`） |
| `OrderBy` のない `Limit` | ✓ | ✓ | ✓ | ✓ | |
| `Limit` のない `Offset` | | ✓ | | | ✓（`OrderBy` の後） |
| `In` のサブクエリの中の `Limit` | ✓ | ✓ | | | ✓ |
| `OnConflict` / `OnDuplicateKeyUpdate` | ✓ / – | ✓ / – | – / ✓ | – / ✓ | |
| `CreateTable(...).IfNotExists()` | ✓ | ✓ | ✓ | ✓ | |
| トランザクションの中の DDL | ✓ | ✓ | | | ✓ |

SQL Server 2019 では、`IS DISTINCT FROM` が 2022 からなので、`IsDistinctFrom` を同じ結果になる展開で書く。

### トランザクション

`InTransactionAsync` は、デリゲートが返ればコミットし、例外を投げればロールバックする。自分でコミットするなら、`BeginTransactionAsync` で `Transaction<TDialect>` を得る。

```csharp
await db.InTransactionAsync(async tx =>
{
    await tx.Update(Author.Table).Set(a => [Set(a.Country, "USA")]).Where(a => Eq(a.Country, "US")).ExecuteAsync();
    await tx.Delete(Book.Table).Where(b => Eq(b.Title, "Solaris")).ExecuteAsync();
});
```

閉じた接続か `DbDataSource` を渡した `Database` は、文ごとに接続を開いて閉じる。開いた接続は開いたままにする。

### 生 SQL

断片は補間文字列である。穴はどれもパラメータになる。テーブル、列、式、`Identifier("name")` を入れた穴は、SQL のその部分になる。ただの `string` は受け取らないので、連結では SQL を組み立てられない。

```csharp
// A typed fragment inside a builder query.
List<(string Title, int Length)> lengths = await db.From(Book.Table)
    .Select(b => (b.Title, Raw<int>($"length({b.Title})")))
    .ToListAsync();

// A whole query in SQL, read into a row type or a tuple by column name or position.
int minimum = 400;
List<(string, int)> raw = await db.QueryAsync<string, int>($"SELECT title, pages FROM {Book.Table} WHERE pages > {minimum} ORDER BY title");
```

### 文の SQL

`ToSql()` は、実行せずに SQL とパラメータを返す。`SqlBuilder<TDialect>` は、接続なしで方言の文を組み立てる。その文には実行のメソッドがない（CS1929）。

```csharp
SqlStatement statement = db.From(Book.Table).Where(b => Gt(b.Pages, 400)).Select(b => b.Title).ToSql();
Console.WriteLine(statement.Text);
// SELECT "books"."Title" FROM "books" WHERE "books"."Pages" > @p0

Console.WriteLine(new SqlBuilder<PostgreSqlDialect>().From(Book.Table).Select(b => b.Title).OrderBy(b => [Asc(b.Title)]).Limit(5).ToSql().Text);
// SELECT "books"."Title" FROM "books" ORDER BY "books"."Title" ASC LIMIT $1

Console.WriteLine(new SqlBuilder<SqlServerDialect>().From(Book.Table).Select(b => b.Title).OrderBy(b => [Asc(b.Title)]).Limit(5).ToSql().Text);
// SELECT [books].[Title] FROM [books] ORDER BY [books].[Title] ASC OFFSET 0 ROWS FETCH NEXT @p0 ROWS ONLY
```

SQL Server では、`OrderBy` のない `Limit` はコンパイルが通らない。SQL Server はそこに `ORDER BY` を要るからである。

### 値コンバーター

自分の型は、コンバーターを通して列の型として保存する。コンバーターは、型かアセンブリに宣言する。

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

`Isbn` の列は `Column<Isbn>` である。`Isbn` の値と比べ、パラメータには `string` が入り、行は `Isbn` として読み返される。変換は生成されたコードで行い、リフレクションは使わない。

## null 許容参照型

`string` や `byte[]` の値が `NULL` になりうるかどうかは、null 許容参照型が有効なときだけ型に表れる。AuraORM を使うプロジェクトで有効にし、その警告をエラーにする。

```xml
<PropertyGroup>
  <Nullable>enable</Nullable>
  <WarningsAsErrors>nullable</WarningsAsErrors>
</PropertyGroup>
```

`string` と `string?` は実行時には同じ型なので、AuraORM は型だけではクエリを拒めない。この設定がないと、外部結合の NULL になりうる側の `string` の列や、`NULL` を許さない `string` の列に渡した `null` が、コンパイルを通る。設定があればエラーになる（結合は CS8619 か CS8600、`Set` は CS8620 か CS8631）。値型（`int` と `int?`）は、どちらでも型で確かめる。null 許容参照型が無効なところでテーブルを宣言すると、ジェネレーターのエラー（`AURA0102`）になる。

## 約束すること

- **SQL は連鎖のとおり。** 呼び出し一つが句一つで、同じ連鎖は、値、カルチャ、タイムゾーン、マシンによらず同じ SQL の文字列になる。例外は、呼び出し側が書く文の形の一部である。`In` の値の数、`INSERT` の行の数と設定した列、どの省略可能な条件が `null` かである。値はパラメータで、現れた順に番号が付く。一つの文に二度書いた式（選択リストに書いた `GROUP BY` のキー）は、一つのパラメータのままである。
- **[設計書の一覧](https://github.com/YUKIKEDA/AuraORM/blob/main/docs/design.ja.md#141-コンパイルできないもの)にある不正なクエリは、コンパイルが通らない。** 一覧には、それぞれがどのエラーになるかを書いてある。
- **どの方言も同じ API で使える。** データベースにない機能は、その方言でのコンパイルエラーになる。各文の方言ごとの SQL はゴールデンテストで、結合テストは、文をどのデータベースでも、サポートする最も古い版と最も新しい版で実行する。
- **行の読み取りは結果（行のオブジェクト、その文字列と配列）だけを確保し**、ボックス化しない。CI のテストが、すべての列の型で確かめる。
- **NativeAOT の発行で警告が出ない。** CI が、Linux、Windows、macOS で、`net8.0` と `net10.0` について、パッケージから発行して確かめる。

## 約束しないこと

- **生 SQL の断片の中身**（穴の型より先）。
- **データベースが C# のスキーマと合っていること。** AuraORM は実行時にスキーマを調べない。
- **データベースによる結果の違い。** 照合順序、タイムゾーン、数値の精度、`ORDER BY` のない行の順序。
- **値コンバーターの正しさ**（`FromProvider(ToProvider(x))` が `x` に戻ること）。
- **変更追跡、ナビゲーションプロパティ、遅延読み込み、LINQ。** AuraORM はその種のオブジェクトリレーショナルマッパーではなく、何も翻訳しない。

## 上限

| | |
| --- | --- |
| .NET | `net8.0` 以降。NativeAOT とトリミング |
| データベース | SQLite 3.39、PostgreSQL 15、MySQL 8.4、MariaDB 10.11、SQL Server 2019 以降 |
| プロバイダー | どの ADO.NET プロバイダーでも（`DbConnection` か `DbDataSource`）。テストは Microsoft.Data.Sqlite、Npgsql、MySqlConnector、Microsoft.Data.SqlClient を使う |
| 一つの文のパラメータの数 | データベースの上限。文を送る前に確かめる（SQL Server なら 2,100） |
| 依存 | .NET の基本ライブラリだけ |

## 性能

BenchmarkDotNet で、Windows 11、Intel Core i5-13400F、.NET 10.0.13（Release、JIT）で測った。測り方とすべての値は[設計書の §16.1](https://github.com/YUKIKEDA/AuraORM/blob/main/docs/design.ja.md#161-目標)にある。

| | 時間 | 確保量 |
| --- | --- | --- |
| メモリ上のリーダーから 6 列の行を 1 行読む | 22.7 ns（手で書いたループ 13.3 ns の 1.71 倍） | 行そのものだけ |
| `WHERE` のある `SELECT` を組み立てて SQL にする | 562 ns | 2,488 B |
| 二つの結合と `GROUP BY` を組み立てて SQL にする | 730 ns | 3,640 B |
| メモリ上の SQLite で 100 行を往復する | 手で書いた ADO.NET の 1.09 倍 | |

文の組み立ての時間の多くは、その確保である。段階、別名、列をその文に結びつける列のオブジェクト、そして SQL の出力が確保する。何度も実行する文も毎回組み立て直す。組み立てた文の再利用は、0.1 の後に予定している。

## 文書

| 文書 | 役割 |
| --- | --- |
| [`docs/design.ja.md`](https://github.com/YUKIKEDA/AuraORM/blob/main/docs/design.ja.md) | 設計（日本語、正本）。各 API の方言ごとの SQL、保証、診断 |
| [`docs/design-decisions.md`](https://github.com/YUKIKEDA/AuraORM/blob/main/docs/design-decisions.md) | 決めたことと、採らなかった案 |
| [`docs/roadmap.md`](https://github.com/YUKIKEDA/AuraORM/blob/main/docs/roadmap.md) | 作業の順序 |
| [`CHANGELOG.md`](https://github.com/YUKIKEDA/AuraORM/blob/main/CHANGELOG.md) | 各版の変更 |
| [`CONTRIBUTING.md`](https://github.com/YUKIKEDA/AuraORM/blob/main/CONTRIBUTING.md) | ビルド、テスト、貢献の方法 |
| [`SECURITY.md`](https://github.com/YUKIKEDA/AuraORM/blob/main/SECURITY.md) | 脆弱性の報告 |

## 検証

CI がマージのゲートで、Windows、Linux、macOS で回る（`.github/workflows/ci.yml`）。書式の検査、警告をエラーにした Release と Debug のビルド、単体・ゴールデン SQL・コンパイル・ジェネレーターのテスト、どのデータベースもサポートする最も古い版と最も新しい版での結合テスト、パッケージから発行した NativeAOT のサンプル、パッケージの中身、この README の例を確かめる。ローカルでは、`./build.ps1` が同じビルドとテストを回し、`./tools/db.ps1` がデータベースを起動する。

## ライセンス

[MIT](https://github.com/YUKIKEDA/AuraORM/blob/main/LICENSE)
