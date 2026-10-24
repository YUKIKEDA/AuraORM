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

## D1-13 条件の数が実行時に決まる絞り込み

- **Decision**: 一覧の `And` / `Or` が `null` を飛ばし、一つもなければ「条件なし」（`OptionalCondition<TLevel>`）を返す。SELECT の `Where` は「条件なし」なら `WHERE` を出さない。UPDATE と DELETE の `Where` は `OptionalCondition` を受け取らない（§4.2）。
- **Reason**: 検索フォームのような用途は約束 1（柔軟性）の中心にある。Drizzle ORM の `and()` と同じ書き方で、Drizzle では全行の UPDATE / DELETE になる場合を型で止める。Issue #3 の設計のレビューの指摘 9 への Grill で決めた。
- **Rejected**: 条件がなければ `WHERE 1 = 1` を出す案（書いていない条件が SQL に出る）。`Where` を何度も書けて `AND` でつなぐ案（D1-3 の「句は一度だけ」を変える）。

## D1-14 接続なしで SQL を作る入口

- **Decision**: `SqlBuilder<TDialect>` を足し、段階の型に「実行できるか」の型引数を持たせる。`SqlBuilder` から作った文には実行のメソッドがない（§12.3）。
- **Reason**: R9（接続なしで SQL を返す）を、プロバイダーのインスタンスなしで満たす。実行できない文を実行するとコンパイルエラーになる（約束 3）。Drizzle ORM の `TBuilderMode` と同じ作りである。
- **Rejected**: 接続のない `Database<TDialect>` を作れるようにし、実行すると例外にする案（実行できない文が型を通る）。Kysely の `DummyDriver` のように空の結果を返す案（黙って誤った結果になる）。開いていない `DbConnection` を渡す使い方を書くだけの案（プロバイダーのパッケージが要る）。

## D1-15 SQLite の `decimal` の比較と並べ替え

- **Decision**: SQLite では `decimal` の大小の比較、`ORDER BY` のキー、`Min` / `Max` / `Sum` / `Avg` を、文を組み立てるときの検査で `InvalidOperationException` にする（§3.3、§14.3）。
- **Reason**: SQLite の `decimal` は `TEXT` なので、これらは黙って文字列の順になる。式の型は方言を持たないので型では止められず、実行前の検査が残る手段である。EF Core の SQLite プロバイダーも、これらをデータベースでは行わない。
- **Rejected**: 方言のインターフェースでコンパイルエラーにする案（式の型が方言を知らないので書けない）。SQLite で `[Precision]` の位取りの整数として `INTEGER` に保存する案（生 SQL から見える値が倍率付きになり、乗除算で位取りの調整が要る）。保証の外と書くだけの案（黙った誤りが残る）。

## D1-16 `SELECT DISTINCT` の `ORDER BY`

- **Decision**: `SelectDistinct` の後の `OrderBy` は、選んだ項目（`DistinctItem<T, TLevel>`、式ではない）だけを受け取る。行全体、結果の型の射影、行全体を含むタプルの `SelectDistinct` には `OrderBy` がない（§5.7）。
- **Reason**: PostgreSQL と SQL Server は、選択リストにない `ORDER BY` の式を拒む。型で止められるので、実行前の検査にはしない（約束 3）。
- **Rejected**: `ToSql()` で `ORDER BY` のキーと選択リストを比べる案（型で表せる誤りを実行時に回す）。

## D1-17 同じ式をもう一度書くときのパラメータ

- **Decision**: 一つの式のオブジェクトを一つの文の中の複数の場所で使うとき（`GroupBy` のキー `k`、SQL Server の `IsNotDistinctFrom` の展開）は、同じパラメータを使う。同じ形の式でも別々に書けば、別のパラメータにする（§5.12）。
- **Reason**: PostgreSQL は、GROUP BY のキーと SELECT の式をパラメータの番号まで含めて比べるので、パラメータを含む式のキーが別の番号になると失敗する（PostgreSQL 18 で確かめた）。Issue #28 の Grill で決めた。
- **Rejected**: GROUP BY のキーを列に限る案（正しい SQL を拒む）。今のまま PostgreSQL で失敗すると書く案（実行時の失敗が残る）。

## D1-18 一つの式の GROUP BY のキーの形

- **Decision**: キーが一つの式なら、後のラムダはその式を `k` として受け取る。タプルなら `k.Item1` から（§5.5）。
- **Reason**: `Select` の、一つの式とタプルの射影の形とそろう。Issue #28 の Grill で決めた。
- **Rejected**: 一つのキーでも `k.Item1` と書く案（要素一つの入れ物で冗長）。

## D1-19 MySQL と MariaDB の HAVING の、列ではないキー

