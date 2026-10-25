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

- **Decision**: 被演算子をかっこで囲まず、被演算子には `ORDER BY` / `LIMIT` を書けないようにする（§5.10）。書こうとすると CS0619 と直し方のメッセージになる（D1-25 と同じ仕組み、#43 のレビュー）。
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

- **Decision**: `OrderBy` の後で `Limit` も `Offset` もない段階は、サブクエリ、派生テーブル、CTE の位置に、全方言で置けない。その段階を受け取る `[Obsolete(error: true)]` の多重定義で、CS0619 と直し方のメッセージにする（§5.11）。
- **Reason**: その並び順は外側で保証されず、SQL Server はメッセージ 1033 で失敗する。Issue #35 の Grill で所有者が決めた。
- **Rejected**: EF Core のように `ORDER BY` を黙って消す案、jOOQ のように `TOP` を足す案（どちらも書いたものと違う SQL になる）。SQL Server だけで拒む案（ほかの方言でも意味のない並べ替えになる）。受け取る多重定義を置かない案（`In` は CS1503 で値の一覧を求めているように読め、`Exists` / `Scalar` は CS0411 で、直し方が分からない。#36 のレビューで所有者が CS0619 に決めた）。

## D1-26 2 行以上を返すスカラーサブクエリ

- **Decision**: §14.2（保証の外）に書く。
- **Reason**: 行の数はデータで決まり、型でも組み立て時にも止められない。SQLite だけ黙って 1 行目を返す。Issue #35 の Grill で所有者が決めた。
- **Rejected**: SQLite で `LIMIT` を足す案（書いたものと違う SQL になり、誤りを隠す）。

## D1-27 GROUP BY のキーのサブクエリ

- **Decision**: サブクエリを含む GROUP BY のキーは、全方言で組み立て時の例外にする（§5.5、§14.3）。
- **Reason**: SQL Server はメッセージ 144 で必ず失敗し、MySQL は相関していればメッセージ 1055 で失敗する。D1-21 と同じく、方言で通ったり拒まれたりしないようにする。Issue #35 の Grill で所有者が決めた。
- **Rejected**: 失敗する方言だけで拒む案。

## D1-28 グループの段階のサブクエリ

- **Decision**: `GroupScalar` / `GroupExists` は `PerGroup` の式と条件を作る。その中で外側の列は `Outer(k)`（列のキーだけ）を通してだけ使え、ほかは組み立て時の例外にする。`Scalar` / `Exists` は `PerRow` のまま（§5.11）。
- **Reason**: `HAVING COUNT(*) > (SELECT …)` は全方言で正しいので書けなければならない。HAVING に外側の行の列を書く誤りは SQLite が報告しないので止める。C# の型は戻り値で多重定義できず、二つの段階を実装する型は推論に失敗する。Issue #35 の Grill で所有者が決めた。
- **Rejected**: `PerRow` だけにする案（正しいクエリが生 SQL でしか書けない）。段階を型引数で明示する案（C# は型引数を一部だけ推論できないので、四つすべてを書くことになる）。
- **Outer(k)**: Grill の後、`k` が `PerGroup` なのでサブクエリの `Where`（`PerRow`）に書けないことが #37 の実装で分かった。所有者が `Outer(k)` を足すと決めた。キーを `PerRow` にしたラムダの引数で渡す案（タプルのキーごとに型が要る）と、相関なしだけにする案（正しいクエリが書けない）は採らなかった。`Outer` を列のキーに限るのは、列でないキーの参照が方言によって `MIN` の有無で通ったり失敗したりし（PostgreSQL、MySQL、MariaDB は `MIN` が要り、SQLite は `MIN` を拒む）、外側の集計の参照は SQLite が拒むためである。

## D1-29 WITH の位置

- **Decision**: WITH は一番外側の文だけに書ける。`With` を持つ文を入れ子にすると組み立て時の例外にする。先頭の CTE は内側から参照できる（§5.11）。
- **Reason**: SQL Server は入れ子の WITH をメッセージ 156 で拒む。先頭の CTE を内側から参照することは全方言で通る。Issue #35 の Grill で所有者が決めた。
- **Rejected**: Drizzle ORM と Kysely のように入れ子の WITH をそのまま出す案（SQL Server で失敗する）。linq2db のように外側に移す案（書いた位置と出る位置が違い、パラメータの番号が入れ替わる。jOOQ の issue 3175）。

## D1-30 集合演算の後の ORDER BY と LIMIT

- **Decision**: 集合演算の後に `OrderBy`（項目の位置の番号）、`Limit`、`Offset` を書ける。`OrderBy` のラムダは結果の型の要素ごとの項目を受け取り、射影に行全体か結果の型があれば組み立て時の例外にする（§5.10）。
- **Reason**: `… UNION … ORDER BY 1 LIMIT n` は全方言で通り、Drizzle ORM、Kysely、jOOQ も許す。項目の列は被演算子ごとに違うので、`SelectDistinct` と同じく位置で指す（D1-16）。Issue #35 の Grill で所有者が決めた。前の §5.10 の「派生テーブルにして外側で並べ替える」は、理由の記録がなかった。
- **Rejected**: 派生テーブルを必須にする案（全方言で通る SQL が書けない）。最初の被演算子の列で指す案（Drizzle ORM は列名に書き換えるが、ほかの被演算子の列の名前が違うと意味が分かりにくい）。
- **#42 の Grill での変更**: 前の Decision は「射影が式か式のタプルのときだけ `OrderBy` がある」（型で出し分ける）だった。今の `Select` は式のタプルと行全体を含むタプルで同じ段階の型を返すので、型で出し分けるには `Select` に式のタプル専用の多重定義と段階の型を足し、`Select` の多重定義と段階の型を倍にする必要がある（D1-23 で退けた形）。所有者が、行全体のときだけ組み立て時の例外にする形に決めた。

