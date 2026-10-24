using static AuraORM.Sql;

namespace AuraORM.Tests;

public sealed class SubqueryTests
{
    /// <summary>
    /// <c>In</c> and <c>NotIn</c> write the subquery in parentheses after <c>IN</c> / <c>NOT IN</c>, and the parameters are
    /// numbered in the order they appear, the outer statement's before and after the subquery's (design §5.11, §5.12).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - <see cref="Account"/> and <see cref="Membership"/>.
    ///
    /// Steps:
    /// - Render accounts whose id is in the memberships, and accounts with a login count over 1 whose id is not in the
    ///   admin memberships, on each dialect.
    ///
    /// Expected:
    /// - <c>WHERE "accounts"."id" IN (SELECT "memberships"."AccountId" FROM "app"."memberships")</c>.
    /// - <c>WHERE ("accounts"."login_count" &gt; @p0 AND "accounts"."id" NOT IN (SELECT … WHERE "memberships"."Role" = @p1))</c>,
    ///   with the parameters 1 and "admin".
    /// </remarks>
    [Fact]
    public void InAndNotIn_WriteTheSubquery()
    {
        SqlAssert.Statement(
            new InMemberships(),
            new(
                "SELECT \"accounts\".\"email\" FROM \"accounts\" WHERE \"accounts\".\"id\" IN (SELECT \"memberships\".\"AccountId\" FROM \"app\".\"memberships\")",
                "SELECT \"accounts\".\"email\" FROM \"accounts\" WHERE \"accounts\".\"id\" IN (SELECT \"memberships\".\"AccountId\" FROM \"app\".\"memberships\")",
                "SELECT `accounts`.`email` FROM `accounts` WHERE `accounts`.`id` IN (SELECT `memberships`.`AccountId` FROM `app`.`memberships`)",
                "SELECT [accounts].[email] FROM [accounts] WHERE [accounts].[id] IN (SELECT [memberships].[AccountId] FROM [app].[memberships])"
            )
        );
        SqlAssert.Statement(
            new NotInAdmins(),
            new(
                "SELECT \"accounts\".\"id\" FROM \"accounts\" WHERE (\"accounts\".\"login_count\" > @p0 AND \"accounts\".\"id\" NOT IN (SELECT \"memberships\".\"AccountId\" FROM \"app\".\"memberships\" WHERE \"memberships\".\"Role\" = @p1))",
                "SELECT \"accounts\".\"id\" FROM \"accounts\" WHERE (\"accounts\".\"login_count\" > $1 AND \"accounts\".\"id\" NOT IN (SELECT \"memberships\".\"AccountId\" FROM \"app\".\"memberships\" WHERE \"memberships\".\"Role\" = $2))",
                "SELECT `accounts`.`id` FROM `accounts` WHERE (`accounts`.`login_count` > @p0 AND `accounts`.`id` NOT IN (SELECT `memberships`.`AccountId` FROM `app`.`memberships` WHERE `memberships`.`Role` = @p1))",
                "SELECT [accounts].[id] FROM [accounts] WHERE ([accounts].[login_count] > @p0 AND [accounts].[id] NOT IN (SELECT [memberships].[AccountId] FROM [app].[memberships] WHERE [memberships].[Role] = @p1))"
            ),
            1,
            "admin"
        );
    }

