using static AuraORM.Sql;

namespace AuraORM.Tests;

public sealed class SelectGroupTests
{
    /// <summary>
    /// A single key is written after <c>WHERE</c> as <c>GROUP BY</c>, and the select list gets the key itself (design §5.5).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - <see cref="Account"/>.
    ///
    /// Steps:
    /// - Render <c>GroupBy(a =&gt; a.Status).Select((k, a) =&gt; (k, Count()))</c> on each dialect.
    ///
    /// Expected:
    /// - <c>SELECT "accounts"."status", COUNT(*) FROM "accounts" GROUP BY "accounts"."status"</c>, with <c>COUNT_BIG</c> on
    ///   SQL Server.
    /// </remarks>
    [Fact]
    public void GroupBy_OneKey_WritesGroupBy()
    {
        SqlAssert.Statement(
            new OneKey(),
            new(
                "SELECT \"accounts\".\"status\", COUNT(*) FROM \"accounts\" GROUP BY \"accounts\".\"status\"",
                "SELECT \"accounts\".\"status\", COUNT(*) FROM \"accounts\" GROUP BY \"accounts\".\"status\"",
                "SELECT `accounts`.`status`, COUNT(*) FROM `accounts` GROUP BY `accounts`.`status`",
                "SELECT [accounts].[status], COUNT_BIG(*) FROM [accounts] GROUP BY [accounts].[status]"
            )
        );
    }

    /// <summary>
    /// A tuple of keys, <c>WHERE</c>, <c>HAVING</c>, <c>ORDER BY</c>, and <c>LIMIT</c> are written in the standard order,
    /// with the parameters in their order of appearance (design §5.1, §5.5, §5.8).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - <see cref="Account"/>.
    ///
    /// Steps:
    /// - Render a statement with a <c>WHERE</c> value, two keys, a <c>HAVING</c> on <c>COUNT(*)</c>, a projection of both keys
    ///   and a sum, an <c>ORDER BY</c> on an aggregate and a key, and a limit, on each dialect.
    ///
    /// Expected:
    /// - <c>… WHERE … GROUP BY "accounts"."status", "accounts"."display_name" HAVING COUNT(*) &gt; @p1 ORDER BY COUNT(*) DESC,
    ///   "accounts"."status" ASC LIMIT @p2</c>, with the sum of each dialect.
    /// - The parameters: 0, 2, 10.
    /// </remarks>
    [Fact]
    public void GroupBy_TupleKeysWithHavingAndOrderBy_WritesClausesInOrder()
    {
        SqlAssert.Statement(
            new TupleKeys(),
            new(
                "SELECT \"accounts\".\"status\", \"accounts\".\"display_name\", SUM(\"accounts\".\"login_count\") FROM \"accounts\" WHERE \"accounts\".\"login_count\" > @p0 GROUP BY \"accounts\".\"status\", \"accounts\".\"display_name\" HAVING COUNT(*) > @p1 ORDER BY COUNT(*) DESC, \"accounts\".\"status\" ASC LIMIT @p2",
                "SELECT \"accounts\".\"status\", \"accounts\".\"display_name\", CAST(SUM(\"accounts\".\"login_count\") AS bigint) FROM \"accounts\" WHERE \"accounts\".\"login_count\" > $1 GROUP BY \"accounts\".\"status\", \"accounts\".\"display_name\" HAVING COUNT(*) > $2 ORDER BY COUNT(*) DESC, \"accounts\".\"status\" ASC LIMIT $3",
                "SELECT `accounts`.`status`, `accounts`.`display_name`, CAST(SUM(`accounts`.`login_count`) AS SIGNED) FROM `accounts` WHERE `accounts`.`login_count` > @p0 GROUP BY `accounts`.`status`, `accounts`.`display_name` HAVING COUNT(*) > @p1 ORDER BY COUNT(*) DESC, `accounts`.`status` ASC LIMIT @p2",
                "SELECT [accounts].[status], [accounts].[display_name], SUM(CAST([accounts].[login_count] AS bigint)) FROM [accounts] WHERE [accounts].[login_count] > @p0 GROUP BY [accounts].[status], [accounts].[display_name] HAVING COUNT_BIG(*) > @p1 ORDER BY COUNT_BIG(*) DESC, [accounts].[status] ASC OFFSET 0 ROWS FETCH NEXT @p2 ROWS ONLY"
            ),
            0,
            2L,
            10
        );
    }