## D1-31 派生テーブルにできる射影

- **Decision**: 結果の型の射影と、一つのテーブルの行全体を派生テーブルと CTE にできる。列の別名は派生テーブルと CTE の中でだけ付ける（§5.11）。どの被演算子にも付ける（D1-38）。
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

## D1-34 外側と同じ名前のテーブルの相関

- **Decision**: 内側の文の `FROM` に外側の文と同じ名前（別名）のテーブルがあり、外側の列を内側で使うと、組み立て時の例外にする。相関しないサブクエリは通す（§5.11、§14.3）。
- **Reason**: 外側の列と内側の列が同じ修飾で出て、SQL は内側のテーブルの列と解釈するので、例外もエラーもなく全方言で誤った結果になる。§5.2 の、同じテーブルを別名なしで二度使う検査を、文をまたいで広げる。#36 と #38 のレビューで見つかった。
- **Rejected**: 内側のテーブルに自動で別名を付ける案（書いていない別名が SQL に出て、予測できる SQL に反する）。

## D1-35 集合演算の被演算子の結果の型

- **Decision**: 集合演算は、結果の型が同じ被演算子だけを結べる。値型で NULL 可否だけが違う（`int` と `int?`）ものも結べない。参照型の NULL 可否の違いは型では止められず、null 許容の警告 CS8620 になり、結果の型は最初の被演算子の型になる（§5.10、#43 のレビュー）。
- **Reason**: 型を合わせる側で `Coalesce` などを書けば済み、結果の型の決め方（どちらかが NULL を許せば NULL を許す）を型の上で表すと、`Union` の多重定義が要素の数だけ増える。Drizzle ORM も形が同じものだけを許す。#42 の Grill で所有者が決めた。
- **Rejected**: NULL 可否が違う被演算子を結び、結果を NULL を許す型にする案（多重定義が増え、型の推論が複雑になる）。

## D1-36 SQLite の集合演算の被演算子の数

- **Decision**: SQLite で被演算子が 500 を超える集合演算は、組み立て時に `ArgumentException` にする。PostgreSQL の上限は設定で決まるので §14.2 に書く（§5.10）。
- **Reason**: 被演算子はループでいくつでも足せる。SQLite の既定の上限は固定の 500 で、パラメータの数の上限（§12.4）と同じ種類の方言の数の上限なので、同じく送る前に止める。#43 のレビューで見つかり、所有者が決めた。
- **Rejected**: §14.2 に書くだけの案（SQLite では実行時のエラーになり、パラメータの上限の扱いとそろわない）。

## D1-37 派生テーブルと CTE の入口

- **Decision**: `sub.As("t")` で名前付きのクエリを作る。`db.With(t)` に並べた文では CTE、並べないときは派生テーブルとして書く。`As` は結果の型と `[Table]` の型ごとにジェネレーターが生成する。式やタプルの射影の `As` は、総称の `[Obsolete(error: true)]` で CS0619 と直し方のメッセージにする。`t.As("t2")` で別名の参照を作る（§3.5、§5.11）。
- **Reason**: C# は制約から型引数を推論しないので、列の集合の型を結果の型から決めるには型ごとの生成が要る（`db.From(…).Select(…).As("t")` が通常・グループ・集合演算の段階で推論されることを試作で確かめた）。CTE にも同じ型が要るので、入口を一つにした。#49 の Grill で所有者が決めた。
- **Rejected**: 設計の前の版の `db.Cte("t", sub)`（`As` と同じ生成がもう一つ要る）。型引数を書く `sub.As<UserOrderTotal.Columns, UserOrderTotal.NullableColumns>("t")`（呼び出しが長く、取り違えやすい）。

## D1-38 名前付きのクエリの中の集合演算の列の別名

- **Decision**: 名前付きのクエリの中の集合演算では、どの被演算子の項目にも `AS "列名"` を付ける（§5.11）。
- **Reason**: 文が名前付きのクエリの中にあるかどうかだけで書き方が決まり、被演算子の位置で変わらない。全方言で通る。#49 の Grill で所有者が決めた。
- **Rejected**: 最初の被演算子にだけ付ける案（列の名前は最初の被演算子で決まるが、位置によって書き方が変わる）。

## D1-39 CTE と同じ名前の本物のテーブル

- **Decision**: `With` に並べた CTE と同じ名前の本物のテーブルを、その文で使うと組み立て時の例外にする（§5.11、§14.3）。
- **Reason**: SQL はその名前を CTE と解釈し、例外もエラーもなく誤った結果になる（全方言で確かめた）。D1-34 と同じ種類の黙った誤り。#49 の Grill で所有者が決めた。
- **Rejected**: そのまま書く案（黙って誤る）。

## D1-40 名前付きのクエリの本体は独立した文

- **Decision**: 名前付きのクエリの本体の中では、外側の文の列を使えない。使うと組み立て時の例外にする。相関はサブクエリで書く（§5.11）。
- **Reason**: 外側の行ごとに中を評価する派生テーブルは `LATERAL`（SQL Server では `CROSS APPLY`）で、方言で書き方が違い、設計の範囲にない（行 Q6 の Issue #51 の範囲の外）。`LATERAL` のない派生テーブルが外側の列を使うと、データベースが拒むか、別の列と解釈する。#50 と #52 のレビューで、実装の振る舞いが設計に書かれていないと指摘されて書いた。
- **Rejected**: 外側の列をそのまま書く案（`LATERAL` なしでは不正な SQL になる）。

