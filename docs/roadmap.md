# ロードマップ

作業の順序と状態だけを置く。受け入れ条件は各 Issue に書く。行の追加と削除は Grill を経る（`.cursor/rules/workflow.mdc`）。設計は [design.ja.md](./design.ja.md) にある。

**今**: X1

## v0.1

| ID | タイトル | Issue | 状態 |
| --- | --- | --- | --- |
| F0 | リポジトリの基盤（規約・テンプレート・ゲート） | #1 | done |
| D1 | 設計書: コンセプト・要件・仕様 | #3 | done |
| C1 | 式と文の木、方言ごとの SQL の出力、パラメータ、ゴールデンテストの仕組み（§4、§5.12） | #5 | done |
| G1 | ソースジェネレーター: テーブルと結果の型、診断、コンパイルテストの仕組み（§3、§15） | #8 | done |
| X1 | 実行: `Database` / `Transaction`、接続の扱い、行の読み取り、生 SQL の断片、生のクエリとコマンド（§11、§12） | - | planned |
| I1 | 結合テストの基盤: CI のサービスコンテナと WSL コンテナのスクリプト、全方言での X1 の結合テスト（§17） | - | planned |
| N1 | NativeAOT のサンプルと CI の検査（§16.3） | - | planned |
| Q1 | SELECT: 単一テーブル、WHERE、射影、DISTINCT、ORDER BY、LIMIT / OFFSET（§5.1〜§5.9） | - | planned |
| A1 | アナライザー: NULL との比較、生 SQL の穴（§4.4、§11.1） | - | planned |
| Q2 | SELECT: JOIN と NULL 化、別名（§5.2、§5.3） | - | planned |
| Q3 | SELECT: GROUP BY / HAVING、集計、評価の段階（§4.5、§5.5） | - | planned |
| Q4 | SELECT: サブクエリ、集合演算、派生テーブル、CTE（§5.10、§5.11） | - | planned |
| M1 | INSERT / UPDATE / DELETE と RETURNING（§6〜§9） | - | planned |
| M2 | UPSERT（§10） | - | planned |
| V1 | 値コンバーター（§3.6） | - | planned |
| S1 | DDL（§13） | - | planned |
| P1 | ベンチマークと性能の目標（§16.1） | - | planned |
| R1 | リリースの仕組み: NuGet の Trusted Publishing、パッケージの検査、README のサンプルの検査 | - | planned |

## v0.1 の後

内容は設計書 §18 にある。

| ID | タイトル | Issue | 状態 |
| --- | --- | --- | --- |
| W1 | ウィンドウ関数 | - | planned |
| U1 | `UPDATE … FROM` / JOIN を伴う UPDATE と DELETE | - | planned |
| U2 | SQL Server の `MERGE` | - | planned |
| L1 | 行ロック | - | planned |
| T2 | セーブポイント | - | planned |
| C2 | 再帰 CTE | - | planned |
| P2 | 組み立て済みの文の再利用 | - | planned |
| J1 | JSON と配列の演算子 | - | planned |
| DF1 | 方言固有の関数と並べ替えの指定 | - | planned |
| MG1 | 差分のマイグレーション | - | planned |
| O1 | Oracle | - | planned |
| OB1 | 観測（`ActivitySource`） | - | planned |

## Intentionally out of scope

- 変更追跡、ナビゲーションプロパティ、遅延読み込み、Unit of Work（設計書 §1.4）
- リレーショナルクエリ（Drizzle の `db.query`）
- LINQ プロバイダー（`IQueryable<T>`）
- 同期 API
- 利用者による方言の追加