    /// <summary>
    /// A key with a value is one parameter wherever the key is written: in the select list, in <c>GROUP BY</c>, in
    /// <c>HAVING</c>, and in <c>ORDER BY</c> (design §5.5, §5.12).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - <see cref="Account"/>; the key <c>Add(a.LoginCount, 1)</c>.
    ///
    /// Steps:
    /// - Render a statement that selects the key and a count, has <c>HAVING</c> on the key, and orders by the key, on each
    ///   dialect.
    ///
    /// Expected:
    /// - <c>("accounts"."login_count" + @p0)</c> at all four places; the <c>HAVING</c> value is <c>@p1</c>.
    /// - On MySQL and MariaDB, the key inside <c>HAVING</c> is <c>MIN(…)</c>, because they do not match an expression there
    ///   with the <c>GROUP BY</c> key (design §5.5); the other places are the same.
    /// - The parameters: 1, 3.
    /// </remarks>
    [Fact]
    public void GroupBy_KeyWithValue_UsesOneParameter()
    {
        SqlAssert.Statement(
            new KeyWithValue(),
            new(
                "SELECT (\"accounts\".\"login_count\" + @p0), COUNT(*) FROM \"accounts\" GROUP BY (\"accounts\".\"login_count\" + @p0) HAVING (\"accounts\".\"login_count\" + @p0) > @p1 ORDER BY (\"accounts\".\"login_count\" + @p0) ASC",
                "SELECT (\"accounts\".\"login_count\" + $1), COUNT(*) FROM \"accounts\" GROUP BY (\"accounts\".\"login_count\" + $1) HAVING (\"accounts\".\"login_count\" + $1) > $2 ORDER BY (\"accounts\".\"login_count\" + $1) ASC",
                "SELECT (`accounts`.`login_count` + @p0), COUNT(*) FROM `accounts` GROUP BY (`accounts`.`login_count` + @p0) HAVING MIN((`accounts`.`login_count` + @p0)) > @p1 ORDER BY (`accounts`.`login_count` + @p0) ASC",
                "SELECT ([accounts].[login_count] + @p0), COUNT_BIG(*) FROM [accounts] GROUP BY ([accounts].[login_count] + @p0) HAVING ([accounts].[login_count] + @p0) > @p1 ORDER BY ([accounts].[login_count] + @p0) ASC"
            ),
            1,
            3
        );
    }

    /// <summary>
    /// A column key inside <c>HAVING</c> is the column on every dialect, MySQL and MariaDB included; only a key that is not a
    /// column gets <c>MIN</c> there (design §5.5).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - <see cref="Account"/>, grouped by <c>a.LoginCount</c>.
    ///
    /// Steps:
    /// - Render <c>Having((k, a) =&gt; Gt(Add(k, 1), 5))</c>, which has the key inside an expression, and select the key.
    ///
    /// Expected:
    /// - <c>HAVING ("accounts"."login_count" + @p0) &gt; @p1</c> on every dialect, with no <c>MIN</c>.
    /// </remarks>
    [Fact]
    public void Having_ColumnKey_IsWrittenAsTheColumn()
    {
        SqlAssert.Statement(
            new ColumnKeyHaving(),
            new(
                "SELECT \"accounts\".\"login_count\" FROM \"accounts\" GROUP BY \"accounts\".\"login_count\" HAVING (\"accounts\".\"login_count\" + @p0) > @p1",
                "SELECT \"accounts\".\"login_count\" FROM \"accounts\" GROUP BY \"accounts\".\"login_count\" HAVING (\"accounts\".\"login_count\" + $1) > $2",
                "SELECT `accounts`.`login_count` FROM `accounts` GROUP BY `accounts`.`login_count` HAVING (`accounts`.`login_count` + @p0) > @p1",
                "SELECT [accounts].[login_count] FROM [accounts] GROUP BY [accounts].[login_count] HAVING ([accounts].[login_count] + @p0) > @p1"
            ),
            1,
            5
        );
    }