## D1-41 後に並べた CTE を前の CTE が読む

- **Decision**: `With` に後から並べた CTE を、前の CTE の本体が読むと、組み立て時の例外にする（§5.11）。
- **Reason**: CTE は前に定義したものしか読めない。派生テーブルとして書き直すと SQL としては正しいが、本体が二度出て、書き手が意図した CTE の参照と違う。#50 と #52 のレビューの後に所有者が決めた。
- **Rejected**: その時点で定義済みの CTE だけを名前で読み、まだのものは派生テーブルにする案（並べる順序で SQL の形が変わり、本体が二度出る）。


## D1-42 MySQL で UPDATE / DELETE のサブクエリが対象のテーブルを読む

- **Decision**: MySQL では、UPDATE / DELETE の文の中と、`OnDuplicateKeyUpdate` の値の中のサブクエリが対象のテーブルを読むと（派生テーブルを通しても）、組み立て時の例外にする（§7、§10.2、§14.3）。
- **Reason**: MySQL はこの文をエラー 1093 で拒む。派生テーブルを通して通るのは実体化されたときだけで、既定は併合であり、どちらになるかを資料は保証しない（8.4 の `update.html`）。#56 の Grill で所有者が決めた。`OnDuplicateKeyUpdate` への拡張は #71 のレビューで決めた。MySQL 8.4 と 9.7 は、`ON DUPLICATE KEY UPDATE` の値でも、別名のないテーブルと派生テーブルを通した形を 1093 で拒み、別名を付けた形だけを通した。その理由は資料になく、別名の有無で規則を分けない。
- **Rejected**: jOOQ のように `SELECT * FROM (…) AS t` で自動で包む案（書いたものと SQL が変わり、包んでも通る保証がない）。そのまま送る案（EF Core / Pomelo。MySQL でだけ実行時に失敗する）。

## D1-43 MySQL と MariaDB の左から順の代入

- **Decision**: MySQL と MariaDB では、UPDATE の代入の値がそれより前の代入の列を読むと、組み立て時の例外にする（§7、§14.3）。
- **Reason**: この二つは代入を左から順に評価し、ほかの方言と結果が違う（`SET n = n + 1, m = n`）。黙って違う結果になる。#56 の Grill で所有者が決めた。
- **Rejected**: §14.2 に書くだけの案（黙って誤る）。代入を並べ替える案（値の入れ替えは並べ替えても書けず、書いた順と SQL が変わる）。

## D1-44 SQL Server の OUTPUT のサブクエリ

- **Decision**: SQL Server で `Returning` の射影にサブクエリがあると、組み立て時の例外にする（§9、§14.3）。
- **Reason**: SQL Server は `OUTPUT` のサブクエリを拒む（メッセージ 10705）。サブクエリは型でほかの式と区別できない。#56 の Grill で所有者が決めた。
- **Rejected**: サーバーのエラーに任せる案（実行するまで分からない）。

## D1-45 RETURNING のサブクエリが対象のテーブルを読む

- **Decision**: どの方言でも、`Returning` のサブクエリが対象のテーブルを読むと、組み立て時の例外にする（§9、§14.3）。
- **Reason**: どの時点の表を見るかが方言で違い（PostgreSQL と MariaDB の DELETE は削除の前、SQLite 3.40 の DELETE は最初の 1 行を消した途中、SQLite の UPDATE は更新の前）、SQLite の資料は結果を決まっていないものとする。#56 の Grill で所有者が決めた。
- **Rejected**: 方言の違いを §14.2 に書く案（同じ文が方言で違う値を黙って返す）。

## D1-46 全列が既定値の複数行の INSERT

- **Decision**: 設定された列が一つもない行が二つ以上あるとき、MySQL と MariaDB は `() VALUES (), ()` を出し、SQLite、PostgreSQL、SQL Server は `ArgumentException` にする（§6.2、§14.3）。
- **Reason**: 三つの方言では `DEFAULT VALUES` が一行しか書けず、SQLite は `VALUES` の中に `DEFAULT` も書けない。#56 の Grill で所有者が決めた。
- **Rejected**: PostgreSQL と SQL Server で列を一つ選んで `DEFAULT` を出す案（EF Core の SQL Server の形。`[AutoIncrement]` の列しかない表では選べる列がなく、規則が複雑になる）。すべての方言で例外にする案（MySQL と MariaDB で書ける文を拒む）。

## D1-47 空の行の一覧の INSERT

- **Decision**: `Values` に空の一覧を渡すと `ArgumentException` にする（§6.2、§14.3）。
- **Reason**: 行のない INSERT は SQL にならない。何もしないで 0 を返すと、呼び出し側の誤りが隠れる。Drizzle ORM も例外にする。#56 の Grill で所有者が決めた。
- **Rejected**: 何も実行せず 0 を返す案（jOOQ、linq2db）。

## D1-48 `ExecuteAsync` の行の数

- **Decision**: `ExecuteAsync` はプロバイダーが返す行の数をそのまま返し、接続の設定（MySqlConnector の `UseAffectedRows`、SQL Server の `NOCOUNT`）で意味が変わることを XML ドキュメントに書く（§6.2）。
- **Reason**: 接続の設定は利用者のもので、AuraORM からは確かめられない。MySqlConnector も既定では、ほかのプロバイダーと同じく条件に合った行の数を返す（各方言の数は結合テストで確かめる）。#56 の Grill で所有者が決めた。
- **Rejected**: 行の数を別の問い合わせで数え直す案（もう一度データベースに問い合わせることになり、同時の変更で数が合わない）。

## D1-49 SQLite の最小版の結合テスト

