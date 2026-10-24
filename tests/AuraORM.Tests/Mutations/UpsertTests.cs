using static AuraORM.Sql;

namespace AuraORM.Tests;

public sealed class UpsertTests
{
    private static readonly Database<SqliteDialect> Sqlite = new(new FakeConnection());
    private static readonly Database<PostgreSqlDialect> PostgreSql = new(new FakeConnection());
    private static readonly Database<MySqlDialect> MySql = new(new FakeConnection());
    private static readonly Database<MariaDbDialect> MariaDb = new(new FakeConnection());

    private static readonly Account.Insert Row = new()
    {
        Email = "a@example.com",
        Status = AccountStatus.Open,
        LoginCount = 3,
    };

    /// <summary>
    /// <c>ON CONFLICT</c> writes <c>DO NOTHING</c> with and without a target, and <c>DO UPDATE SET</c> with unqualified
    /// targets, the existing row's columns qualified, the proposed row's as <c>"excluded"."a"</c>, and <c>WHERE</c> (design
    /// §10.1).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - <see cref="Account"/>; a row that sets the email, the status, and the login count.
    ///
    /// Steps:
    /// - Render, on SQLite and PostgreSQL, <c>OnConflictDoNothing()</c>, <c>OnConflict(a =&gt; [a.Email, a.Id]).DoNothing()</c>,
    ///   and <c>OnConflict(a =&gt; [a.Email]).DoUpdate(…)</c> of the display name from excluded and the login count plus one,
    ///   with a <c>Where</c> that compares the login counts.
    ///
    /// Expected:
    /// - <c>… ON CONFLICT DO NOTHING</c>; <c>… ON CONFLICT ("email", "id") DO NOTHING</c>.
    /// - <c>… ON CONFLICT ("email") DO UPDATE SET "display_name" = "excluded"."display_name", "login_count" =
    ///   ("accounts"."login_count" + @p3) WHERE "accounts"."login_count" &lt; "excluded"."login_count"</c>.
    /// </remarks>
    [Fact]
    public void OnConflict_WritesTheClause()
    {
        const string insert = "INSERT INTO \"accounts\" (\"email\", \"status\", \"login_count\") VALUES ";
        SqlAssert.Compare(
            "SQLite",
            insert + "(@p0, @p1, @p2) ON CONFLICT DO NOTHING",
            Sqlite.Insert(Account.Table).Values(Row).OnConflictDoNothing().ToSql(),
            "a@example.com",
            (short)1,
            3
        );
        SqlAssert.Compare(
            "PostgreSQL",
            insert + "($1, $2, $3) ON CONFLICT (\"email\", \"id\") DO NOTHING",
            PostgreSql.Insert(Account.Table).Values(Row).OnConflict(a => [a.Email, a.Id]).DoNothing().ToSql(),
            "a@example.com",
            (short)1,
            3
        );
        const string update =
            "ON CONFLICT (\"email\") DO UPDATE SET \"display_name\" = \"excluded\".\"display_name\", \"login_count\" = (\"accounts\".\"login_count\" + ";
        const string where = ") WHERE \"accounts\".\"login_count\" < \"excluded\".\"login_count\"";
        SqlAssert.Compare("SQLite", insert + "(@p0, @p1, @p2) " + update + "@p3" + where, Update(Sqlite).ToSql(), "a@example.com", (short)1, 3, 1);
        SqlAssert.Compare(
            "PostgreSQL",
            insert + "($1, $2, $3) " + update + "$4" + where,
            Update(PostgreSql).ToSql(),
            "a@example.com",
            (short)1,
            3,
            1
        );
    }

