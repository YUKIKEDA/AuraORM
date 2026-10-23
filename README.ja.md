# AuraORM

[Drizzle ORM](https://orm.drizzle.team/) の考え方を取り入れた、.NET 向けの型安全な SQL クエリビルダー。

> **状況: 作り直し中。** AuraORM は要件から設計し直している。NuGet にはまだ公開しておらず、設計書にある API もまだ実装していない。進み具合は [`docs/roadmap.md`](https://github.com/YUKIKEDA/AuraORM/blob/main/docs/roadmap.md) にある。

[English](https://github.com/YUKIKEDA/AuraORM/blob/main/README.md)

## 方針

- **SQL は SQL のまま。** API は SQL の句に一つずつ対応する。SQL として正しく、API で書けるものはすべてコンパイルが通る。API で書けないところは生 SQL の断片で書ける。その場合も値はパラメータとして渡す。
- **型が拒むのは不正なクエリだけ。** クエリに含まれないテーブルの列、型の違う比較、順序の誤った句、`INSERT` での必須列の欠落、`WHERE` のない `UPDATE` / `DELETE`、そのデータベースにない機能は、コンパイルが通らない。
- **SQL を予測できる。** ビルダーの呼び出し一つが SQL の句一つになる。見えないクエリや結合、往復は発生しない。どのクエリも、実行する前に SQL とパラメータを確認できる。
- **AI エージェントが書いたコードでも安全。** 誤りは実行時の誤った結果ではなく、コンパイルエラーとして現れる。エラーには固定の診断 ID があり、メッセージは直し方を示す。
- **NativeAOT とトリミングに対応する。** リフレクション、式ツリー、動的コード生成は使わない。結果を読み取るコードはコンパイル時に生成する。
- **小さい。** ランタイムのライブラリは .NET の基本ライブラリと `System.Data.Common` だけに依存する。ADO.NET プロバイダーはどれでも使える。

## データベース

SQLite、PostgreSQL、MySQL（MariaDB を含む）、SQL Server。

## 要件

.NET 8 以降。

## 貢献

[`CONTRIBUTING.md`](https://github.com/YUKIKEDA/AuraORM/blob/main/CONTRIBUTING.md) を参照。

## ライセンス

[MIT](https://github.com/YUKIKEDA/AuraORM/blob/main/LICENSE)