    /// <summary>
    /// <c>Having</c> with the list form of <c>And</c> writes no <c>HAVING</c> when the list has no condition, and the
    /// condition when it has one (design §4.2).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - <see cref="Account"/>; a list of nullable <see cref="PerGroup"/> conditions.
    ///
    /// Steps:
    /// - Render the statement with a list of nulls, and with a list holding <c>Gt(Count(), 1L)</c>, on each dialect.
    ///
    /// Expected:
    /// - No <c>HAVING</c> and no parameter for the empty list; <c>HAVING COUNT(*) &gt; @p0</c> for the other.
    /// </remarks>
    [Fact]
    public void Having_OptionalCondition_WritesHavingOnlyWhenThereIsACondition()
    {
        SqlAssert.Statement(
            new OptionalHaving(false),
            new(
                "SELECT \"accounts\".\"status\" FROM \"accounts\" GROUP BY \"accounts\".\"status\"",
                "SELECT \"accounts\".\"status\" FROM \"accounts\" GROUP BY \"accounts\".\"status\"",
                "SELECT `accounts`.`status` FROM `accounts` GROUP BY `accounts`.`status`",
                "SELECT [accounts].[status] FROM [accounts] GROUP BY [accounts].[status]"
            )
        );
        SqlAssert.Statement(
            new OptionalHaving(true),
            new(
                "SELECT \"accounts\".\"status\" FROM \"accounts\" GROUP BY \"accounts\".\"status\" HAVING COUNT(*) > @p0",
                "SELECT \"accounts\".\"status\" FROM \"accounts\" GROUP BY \"accounts\".\"status\" HAVING COUNT(*) > $1",
                "SELECT `accounts`.`status` FROM `accounts` GROUP BY `accounts`.`status` HAVING COUNT(*) > @p0",
                "SELECT [accounts].[status] FROM [accounts] GROUP BY [accounts].[status] HAVING COUNT_BIG(*) > @p0"
            ),
            1L
        );
    }

    /// <summary>
    /// A grouped statement over a join gets the key and the column set of each table; <c>SELECT DISTINCT</c> after
    /// <c>GROUP BY</c> orders by the position of its item (design §5.3, §5.5, §5.7).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - <see cref="Account"/> left-joined with <see cref="Membership"/>.
    ///
    /// Steps:
    /// - Render a statement grouped by the role of the joined table that counts distinct accounts, and a
    ///   <c>SelectDistinct</c> of a count ordered by it, on each dialect.
    ///
    /// Expected:
    /// - <c>… LEFT JOIN … GROUP BY "memberships"."Role"</c>, and <c>SELECT DISTINCT COUNT(*) … ORDER BY 1 DESC</c>.
    /// </remarks>
    [Fact]
    public void GroupBy_JoinAndDistinct_WritesTheirClauses()
    {
        SqlAssert.Statement(
            new JoinedKey(),
            new(
                "SELECT \"memberships\".\"Role\", COUNT(DISTINCT \"accounts\".\"id\") FROM \"accounts\" LEFT JOIN \"app\".\"memberships\" ON \"memberships\".\"AccountId\" = \"accounts\".\"id\" GROUP BY \"memberships\".\"Role\"",
                "SELECT \"memberships\".\"Role\", COUNT(DISTINCT \"accounts\".\"id\") FROM \"accounts\" LEFT JOIN \"app\".\"memberships\" ON \"memberships\".\"AccountId\" = \"accounts\".\"id\" GROUP BY \"memberships\".\"Role\"",
                "SELECT `memberships`.`Role`, COUNT(DISTINCT `accounts`.`id`) FROM `accounts` LEFT JOIN `app`.`memberships` ON `memberships`.`AccountId` = `accounts`.`id` GROUP BY `memberships`.`Role`",
                "SELECT [memberships].[Role], COUNT_BIG(DISTINCT [accounts].[id]) FROM [accounts] LEFT JOIN [app].[memberships] ON [memberships].[AccountId] = [accounts].[id] GROUP BY [memberships].[Role]"
            )
        );
        SqlAssert.Statement(
            new DistinctCount(),
            new(
                "SELECT DISTINCT COUNT(*) FROM \"accounts\" GROUP BY \"accounts\".\"status\" ORDER BY 1 DESC",
                "SELECT DISTINCT COUNT(*) FROM \"accounts\" GROUP BY \"accounts\".\"status\" ORDER BY 1 DESC",
                "SELECT DISTINCT COUNT(*) FROM `accounts` GROUP BY `accounts`.`status` ORDER BY 1 DESC",
                "SELECT DISTINCT COUNT_BIG(*) FROM [accounts] GROUP BY [accounts].[status] ORDER BY 1 DESC"
            )
        );
    }

