# AuraORM

AuraORMは、C#向けの型安全で軽量なORM（Object-Relational Mapping）フレームワークです。ソースジェネレーターを使用して、コンパイル時にテーブル定義やマッピングコードを生成することで、リフレクション不要の高パフォーマンスと型安全性を実現します。

## 特徴

- **ソースジェネレーター** - コンパイル時にコードを生成し、実行時のリフレクションを排除
- **型安全** - コンパイル時にクエリの型チェックを実行
- **Fluent API** - メソッドチェーンによる直感的なクエリ構築
- **JOINサポート** - INNER / LEFT / RIGHT / FULL JOINに対応
- **トランザクション** - 自動コミット / ロールバック付きのトランザクションAPI
- **生SQL** - パラメータ化された生SQLクエリの実行をサポート
- **マルチデータベース** - SQL方言インターフェースによる拡張性（現在はSQLiteをサポート）

## プロジェクト構成

```
AuraORM/
├── AuraORM.Builder/      # メインのORMライブラリ
├── AuraORM.Generator/    # ソースジェネレーター
└── AuraORM.Tests/        # テストプロジェクト
```

## インストール

```xml
<ItemGroup>
  <ProjectReference Include="..\AuraORM.Builder\AuraORM.Builder.csproj" />
  <ProjectReference Include="..\AuraORM.Generator\AuraORM.Generator.csproj" 
                    OutputItemType="Analyzer" 
                    ReferenceOutputAssembly="false" />
</ItemGroup>
```

## クイックスタート

### 1. エンティティの定義

`[Table]` 属性を使用してエンティティを定義します。ソースジェネレーターが自動的に `UserTable` クラスやマッピング用の拡張メソッドを生成します。

```csharp
using AuraORM.Builder.Attributes;

[Table("users")]
public partial record User(
    [Pk, AutoIncrement, Column("id")] int Id,
    [Column("name")] string Name,
    [Column("age")] int? Age,
    [Column("createdat")] DateTime CreatedAt
);
```

### 2. クエリ結果型の定義（オプション）

特定のカラムのみ取得する場合や、JOIN結果をマッピングする場合は `[QueryResult]` 属性を使用します。

```csharp
[QueryResult]
public partial record UserNameResult(
    [Column("name")] string Name
);

[QueryResult]
public partial record UserOrderSummary(
    [Column("users_id")] int UserId,
    [Column("users_name")] string UserName,
    [Column("orders_productname")] string ProductName,
    [Column("orders_amount")] decimal Amount
);
```

> JOIN時にカラム名が全テーブル選択の場合は `テーブル名_カラム名` の形式でエイリアスが自動生成されます。

### 3. DbContextの初期化

```csharp
using Microsoft.Data.Sqlite;
using AuraORM.Builder;

var connection = new SqliteConnection("Data Source=database.db");
connection.Open();

var schema = new Schema();
var db = new DbContext(connection, schema);
```

### 4. クエリの実行

#### SELECT

```csharp
var userTable = UserTable.Instance;

// 全件取得
var users = await db.Select().From(userTable).ToListAsync();

// 特定のカラムを取得（QueryResult型でマッピング）
var names = await db.Select(userTable.Name)
    .From(userTable)
    .ToListAsync<UserNameResult>();

// WHERE句
var adults = await db.Select()
    .From(userTable)
    .Where(userTable.Age > 20)
    .ToListAsync();

// ORDER BY / LIMIT / OFFSET
var page = await db.Select()
    .From(userTable)
    .OrderBy(new OrderByCondition("name", descending: false))
    .Limit(10)
    .Offset(20)
    .ToListAsync();
```

#### JOIN

```csharp
var userTable = UserTable.Instance;
var orderTable = OrderTable.Instance;

// INNER JOIN
var results = await db.Select()
    .From(userTable)
    .InnerJoin(orderTable, userTable.Id == orderTable.UserId)
    .ToListAsync<UserOrderSummary>();

// LEFT JOIN
var allUsers = await db.Select()
    .From(userTable)
    .LeftJoin(orderTable, userTable.Id == orderTable.UserId)
    .ToListAsync<UserOrderSummaryLeftJoin>();

// JOINでカラム指定
var selected = await db.Select(userTable.Name, orderTable.ProductName)
    .From(userTable)
    .InnerJoin(orderTable, userTable.Id == orderTable.UserId)
    .Where(userTable.Age > 20)
    .ToListAsync<NameAndProductNameResult>();
```

> RIGHT JOIN (`RightJoin`) / FULL JOIN (`FullJoin`) も同様の構文で利用できます。

#### INSERT

```csharp
var userTable = UserTable.Instance;

// 単一行の挿入
var affected = await db.Insert(userTable).Values(new()
{
    { (userTable.Name, "太郎"), (userTable.Age, 25), (userTable.CreatedAt, DateTime.Now) }
}).ExecuteAsync();

// 複数行の一括挿入
await db.Insert(userTable).Values(new()
{
    { (userTable.Name, "太郎"), (userTable.Age, 25), (userTable.CreatedAt, DateTime.Now) },
    { (userTable.Name, "花子"), (userTable.Age, 30), (userTable.CreatedAt, DateTime.Now) },
    { (userTable.Name, "次郎"), (userTable.Age, 18), (userTable.CreatedAt, DateTime.Now) }
}).ExecuteAsync();
```

#### UPDATE

```csharp
var userTable = UserTable.Instance;

await db.Update(userTable).Set(new()
{
    (userTable.Name, "太郎（更新）"), (userTable.Age, 26)
}).Where(userTable.Id == 1).ExecuteAsync();
```

#### DELETE

```csharp
var userTable = UserTable.Instance;

await db.Delete(userTable)
    .Where(userTable.Id == 1)
    .ExecuteAsync();
```

