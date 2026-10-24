using static AuraORM.Sql;

namespace AuraORM.Tests;

public sealed class NamedQueryTests
{
    /// <summary>
    /// A named query is a derived table where it is used, and a CTE when the statement lists it in <c>With</c>; its items are
    /// named after the result type's members, and the parameters are numbered in the order they appear (design §5.11, §5.12).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - <see cref="EmailCount"/>; accounts grouped by email with a <c>Where</c> value, as <c>"t"</c>.
    ///
    /// Steps:
    /// - Render a statement from <c>t</c> with a <c>Where</c> value, without <c>With</c> and with <c>With(t)</c>, on each
    ///   dialect.
    ///
    /// Expected:
    /// - <c>FROM (SELECT "accounts"."email" AS "Email", COUNT(*) AS "Count" … ) AS "t" WHERE "t"."Count" &gt; @p1</c>.
    /// - <c>WITH "t" AS (SELECT … ) SELECT "t"."Email" FROM "t" WHERE "t"."Count" &gt; @p1</c>.
    /// </remarks>
    [Fact]
    public void NamedQuery_DerivedTableOrCte()
    {
        SqlAssert.Statement(
            new Totals(withCte: false),
            new(
                "SELECT \"t\".\"Email\" FROM (SELECT \"accounts\".\"email\" AS \"Email\", COUNT(*) AS \"Count\" FROM \"accounts\" WHERE \"accounts\".\"login_count\" > @p0 GROUP BY \"accounts\".\"email\") AS \"t\" WHERE \"t\".\"Count\" > @p1",
                "SELECT \"t\".\"Email\" FROM (SELECT \"accounts\".\"email\" AS \"Email\", COUNT(*) AS \"Count\" FROM \"accounts\" WHERE \"accounts\".\"login_count\" > $1 GROUP BY \"accounts\".\"email\") AS \"t\" WHERE \"t\".\"Count\" > $2",
                "SELECT `t`.`Email` FROM (SELECT `accounts`.`email` AS `Email`, COUNT(*) AS `Count` FROM `accounts` WHERE `accounts`.`login_count` > @p0 GROUP BY `accounts`.`email`) AS `t` WHERE `t`.`Count` > @p1",
                "SELECT [t].[Email] FROM (SELECT [accounts].[email] AS [Email], COUNT_BIG(*) AS [Count] FROM [accounts] WHERE [accounts].[login_count] > @p0 GROUP BY [accounts].[email]) AS [t] WHERE [t].[Count] > @p1"
            ),
            0,
            1L
        );
        SqlAssert.Statement(
            new Totals(withCte: true),
            new(
                "WITH \"t\" AS (SELECT \"accounts\".\"email\" AS \"Email\", COUNT(*) AS \"Count\" FROM \"accounts\" WHERE \"accounts\".\"login_count\" > @p0 GROUP BY \"accounts\".\"email\") SELECT \"t\".\"Email\" FROM \"t\" WHERE \"t\".\"Count\" > @p1",
                "WITH \"t\" AS (SELECT \"accounts\".\"email\" AS \"Email\", COUNT(*) AS \"Count\" FROM \"accounts\" WHERE \"accounts\".\"login_count\" > $1 GROUP BY \"accounts\".\"email\") SELECT \"t\".\"Email\" FROM \"t\" WHERE \"t\".\"Count\" > $2",
                "WITH `t` AS (SELECT `accounts`.`email` AS `Email`, COUNT(*) AS `Count` FROM `accounts` WHERE `accounts`.`login_count` > @p0 GROUP BY `accounts`.`email`) SELECT `t`.`Email` FROM `t` WHERE `t`.`Count` > @p1",
                "WITH [t] AS (SELECT [accounts].[email] AS [Email], COUNT_BIG(*) AS [Count] FROM [accounts] WHERE [accounts].[login_count] > @p0 GROUP BY [accounts].[email]) SELECT [t].[Email] FROM [t] WHERE [t].[Count] > @p1"
            ),
            0,
            1L
        );
    }