    /// <summary>
    /// A correlated <c>Exists</c> and a correlated <c>Scalar</c> use the outer statement's columns inside the subquery
    /// (design §5.11).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - <see cref="Account"/> and <see cref="Membership"/>.
    ///
    /// Steps:
    /// - Render accounts that have a membership (<c>Exists</c>), and each account's email with its latest membership date
    ///   (<c>Scalar</c> of <c>Max</c>), on each dialect.
    ///
    /// Expected:
    /// - <c>WHERE EXISTS (SELECT "memberships"."Role" FROM "app"."memberships" WHERE "memberships"."AccountId" = "accounts"."id")</c>.
    /// - <c>SELECT "accounts"."email", (SELECT MAX("memberships"."joined_on") FROM … WHERE … = "accounts"."id")</c>.
    /// </remarks>
    [Fact]
    public void ExistsAndScalar_Correlated_UseTheOuterColumns()
    {
        SqlAssert.Statement(
            new ExistsMembership(),
            new(
                "SELECT \"accounts\".\"id\" FROM \"accounts\" WHERE EXISTS (SELECT \"memberships\".\"Role\" FROM \"app\".\"memberships\" WHERE \"memberships\".\"AccountId\" = \"accounts\".\"id\")",
                "SELECT \"accounts\".\"id\" FROM \"accounts\" WHERE EXISTS (SELECT \"memberships\".\"Role\" FROM \"app\".\"memberships\" WHERE \"memberships\".\"AccountId\" = \"accounts\".\"id\")",
                "SELECT `accounts`.`id` FROM `accounts` WHERE EXISTS (SELECT `memberships`.`Role` FROM `app`.`memberships` WHERE `memberships`.`AccountId` = `accounts`.`id`)",
                "SELECT [accounts].[id] FROM [accounts] WHERE EXISTS (SELECT [memberships].[Role] FROM [app].[memberships] WHERE [memberships].[AccountId] = [accounts].[id])"
            )
        );
        SqlAssert.Statement(
            new LatestMembership(),
            new(
                "SELECT \"accounts\".\"email\", (SELECT MAX(\"memberships\".\"joined_on\") FROM \"app\".\"memberships\" WHERE \"memberships\".\"AccountId\" = \"accounts\".\"id\") FROM \"accounts\"",
                "SELECT \"accounts\".\"email\", (SELECT MAX(\"memberships\".\"joined_on\") FROM \"app\".\"memberships\" WHERE \"memberships\".\"AccountId\" = \"accounts\".\"id\") FROM \"accounts\"",
                "SELECT `accounts`.`email`, (SELECT MAX(`memberships`.`joined_on`) FROM `app`.`memberships` WHERE `memberships`.`AccountId` = `accounts`.`id`) FROM `accounts`",
                "SELECT [accounts].[email], (SELECT MAX([memberships].[joined_on]) FROM [app].[memberships] WHERE [memberships].[AccountId] = [accounts].[id]) FROM [accounts]"
            )
        );
    }

    /// <summary>
    /// <c>GroupScalar</c> in <c>HAVING</c> and <c>GroupExists</c> correlate with the grouped statement through
    /// <c>Outer(k)</c>, which writes the key's column (design §5.11).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - <see cref="Account"/> grouped by its id, and <see cref="Membership"/>.
    ///
    /// Steps:
    /// - Render a statement whose <c>HAVING</c> compares <c>COUNT(*)</c> with a <c>GroupScalar</c> count of the account's
    ///   memberships, and whose projection is <c>GroupExists</c> of an admin membership, on each dialect.
    ///
    /// Expected:
    /// - <c>HAVING COUNT(*) &lt;= (SELECT COUNT(*) FROM … WHERE "memberships"."AccountId" = "accounts"."id")</c> and the
    ///   parameter "admin" in the select list's subquery.
    /// </remarks>
    [Fact]
    public void GroupScalarAndGroupExists_CorrelateThroughOuter()
    {
        SqlAssert.Statement(
            new GroupSubqueries(),
            new(
                "SELECT \"accounts\".\"id\", CASE WHEN EXISTS (SELECT \"memberships\".\"Role\" FROM \"app\".\"memberships\" WHERE (\"memberships\".\"AccountId\" = \"accounts\".\"id\" AND \"memberships\".\"Role\" = @p0)) THEN @p1 ELSE @p2 END FROM \"accounts\" GROUP BY \"accounts\".\"id\" HAVING COUNT(*) <= (SELECT COUNT(*) FROM \"app\".\"memberships\" WHERE \"memberships\".\"AccountId\" = \"accounts\".\"id\")",
                "SELECT \"accounts\".\"id\", CASE WHEN EXISTS (SELECT \"memberships\".\"Role\" FROM \"app\".\"memberships\" WHERE (\"memberships\".\"AccountId\" = \"accounts\".\"id\" AND \"memberships\".\"Role\" = $1)) THEN $2 ELSE $3 END FROM \"accounts\" GROUP BY \"accounts\".\"id\" HAVING COUNT(*) <= (SELECT COUNT(*) FROM \"app\".\"memberships\" WHERE \"memberships\".\"AccountId\" = \"accounts\".\"id\")",
                "SELECT `accounts`.`id`, CASE WHEN EXISTS (SELECT `memberships`.`Role` FROM `app`.`memberships` WHERE (`memberships`.`AccountId` = `accounts`.`id` AND `memberships`.`Role` = @p0)) THEN @p1 ELSE @p2 END FROM `accounts` GROUP BY `accounts`.`id` HAVING COUNT(*) <= (SELECT COUNT(*) FROM `app`.`memberships` WHERE `memberships`.`AccountId` = `accounts`.`id`)",
                "SELECT [accounts].[id], CASE WHEN EXISTS (SELECT [memberships].[Role] FROM [app].[memberships] WHERE ([memberships].[AccountId] = [accounts].[id] AND [memberships].[Role] = @p0)) THEN @p1 ELSE @p2 END FROM [accounts] GROUP BY [accounts].[id] HAVING COUNT_BIG(*) <= (SELECT COUNT_BIG(*) FROM [app].[memberships] WHERE [memberships].[AccountId] = [accounts].[id])"
            ),
            "admin",
            true,
            false
        );
    }