- **Decision**: CI の SQLite だけのジョブで、ネイティブのライブラリを `SQLitePCLRaw.lib.e_sqlite3` 2.1.3（SQLite 3.39.2）に替えて結合テストを回す。provider は core と同じ 2.1.13 を参照して、テストの始めに設定する（§17）。
- **Reason**: 既存のパッケージで最小版を得られ、CI でビルドしなくてよい。bundle の 2.1.3 は provider の版が core と合わず失敗することを試して確かめた。#56 の Grill で所有者が決めた。
- **Rejected**: sqlite.org の 3.39 のソースを CI でビルドする案（コンパイラーと手順が要り、OS ごとに違う）。

## D1-50 INSERT / UPDATE / DELETE の対象の別名

- **Decision**: INSERT、UPDATE、DELETE の対象に別名付きのテーブルを渡すと `ArgumentException` にする（§6.2、§14.3）。
- **Reason**: `Table.As` は同じ型を返すので型では止められない。SQL Server では対象の別名を `FROM` 句でしか付けられず（§18 の U1）、方言で規則を分けないために三つの文で揃える。対象のテーブルを読むサブクエリは、内側に別名を付ければ書ける。#56 の Grill で所有者が決めた。
- **Rejected**: SQL Server 以外で別名を出す案（方言で規則が分かれる）。別名を捨ててテーブル名で書く案（書いたものと SQL が変わる）。

## D1-51 `Set` の式の型引数

- **Decision**: 式を代入する `Set` は、式の型を型引数に取る（`Set<T, TExpr>(Column<T>, TExpr) where TExpr : IExpr<T, PerRow>`）。`Set(u.Nickname, null)` は値の多重定義を選び、NULL を代入する（§7）。
- **Reason**: 式の引数を `IExpr<T, PerRow>` にすると、`null` が値（`T`）と式の両方に当てはまり、CS0121（呼び出しがあいまい）になる。`null` からは `TExpr` を推論できないので、型引数にすると値の多重定義だけが残る。行 M1 の実装（#60）でコンパイルのテストが見つけた。
- **Rejected**: 利用者に `(string?)null` と書かせる案（よく使う代入で型を書かせる）。C# 13 の `OverloadResolutionPriority` で優先する案（C# 12 の利用者では効かない）。

## D1-52 `[AutoIncrement]` の列への代入と、UPDATE / DELETE の `OptionalCondition`

- **Decision**: `ReadOnlyColumn<T>` を受け取る `Set` と、`OptionalCondition` を受け取る UPDATE / DELETE の `Where` を `[Obsolete(…, error: true)]` で置き、CS0619 と直し方のメッセージにする（§7、§14.1）。
- **Reason**: D1-51 の型引数のため、`[AutoIncrement]` の列への代入は値の側の型引数の誤り（CS0311）として報告され、列の誤りだと分からない。`OptionalCondition` はラムダの戻り値の変換の誤り（CS0029）になり、`AllRows()` を書けばよいと分からない。D1-25 と同じ考えで、直し方を示す。行 M1 の実装（#60）で決めた。
- **Rejected**: コンパイラーのエラーのままにする案（直し方が分からない）。

## D1-53 `Returning` の文を途中で閉じるときの `Cancel`

- **Decision**: `Returning` の文を最後まで読まずに閉じるとき、`DbCommand.Cancel()` を呼ばず、残りの行をプロバイダーに読み捨てさせる（§9、§12.2）。
- **Reason**: MariaDB 12.3 と SQL Server 2025 では、20 万行の変更の最初の行を読んで `Cancel` すると、例外なしに文全体が取り消された。最初の行が返るのに変更がない。#63 で所有者が決めた。
- **Rejected**: SELECT と同じく `Cancel` を呼ぶ案（変更が黙って消える）。`Returning` の後に `First…` を置かない案（途中でやめた `AsAsyncEnumerable()` に同じ問題が残る）。

## D1-54 `ON DUPLICATE KEY UPDATE` の左から順の代入

- **Decision**: MySQL と MariaDB の `OnDuplicateKeyUpdate` にも、前の代入の列を読む値の検査（D1-43）を当てる（§10.2）。
- **Reason**: `ON DUPLICATE KEY UPDATE` の代入も左から順に評価され（8.4 と 9.7、10.11 と 12.3 で確かめた）、SQLite と PostgreSQL の `DO UPDATE` と結果が違う。#70 の Grill で所有者が決めた。
- **Rejected**: 検査しない案（黙って違う値が入る）。

## D1-55 MySQL の行の別名と、含めていない列

- **Decision**: MySQL は行の別名 `AS excluded` で挿入しようとした行を読み、INSERT に含めていない列を `excluded` で読むと組み立て時の例外にする（§10.2）。
- **Reason**: MySQL は別名の列の並びにない列をエラー 1054 で拒む。`VALUES(列)` は 8.0.20 から非推奨で、削除されうる。#70 の Grill で所有者が決めた。
- **Rejected**: 読まれる列を `DEFAULT` で列の並びに足す案（書いていない列が SQL に出る。試していない）。MySQL でも `VALUES(列)` を使う案（非推奨）。jOOQ のように既存の行の列に書き換える案（意味が変わる）。

## D1-56 UPSERT の行の名前 `excluded`