- **Decision**: MySQL と MariaDB では、列ではない GROUP BY のキーを HAVING に書くと `MIN(キー)` と出力する（§5.5）。
- **Reason**: この二つは HAVING の式を GROUP BY の式と照らし合わせず、正しいクエリが `Unknown column` で失敗する（MySQL のバグ 24083、78395）。キーはグループの中で一つの値なので、`MIN` で結果は変わらない。SQL は固定で `ToSql()` に見える。Issue #30 の実装中に結合テストで見つかり、所有者が決めた。
- **Rejected**: Pomelo.EntityFrameworkCore.MySql のように文をサブクエリに移し、条件を別名つきで射影に足す案（見えないサブクエリと列が増え、予測できる SQL に反する）。MySQL と MariaDB で実行時に例外にする案（正しいクエリが二つの方言で失敗する。型は方言ごとに列のキーと式のキーを見分けられない）。

## D1-20 SQL Server の `float` の `Sum`

- **Decision**: SQL Server では `Sum(float)` を `CAST(SUM(a) AS real)` と出力し、`float?` で読む（§4.2）。
- **Reason**: SQL Server の `SUM` は `real` に `float`（`double`）を返し、型付きの取得メソッドで `float` として読めない（§12）。EF Core も同じ `CAST` を入れる。Issue #30 の実装中に結合テストで見つかり、所有者が決めた。
- **Rejected**: 全方言で `double?` を返す案（.NET の `Sum(float)` と EF Core が `float` を返すのと違い、PostgreSQL にも `CAST` が要る）。読み取りで `double` を `float` に変える案（§12 の、広げも狭めもしない読み取りに反する）。

## D1-21 列を含まない GROUP BY のキー

- **Decision**: 列を一つも含まない GROUP BY のキーは、全方言で文を組み立てるときに `InvalidOperationException` にする（§5.5、§14.3）。生 SQL の断片は中を見ないので通す。
- **Reason**: SQL Server はメッセージ 164 で必ず失敗し、MySQL と MariaDB は値一つのキーを列の位置として読む。SQLite と PostgreSQL で通る文も、表全体を一つのグループにするだけで意味がない。型では止められない（`Value` も列も `IExpr<T, PerRow>`）。#31 のレビューで見つかり、所有者が決めた。
- **Rejected**: 失敗する方言だけで拒む案（同じ文が方言によって通ったり拒まれたりし、規則も複雑になる）。§14.2 に書くだけの案（SQL Server の実行時の失敗が残る）。

## D1-22 値だけの ORDER BY のキー

- **Decision**: 値だけ（パラメータ一つ）の ORDER BY のキーは、全方言で文を組み立てるときに `InvalidOperationException` にする（§5.8、§14.3）。値の式と関数は通す。
- **Reason**: MySQL と MariaDB は値を SQL に埋め込むので `ORDER BY 1` になり、エラーにならずに 1 列目で並ぶ。SQL Server はメッセージ 1008 で失敗する。行 Q1 からあった問題で、#31 のレビューで同じ種類の問題として見つかり、所有者が #31 で直すと決めた。
- **Rejected**: 別の行として後で直す案（結果が黙って変わる不具合がその行まで残る）。

## D1-23 サブクエリの型

- **Decision**: `In(a, sub)` と `Scalar(sub)` は、サブクエリの結果の型 `T` が合うものを受け取る。新しい段階の型は作らない。外側と方言の違うサブクエリは組み立て時の例外にする（§5.11）。
- **Reason**: タプルと行全体の射影は結果の型がタプルと行の型なので、型が合えば項目は一つに決まる。jOOQ と EF Core も結果の型で一つの列を求める。式の型は方言を持たない。Issue #35 の Grill で所有者が決めた。
- **Rejected**: 一つの式の射影の段階の型を別に作る案（段階の型が倍になり、`T` で同じことが決まる）。

## D1-24 MySQL と MariaDB の IN と LIMIT

- **Decision**: MySQL と MariaDB では、`Limit` の後のサブクエリを `In` / `NotIn` に渡すとコンパイルエラー（`ILimitInSubqueryDialect`）にする（§2.4、§5.11）。
- **Reason**: この二つはメッセージ 1235 で失敗する。方言にない機能は型で止める（D1-12）。Issue #35 の Grill で所有者が決めた。
- **Rejected**: jOOQ と linq2db のように派生テーブルで包む案（見えないサブクエリが増え、予測できる SQL に反する）。実行時の例外（型で止められる）。

## D1-25 LIMIT のない ORDER BY のサブクエリ

- **Decision**: `OrderBy` の後で `Limit` も `Offset` もない段階は、サブクエリ、派生テーブル、CTE の位置に、全方言で置けない（CS1503、§5.11）。
- **Reason**: その並び順は外側で保証されず、SQL Server はメッセージ 1033 で失敗する。Issue #35 の Grill で所有者が決めた。
- **Rejected**: EF Core のように `ORDER BY` を黙って消す案、jOOQ のように `TOP` を足す案（どちらも書いたものと違う SQL になる）。SQL Server だけで拒む案（ほかの方言でも意味のない並べ替えになる）。

## D1-26 2 行以上を返すスカラーサブクエリ