    /// <summary>
    /// <c>ON DUPLICATE KEY UPDATE</c> reads the proposed row through MySQL's row alias <c>AS `excluded`</c> and through
    /// <c>VALUES(`a`)</c> on MariaDB (design §10.2).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - <see cref="Account"/>; a row that sets the login count.
    ///
    /// Steps:
    /// - Render, on MySQL and MariaDB, <c>OnDuplicateKeyUpdate</c> of the login count to itself plus the proposed one.
    ///
    /// Expected:
    /// - MySQL: <c>… VALUES (@p0, @p1, @p2) AS `excluded` ON DUPLICATE KEY UPDATE `login_count` = (`accounts`.`login_count` +
    ///   `excluded`.`login_count`)</c>.
    /// - MariaDB: <c>… VALUES (@p0, @p1, @p2) ON DUPLICATE KEY UPDATE `login_count` = (`accounts`.`login_count` +
    ///   VALUES(`login_count`))</c>.
    /// </remarks>
    [Fact]
    public void OnDuplicateKeyUpdate_WritesTheProposedRowOfTheDialect()
    {
        SqlAssert.Compare(
            "MySQL",
            "INSERT INTO `accounts` (`email`, `status`, `login_count`) VALUES (@p0, @p1, @p2) AS `excluded` ON DUPLICATE KEY UPDATE `login_count` = (`accounts`.`login_count` + `excluded`.`login_count`)",
            AddLogins(MySql).ToSql(),
            "a@example.com",
            (short)1,
            3
        );
        SqlAssert.Compare(
            "MariaDB",
            "INSERT INTO `accounts` (`email`, `status`, `login_count`) VALUES (@p0, @p1, @p2) ON DUPLICATE KEY UPDATE `login_count` = (`accounts`.`login_count` + VALUES(`login_count`))",
            AddLogins(MariaDb).ToSql(),
            "a@example.com",
            (short)1,
            3
        );
    }

    /// <summary>
    /// A row that sets no column takes the upsert after <c>DEFAULT VALUES</c> on PostgreSQL and after <c>() VALUES ()</c> on
    /// MySQL and MariaDB; SQLite rejects it (design §10.1, §10.2, §14.3).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - <see cref="Note"/>, whose columns all may be left out.
    ///
    /// Steps:
    /// - Render an empty note with <c>OnConflictDoNothing()</c> on PostgreSQL, and with <c>OnDuplicateKeyUpdate</c> of the
    ///   priority plus one on MySQL and MariaDB; build <c>OnConflictDoNothing()</c> and <c>OnConflict(…)</c> on SQLite.
    ///
    /// Expected:
    /// - <c>INSERT INTO "notes" DEFAULT VALUES ON CONFLICT DO NOTHING</c>;
    ///   <c>INSERT INTO `notes` () VALUES () AS `excluded` ON DUPLICATE KEY UPDATE `Priority` = (`notes`.`Priority` + @p0)</c>;
    ///   MariaDB without <c>AS `excluded`</c>.
    /// - SQLite: <see cref="ArgumentException"/> naming <c>DEFAULT VALUES</c> for both.
    /// </remarks>
    [Fact]
    public void Upsert_RowsThatSetNoColumn_FollowTheDialect()
    {
        Assert.Equal(
            "INSERT INTO \"notes\" DEFAULT VALUES ON CONFLICT DO NOTHING",
            PostgreSql.Insert(Note.Table).Values(new Note.Insert()).OnConflictDoNothing().ToSql().Text
        );
        Assert.Equal(
            "INSERT INTO `notes` () VALUES () AS `excluded` ON DUPLICATE KEY UPDATE `Priority` = (`notes`.`Priority` + @p0)",
            MySql.Insert(Note.Table).Values(new Note.Insert()).OnDuplicateKeyUpdate((n, x) => [Set(n.Priority, Add(n.Priority, 1))]).ToSql().Text
        );
        Assert.Equal(
            "INSERT INTO `notes` () VALUES () ON DUPLICATE KEY UPDATE `Priority` = (`notes`.`Priority` + @p0)",
            MariaDb.Insert(Note.Table).Values(new Note.Insert()).OnDuplicateKeyUpdate((n, x) => [Set(n.Priority, Add(n.Priority, 1))]).ToSql().Text
        );
        var empty = Sqlite.Insert(Note.Table).Values(new Note.Insert());
        Assert.Contains("DEFAULT VALUES", Assert.Throws<ArgumentException>(() => empty.OnConflictDoNothing()).Message, StringComparison.Ordinal);
        Assert.Throws<ArgumentException>(() => empty.OnConflict(n => [n.Id]));
    }

    /// <summary>
    /// <c>Returning</c> follows the upsert clause (design §10.3).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - <see cref="Account"/>.
    ///
    /// Steps:
    /// - Render a <c>DoUpdate</c> returning the id on PostgreSQL, and an <c>OnDuplicateKeyUpdate</c> returning the id on
    ///   MariaDB.
    ///
    /// Expected:
    /// - <c>… DO UPDATE SET … WHERE … RETURNING "id"</c>; <c>… ON DUPLICATE KEY UPDATE … RETURNING `id`</c>.
    /// </remarks>
    [Fact]
    public void Upsert_Returning_FollowsTheClause()
    {
        Assert.EndsWith(
            "WHERE \"accounts\".\"login_count\" < \"excluded\".\"login_count\" RETURNING \"id\"",
            Update(PostgreSql).Returning(a => a.Id).ToSql().Text,
            StringComparison.Ordinal
        );
        Assert.EndsWith("VALUES(`login_count`)) RETURNING `id`", AddLogins(MariaDb).Returning(a => a.Id).ToSql().Text, StringComparison.Ordinal);
    }