    /// <summary>
    /// A <c>GROUP BY</c> key without a column throws when the statement is built, on every dialect; a key with a column or a
    /// raw SQL fragment does not (design §5.5, §14.3).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - <see cref="Account"/>.
    ///
    /// Steps:
    /// - Build a statement grouped by <c>Value(2)</c>, by <c>Add(Value(1), Value(2))</c>, by <c>CurrentTimestamp()</c>, and by
    ///   the tuple <c>(a.Status, Value(2))</c>, on each dialect; then by <c>Raw&lt;int&gt;($"1")</c> and by
    ///   <c>Add(a.LoginCount, 1)</c>.
    ///
    /// Expected:
    /// - The first four throw <see cref="InvalidOperationException"/> on every dialect; the last two render.
    /// </remarks>
    /// <param name="form">The key.</param>
    [Theory]
    [InlineData(KeyForm.Value)]
    [InlineData(KeyForm.ValueExpression)]
    [InlineData(KeyForm.CurrentTimestamp)]
    [InlineData(KeyForm.ColumnAndValue)]
    public void GroupBy_KeyWithoutColumn_Throws(KeyForm form)
    {
        var query = new KeyOnlyValues(form);
        Assert.Throws<InvalidOperationException>(() => query.Build(new Database<SqliteDialect>(new FakeConnection())));
        Assert.Throws<InvalidOperationException>(() => query.Build(new Database<PostgreSqlDialect>(new FakeConnection())));
        Assert.Throws<InvalidOperationException>(() => query.Build(new Database<MySqlDialect>(new FakeConnection())));
        Assert.Throws<InvalidOperationException>(() => query.Build(new Database<MariaDbDialect>(new FakeConnection())));
        Assert.Throws<InvalidOperationException>(() => query.Build(new Database<SqlServerDialect>(new FakeConnection())));
    }

    /// <inheritdoc cref="GroupBy_KeyWithoutColumn_Throws"/>
    [Fact]
    public void GroupBy_KeyWithColumnOrRawSql_Renders()
    {
        SqlAssert.Statement(
            new KeyOnlyValues(KeyForm.Raw),
            new(
                "SELECT COUNT(*) FROM \"accounts\" GROUP BY 1",
                "SELECT COUNT(*) FROM \"accounts\" GROUP BY 1",
                "SELECT COUNT(*) FROM `accounts` GROUP BY 1",
                "SELECT COUNT_BIG(*) FROM [accounts] GROUP BY 1"
            )
        );
        SqlAssert.Statement(
            new KeyOnlyValues(KeyForm.ColumnExpression),
            new(
                "SELECT COUNT(*) FROM \"accounts\" GROUP BY (\"accounts\".\"login_count\" + @p0)",
                "SELECT COUNT(*) FROM \"accounts\" GROUP BY (\"accounts\".\"login_count\" + $1)",
                "SELECT COUNT(*) FROM `accounts` GROUP BY (`accounts`.`login_count` + @p0)",
                "SELECT COUNT_BIG(*) FROM [accounts] GROUP BY ([accounts].[login_count] + @p0)"
            ),
            1
        );
    }

