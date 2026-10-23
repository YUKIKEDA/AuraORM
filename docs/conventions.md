# 開発規約

コードレビューで同じ指摘を繰り返さないための規約をまとめる。エージェントが守る強制力のある規約は `.cursor/rules/` にあり、本書はその人間向けの要約と、ディレクトリ構成の正本である。設計の中身は [design.ja.md](./design.ja.md) にある。

## ディレクトリ構成

プロジェクトやフォルダは、それを必要とする Issue の PR で作る。先回りして空のものを作らない。プロジェクトを追加した PR で、下の表に1行足す。

```text
AuraORM.slnx                 ソリューション（.sln は使わない）
global.json                  .NET SDK の固定（10.0 系）とテストランナー（Microsoft.Testing.Platform）
Directory.Build.props        全プロジェクト共通の設定（警告はエラー、解析、AOT 互換、パッケージ情報）
Directory.Packages.props     パッケージのバージョン（Central Package Management）
BannedSymbols.txt            ランタイムのライブラリで禁止する API（リフレクション、式ツリー、コンソール、時刻、カルチャ）
stylecop.json                StyleCop の設定
.editorconfig                命名・スタイル・解析ルールの重大度
.csharpierrc.json            フォーマッタ（CSharpier）の設定
.config/dotnet-tools.json    ローカルツール（CSharpier）
build.ps1                    ローカルのゲート（CI も同じスクリプトを実行する。マージの条件は CI）
src/                         パッケージに入るプロジェクト
tests/                       テスト（tests/*.Tests）
tools/                       開発用のスクリプトとツール。`databases.json`（結合テストのデータベースのイメージとバージョン、CI も読む）、`db.ps1`（WSL コンテナでそれを立てる）、`AuraORM.StageGenerator`（SELECT の段階の型を書き出す）
docs/                        設計・判断・ロードマップ・規約（本書）
.cursor/rules/               エージェント向けの規約
.agents/skills/              スキル（.claude/skills/ に写しを置く）
.github/                     Issue / PR テンプレート、CI、Dependabot
```

| プロジェクト | 責務 |
| --- | --- |
| `src/AuraORM` | ランタイムのライブラリ（`net8.0`、`IsAotCompatible`、パッケージ依存なし）。式と関数（`Expressions/`）、方言の型と機能のインターフェース（`Dialects/`）、文の木と方言ごとの SQL の出力（`Rendering/`）、SELECT のビルダーの段階の型（`Select/`、テーブルとタプルの数だけ違う型は `Select/Generated/`）、`SqlStatement`（`Statements/`）、生成コードが使う型（`Tables/`、`Projections/`）、行の読み取り（`Reading/`）、属性とテーブルの情報（`Schema/`） |
| `src/AuraORM.Generators` | ソースジェネレーター（`netstandard2.0`、Roslyn 4.8）。`[Table]` と `[QueryResult]` の型を読み（`SchemaReader`）、モデル（`Model/`）から C# 12 のコードを出す（`TableEmitter`、`ResultEmitter`）。診断の ID は `Diagnostics` と `AnalyzerReleases.*.md`。`AuraORM` のパッケージの `analyzers/dotnet/cs` に入る |
| `samples/AuraORM.Samples.Sqlite` | NativeAOT の検査に使うサンプル（`net8.0`、`PublishAot`）。SQLite で API を通し、結果を確かめて終了コードで知らせる。CI がこれを NativeAOT で発行して実行し、警告が 1 件でもあれば失敗する。行ごとに、追加した API をここで通す |
| `tools/AuraORM.StageGenerator` | SELECT の段階の型のうち、テーブルの数とタプルの要素の数だけが違うもの（`SelectFrom`、`SelectWhere`、`SelectProjected`、`Select` の多重定義など）を `src/AuraORM/Select/Generated` に書き出すコンソールアプリ（`net10.0`、パッケージには入らない）。出力はコミットする。段階の型を変えるときは `dotnet run --project tools/AuraORM.StageGenerator` の後に `dotnet csharpier format src/AuraORM/Select/Generated` を実行する。CI は同じことをして、コミットとの差分があれば失敗する |
| `tests/AuraORM.Generators.Tests` | ジェネレーターのテスト（`net8.0` と `net10.0`）。断片を C# 12 の利用者のコードとしてジェネレーター付きでコンパイルし、成功するか、診断の ID を確かめる（`Support/GeneratorHarness`） |
| `tests/AuraORM.Integration.Tests` | 結合テスト（`net8.0` と `net10.0`）。同じテストを方言ごとに回すため、テストのクラスは方言を型引数に取る抽象クラスで、方言ごとに派生クラスを置く（`RawQueryTests<TDialect>` と `SqliteRawQueryTests`、`SelectTests<TDialect>` と `SqliteSelectTests`。サーバーの分は `Execution/ServerTests.cs`）。データベースは `Support/ITestTarget<TDialect>` が用意する。SQLite はプロセス内のメモリ上のデータベース。サーバーは環境変数 `AURA_TEST_{NAME}` の接続文字列を使い、未設定なら飛ばす。`AURA_TEST_REQUIRE` に名前のあるデータベースは、未設定なら失敗する。サーバーごとのテストは xUnit のコレクションで一つずつ回す |
| `tests/AuraORM.Tests` | ランタイムのテスト（`net8.0` と `net10.0`）。SQL のゴールデンテスト（`Support/SqlAssert`、期待値は方言ごとに手で書く。どの方言でも同じに組み立てる文は `Support/IQueryCase` の小さなクラスにして全方言で回す）、利用者のコードとしてのコンパイルテスト（`Support/CompileAssert`、Roslyn 4.8、C# 12）、ジェネレーターが作ったテスト用のテーブル（`Support/Schema.cs`）を使うテスト。行の読み取りは `DataTableReader` で確かめる |