    /// <summary>
    /// The target and the assignments of an upsert are checked when they are given: a target has at least one column, each
    /// once, each of the table; the assignments have at least one, each column once, each of the existing row and not of
    /// excluded (design §10.1, §10.2, §14.3).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - <see cref="Account"/>.
    ///
    /// Steps:
    /// - Give an empty target, a target with the email twice, and a target with a column of another statement; give an empty
    ///   list of <c>DoUpdate</c>, one that assigns the login count twice, and one that assigns <c>excluded.DisplayName</c>;
    ///   give <c>OnDuplicateKeyUpdate</c> an assignment to <c>excluded.DisplayName</c>.
    ///
    /// Expected:
    /// - <see cref="ArgumentException"/> for the empty and repeated targets and lists; <see cref="InvalidOperationException"/>
    ///   for the column of another statement and for each assignment to excluded.
    /// </remarks>
    [Fact]
    public void Upsert_InvalidTargetOrAssignments_Throw()
    {
        var insert = Sqlite.Insert(Account.Table).Values(Row);
        Assert.Throws<ArgumentException>(() => insert.OnConflict(a => []));
        Assert.Throws<ArgumentException>(() => insert.OnConflict(a => [a.Email, a.Email]));
        ITableColumn? other = null;
        _ = Sqlite
            .From(Account.Table)
            .Select(a =>
            {
                other = a.Email;
                return a.Id;
            });
        Assert.Throws<InvalidOperationException>(() => insert.OnConflict(a => [other!]));

        var conflict = insert.OnConflict(a => [a.Email]);
        Assert.Throws<ArgumentException>(() => conflict.DoUpdate((a, x) => []));
        Assert.Throws<ArgumentException>(() => conflict.DoUpdate((a, x) => [Set(a.LoginCount, 1), Set(a.LoginCount, 2)]));
        Assert.Contains(
            "excluded.display_name",
            Assert.Throws<InvalidOperationException>(() => conflict.DoUpdate((a, x) => [Set(x.DisplayName, "b")])).Message,
            StringComparison.Ordinal
        );
        Assert.Throws<InvalidOperationException>(() =>
            MySql.Insert(Account.Table).Values(Row).OnDuplicateKeyUpdate((a, x) => [Set(x.DisplayName, "b")])
        );
    }

