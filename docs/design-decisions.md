# 退けた案

設計で検討して退けた案と、その理由を残す。同じ形の案を再び出すときは、ここにある理由との違いを示す（`.cursor/rules/consent.mdc`）。採った案は [design.ja.md](./design.ja.md) にある。

各項目は Decision（採った案）、Reason（理由）、Rejected（退けた案）の順に書く。

## D1-1 スキーマの定義の形

- **Decision**: 属性を付けたレコード（またはクラス）が行の型で、ジェネレーターが列・テーブル参照・INSERT のモデルを作る（§3）。
- **Reason**: 行の型が利用者のコードのままで、メソッドやインターフェースを足せる。NULL 可否を C# の型で一度だけ書けばよい。ジェネレーターは宣言された型と属性をセマンティックモデルから読むので、解析が壊れにくい。
- **Rejected**: Drizzle のように、テーブルを列の定義（`Integer("id").PrimaryKey()`）で書き、行の型を生成する案。C# のフィールドには型の推論がないため、`NotNull()` や既定値の有無を行の型に反映するには、ジェネレーターがメソッドの連鎖の構文を読む（ヘルパーや変数を挟むと読めない）か、状態を型引数に持たせる（宣言が長くなる）しかない。

## D1-2 比較の書き方

- **Decision**: Drizzle と同じ関数形式（`Eq(u.Age, 20)`、`And(…)`）（§4.2）。
- **Reason**: オーバーロードで、nullable な値型と非 nullable の比較（`Eq(o.UserId, u.Id)`）を正しく型付けできる。`null` との比較をアナライザーで止められる。評価の段階（`TLevel`）を型引数で扱える。
- **Rejected**: 演算子形式（`u.Age > 20 & u.Name == "x"`）。C# の演算子は型引数を持てないので、`Expr<long?>` と `Expr<long>` の比較を一般的に定義できない。`== null` がそのままコンパイルされ `= NULL` になる。`==` を述語にすると `Equals` / `GetHashCode` との食い違いが生じ、`operator true` を定義すれば `if (u.Age == 5)` が通ってしまう。

## D1-3 ビルダーの順序と列の取り方

- **Decision**: `From` から始め、列はビルダーが渡すラムダの引数からだけ取る（§5.1）。
- **Reason**: FROM にないテーブルの列を構造的に書けなくなる。ラムダの引数の型を決めるには、テーブルが先に決まっている必要がある。
- **Rejected**: SQL と同じ `Select(...).From(...)` の順で、テーブルの静的なインスタンス（`UserTable.Instance.Name`）から列を取る案（jOOQ と旧 AuraORM の形）。どこからでも列を参照できるので、FROM にないテーブルの列を使う誤りをコンパイル時に防げない。

## D1-4 集計の正しさ

- **Decision**: 式に評価の段階（`PerRow` / `PerGroup`）を型引数として持たせる（§4.5）。
- **Reason**: SQLite は GROUP BY のキーでない列の参照を報告せず、任意の行の値を返す。誤った結果が黙って返るので、型で防ぐ価値が大きい。試作で、四つの誤りの形がすべてコンパイルエラーになり、正しい形がコンパイルできることを確かめた（Issue #3）。
- **Rejected**: 検査しない案（Drizzle、Kysely、jOOQ と同じ）。誤りがデータベースまで届き、SQLite では結果が誤ったまま返る。文を組み立てるときに実行時に検査する案。コンパイル時に防げるものを実行時に回すことになる。

## D1-5 外部結合の NULL 化

- **Decision**: テーブル参照が通常と NULL 化の二つの列の集合の型を持ち、`LeftJoin` / `RightJoin` / `FullJoin` の後のラムダには NULL 化の型を渡す（§5.3）。
- **Reason**: 外部結合で NULL になる列を、NULL を許さない型で読む誤りを防げる。結果の型（`List<(string, decimal?)>`）にも反映される。
- **Rejected**: 結合の種類によらず同じ列の型を使い、NULL を読んだときに実行時に例外にする案。結合のテーブル数の上限（8）はこの案にはないが、誤りが実行時まで残る。