- **Decision**: §14.2（保証の外）に書く。
- **Reason**: 行の数はデータで決まり、型でも組み立て時にも止められない。SQLite だけ黙って 1 行目を返す。Issue #35 の Grill で所有者が決めた。
- **Rejected**: SQLite で `LIMIT` を足す案（書いたものと違う SQL になり、誤りを隠す）。

## D1-27 GROUP BY のキーのサブクエリ

- **Decision**: サブクエリを含む GROUP BY のキーは、全方言で組み立て時の例外にする（§5.5、§14.3）。
- **Reason**: SQL Server はメッセージ 144 で必ず失敗し、MySQL は相関していればメッセージ 1055 で失敗する。D1-21 と同じく、方言で通ったり拒まれたりしないようにする。Issue #35 の Grill で所有者が決めた。
- **Rejected**: 失敗する方言だけで拒む案。

## D1-28 グループの段階のサブクエリ

- **Decision**: `GroupScalar` / `GroupExists` は `PerGroup` の式と条件を作る。その中で外側の列は `k` を通してだけ使え、ほかは組み立て時の例外にする。`Scalar` / `Exists` は `PerRow` のまま（§5.11）。
- **Reason**: `HAVING COUNT(*) > (SELECT …)` は全方言で正しいので書けなければならない。HAVING に外側の行の列を書く誤りは SQLite が報告しないので止める。C# の型は戻り値で多重定義できず、二つの段階を実装する型は推論に失敗する。Issue #35 の Grill で所有者が決めた。
- **Rejected**: `PerRow` だけにする案（正しいクエリが生 SQL でしか書けない）。段階を型引数で明示する案（C# は型引数を一部だけ推論できないので、四つすべてを書くことになる）。

## D1-29 WITH の位置

- **Decision**: WITH は一番外側の文だけに書ける。`With` を持つ文を入れ子にすると組み立て時の例外にする。先頭の CTE は内側から参照できる（§5.11）。
- **Reason**: SQL Server は入れ子の WITH をメッセージ 156 で拒む。先頭の CTE を内側から参照することは全方言で通る。Issue #35 の Grill で所有者が決めた。
- **Rejected**: Drizzle ORM と Kysely のように入れ子の WITH をそのまま出す案（SQL Server で失敗する）。linq2db のように外側に移す案（書いた位置と出る位置が違い、パラメータの番号が入れ替わる。jOOQ の issue 3175）。

## D1-30 集合演算の後の ORDER BY と LIMIT

- **Decision**: 集合演算の後に `OrderBy`（項目の位置の番号）、`Limit`、`Offset` を書ける。射影が式か式のタプルのときだけ `OrderBy` がある（§5.10）。
- **Reason**: `… UNION … ORDER BY 1 LIMIT n` は全方言で通り、Drizzle ORM、Kysely、jOOQ も許す。項目の列は被演算子ごとに違うので、`SelectDistinct` と同じく位置で指す（D1-16）。Issue #35 の Grill で所有者が決めた。前の §5.10 の「派生テーブルにして外側で並べ替える」は、理由の記録がなかった。
- **Rejected**: 派生テーブルを必須にする案（全方言で通る SQL が書けない）。最初の被演算子の列で指す案（Drizzle ORM は列名に書き換えるが、ほかの被演算子の列の名前が違うと意味が分かりにくい）。

## D1-31 派生テーブルにできる射影

- **Decision**: 結果の型の射影と、一つのテーブルの行全体を派生テーブルと CTE にできる。列の別名は派生テーブルと CTE の中でだけ付ける（§5.11）。
- **Reason**: 列に名前があり、外側のラムダに型の付いた列の集合を渡せる。一番外側の SELECT の形を変えない。Drizzle ORM と Kysely も名前のない式には別名を求める。Issue #35 の Grill で所有者が決めた。
- **Rejected**: 式やタプルの射影に `c1`、`c2` などの名前を付ける案（名前が意味を持たず、外側で取り違えやすい）。

## D1-32 NULL を含みうるサブクエリの NOT IN

- **Decision**: `NotIn` のサブクエリの結果の型が NULL を許すときは AURA0203（エラー）にする。SQL は書き換えない（§4.4、§5.11）。
- **Reason**: 一行でも NULL があれば `NOT IN` はどの行にも一致しない（全方言で確かめた）。予測できる SQL を保つ。Issue #35 の Grill で所有者が決めた。Grill では値型を型で、参照型をアナライザーの警告で止めると書いたが、制約だけが違う多重定義は C# で書けず（CS0111）、§15 は診断をすべてエラーとするので、両方をアナライザーのエラーにした。
- **Rejected**: EF Core のように `NOT EXISTS` などに書き換える案（書いたものと違う SQL になる）。文書で注意するだけの案（jOOQ。誤りが残る）。

## D1-33 複数の CTE

- **Decision**: `db.With(a, b)` で複数の CTE を順に書け、後の CTE は前の CTE を参照できる。再帰 CTE は §18 の C2（§5.11）。
- **Reason**: 全方言で通る。Issue #35 の Grill で所有者が決めた。
- **Rejected**: なし。

