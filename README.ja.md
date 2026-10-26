# AuraORM

[Drizzle ORM](https://orm.drizzle.team/) の考え方を取り入れた、.NET 向けの型安全な SQL クエリビルダー。

> **状況: 最初のリリースの前。** 0.1 の API は実装したが、NuGet にはまだ公開していない。進み具合は [`docs/roadmap.md`](https://github.com/YUKIKEDA/AuraORM/blob/main/docs/roadmap.md) にある。

[English](https://github.com/YUKIKEDA/AuraORM/blob/main/README.md)

## 方針

- **SQL は SQL のまま。** API は SQL の句に一つずつ対応する。SQL として正しく、API で書けるものはすべてコンパイルが通る。API で書けないところは生 SQL の断片で書ける。その場合も値はパラメータとして渡す。
- **型が拒むのは不正なクエリだけ。** クエリに含まれないテーブルの列、型の違う比較、順序の誤った句、`INSERT` での必須列の欠落、`WHERE` のない `UPDATE` / `DELETE`、そのデータベースにない機能は、コンパイルが通らない。
- **SQL を予測できる。** ビルダーの呼び出し一つが SQL の句一つになる。見えないクエリや結合、往復は発生しない。どのクエリも、実行する前に SQL とパラメータを確認できる。
- **AI エージェントが書いたコードでも安全。** 誤りは実行時の誤った結果ではなく、コンパイルエラーとして現れる。エラーには固定の診断 ID があり、メッセージは直し方を示す。
- **NativeAOT とトリミングに対応する。** リフレクション、式ツリー、動的コード生成は使わない。結果を読み取るコードはコンパイル時に生成する。
- **小さい。** ランタイムのライブラリは .NET の基本ライブラリと `System.Data.Common` だけに依存する。ADO.NET プロバイダーはどれでも使える。

## 使い方

パッケージと ADO.NET のプロバイダー（ここでは SQLite）を足す。

```shell
dotnet add package AuraORM
dotnet add package Microsoft.Data.Sqlite
```

テーブルをレコードで宣言する。パッケージに入っているソースジェネレーターが、型付きの列、`INSERT` のモデル、行の読み取りを書く。

```csharp
using AuraORM;

[Table("books")]
public sealed partial record Book([PrimaryKey] long Id, string Title, string? Author, int Pages);
```

テーブルを作り、行を入れて、問い合わせる。呼び出し一つが SQL の句一つで、値はパラメータになる。

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

`UPDATE` と `DELETE` には `WHERE` が要る。すべての行が対象なら `AllRows()` と書く。

```csharp
await db.Update(Book.Table).Set(b => [Set(b.Author, "Carl Sagan")]).Where(b => Eq(b.Id, 2L)).ExecuteAsync();
await db.Delete(Book.Table).Where(b => Lt(b.Pages, 100)).ExecuteAsync();
```

どの文も、実行せずに SQL とパラメータを確認できる。

```csharp
SqlStatement statement = db.From(Book.Table).Where(b => Gt(b.Pages, 400)).Select(b => b.Title).ToSql();
Console.WriteLine(statement.Text);
// SELECT "books"."Title" FROM "books" WHERE "books"."Pages" > @p0
```

## null 許容参照型

`string` や `byte[]` の値が `NULL` になりうるかどうかは、null 許容参照型が有効なときだけ型に表れる。AuraORM を使うプロジェクトで有効にし、その警告をエラーにする。

```xml
<PropertyGroup>
  <Nullable>enable</Nullable>
  <WarningsAsErrors>nullable</WarningsAsErrors>
</PropertyGroup>
```

`string` と `string?` は実行時には同じ型なので、AuraORM は型だけではクエリを拒めない。この設定がないと、外部結合の NULL になりうる側の `string` の列や、`NULL` を許さない `string` の列に渡した `null` が、コンパイルを通る。設定があればエラーになる（結合は CS8619 か CS8600、`Set` は CS8620 か CS8631）。値型（`int` と `int?`）は、どちらでも型で確かめる。

## データベース

SQLite、PostgreSQL、MySQL（MariaDB を含む）、SQL Server。

## 要件

.NET 8 以降。

## 貢献

[`CONTRIBUTING.md`](https://github.com/YUKIKEDA/AuraORM/blob/main/CONTRIBUTING.md) を参照。

## ライセンス

[MIT](https://github.com/YUKIKEDA/AuraORM/blob/main/LICENSE)