## ビルドとゲート

- PR を出す前に `./build.ps1` を通す。ツールの復元、CSharpier の検査、復元、Release と Debug のビルド、全テストプロジェクトの実行を行う
- CI は同じスクリプトを Windows・Linux・macOS で回す。データベースの結合テストと NativeAOT の検査も CI で回す。マージの条件は CI がすべて緑であること。ローカルでの実行は開発の補助であり、ゲートの代わりにならない
- 警告は最初からすべてエラー（`TreatWarningsAsErrors`）。ビルドを通すために `NoWarn` や `#pragma warning disable` を足さない。ルールがこのリポジトリに合わない場合は、理由をコメントに書いて `.editorconfig` を変え、PR でそう明記する
- `src/AuraORM` は `IsAotCompatible`。trim / AOT の警告もエラーになる。`BannedSymbols.txt` の API はビルドエラーになる
- パッケージを足す前に `.cursor/rules/dependencies.mdc` を確かめる。ランタイムのライブラリには依存を足さない

## データベースとコンテナ

- Docker は使わない
- ローカルの結合テスト用データベースは WSL コンテナ（`wslc.exe`、WSL 2.9.3 以降）で動かす。`./tools/db.ps1 up` で立て、`./tools/db.ps1 test` でテストを回し、`./tools/db.ps1 down` で止める
- CI では GitHub Actions のサービスコンテナで、データベースごとに最小と最新のバージョンを回す。イメージとバージョンは `tools/databases.json` が正本で、CI とスクリプトの両方がそれを読む
- 詳細は `.cursor/rules/containers.mdc`

## C# の書き方

- フォーマットは CSharpier だけに任せる
- リポジトリ内のコードは現代的な C# を使ってよい（型が明らかなときの `var`、コレクション式、プライマリコンストラクタなど）。ソースジェネレーターが利用者のプロジェクトに出力するコードは C# 12 でコンパイルできるようにする
- private フィールドは static も含めて `_camelCase`、定数は PascalCase、`this.` は付けない、インターフェースは `I` で始める
- 名前空間はアセンブリ名に合わせ、フォルダには合わせない
- 型は一つのファイルに一つが既定。例外は、方言の機能のマーカーインターフェース（`Dialects/Capabilities.cs`）と、`Sql` の関数を分野ごとに分けた `partial` のファイル（`Sql.Comparison.cs` など）、ジェネレーターのモデルのレコード（`src/AuraORM.Generators/Model/Models.cs`）、`AuraORM.StageGenerator` が書き出す段階の型（`src/AuraORM/Select/Generated/`、同じ名前でテーブルの数だけ違う型を一つのファイルに置く。手で編集しない）
- 不正な状態は型で表せないようにする。`bool` と、その `bool` 次第で意味を失う nullable の組み合わせは使わない
- 引数・状態の検査は `ThrowHelper` 経由で例外を投げる。データベースの例外はそのまま呼び出し元に届ける
- 公開 API には英語の XML ドキュメントを書く。internal は必須にしない
- SQL の生成・パラメータの設定・行の読み取りの経路の規則は `.cursor/rules/perf.mdc` にある

## テスト

- xUnit v3 と標準の `Assert` を使う。テストは `net8.0` と `net10.0` の両方で走る
- メソッド名は `{対象}_{状況}_{期待}`。各テストに `summary` と `remarks`（Preconditions / Steps / Expected）を英語で書く
- テストの層（SQL のゴールデン、コンパイルの可否、ジェネレーター、データベースごとの結合テスト、NativeAOT のサンプル）は `.cursor/rules/testing.mdc` と設計書にある

## 言語

- コード・コメント・XML ドキュメント・テスト・コミットの件名・PR タイトル・ルール・スキル・`README.md` は英語
- 設計書と `docs/` の文書、`README.ja.md`、Issue と PR の本文、コミットの本文は日本語でよい
- 詳細は `.cursor/rules/language.mdc`