- **Decision**: 挿入しようとした行を、どの方言でも `excluded` と呼ぶ（MySQL の行の別名も `excluded`）。テーブルの名前が `excluded` のときは、SQLite、PostgreSQL、MySQL で組み立て時の例外にする（§10.1、§10.2）。
- **Reason**: 名前を揃えると、方言をまたいで同じラムダを書ける。jOOQ も MySQL に `AS excluded` を出す。`excluded` という名前のテーブルでは、SQLite 3.40 が既存の行の値を黙って読み、PostgreSQL（15 と 18）は「あいまい」、MySQL（8.4 と 9.7）はエラー 1066 で拒んだ。#70 の Grill（Q12）で、試してから決めると所有者が決め、その結果で決めた。
- **Rejected**: 設計の前の版の `AS new`（方言で名前が分かれる）。`excluded` という名前のテーブルをそのまま書く案（SQLite で誤った値が入る）。

## D1-57 UPSERT の、同じ値で衝突する複数の行

- **Decision**: 一つの文の複数の行が同じ一意の値で衝突しても検査せず、§14.2 と XML ドキュメントに書く（§10.3）。
- **Reason**: どの値が重なるかはデータで決まる。PostgreSQL はエラーにするので黙った誤りにはならず、ほかの方言は行ごとに順に適用する。調べたライブラリ（Drizzle ORM、Kysely、jOOQ、linq2db）にも検査はない。#70 の Grill で所有者が決めた。
- **Rejected**: パラメータの値を比べて組み立て時に例外にする案（MySQL ではどの一意キーで衝突するかが分からず、半分しか調べられない）。

## D1-58 SQLite の `DEFAULT VALUES` と UPSERT

- **Decision**: SQLite で、設定した列が一つもない行に UPSERT の句を付けると `ArgumentException` にする（§10.1）。
- **Reason**: SQLite の資料に「`DEFAULT VALUES` の後には UPSERT の句を書けない」とある。PostgreSQL、MySQL、MariaDB は書ける。#70 の Grill で所有者が決めた。
- **Rejected**: そのまま書いて SQLite の構文エラーに任せる案（実行するまで分からない）。

## D1-59 対象のない `DO NOTHING`

- **Decision**: 対象のない `OnConflictDoNothing()` と、対象のある `OnConflict(…).DoNothing()` / `DoUpdate(…)` を置く。対象のない `DoUpdate` は置かない（§10.1）。
- **Reason**: PostgreSQL の資料によれば、対象を省けるのは `DO NOTHING` だけである。Drizzle ORM と jOOQ も対象のない `DO NOTHING` を持つ。#70 の Grill で所有者が決めた。
- **Rejected**: 対象を必ず書かせる案（どの衝突でも飛ばす書き方ができない）。

## D1-60 UPSERT の行の数

- **Decision**: `ExecuteAsync` は UPSERT の行の数をプロバイダーの値のまま返し、方言ごとの意味（MySQL と MariaDB は更新した行を 2 と数える）を XML ドキュメントに書く（§10.3）。
- **Reason**: D1-48 と同じ。数え直すには、もう一度問い合わせることになる。#70 の Grill で所有者が決めた。
- **Rejected**: 方言の違いを吸収して数え直す案。

## D1-61 UPSERT の代入先の検査

- **Decision**: `DoUpdate` と `OnDuplicateKeyUpdate` の代入先が既存の行の列でないと（`excluded` の列）、組み立て時に `InvalidOperationException` にする（§10.1）。
- **Reason**: `excluded` の列もテーブルの列の型（`Column<T>`）なので型では止められず、代入先は列名だけで書くので、黙ってテーブルの列を更新してしまう。代入できない列の型を生成すると `Table<…>` の型引数が一つ増える（C# は制約から型引数を推論しない。D1-37）。#70 の Grill で所有者が決めた。
- **Rejected**: 代入できない列の型をジェネレーターに出させる案（既存のすべての型の記述が変わる）。

## D1-62 MySQL の「何もしない」と、衝突の対象の照合

- **Decision**: MySQL と MariaDB に「何もしない」形（`INSERT IGNORE`）は置かず、代入できる列を自分自身に代入して（`Set(u.Name, u.Name)`）書けることを XML ドキュメントに書く。衝突の対象をスキーマの一意の制約と照らさない（§10.2、§10.3）。
- **Reason**: `INSERT IGNORE` は一意キーの衝突のほかのエラーも警告にする。データベースには C# に宣言していないインデックスがありうる。#70 の Grill で所有者が決めた。
- **Rejected**: `InsertIgnore()` を置く案。宣言した制約と照らして例外にする案（正しい文を拒みうる）。

## D1-63 部分インデックスの対象と制約の名前

- **Decision**: UPSERT の式の衝突の対象、部分インデックスの対象（`ON CONFLICT (…) WHERE …`）、制約の名前（`ON CONFLICT ON CONSTRAINT …`）は、v0.1 の後の行 U3 にする（§10.1、§18）。
- **Reason**: 要件 R4 は `ON CONFLICT` と `ON DUPLICATE KEY UPDATE` までで、列の対象で足りる。#70 の Grill で所有者が決めた。
- **Rejected**: 行 M2 に入れる案（範囲が広がる）。

## D1-64 MariaDB の `VALUE(列)`

- **Decision**: MariaDB では、挿入しようとした行の列を、テーブル名で修飾した `VALUE(テーブル.列)` で読む。`excluded` の列を読むサブクエリの `FROM` に、修飾の名前のテーブルがあれば組み立て時の例外にする（§10.2）。
- **Reason**: 旧名の `VALUES(列)` は `ON DUPLICATE KEY UPDATE` の中でしか通らず、値のサブクエリの中ではエラー 1064 になる。`VALUE(列)` は、句の直下でもサブクエリの中でも通り、含めていない列を既定値で読む意味も同じである。ただし修飾しないと、サブクエリの表の同じ名前の列と読まれ、黙って NULL になったので、修飾する（10.11.19 と 12.3.3 で確かめた）。`VALUE()` にすることは #75 のレビューの後に所有者が決め、修飾は結合テストで見つけた。
- **Rejected**: サブクエリの中で読むと組み立て時の例外にする案（書ける文を拒む）。サブクエリの外へ括り出す案（書いたものと SQL が変わる）。