## D1-6 UPSERT の API

- **Decision**: 方言ごとの構文をそのまま API にする（`OnConflict` と `OnDuplicateKeyUpdate`）（§10）。
- **Reason**: MySQL の `ON DUPLICATE KEY UPDATE` は、どの一意キーの衝突にも反応する。衝突の対象を指定する `ON CONFLICT` と、意味が違う。
- **Rejected**: 一つの `Upsert` API で方言ごとに出し分ける案。同じ呼び出しが方言によって違う意味になり、約束 2 に反する。

## D1-7 `First` に `LIMIT` を足すか

- **Decision**: 足さない。最初の行を読んだらリーダーを閉じる（§12.2）。
- **Reason**: ライブラリは呼び出し側が書いていない句を足さない（約束 2）。
- **Rejected**: EF Core と同じく `FirstAsync` が `LIMIT 1`（SQL Server では `TOP 1`）を足す案。

## D1-8 同期 API

- **Decision**: 実行は非同期だけにする。
- **Reason**: ADO.NET の非同期 API があり、同じ機能を二つの形で持つと、公開面とテストが倍になる。同期で呼ぶ必要がある場面は少ない。
- **Rejected**: `ToList()` などの同期版を併せて持つ案。

## D1-9 PostgreSQL のパラメータの形

- **Decision**: 名前のない位置パラメータ（`$1`）を使う（§5.12）。
- **Reason**: Npgsql は名前付きパラメータ（`@p0`）のとき SQL を解析して書き換える。位置パラメータならそのまま送られ、Npgsql もこちらを勧めている。
- **Rejected**: すべての方言で `@p0` を使う案。

## D1-10 集合演算の被演算子

- **Decision**: 被演算子をかっこで囲まず、被演算子には `ORDER BY` / `LIMIT` を書けないようにする（§5.10）。
- **Reason**: SQLite は被演算子のかっこを受け付けず、`ORDER BY` / `LIMIT` は複合 SELECT 全体の末尾にしか書けない。SQL Server も末尾にしか書けない。
- **Rejected**: 被演算子をかっこで囲み、それぞれに `ORDER BY` / `LIMIT` を許す案。SQLite と SQL Server で動かない。

## D1-11 式の型をクラスにするか、共変のインターフェースにするか

- **Decision**: 式と射影の型は `T` について共変のインターフェース（`IExpr<out T, TLevel>`、`IProjection<out T, TLevel>`）にする。外部での実装は `internal` のメンバーで防ぐ（§4.1）。
- **Reason**: 参照型の NULL 可否は注釈だけの違いなので、クラスの型引数としては不変になる。`string?` の列と `string` の列を比べると、利用者のコードで CS8620 が出る。警告をエラーにしている利用者のビルドは通らない。試作で、クラスでは 8 件の CS8620、共変のインターフェースでは 0 件だった（Issue #5）。
- **Rejected**: 抽象クラス `Expr<T, TLevel>` を公開の型にする案。

## D1-12 方言にない機能と、方言で位置の違う句

- **Decision**: 方言にない機能や、方言がその位置で受け付けない句（SQL Server の `ORDER BY` のない `Limit`、SQLite / MySQL の `Limit` のない `Offset` など）は、方言の機能のインターフェースで制約し、コンパイルエラーにする。ライブラリは句を補わない（§2.4、§5.9）。値で決まり型で表せないもの（SQL Server の `Limit(0)`）だけを実行前の検査にする（§14.3）。
- **Reason**: 約束 2（呼び出し側が書いていない句を足さない）と約束 3（そのデータベースにない機能はコンパイルエラー）。Issue #3 の Grill の質問 8 で決めた。
- **Rejected**: 実行時に `NotSupportedException` を投げる案（エージェントが実行まで気づかない）。SQL Server で `ORDER BY (SELECT NULL)` を補って `OFFSET … FETCH` を書く案（書いていない句が SQL に出て、行の順序も決まらない）。
