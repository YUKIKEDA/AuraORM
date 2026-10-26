# ロードマップ

作業の順序と状態だけを置く。受け入れ条件は各 Issue に書く。行の追加と削除は Grill を経る（`.cursor/rules/workflow.mdc`）。設計は [design.ja.md](./design.ja.md) にある。

**今**: P1

## v0.1

| ID | タイトル | Issue | 状態 |
| --- | --- | --- | --- |
| F0 | リポジトリの基盤（規約・テンプレート・ゲート） | #1 | done |
| D1 | 設計書: コンセプト・要件・仕様 | #3 | done |
| C1 | 式と文の木、方言ごとの SQL の出力、パラメータ、ゴールデンテストの仕組み（§4、§5.12） | #5 | done |
| G1 | ソースジェネレーター: テーブルと結果の型、診断、コンパイルテストの仕組み（§3、§15） | #8 | done |
| X1 | 実行: `Database` / `Transaction`、接続の扱い、行の読み取り、生 SQL の断片、生のクエリとコマンド（§11、§12） | #10 | done |
| I1 | 結合テストの基盤: CI のサービスコンテナと WSL コンテナのスクリプト、全方言での X1 の結合テスト（§17） | #12 | done |
| N1 | NativeAOT のサンプルと CI の検査（§16.3） | #14 | done |
| Q1 | SELECT: 単一テーブル、WHERE、射影、DISTINCT、ORDER BY、LIMIT / OFFSET、段階の型の生成ツールと CI の検査（§5.1〜§5.9） | #16 | done |
| A1 | アナライザー: NULL との比較、生 SQL の穴（§4.4、§11.1） | #21 | done |
| Q2 | SELECT: JOIN と NULL 化、別名（§5.2、§5.3） | #23 | done |
| Q3 | SELECT: GROUP BY / HAVING、集計、評価の段階（§4.5、§5.5） | #30 | done |
| Q4 | SELECT: サブクエリ（`In` / `NotIn` / `Exists` / `Scalar`、`GroupScalar` / `GroupExists`、相関）（§5.11、§14） | #37 | done |
| Q5 | SELECT: 集合演算と、その後の `OrderBy` / `Limit` / `Offset`（§5.10） | #44 | done |
| Q6 | SELECT: 派生テーブル、CTE、`[QueryResult]` の型の `Columns`（§3.5、§5.11） | #51 | done |
| M1 | INSERT / UPDATE / DELETE と RETURNING、SQLite の最小版（3.39）の結合テスト（§6〜§9、§17） | #60、#61 | done |
| M2 | UPSERT（§10） | #74 | done |
| V1 | 値コンバーター（§3.6） | #82 | done |
| S1 | DDL（§13） | #86、#90 | done |
| P1 | ベンチマークと性能の目標（§16.1） | #94 | planned |
| R1 | リリースの仕組み: NuGet の Trusted Publishing、パッケージの検査（ジェネレーターが `analyzers/dotnet/cs` に入り、依存が空であること。NativeAOT のサンプルをパックしたパッケージで参照する）、README のサンプルの検査、README に null 許容参照型の設定（`<Nullable>enable</Nullable>` と null 許容の警告をエラーにすること）が参照型の保証に要ると書く（§14.2） | - | planned |

## v0.1 の後

内容は設計書 §18 にある。

| ID | タイトル | Issue | 状態 |
| --- | --- | --- | --- |
| W1 | ウィンドウ関数 | - | planned |
| U1 | `UPDATE … FROM` / JOIN を伴う UPDATE と DELETE | - | planned |
| U2 | SQL Server の `MERGE` | - | planned |
| U3 | UPSERT の式の衝突の対象、部分インデックスの対象、制約の名前 | - | planned |
| VC2 | 値コンバーターで変換した型の `Min` / `Max` | - | planned |
| L1 | 行ロック | - | planned |
| T2 | セーブポイント | - | planned |
| C2 | 再帰 CTE | - | planned |
| P2 | 組み立て済みの文の再利用 | - | planned |
| P3 | 行の読み取りの残りの差の原因 | - | planned |
| P4 | SQL の組み立てで Sqlify との差の原因 | #111 | done |
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