## D1-65 型ごとの値コンバーター

- **Decision**: 値コンバーターは型ごとに一つにし、モデルの型に `[ValueConverter(typeof(C))]`、自分で定義していない型には `[assembly: ValueConverter(typeof(C))]` で宣言する。列の `[Converter]` はなくす（§3.6）。
- **Reason**: 値は列とつながらない所（生 SQL の穴、`Case` の値、`[QueryResult]` のメンバー）からも入る。型で決めれば、式の木に変換の情報を流さずに、どこでも同じ変換が当たる。NativeAOT 向けの Dapper.AOT も、ビルド時に型ごとに宣言する。#78 の Grill で所有者が決めた。
- **Rejected**: 設計の前の版の、列ごとの `[Converter]`（列と比べる値に変換を伝えるため、すべての関数の値の引数に列の情報を渡すことになり、列とつながらない値は変換できない。EF Core、linq2db、jOOQ、Drizzle ORM はこの形で、EF Core は生 SQL で使えないと書いている）。

## D1-66 コンバーターの登録

- **Decision**: ジェネレーターが、コンバーターを宣言したアセンブリと、その型をテーブルや結果の型に使うアセンブリに、登録のモジュールの初期化子を出す。初期化子はアセンブリごとに一つのクラス `AuraORM.Generated.__AuraValueConverters` に置き、同じ完全名の型があれば AURA0116 にする。登録のない型の値を使うと `InvalidOperationException` にする。二つのアセンブリが別のコンバーターを登録したときは、初期化子では投げず、その型の値を使うときに `InvalidOperationException` にする（§3.6、§14.3）。
- **Reason**: .NET はモジュールの初期化子を、そのモジュールのメソッドの最初の呼び出しまでに実行する（ECMA-335 の補遺）。モデルの型のコンストラクターかテーブルを使えば登録が済む。リフレクションで探すと NativeAOT で使えない。#78 の Grill（Q1）の決定を実現する形として決めた。
- **Rejected**: 実行時にアセンブリを走査して探す案（リフレクションが要る）。利用者に登録の呼び出しを書かせる案（書き忘れが実行時まで分からない）。衝突を初期化子で投げる案（`TypeInitializationException` になり、使わない型の衝突でも起動で落ちる。#79 のレビューで決めた）。

## D1-67 コンバーターの変換先と NULL

- **Decision**: 変換先は §3.3 の表の、NULL を許さない型（列挙型を除く）に限り、コンバーターは重ねない。モデルの型は表にない型に限る。NULL はコンバーターを通さない（§3.6）。
- **Reason**: 変換先が表の型なら、パラメータと読み取りは既存のとおりにプロバイダーに任せられる。表の型の変換を変えると、その型のすべての値の扱いが変わる。EF Core、linq2db、Drizzle ORM も NULL をコンバーターに渡さない。#78 の Grill で所有者が決めた。
- **Rejected**: NULL もコンバーターに渡す案（すべてのコンバーターが NULL を扱うことになる）。

## D1-68 変換した列の比較と並べ替え

- **Decision**: 比較と並べ替えは変換後の値で行う。型の制約を持つ関数と `Min` / `Max` はモデルの型に使えない。変換後の型が `decimal` なら SQLite の検査を当てる（§3.6）。変換した型の集計は §18 の VC2 にする。
- **Reason**: SQL は変換後の値しか知らない。型の制約は、意味のない演算（ID の足し算）をコンパイルエラーにする。`Min` / `Max` は #78 の Grill では書けるとしたが、§3.3 の型ごとと列挙型の多重定義しかなく、総称の制約を広げると `MAX` できない型まで通るので、行 V1 の実装の中で所有者が使えないことに決めた。
- **Rejected**: 比較と並べ替えを禁じる案（ID の等しさの比較と並べ替えは必要である）。

## D1-69 コンバーターの例外

- **Decision**: コンバーターが投げた例外は包まずに届ける（§3.6）。
- **Reason**: `DbException` を包まない方針（`.cursor/rules/csharp.mdc`）と同じで、利用者のコードの例外を、その型のまま捕まえられる。#78 の Grill で所有者が決めた。
- **Rejected**: AuraORM の例外に包む案。

## D1-70 DDL の文の形

- **Decision**: DDL は `CreateTable`、`CreateIndexes`、`DropIndexes`、`DropTable` の四つの文にする。一つの呼び出しは一つのテーブルで、`CreateIndexes` と `DropIndexes` は `[Index]` ごとに一つの文の一覧（`ToSql()` は `IReadOnlyList<SqlStatement>`）を順に実行する。`ExecuteAsync` は `Task` を返す。テーブルを作る順序と消す順序は利用者が決める（§13.1）。
- **Reason**: 要件 R8 は CREATE / DROP の TABLE と INDEX である。DDL の影響行数には意味がない。文を一つずつにすれば、一つの命令に複数の文を書けるかをプロバイダーに頼らずにすむ。`DropIndexes` は #86 の Grill（Q1）で、`CreateIndexes` と対になる形として所有者が決めた。
- **Rejected**: 名前を文字列で受け取る `DropIndex(T, "名前")`（名前の誤りが型で分からない）。索引の文を `;` でつないだ一つの文（一つの命令の複数の文の扱いがプロバイダーで違う）。EF Core の `EnsureCreated` のように、テーブルを外部キーの依存の順に並べる一括の作成（一つの呼び出しが一つの文という形から外れ、差分のマイグレーション MG1 の領分に入る）。