    /// <summary>
    /// An <c>ORDER BY</c> key that is only a value throws when the statement is built, on every dialect and for a decimal
    /// value too; an expression of values, a function, and a raw SQL fragment do not (design §5.8, §14.3). After
    /// <c>GROUP BY</c> a value cannot be a key at all, because <c>Value</c> is <see cref="PerRow"/>.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - <see cref="Account"/>.
    ///
    /// Steps:
    /// - Build a statement ordered by <c>Desc(Value(1))</c> on each dialect, and one ordered by <c>Asc(Value(1.5m))</c> on
    ///   PostgreSQL, where a decimal key is allowed.
    /// - Render statements ordered by <c>Add(Value(1), Value(0))</c>, by <c>CurrentTimestamp()</c>, and by
    ///   <c>Raw&lt;int&gt;($"{1}")</c>.
    ///
    /// Expected:
    /// - The first two throw <see cref="InvalidOperationException"/>; the others render.
    /// </remarks>
    [Fact]
    public void OrderBy_KeyThatIsOnlyAValue_Throws()
    {
        var query = new OrderByValue();
        Assert.Throws<InvalidOperationException>(() => query.Build(new Database<SqliteDialect>(new FakeConnection())));
        Assert.Throws<InvalidOperationException>(() => query.Build(new Database<PostgreSqlDialect>(new FakeConnection())));
        Assert.Throws<InvalidOperationException>(() => query.Build(new Database<MySqlDialect>(new FakeConnection())));
        Assert.Throws<InvalidOperationException>(() => query.Build(new Database<MariaDbDialect>(new FakeConnection())));
        Assert.Throws<InvalidOperationException>(() => query.Build(new Database<SqlServerDialect>(new FakeConnection())));

        var pg = new Database<PostgreSqlDialect>(new FakeConnection());
        Assert.Throws<InvalidOperationException>(() => pg.From(Account.Table).Select(a => a.Id).OrderBy(a => [Asc(Value(1.5m))]).ToSql());
        Assert.Equal(
            "SELECT \"accounts\".\"id\" FROM \"accounts\" ORDER BY ($1 + $2) DESC",
            pg.From(Account.Table).Select(a => a.Id).OrderBy(a => [Desc(Add(Value(1), Value(0)))]).ToSql().Text
        );
        Assert.Equal(
            "SELECT \"accounts\".\"id\" FROM \"accounts\" ORDER BY CURRENT_TIMESTAMP DESC",
            pg.From(Account.Table).Select(a => a.Id).OrderBy(a => [Desc(CurrentTimestamp())]).ToSql().Text
        );
        Assert.Equal(
            "SELECT \"accounts\".\"id\" FROM \"accounts\" ORDER BY $1 ASC",
            pg.From(Account.Table).Select(a => a.Id).OrderBy(a => [Asc(Raw<int>($"{1}"))]).ToSql().Text
        );
    }

    public enum KeyForm
    {
        Value,
        ValueExpression,
        CurrentTimestamp,
        ColumnAndValue,
        Raw,
        ColumnExpression,
    }