    /// <summary>
    /// A subquery with a row limit renders on the dialects that accept it in <c>IN</c>, and <c>Exists</c> and <c>Scalar</c>
    /// accept a row limit on every dialect (design §5.11).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - <see cref="Account"/> and <see cref="Membership"/>.
    ///
    /// Steps:
    /// - Render <c>In</c> with an ordered, limited subquery on SQLite, PostgreSQL, and SQL Server; render <c>Exists</c> with
    ///   a limited subquery on each dialect.
    ///
    /// Expected:
    /// - <c>IN (SELECT … ORDER BY … LIMIT @p0)</c>, and <c>OFFSET 0 ROWS FETCH NEXT @p0 ROWS ONLY</c> on SQL Server.
    /// - <c>EXISTS (SELECT … LIMIT @p0)</c> on MySQL and MariaDB.
    /// </remarks>
    [Fact]
    public void Limit_InSubquery_RendersWhereTheDialectAcceptsIt()
    {
        Assert.Equal(
            "SELECT \"accounts\".\"id\" FROM \"accounts\" WHERE \"accounts\".\"id\" IN (SELECT \"memberships\".\"AccountId\" FROM \"app\".\"memberships\" ORDER BY \"memberships\".\"joined_on\" DESC LIMIT @p0)",
            LimitedIn(new Database<SqliteDialect>(new FakeConnection())).Text
        );
        Assert.Equal(
            "SELECT \"accounts\".\"id\" FROM \"accounts\" WHERE \"accounts\".\"id\" IN (SELECT \"memberships\".\"AccountId\" FROM \"app\".\"memberships\" ORDER BY \"memberships\".\"joined_on\" DESC LIMIT $1)",
            LimitedIn(new Database<PostgreSqlDialect>(new FakeConnection())).Text
        );
        Assert.Equal(
            "SELECT [accounts].[id] FROM [accounts] WHERE [accounts].[id] IN (SELECT [memberships].[AccountId] FROM [app].[memberships] ORDER BY [memberships].[joined_on] DESC OFFSET 0 ROWS FETCH NEXT @p0 ROWS ONLY)",
            LimitedIn(new Database<SqlServerDialect>(new FakeConnection())).Text
        );
        SqlStatement exists = new Database<MySqlDialect>(new FakeConnection())
            .From(Account.Table)
            .Where(a => Exists(Db<MySqlDialect>().From(Membership.Table).Where(m => Eq(m.AccountId, a.Id)).Select(m => m.Role).Limit(1)))
            .Select(a => a.Id)
            .ToSql();
        Assert.Equal(
            "SELECT `accounts`.`id` FROM `accounts` WHERE EXISTS (SELECT `memberships`.`Role` FROM `app`.`memberships` WHERE `memberships`.`AccountId` = `accounts`.`id` LIMIT @p0)",
            exists.Text
        );
    }