    /// <summary>
    /// A whole row of a table is a named query whose items are named after the columns; a later CTE reads an earlier one, a
    /// named query is the target of a left join, and a second reference has an alias (design §5.11).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - <see cref="Membership"/> rows as <c>"a"</c>; <c>"b"</c> reads <c>"a"</c>.
    ///
    /// Steps:
    /// - Render <c>With(a, b)</c> selecting from <c>b</c> left-joined with <c>a.As("a2")</c>, on PostgreSQL; render the same
    ///   without <c>With</c>.
    ///
    /// Expected:
    /// - <c>WITH "a" AS (SELECT … AS "AccountId", … AS "Role", … AS "joined_on" …), "b" AS (… FROM "a" …) SELECT … FROM "b" LEFT JOIN
    ///   "a" AS "a2" ON …</c>.
    /// - Without <c>With</c>, each reference is the query written again as a derived table.
    /// </remarks>
    [Fact]
    public void NamedQuery_TwoCtesJoinAndAlias()
    {
        var db = new Database<PostgreSqlDialect>(new FakeConnection());
        var a = db.From(Membership.Table).Where(m => Eq(m.Role, "admin")).Select(m => m).As("a");
        var b = db.From(a).Where(x => Gt(x.AccountId, 1L)).Select(x => x).As("b");

        SqlStatement withCtes = db.With(a, b)
            .From(b)
            .LeftJoin(a.As("a2"), (x, y) => Eq(y.AccountId, x.AccountId))
            .Select((x, y) => (x.Role, y.Role))
            .ToSql();
        SqlStatement derived = db.From(b).LeftJoin(a.As("a2"), (x, y) => Eq(y.AccountId, x.AccountId)).Select((x, y) => (x.Role, y.Role)).ToSql();

        const string A =
            "SELECT \"memberships\".\"AccountId\" AS \"AccountId\", \"memberships\".\"Role\" AS \"Role\", \"memberships\".\"joined_on\" AS \"joined_on\" FROM \"app\".\"memberships\" WHERE \"memberships\".\"Role\" = $1";
        Assert.Equal(
            "WITH \"a\" AS ("
                + A
                + "), \"b\" AS (SELECT \"a\".\"AccountId\" AS \"AccountId\", \"a\".\"Role\" AS \"Role\", \"a\".\"joined_on\" AS \"joined_on\" FROM \"a\" WHERE \"a\".\"AccountId\" > $2) SELECT \"b\".\"Role\", \"a2\".\"Role\" FROM \"b\" LEFT JOIN \"a\" AS \"a2\" ON \"a2\".\"AccountId\" = \"b\".\"AccountId\"",
            withCtes.Text
        );
        Assert.Equal(["admin", 1L], withCtes.Parameters.Select(p => p.Value));
        Assert.Equal(
            "SELECT \"b\".\"Role\", \"a2\".\"Role\" FROM (SELECT \"a\".\"AccountId\" AS \"AccountId\", \"a\".\"Role\" AS \"Role\", \"a\".\"joined_on\" AS \"joined_on\" FROM ("
                + A
                + ") AS \"a\" WHERE \"a\".\"AccountId\" > $2) AS \"b\" LEFT JOIN ("
                + A
                + ") AS \"a2\" ON \"a2\".\"AccountId\" = \"b\".\"AccountId\"",
            derived.Text
        );
        Assert.Equal(["admin", 1L], derived.Parameters.Select(p => p.Value));
    }

    /// <summary>
    /// A set operation is a named query, and every operand names its items (design §5.11, D1-38).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - <see cref="EmailCount"/> projections of accounts and of memberships.
    ///
    /// Steps:
    /// - Render a CTE of their union, on SQLite.
    ///
    /// Expected:
    /// - <c>WITH "u" AS (SELECT … AS "Email", … AS "Count" … UNION SELECT … AS "Email", … AS "Count" …) SELECT "u"."Email" FROM "u"</c>.
    /// </remarks>
    [Fact]
    public void NamedQuery_SetOperation_NamesEveryOperand()
    {
        var db = new Database<SqliteDialect>(new FakeConnection());
        var u = db.From(Account.Table)
            .Select(a => new EmailCount.RowProjection { Email = a.Email, Count = a.Id })
            .Union(db.From(Membership.Table).Select(m => new EmailCount.RowProjection { Email = m.Role, Count = m.AccountId }))
            .As("u");

        Assert.Equal(
            "WITH \"u\" AS (SELECT \"accounts\".\"email\" AS \"Email\", \"accounts\".\"id\" AS \"Count\" FROM \"accounts\" UNION SELECT \"memberships\".\"Role\" AS \"Email\", \"memberships\".\"AccountId\" AS \"Count\" FROM \"app\".\"memberships\") SELECT \"u\".\"Email\" FROM \"u\"",
            db.With(u).From(u).Select(x => x.Email).ToSql().Text
        );
    }