    /// <summary>
    /// The checks of the dialects apply when the statement is rendered: a table named like the proposed row on SQLite,
    /// PostgreSQL, and MySQL; a proposed column the rows do not set on MySQL; a value that reads a column assigned before it on
    /// MySQL and MariaDB; a subquery on the target on MySQL (design §10.1, §10.2, §14.3).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - <see cref="ExcludedRow"/> (the table <c>Excluded</c>) and <see cref="Account"/>.
    ///
    /// Steps:
    /// - Render an upsert of <c>Excluded</c> on each dialect; an <c>OnDuplicateKeyUpdate</c> that reads
    ///   <c>excluded.DisplayName</c>, which the row does not set; one whose second value reads the email that the first
    ///   assigns; one whose value is a scalar subquery on the accounts; and the same as <c>DoUpdate</c> on SQLite and
    ///   PostgreSQL.
    ///
    /// Expected:
    /// - <c>Excluded</c>: <see cref="InvalidOperationException"/> on SQLite, PostgreSQL, and MySQL; MariaDB renders
    ///   <c>VALUES(`N`)</c>.
    /// - The proposed column not set: <see cref="InvalidOperationException"/> naming error 1054 on MySQL; MariaDB renders it.
    /// - The email read after it is assigned: <see cref="InvalidOperationException"/> on MySQL and MariaDB; SQLite and
    ///   PostgreSQL render it.
    /// - The subquery: <see cref="InvalidOperationException"/> naming error 1093 on MySQL; the other dialects render it.
    /// </remarks>
    [Fact]
    public void Upsert_DialectChecks_ThrowWhenRendered()
    {
        var row = new ExcludedRow.Insert { Id = 1, N = 2 };
        Assert.Throws<InvalidOperationException>(() => Sqlite.Insert(ExcludedRow.Table).Values(row).OnConflictDoNothing().ToSql());
        Assert.Throws<InvalidOperationException>(() => PostgreSql.Insert(ExcludedRow.Table).Values(row).OnConflict(e => [e.Id]).DoNothing().ToSql());
        Assert.Throws<InvalidOperationException>(() =>
            MySql.Insert(ExcludedRow.Table).Values(row).OnDuplicateKeyUpdate((e, x) => [Set(e.N, x.N)]).ToSql()
        );
        Assert.EndsWith(
            "ON DUPLICATE KEY UPDATE `N` = VALUES(`N`)",
            MariaDb.Insert(ExcludedRow.Table).Values(row).OnDuplicateKeyUpdate((e, x) => [Set(e.N, x.N)]).ToSql().Text,
            StringComparison.Ordinal
        );

        var unset = MySql.Insert(Account.Table).Values(Row).OnDuplicateKeyUpdate((a, x) => [Set(a.DisplayName, x.DisplayName)]);
        Assert.Contains("1054", Assert.Throws<InvalidOperationException>(() => unset.ToSql()).Message, StringComparison.Ordinal);
        _ = MariaDb.Insert(Account.Table).Values(Row).OnDuplicateKeyUpdate((a, x) => [Set(a.DisplayName, x.DisplayName)]).ToSql();

        Assert.Throws<InvalidOperationException>(() =>
            MySql.Insert(Account.Table).Values(Row).OnDuplicateKeyUpdate((a, x) => [Set(a.Email, "b"), Set(a.DisplayName, a.Email)]).ToSql()
        );
        Assert.Throws<InvalidOperationException>(() =>
            MariaDb.Insert(Account.Table).Values(Row).OnDuplicateKeyUpdate((a, x) => [Set(a.Email, "b"), Set(a.DisplayName, a.Email)]).ToSql()
        );
        _ = Sqlite
            .Insert(Account.Table)
            .Values(Row)
            .OnConflict(a => [a.Id])
            .DoUpdate((a, x) => [Set(a.Email, "b"), Set(a.DisplayName, a.Email)])
            .ToSql();
        _ = PostgreSql
            .Insert(Account.Table)
            .Values(Row)
            .OnConflict(a => [a.Id])
            .DoUpdate((a, x) => [Set(a.Email, "b"), Set(a.DisplayName, a.Email)])
            .ToSql();

        Assert.Contains("1093", Assert.Throws<InvalidOperationException>(() => MaxLogins(MySql).ToSql()).Message, StringComparison.Ordinal);
        _ = MaxLogins(MariaDb).ToSql();
        _ = Sqlite.Insert(Account.Table).Values(Row).OnConflict(a => [a.Id]).DoUpdate((a, x) => [Set(a.LoginCount, MaxOf(Sqlite))]).ToSql();
        _ = PostgreSql.Insert(Account.Table).Values(Row).OnConflict(a => [a.Id]).DoUpdate((a, x) => [Set(a.LoginCount, MaxOf(PostgreSql))]).ToSql();
    }

    private static UpsertStatement<TDialect, Executable, Account.Columns> Update<TDialect>(Database<TDialect> db)
        where TDialect : IOnConflictDialect =>
        db.Insert(Account.Table)
            .Values(Row)
            .OnConflict(a => [a.Email])
            .DoUpdate((a, x) => [Set(a.DisplayName, x.DisplayName), Set(a.LoginCount, Add(a.LoginCount, 1))])
            .Where((a, x) => Lt(a.LoginCount, x.LoginCount));

    private static UpsertStatement<TDialect, Executable, Account.Columns> AddLogins<TDialect>(Database<TDialect> db)
        where TDialect : IOnDuplicateKeyDialect =>
        db.Insert(Account.Table).Values(Row).OnDuplicateKeyUpdate((a, x) => [Set(a.LoginCount, Add(a.LoginCount, x.LoginCount))]);

    private static UpsertStatement<TDialect, Executable, Account.Columns> MaxLogins<TDialect>(Database<TDialect> db)
        where TDialect : IOnDuplicateKeyDialect =>
        db.Insert(Account.Table).Values(Row).OnDuplicateKeyUpdate((a, x) => [Set(a.LoginCount, MaxOf(db))]);

    private static IExpr<int, PerRow> MaxOf<TDialect>(Database<TDialect> db)
        where TDialect : IDialect => Coalesce(Scalar(db.From(Account.Table).Select(x => Max(x.LoginCount))), 0);
}