### 5. トランザクション

`TransactionAsync` を使用すると、正常終了時に自動コミット、例外発生時に自動ロールバックされます。

```csharp
// 戻り値なし
await db.TransactionAsync(async tx =>
{
    await tx.Insert(UserTable.Instance).Values(new()
    {
        { (userTable.Name, "新規ユーザー"), (userTable.Age, 22), (userTable.CreatedAt, DateTime.Now) }
    }).ExecuteAsync();

    await tx.Update(UserTable.Instance).Set(new()
    {
        { (userTable.Age, 99) }
    }).Where(userTable.Name == "太郎").ExecuteAsync();

    await tx.Delete(UserTable.Instance)
        .Where(userTable.Name == "次郎")
        .ExecuteAsync();
});

// 戻り値あり
var result = await db.TransactionAsync(async tx =>
{
    await tx.Insert(UserTable.Instance).Values(new()
    {
        { (userTable.Name, "テスト"), (userTable.Age, 25), (userTable.CreatedAt, DateTime.Now) }
    }).ExecuteAsync();

    return 42;
});
```

### 6. 生SQL

クエリビルダーでは表現しにくい複雑なSQLは、`RawSql` で直接記述できます。

```csharp
// SELECT（ToListAsync<T>で結果をマッピング）
var results = await db.RawSql(
    "SELECT name FROM users WHERE age > @minAge ORDER BY age DESC",
    [("@minAge", 20)]
).ToListAsync<UserNameResult>();

// DML（ExecuteAsyncで実行）
var affected = await db.RawSql(
    "UPDATE users SET name = @name WHERE id = @id",
    [("@name", "更新済み"), ("@id", 1)]
).ExecuteAsync();
```

## 属性

| 属性 | 対象 | 説明 |
|------|------|------|
| `[Table("table_name")]` | class / record | テーブルエンティティを定義。ソースジェネレーターが `{Type}Table` クラスを生成 |
| `[Pk]` | プロパティ / パラメータ | 主キーを指定 |
| `[AutoIncrement]` | プロパティ / パラメータ | 自動インクリメントを指定。INSERT時にカラムから除外される |
| `[Column("column_name")]` | プロパティ / パラメータ | データベースのカラム名を明示的に指定 |
| `[QueryResult]` | class / record | クエリ結果のマッピング型を定義。カラム名ベースでマッピングされる |

## WHERE条件

### 比較演算子

```csharp
.Where(userTable.Name == "太郎")    // =
.Where(userTable.Name != "太郎")    // <>
.Where(userTable.Age > 18)          // >
.Where(userTable.Age >= 18)         // >=
.Where(userTable.Age < 65)          // <
.Where(userTable.Age <= 65)         // <=
```

### 複合条件

```csharp
// AND
.Where((userTable.Age > 20).And(userTable.Name != "Bob"))

// OR
.Where((userTable.Name == "太郎").Or(userTable.Name == "花子"))

// Where を複数回呼び出すと AND で結合される
.Where(userTable.Age > 20)
.Where(userTable.Name != "Bob")
```

### IN / NOT IN

```csharp
.Where(userTable.Age.In(20, 30, 40))
.Where(userTable.Age.NotIn(20, 30, 40))
```

### NULL条件

```csharp
.Where(userTable.Age.IsNull())
.Where(userTable.Age.IsNotNull())
```

### BETWEEN

```csharp
.Where(userTable.Age.Between(20, 40))
```

### LIKE

```csharp
.Where(userTable.Name.Like("太%"))
```

## ソースジェネレーター

### `[Table]` 属性によって生成されるもの

- `{Type}Table` クラス（`ITable` を実装）
  - 各カラムに対応する `Column<T>` 静的プロパティ
  - `Instance` シングルトン
  - テーブル名やカラム名の情報
- `From({Type}Table)` 拡張メソッド（型推論のサポート）
- `ToListAsync()` 拡張メソッド（エンティティへのマッピング）
- `Map{Type}()` 拡張メソッド（`IDataReader` からのマッピング）

### `[QueryResult]` 属性によって生成されるもの

- `ToListAsync<TResult>()` 拡張メソッド（カラム名ベースでのマッピング）
- `Map{Type}ByColumnName()` 拡張メソッド（`IDataReader` からのマッピング）

## SQL方言

`ISqlDialect` インターフェースを通じて、異なるデータベースをサポートできます。デフォルトはSQLiteです。

```csharp
// デフォルト（SQLite）
var db = new DbContext(connection, schema);

// 明示的に指定
var db = new DbContext(connection, schema, new SqliteDialect());
```

## 要件

- .NET Standard 2.0 以上（AuraORM.Builder）
- C# 最新版

## 開発状況

### 実装済み機能

- SELECT / INSERT / UPDATE / DELETE クエリビルダー
- WHERE条件（`=`, `<>`, `>`, `<`, `>=`, `<=`, `IN`, `NOT IN`, `IS NULL`, `IS NOT NULL`, `BETWEEN`, `LIKE`）
- AND / OR による複合条件
- JOIN（INNER / LEFT / RIGHT / FULL）
- ORDER BY / LIMIT / OFFSET
- 一括INSERT（複数行）
- トランザクション（自動コミット / ロールバック）
- 生SQLクエリの実行
- `[QueryResult]` によるカスタム結果型のマッピング
- ソースジェネレーターによるテーブル定義・マッピングコードの自動生成
- SQLiteサポート
- Nullable型 / Enum型のサポート

### 計画中の機能

- Returning句のサポート
- 集計関数のサポート
- yield returnのサポート
- 型安全性の向上（クエリの順番の検証など）

## ライセンス

MIT License