## D1-71 トランザクションの中の DDL

- **Decision**: `Transaction` の DDL は、`ITransactionalDdlDialect`（SQLite、PostgreSQL、SQL Server）だけで書ける。MySQL と MariaDB は CS0311 にする（§12.5、§13.1）。
- **Reason**: MySQL と MariaDB の DDL は暗黙のコミットを起こし、ロールバックで戻らないうえ、それより前の文までコミットする。トランザクションの中に書けると、利用者が書いたとおりに動かない。#86 の Grill（Q13）で所有者が決めた。
- **Rejected**: すべての方言で書けるようにし、資料で知らせる案（ロールバックした INSERT がコミットされる）。

## D1-72 一意制約と一意の索引

- **Decision**: `[Unique]` は CREATE TABLE の表の制約、`Unique = true` の `[Index]` は `CREATE UNIQUE INDEX` にする。SQL Server では、NULL を許す列を含む一意の `[Index]` に `WHERE … IS NOT NULL` を付け、NULL を許す列の `[Unique]` は組み立て時に `InvalidOperationException` にする（§13.4）。
- **Reason**: 表の制約はテーブルと一緒にでき、外部キーの参照先になれる。SQL Server の一意制約だけが NULL を一つしか許さないので、フィルターで他の方言と同じ意味にする。表の制約にはフィルターを付けられず、フィルター付きの索引は外部キーの参照先にならない（エラー 1776）。EF Core も同じフィルターを付ける。#86 の Grill（Q2、Q3）で所有者が決めた。
- **Rejected**: 方言で NULL の扱いが違うことを資料に書くだけの案（同じ定義で、SQL Server だけ 2 行目の NULL が失敗する）。`[Unique]` も `CreateIndexes` の索引にする案（外部キーの参照先がテーブルとは別の呼び出しでできることになる）。

## D1-73 外部キーの参照先と、MySQL の外部キーの索引

- **Decision**: `[References]` の参照先は、単独の主キーか `[Unique]` の列に限る（AURA0108）。MySQL と MariaDB では、主キーの先頭でも `[Unique]` でもない外部キーの列に、CREATE TABLE で `KEY (列)` を書く（§13.5）。`ON DELETE` と `CASCADE` は持たない。
- **Reason**: どの方言も、主キーでも一意でもない列への外部キーを失敗させる（SQLite は INSERT の時に）。MySQL と MariaDB は、外部キーのために暗黙に作った索引を、同じ列で始まる索引を作ると消し、その索引を `DropIndexes` で消せなくする（エラー 1553）。明示した `KEY` は消されない。参照先は #86 の Grill（Q4）で所有者が決めた。`KEY` は設計の PR の確かめで見つかった。
- **Rejected**: 参照先を検査せず、データベースのエラーに任せる案（SQLite では CREATE TABLE が通り、INSERT まで分からない）。MySQL で `DropIndexes` を組み立て時に拒む案（外部キーの列で始まる索引を持つテーブルで、索引を消す方法がなくなる）。

## D1-74 現在時刻の既定値

- **Decision**: `SqlDefault.CurrentTimestamp` は、すべての方言で UTC の現在時刻にする。付けられるのは `DateTime` と `DateTimeOffset` の列だけで、ほかは AURA0117 にする（§13.3）。
- **Reason**: SQL の `CURRENT_TIMESTAMP` は、SQLite では UTC、PostgreSQL と MySQL ではセッションのタイムゾーン、SQL Server ではサーバーの時刻で、同じ定義で入る値が変わる。`DateOnly` と `TimeOnly` は、MySQL の `DATE` の既定値がエラーになり、SQLite の文字列の形が読み取りと合わない。#86 の Grill（Q5、Q6）で所有者が決めた。
- **Rejected**: `CURRENT_TIMESTAMP` をそのまま出す案（値がサーバーとセッションの設定で変わる）。

## D1-75 既定値のリテラル

- **Decision**: `[Default]` の定数は方言ごとのリテラルで書く。MySQL と MariaDB の長さのない文字列とバイト列の列ではかっこで囲む。MySQL と MariaDB でバックスラッシュを含む文字列は組み立て時に `InvalidOperationException`、配列、有限でない数、NULL を許さない列の `null` は AURA0113 にする（§13.3）。
- **Reason**: DDL の既定値はパラメータにできない。MySQL は TEXT と BLOB の既定値を式の形でしか受け付けない（Pomelo も同じ形にする）。MySQL と MariaDB の文字列のバックスラッシュの意味は `sql_mode` で変わり、同じ SQL で既定値が変わる。SQLite の `decimal` のリテラルは、パラメータで書いた値と文字列として等しくなる形にする。MySQL のかっこは #86 の Grill（Q7）で所有者が決めた。
- **Rejected**: バックスラッシュを二重にする案（`NO_BACKSLASH_ESCAPES` のサーバーでは二つのバックスラッシュが残る）。

## D1-76 方言の上限

- **Decision**: 型の長さ、精度、位取り、キーの大きさの方言の上限を、DDL を組み立てるときに検査し、`InvalidOperationException` にする。方言に依存しない下限（1 以上、位取りは 0 以上で精度以下）はジェネレーターの AURA0119 にする。MariaDB にも MySQL と同じキーの上限を当てる（§13.7）。
- **Reason**: SQL Server は上限を超えるキーを警告だけで通し、長い値の INSERT まで失敗しない。ほかの上限も、データベースに送る前に、どの列の誤りかを示せる。MariaDB は長すぎる一意制約を HASH の索引にするが、外部キーの参照先にならず、複数の列の索引と主キーはエラーにするので、一つの規則にそろえる。#86 の Grill（Q8）で所有者が決めた。
- **Rejected**: 上限を資料に書くだけの案（SQL Server ではデータを入れるまで誤りが分からない）。上限をジェネレーターで検査する案（ジェネレーターは方言を知らない）。

