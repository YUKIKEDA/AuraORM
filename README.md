# AuraORM

AuraORMは、C#向けの型安全で軽量なORM（Object-Relational Mapping）フレームワークです。ソースジェネレーターを使用して、コンパイル時にテーブル定義とクエリビルダーを生成することで、実行時のパフォーマンスと型安全性を実現します。

## 特徴

- 🚀 **ソースジェネレーター**: コンパイル時にコードを生成し、実行時のオーバーヘッドを最小化
- 🔒 **型安全**: コンパイル時にクエリの型チェックを実行
- 📝 **属性ベース**: シンプルな属性を使用したテーブル定義
- 🔧 **クエリビルダー**: フルエントなAPIによるSQLクエリ構築
- 🗄️ **マルチデータベース**: SQL方言インターフェースによる拡張性（現在はSQLiteをサポート）

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

`[Table]`属性を使用してエンティティを定義します。ソースジェネレーターが自動的にテーブル定義クラスを生成します。

```csharp
using AuraORM.Builder.Attributes;

[Table("users")]
public partial record User(
    [Pk, AutoIncrement] int Id,
    string Name,
    int? Age,
    DateTime CreatedAt
);
```

### 2. DbContextの初期化

```csharp
using System.Data.Common;
using Microsoft.Data.Sqlite;
using AuraORM.Builder;

var connection = new SqliteConnection("Data Source=database.db");
var schema = new Schema();
var dbContext = new DbContext(connection, schema);
```

### 3. クエリの実行

#### SELECTクエリ

```csharp
var userTable = UserTable.Instance;

// 全件取得
var selectAll = dbContext.Select().From<User, UserTable>(userTable);
var (sql, parameters) = selectAll.Build();
// SQL: SELECT * FROM "users";

// 特定のカラムを取得
var selectColumns = dbContext.Select(userTable.Name, userTable.Age)
    .From<User, UserTable>(userTable);
var (sql2, parameters2) = selectColumns.Build();
// SQL: SELECT "name", "age" FROM "users";

// WHERE句を使用
var selectWhere = dbContext.Select()
    .From<User, UserTable>(userTable)
    .Where(userTable.Name == "John");
var (sql3, parameters3) = selectWhere.Build();
// SQL: SELECT * FROM "users" WHERE "name" = @p0;

// ORDER BY句を使用
var selectOrderBy = dbContext.Select()
    .From<User, UserTable>(userTable)
    .OrderBy(new OrderByCondition("created_at", descending: true));
var (sql4, parameters4) = selectOrderBy.Build();
// SQL: SELECT * FROM "users" ORDER BY "created_at" DESC;

// LIMIT/OFFSETを使用
var selectLimit = dbContext.Select()
    .From<User, UserTable>(userTable)
    .Limit(10)
    .Offset(20);
var (sql5, parameters5) = selectLimit.Build();
// SQL: SELECT * FROM "users" LIMIT 10 OFFSET 20;
```

#### INSERTクエリ

```csharp
var insertQuery = dbContext.Insert(userTable).Values(new()
{
    { (userTable.Name, "John Doe"), (userTable.Age, 30), (userTable.CreatedAt, DateTime.UtcNow) }
});
var (sql, parameters) = insertQuery.Build();
// SQL: INSERT INTO "users" ("name", "age", "created_at") VALUES (@p0, @p1, @p2);
```

#### UPDATEクエリ

```csharp
var updateQuery = dbContext.Update(userTable)
    .Set(userTable.Name, "Jane Doe")
    .Set(userTable.Age, 31)
    .Where(userTable.Id == 1);
var (sql, parameters) = updateQuery.Build();
// SQL: UPDATE "users" SET "name" = @p0, "age" = @p1 WHERE "id" = @p2;
```

#### DELETEクエリ

```csharp
var deleteQuery = dbContext.Delete(userTable)
    .Where(userTable.Id == 1);
var (sql, parameters) = deleteQuery.Build();
// SQL: DELETE FROM "users" WHERE "id" = @p0;
```

## 属性

### TableAttribute

テーブル名を指定します。

```csharp
[Table("users")]
public partial record User(...)
```

### PkAttribute

主キーを指定します。

```csharp
[Pk] int Id
```

### AutoIncrementAttribute

自動インクリメントを指定します。通常は`[Pk]`と組み合わせて使用します。

```csharp
[Pk, AutoIncrement] int Id
```

### ColumnAttribute

カラム名を明示的に指定します（オプション）。指定しない場合は、プロパティ名がスネークケースに変換されます。

```csharp
[Column("user_name")] string Name
```

## WHERE条件

### 基本的な条件

```csharp
// 等号
.Where(userTable.Name == "John")

// 不等号
.Where(userTable.Name != "John")

// 比較演算子
.Where(userTable.Age > 18)
.Where(userTable.Age >= 18)
.Where(userTable.Age < 65)
.Where(userTable.Age <= 65)
```

### 複合条件

```csharp
// AND条件
.Where((userTable.Name == "John").And(userTable.Age > 18))

// OR条件
.Where((userTable.Name == "John").Or(userTable.Name == "Jane"))
```

### IN/NOT IN条件

```csharp
// IN
.Where(userTable.Age.In(20, 30, 40))

// NOT IN
.Where(userTable.Age.NotIn(20, 30, 40))
```

### NULL条件

```csharp
// IS NULL
.Where(userTable.Age.IsNull())

// IS NOT NULL
.Where(userTable.Age.IsNotNull())
```

### BETWEEN条件

```csharp
.Where(userTable.Age.Between(18, 65))
```

### LIKE条件

```csharp
.Where(userTable.Name.Like("John%"))
```

## SQL方言

AuraORMは`ISqlDialect`インターフェースを通じて、異なるデータベースをサポートできます。現在はSQLiteがデフォルトでサポートされています。

```csharp
var dialect = new SqliteDialect();
var dbContext = new DbContext(connection, schema, dialect);
```

## 要件

- .NET Standard 2.0 以上（AuraORM.Builder）
- .NET 8.0 以上（テストプロジェクト）
- C# 最新版

## 開発状況

現在の実装状況については、[TODO.md](TODO.md)を参照してください。

### 実装済み機能

- ✅ SELECTクエリビルダー
- ✅ INSERTクエリビルダー
- ✅ UPDATEクエリビルダー
- ✅ DELETEクエリビルダー
- ✅ WHERE条件（等号、不等号、比較演算子、IN、NOT IN、IS NULL、IS NOT NULL、BETWEEN、LIKE）
- ✅ ORDER BY句
- ✅ LIMIT/OFFSET句
- ✅ ソースジェネレーターによるテーブル定義の自動生成
- ✅ SQLiteサポート

### 計画中の機能

- ⏳ JOIN句のサポート
- ⏳ 生SQLサポート
- ⏳ SQL実行のサポート
- ⏳ トランザクション構文のサポート

## ライセンス

[ライセンス情報をここに追加]

## コントリビューション

プルリクエストやイシューの報告を歓迎します。