    /// <summary>
    /// The checks that the types cannot make throw when the statement is built: a <c>With</c> statement used as a named query
    /// or a subquery, a table with the name of a CTE, a second reference without an alias, a named query of another dialect,
    /// a named query that uses a column of the statement around it (a derived table is a statement of its own), an aliased
    /// reference in <c>With</c>, and two CTEs of the same name (design §5.11, §14.3).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - <see cref="Account"/>, <see cref="Membership"/>, and named queries of them.
    ///
    /// Steps:
    /// - Build each statement.
    ///
    /// Expected:
    /// - <see cref="InvalidOperationException"/> for the first six (the sixth names the outer column), <see cref="ArgumentException"/>
    ///   for the last two.
    /// </remarks>
    [Fact]
    public void NamedQuery_ChecksTheTypesCannotMake_Throw()
    {
        var db = new Database<PostgreSqlDialect>(new FakeConnection());
        var t = db.From(Membership.Table).Select(m => m).As("t");
        var accounts = db.From(Membership.Table).Select(m => m).As("accounts");

        Assert.Throws<InvalidOperationException>(() => db.With(t).From(t).Select(x => x).As("w"));
        Assert.Throws<InvalidOperationException>(() =>
            db.From(Account.Table).Where(a => Exists(db.With(t).From(t).Select(x => x.Role))).Select(a => a.Id).ToSql()
        );
        Assert.Throws<InvalidOperationException>(() => db.With(accounts).From(Account.Table).Select(a => a.Id).ToSql());
        Assert.Throws<InvalidOperationException>(() => db.From(t).CrossJoin(t).Select((x, y) => x.Role));
        var sqlite = new Database<SqliteDialect>(new FakeConnection()).From(Membership.Table).Select(m => m).As("s");
        Assert.Throws<InvalidOperationException>(() => db.From(sqlite).Select(x => x.Role).ToSql());
        var correlated = Assert.Throws<InvalidOperationException>(() =>
            db.From(Account.Table)
                .Where(a => Exists(db.From(db.From(Membership.Table).Where(m => Eq(m.AccountId, a.Id)).Select(m => m).As("d")).Select(d => d.Role)))
                .Select(a => a.Id)
                .ToSql()
        );
        Assert.Contains("accounts.id", correlated.Message, StringComparison.Ordinal);
        Assert.Throws<ArgumentException>(() => db.With(t.As("t2")));
        Assert.Throws<ArgumentException>(() => db.With(t, db.From(Account.Table).Select(a => a).As("t")));
    }

    /// <summary>
    /// <c>SelectNamedQueries.Create</c>, which the generated <c>As</c> calls, checks the query itself too, for a caller that
    /// does not go through the typed <c>As</c>: a query with <c>OrderBy</c> but no limit, and an empty name (design §5.11).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - <see cref="Membership"/>.
    ///
    /// Steps:
    /// - Call <c>Create</c> with an ordered query, and with an empty name.
    ///
    /// Expected:
    /// - <see cref="ArgumentException"/> for both.
    /// </remarks>
    [Fact]
    public void Create_OrderedOrUnnamed_Throws()
    {
        var db = new Database<PostgreSqlDialect>(new FakeConnection());
        var ordered = db.From(Membership.Table).Select(m => m).OrderBy(m => [Asc(m.AccountId)]);
        var rows = db.From(Membership.Table).Select(m => m);

        Assert.Throws<ArgumentException>(() =>
            SelectNamedQueries.Create<Membership.Columns, Membership.NullableColumns, PostgreSqlDialect, Executable, Membership>(
                ordered,
                "o",
                ["AccountId", "Role", "joined_on"]
            )
        );
        Assert.Throws<ArgumentException>(() =>
            SelectNamedQueries.Create<Membership.Columns, Membership.NullableColumns, PostgreSqlDialect, Executable, Membership>(
                rows,
                string.Empty,
                ["AccountId", "Role", "joined_on"]
            )
        );
    }

    private sealed class Totals(bool withCte) : IQueryCase
    {
        public SqlStatement Build<TDialect>(Database<TDialect> db)
            where TDialect : IDialect
        {
            var t = db.From(Account.Table)
                .Where(a => Gt(a.LoginCount, 0))
                .GroupBy(a => a.Email)
                .Select((k, a) => new EmailCount.GroupProjection { Email = k, Count = Count() })
                .As("t");
            var start = withCte ? db.With(t).From(t) : db.From(t);
            return start.Where(x => Gt(x.Count, 1L)).Select(x => x.Email).ToSql();
        }
    }
}