## D1-77 制約の名前と索引の名前

- **Decision**: 主キー、一意制約、外部キーには名前を付けない。同じスキーマのテーブルの間で、または一つのテーブルの中で、`[Index]` の名前が重複すれば AURA0118 にする（§13.4）。
- **Reason**: 制約の名前を付けないので、名前の衝突が起きない。索引の名前は PostgreSQL と SQLite ではスキーマの中で一意でなければならず、テーブルごとに一意でよい MySQL、MariaDB、SQL Server でだけ通る定義を作らないためである。#86 の Grill（Q9）で所有者が決めた。
- **Rejected**: 制約に `PK_表` などの名前を付ける案（名前を使う API がなく、名前の長さの上限と衝突の検査が要る）。

## D1-78 スキーマ

- **Decision**: テーブルの名前をスキーマで修飾し、スキーマは作らない。MySQL と MariaDB で、スキーマのあるテーブルからスキーマのないテーブルを参照する外部キーは、組み立て時に `InvalidOperationException` にする（§13.6）。
- **Reason**: 要件 R8 にスキーマの作成はない。MySQL と MariaDB は修飾のない参照先を作るテーブルのデータベースで探すが、AuraORM の修飾のない名前は接続の既定のデータベースを指し、組み立ての時にはその名前が分からない。スキーマは #86 の Grill（Q11）で所有者が決めた。
- **Rejected**: MySQL で修飾のない参照先をそのまま出す案（別のデータベースのテーブルを探してエラー 1824 になる）。

## D1-79 名前の長さの上限

- **Decision**: DDL の文に書く名前（スキーマ、テーブル、列、索引）の長さを、方言の上限（PostgreSQL 63 バイト、MySQL と MariaDB 64 文字、SQL Server 128 文字の UTF-16 の単位）で検査し、組み立て時に `InvalidOperationException` にする。MySQL と MariaDB では、U+0000 と基本多言語面の外の文字、データベースが付ける外部キーの名前（`テーブル名_ibfk_N`。MySQL 64 文字、MariaDB 63 文字）も検査する（§13.7）。
- **Reason**: PostgreSQL は長すぎる名前を黙って切り詰めるので、別の名前が同じ名前になり、AURA0118 も `IF EXISTS` もそれを見つけられない。ほかの方言はエラーにするが、どの名前の誤りかを送る前に示せる。上限は方言ごとなので、ほかの上限（D1-76）と同じく組み立て時に検査する。#87 のレビューで見つかった。
- **Rejected**: PostgreSQL の切り詰めに任せる案（黙った衝突になる）。ジェネレーターで最も短い上限（63 バイト）に限る案（SQL Server と MySQL で通る名前を拒む）。MySQL と MariaDB の ASCII でない名前のテーブルの、ファイルの名前の上限まで検査する案（上限がサーバーのファイルシステムで決まる。§14.2）。

## D1-80 行の読み取りの時間の目標

- **Decision**: 1 行あたり、手書きに対して 5% 以内のままにし、測り方を決める。100 行と 1 行の差から 1 行あたりの時間を出し、A、B、B、A の交互の順序で 3 回以上回した中央値で比べる。改善の PR で届かなければ、残りの差の内訳を測ってから、データで目標を直す（§16.1）。
- **Reason**: 試作で、行ごとと値ごとの間接的な呼び出しの費用は、手書きとのばらつきの中だった。届かないことを示すデータがないまま目標を下げると、手書きに近いという約束を、確かめずに手放すことになる。一回の実行の比は、手書きそのものが実行ごとに 25% 動くので、5% の差を示せない。#98 の Grill で所有者が決めた。
- **Rejected**: 手書きの 1.5 倍以内に下げる案（Grill で最初に推奨したが、届かない理由とした間接的な呼び出しの費用が、測るとばらつきの中だった）。Dapper.AOT より遅くない案（比べる相手の版が上がると、目標が動く）。

## D1-81 SQL の組み立ての目標と確保量

- **Decision**: 時間は、WHERE のある SELECT と、二つの結合と GROUP BY の SELECT で、linq2db と EF Core より速いこととする。確保量は記録だけにし、減らすのは組み立て済みの文の再利用（§18 の P2）で扱う（§16.1）。
- **Reason**: 時間は、比べる相手との比なら PC が変わっても意味を保つ。確保量の差は、同じ形の文でも列のオブジェクト、段階、文の木を毎回作り、SQL を毎回出力することによる。これを減らすのは、文を再利用する P2 の仕事である。#98 の Grill で所有者が決めた。
- **Rejected**: 絶対の時間（ns）を目標にする案（PC で変わる）。確保量を linq2db 以下にする案（P1 の範囲で減らす手がない）。

## D1-82 往復の目標と確かめ方

- **Decision**: メモリ上の SQLite での往復は、100 行で手書きの 1.1 倍以内、1 行で 1.35 倍以内とする。§16.1 の時間の目標は CI のゲートにせず、改善の PR と、経路を変える PR が、交互の順序で測って示す。確保量のテスト（N4）は CI のゲートである（§16.1）。
- **Reason**: 往復の目標は、今の水準を下回らないことである。共有のランナーの時間はばらつき、ゲートにすると、変更と関係なく落ちる。確保量は、ランナーによらず決まる。#98 の Grill で所有者が決めた。
- **Rejected**: 時間を CI のゲートにする案（ばらつきで落ちる）。