    /// <summary>
    /// The checks that the types cannot make throw when the statement is built: a subquery of another dialect, a subquery in a
    /// <c>GROUP BY</c> key, a column of the grouped statement used in a group subquery without <c>Outer</c>, <c>Outer</c>
    /// outside a group subquery, <c>Outer</c> of something that is not a column key, and <c>Scalar</c> of a whole row
    /// (design §5.5, §5.11, §14.3).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - <see cref="Account"/> and <see cref="Membership"/>.
    ///
    /// Steps:
    /// - Build each statement.
    ///
    /// Expected:
    /// - <see cref="InvalidOperationException"/> for the first four, <see cref="ArgumentException"/> for the last two; the
    ///   message of the fourth names <c>Outer</c>.
    /// </remarks>
    [Fact]
    public void Subquery_ChecksTheTypesCannotMake_Throw()
    {
        Database<SqlServerDialect> mssql = Db<SqlServerDialect>();
        var sqliteSubquery = Db<SqliteDialect>().From(Membership.Table).Select(m => m.AccountId).Limit(1);
        Assert.Throws<InvalidOperationException>(() => mssql.From(Account.Table).Where(a => Exists(sqliteSubquery)).Select(a => a.Id).ToSql());
        Assert.Throws<InvalidOperationException>(() =>
            mssql
                .From(Account.Table)
                .GroupBy(a => Scalar(Db<SqlServerDialect>().From(Membership.Table).Where(m => Eq(m.AccountId, a.Id)).Select(m => Count())))
                .Select((k, a) => Count())
                .ToSql()
        );
        Assert.Throws<InvalidOperationException>(() =>
            mssql
                .From(Account.Table)
                .GroupBy(a => a.Id)
                .Having((k, a) => GroupExists(Db<SqlServerDialect>().From(Membership.Table).Where(m => Eq(m.AccountId, a.Id)).Select(m => m.Role)))
                .Select((k, a) => k)
                .ToSql()
        );
        IExpr<long, PerRow>? leaked = null;
        _ = mssql.From(Account.Table).GroupBy(a => a.Id).Select((k, a) => (leaked = Outer(k)) is null ? k : k);
        var outside = Assert.Throws<InvalidOperationException>(() =>
            mssql.From(Membership.Table).Where(m => Eq(m.AccountId, leaked!)).Select(m => m.Role).ToSql()
        );
        Assert.Contains("Outer", outside.Message, StringComparison.Ordinal);
        Assert.Throws<ArgumentException>(() =>
            mssql
                .From(Account.Table)
                .GroupBy(a => Add(a.LoginCount, 1))
                .Select((k, a) => GroupScalar(mssql.From(Account.Table.As("o")).Where(o => Eq(o.LoginCount, Outer(k))).Select(o => Count())))
        );
        Assert.Throws<ArgumentException>(() => mssql.From(Account.Table).Select(a => Scalar(mssql.From(Membership.Table).Select(m => m))));
    }

    /// <summary>
    /// A correlation with an outer table whose name an inner table has too throws when the statement is built, because both
    /// columns would be written <c>"accounts"."…"</c> and SQL would read them as the inner table's; an alias on either table,
    /// or a subquery that does not use the outer columns, renders (design §5.11, §14.3).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - <see cref="Account"/> as the outer table and as the subquery's table.
    ///
    /// Steps:
    /// - Build a self-correlated <c>Exists</c> (same email, larger id) with no alias; one where the subquery's table is joined
    ///   after another table; then the same with <c>As("x")</c> on the inner table, and on the outer table; then an
    ///   uncorrelated <c>In</c> on the same table.
    ///
    /// Expected:
    /// - The first two throw <see cref="InvalidOperationException"/> whose message names <c>As</c>.
    /// - With an alias, the SQL qualifies the inner columns with <c>"x"</c> (or the outer with it); the uncorrelated one renders.
    /// </remarks>
    [Fact]
    public void Correlation_OuterTableOfTheSameName_Throws()
    {
        Database<PostgreSqlDialect> pg = Db<PostgreSqlDialect>();
        var selfCorrelated = Assert.Throws<InvalidOperationException>(() =>
            pg.From(Account.Table)
                .Where(a => Exists(pg.From(Account.Table).Where(x => And(Eq(x.Email, a.Email), Gt(x.Id, a.Id))).Select(x => x.Id)))
                .Select(a => a.Id)
                .ToSql()
        );
        var joined = Assert.Throws<InvalidOperationException>(() =>
            pg.From(Account.Table)
                .Where(a =>
                    Exists(
                        pg.From(Membership.Table)
                            .InnerJoin(Account.Table, (m, x) => Eq(x.Id, m.AccountId))
                            .Where((m, x) => Eq(x.Email, a.Email))
                            .Select((m, x) => m.Role)
                    )
                )
                .Select(a => a.Id)
                .ToSql()
        );

        Assert.Contains("As(", selfCorrelated.Message, StringComparison.Ordinal);
        Assert.Contains("As(", joined.Message, StringComparison.Ordinal);
        SqlStatement innerAlias = pg.From(Account.Table)
            .Where(a => Exists(pg.From(Account.Table.As("x")).Where(x => And(Eq(x.Email, a.Email), Gt(x.Id, a.Id))).Select(x => x.Id)))
            .Select(a => a.Id)
            .ToSql();
        SqlStatement outerAlias = pg.From(Account.Table.As("a"))
            .Where(a => Exists(pg.From(Account.Table).Where(x => Eq(x.Email, a.Email)).Select(x => x.Id)))
            .Select(a => a.Id)
            .ToSql();
        SqlStatement uncorrelated = pg.From(Account.Table)
            .Where(a => In(a.Id, pg.From(Account.Table).Where(x => Gt(x.LoginCount, 1)).Select(x => x.Id)))
            .Select(a => a.Id)
            .ToSql();
        Assert.Equal(
            "SELECT \"accounts\".\"id\" FROM \"accounts\" WHERE EXISTS (SELECT \"x\".\"id\" FROM \"accounts\" AS \"x\" WHERE (\"x\".\"email\" = \"accounts\".\"email\" AND \"x\".\"id\" > \"accounts\".\"id\"))",
            innerAlias.Text
        );
        Assert.Equal(
            "SELECT \"a\".\"id\" FROM \"accounts\" AS \"a\" WHERE EXISTS (SELECT \"accounts\".\"id\" FROM \"accounts\" WHERE \"accounts\".\"email\" = \"a\".\"email\")",
            outerAlias.Text
        );
        Assert.Equal(
            "SELECT \"accounts\".\"id\" FROM \"accounts\" WHERE \"accounts\".\"id\" IN (SELECT \"accounts\".\"id\" FROM \"accounts\" WHERE \"accounts\".\"login_count\" > $1)",
            uncorrelated.Text
        );
    }