    private sealed class KeyOnlyValues(KeyForm form) : IQueryCase
    {
        public SqlStatement Build<TDialect>(Database<TDialect> db)
            where TDialect : IDialect =>
            form switch
            {
                KeyForm.Value => db.From(Account.Table).GroupBy(a => Value(2)).Select((k, a) => (k, Count())).ToSql(),
                KeyForm.ValueExpression => db.From(Account.Table).GroupBy(a => Add(Value(1), Value(2))).Select((k, a) => Count()).ToSql(),
                KeyForm.CurrentTimestamp => db.From(Account.Table).GroupBy(a => CurrentTimestamp()).Select((k, a) => Count()).ToSql(),
                KeyForm.ColumnAndValue => db.From(Account.Table).GroupBy(a => (a.Status, Value(2))).Select((k, a) => (k.Item1, Count())).ToSql(),
                KeyForm.Raw => db.From(Account.Table).GroupBy(a => Raw<int>($"1")).Select((k, a) => Count()).ToSql(),
                _ => db.From(Account.Table).GroupBy(a => Add(a.LoginCount, 1)).Select((k, a) => Count()).ToSql(),
            };
    }

    private sealed class OrderByValue : IQueryCase
    {
        public SqlStatement Build<TDialect>(Database<TDialect> db)
            where TDialect : IDialect => db.From(Account.Table).Select(a => a.Id).OrderBy(a => [Desc(Value(1))]).ToSql();
    }

    private sealed class OneKey : IQueryCase
    {
        public SqlStatement Build<TDialect>(Database<TDialect> db)
            where TDialect : IDialect => db.From(Account.Table).GroupBy(a => a.Status).Select((k, a) => (k, Count())).ToSql();
    }

    private sealed class TupleKeys : IQueryCase
    {
        public SqlStatement Build<TDialect>(Database<TDialect> db)
            where TDialect : IDialect =>
            db.From(Account.Table)
                .Where(a => Gt(a.LoginCount, 0))
                .GroupBy(a => (a.Status, a.DisplayName))
                .Having((k, a) => Gt(Count(), 2L))
                .Select((k, a) => (k.Item1, k.Item2, Sum(a.LoginCount)))
                .OrderBy((k, a) => [Desc(Count()), Asc(k.Item1)])
                .Limit(10)
                .ToSql();
    }

    private sealed class KeyWithValue : IQueryCase
    {
        public SqlStatement Build<TDialect>(Database<TDialect> db)
            where TDialect : IDialect =>
            db.From(Account.Table)
                .GroupBy(a => Add(a.LoginCount, 1))
                .Having((k, a) => Gt(k, 3))
                .Select((k, a) => (k, Count()))
                .OrderBy((k, a) => [Asc(k)])
                .ToSql();
    }

    private sealed class ColumnKeyHaving : IQueryCase
    {
        public SqlStatement Build<TDialect>(Database<TDialect> db)
            where TDialect : IDialect =>
            db.From(Account.Table).GroupBy(a => a.LoginCount).Having((k, a) => Gt(Add(k, 1), 5)).Select((k, a) => k).ToSql();
    }

    private sealed class OptionalHaving(bool withCondition) : IQueryCase
    {
        public SqlStatement Build<TDialect>(Database<TDialect> db)
            where TDialect : IDialect =>
            db.From(Account.Table)
                .GroupBy(a => a.Status)
                .Having((k, a) => And(new List<Condition<PerGroup>?> { null, withCondition ? Gt(Count(), 1L) : null }))
                .Select((k, a) => k)
                .ToSql();
    }

    private sealed class JoinedKey : IQueryCase
    {
        public SqlStatement Build<TDialect>(Database<TDialect> db)
            where TDialect : IDialect =>
            db.From(Account.Table)
                .LeftJoin(Membership.Table, (a, m) => Eq(m.AccountId, a.Id))
                .GroupBy((a, m) => m.Role)
                .Select((k, a, m) => (k, CountDistinct(a.Id)))
                .ToSql();
    }

    private sealed class DistinctCount : IQueryCase
    {
        public SqlStatement Build<TDialect>(Database<TDialect> db)
            where TDialect : IDialect =>
            db.From(Account.Table).GroupBy(a => a.Status).SelectDistinct((k, a) => Count()).OrderBy(c => [Desc(c)]).ToSql();
    }
}