    private static SqlStatement LimitedIn<TDialect>(Database<TDialect> db)
        where TDialect : ILimitInSubqueryDialect =>
        db.From(Account.Table)
            .Where(a =>
                In(
                    a.Id,
                    new Database<TDialect>(new FakeConnection())
                        .From(Membership.Table)
                        .Select(m => m.AccountId)
                        .OrderBy(m => [Desc(m.JoinedOn)])
                        .Limit(1)
                )
            )
            .Select(a => a.Id)
            .ToSql();

    private static Database<TDialect> Db<TDialect>()
        where TDialect : IDialect => new(new FakeConnection());

    private sealed class InMemberships : IQueryCase
    {
        public SqlStatement Build<TDialect>(Database<TDialect> db)
            where TDialect : IDialect =>
            db.From(Account.Table).Where(a => In(a.Id, db.From(Membership.Table).Select(m => m.AccountId))).Select(a => a.Email).ToSql();
    }

    private sealed class NotInAdmins : IQueryCase
    {
        public SqlStatement Build<TDialect>(Database<TDialect> db)
            where TDialect : IDialect =>
            db.From(Account.Table)
                .Where(a => And(Gt(a.LoginCount, 1), NotIn(a.Id, db.From(Membership.Table).Where(m => Eq(m.Role, "admin")).Select(m => m.AccountId))))
                .Select(a => a.Id)
                .ToSql();
    }

    private sealed class ExistsMembership : IQueryCase
    {
        public SqlStatement Build<TDialect>(Database<TDialect> db)
            where TDialect : IDialect =>
            db.From(Account.Table)
                .Where(a => Exists(db.From(Membership.Table).Where(m => Eq(m.AccountId, a.Id)).Select(m => m.Role)))
                .Select(a => a.Id)
                .ToSql();
    }

    private sealed class LatestMembership : IQueryCase
    {
        public SqlStatement Build<TDialect>(Database<TDialect> db)
            where TDialect : IDialect =>
            db.From(Account.Table)
                .Select(a => (a.Email, Scalar(db.From(Membership.Table).Where(m => Eq(m.AccountId, a.Id)).Select(m => Max(m.JoinedOn)))))
                .ToSql();
    }

    private sealed class GroupSubqueries : IQueryCase
    {
        public SqlStatement Build<TDialect>(Database<TDialect> db)
            where TDialect : IDialect =>
            db.From(Account.Table)
                .GroupBy(a => a.Id)
                .Having((k, a) => Le(Count(), GroupScalar(db.From(Membership.Table).Where(m => Eq(m.AccountId, Outer(k))).Select(m => Count()))))
                .Select(
                    (k, a) =>
                        (
                            k,
                            Case(
                                    GroupExists(
                                        db.From(Membership.Table).Where(m => And(Eq(m.AccountId, Outer(k)), Eq(m.Role, "admin"))).Select(m => m.Role)
                                    ),
                                    true
                                )
                                .Else(false)
                        )
                )
                .ToSql();
    }
}
