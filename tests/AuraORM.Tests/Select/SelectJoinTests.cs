using static AuraORM.Sql;

namespace AuraORM.Tests;

public sealed class SelectJoinTests
{
    /// <summary>
    /// Each join writes its keyword, the joined table (with its schema), and <c>ON</c>, after <c>FROM</c>; the lambdas of the
    /// statement get the column sets of both tables (design §5.3).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - <see cref="Account"/> and <see cref="Membership"/> (schema <c>app</c>), joined on the account id.
    ///
    /// Steps:
    /// - Render an inner join selecting a column of each table, on each dialect.
    ///
    /// Expected:
    /// - <c>FROM "accounts" INNER JOIN "app"."memberships" ON "memberships"."AccountId" = "accounts"."id"</c>.
    /// </remarks>
    [Fact]
    public void InnerJoin_TwoTables_WritesJoinAndOn()
    {
        SqlAssert.Statement(
            new Inner(),
            new(
                "SELECT \"accounts\".\"email\", \"memberships\".\"Role\" FROM \"accounts\" INNER JOIN \"app\".\"memberships\" ON \"memberships\".\"AccountId\" = \"accounts\".\"id\"",
                "SELECT \"accounts\".\"email\", \"memberships\".\"Role\" FROM \"accounts\" INNER JOIN \"app\".\"memberships\" ON \"memberships\".\"AccountId\" = \"accounts\".\"id\"",
                "SELECT `accounts`.`email`, `memberships`.`Role` FROM `accounts` INNER JOIN `app`.`memberships` ON `memberships`.`AccountId` = `accounts`.`id`",
                "SELECT [accounts].[email], [memberships].[Role] FROM [accounts] INNER JOIN [app].[memberships] ON [memberships].[AccountId] = [accounts].[id]"
            )
        );
    }

    /// <summary>
    /// <c>LEFT JOIN</c>, <c>RIGHT JOIN</c>, and <c>CROSS JOIN</c> write their keywords on every dialect, and <c>FULL JOIN</c> on
    /// the dialects that have it; the parameters of <c>ON</c> come before those of <c>WHERE</c>, in the order they appear
    /// (design §5.3, §5.12).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - <see cref="Account"/> and <see cref="Membership"/>.
    ///
    /// Steps:
    /// - Render a left join with a value in <c>ON</c> and in <c>WHERE</c>, a right join, a cross join, and a full join on
    ///   PostgreSQL.
    ///
    /// Expected:
    /// - The SQL of each, with the parameters in their order of appearance.
    /// </remarks>
    [Fact]
    public void OuterAndCrossJoins_WriteTheirKeywords()
    {
        SqlAssert.Statement(
            new Left(),
            new(
                "SELECT \"accounts\".\"id\" FROM \"accounts\" LEFT JOIN \"app\".\"memberships\" ON (\"memberships\".\"AccountId\" = \"accounts\".\"id\" AND \"memberships\".\"Role\" = @p0) WHERE \"accounts\".\"login_count\" > @p1",
                "SELECT \"accounts\".\"id\" FROM \"accounts\" LEFT JOIN \"app\".\"memberships\" ON (\"memberships\".\"AccountId\" = \"accounts\".\"id\" AND \"memberships\".\"Role\" = $1) WHERE \"accounts\".\"login_count\" > $2",
                "SELECT `accounts`.`id` FROM `accounts` LEFT JOIN `app`.`memberships` ON (`memberships`.`AccountId` = `accounts`.`id` AND `memberships`.`Role` = @p0) WHERE `accounts`.`login_count` > @p1",
                "SELECT [accounts].[id] FROM [accounts] LEFT JOIN [app].[memberships] ON ([memberships].[AccountId] = [accounts].[id] AND [memberships].[Role] = @p0) WHERE [accounts].[login_count] > @p1"
            ),
            "admin",
            1
        );
        SqlAssert.Statement(
            new Right(),
            new(
                "SELECT \"memberships\".\"Role\" FROM \"accounts\" RIGHT JOIN \"app\".\"memberships\" ON \"memberships\".\"AccountId\" = \"accounts\".\"id\"",
                "SELECT \"memberships\".\"Role\" FROM \"accounts\" RIGHT JOIN \"app\".\"memberships\" ON \"memberships\".\"AccountId\" = \"accounts\".\"id\"",
                "SELECT `memberships`.`Role` FROM `accounts` RIGHT JOIN `app`.`memberships` ON `memberships`.`AccountId` = `accounts`.`id`",
                "SELECT [memberships].[Role] FROM [accounts] RIGHT JOIN [app].[memberships] ON [memberships].[AccountId] = [accounts].[id]"
            )
        );
        SqlAssert.Statement(
            new Cross(),
            new(
                "SELECT \"accounts\".\"id\", \"memberships\".\"Role\" FROM \"accounts\" CROSS JOIN \"app\".\"memberships\"",
                "SELECT \"accounts\".\"id\", \"memberships\".\"Role\" FROM \"accounts\" CROSS JOIN \"app\".\"memberships\"",
                "SELECT `accounts`.`id`, `memberships`.`Role` FROM `accounts` CROSS JOIN `app`.`memberships`",
                "SELECT [accounts].[id], [memberships].[Role] FROM [accounts] CROSS JOIN [app].[memberships]"
            )
        );
        SqlAssert.Compare(
            "PostgreSQL",
            "SELECT \"accounts\".\"id\", \"memberships\".\"Role\" FROM \"accounts\" FULL JOIN \"app\".\"memberships\" ON \"memberships\".\"AccountId\" = \"accounts\".\"id\"",
            Db<PostgreSqlDialect>()
                .From(Account.Table)
                .FullJoin(Membership.Table, (a, m) => Eq(m.AccountId, a.Id))
                .Select((a, m) => (a.Id, m.Role))
                .ToSql()
        );
    }

    /// <summary>
    /// A table with an alias is written <c>"table" AS "alias"</c> and its columns <c>"alias"."column"</c>, so a table can be
    /// joined to itself (design §5.2).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - <see cref="Account"/>, and the same table as <c>b</c>.
    ///
    /// Steps:
    /// - Render a statement from an aliased table, and a self-join with the second use aliased.
    ///
    /// Expected:
    /// - <c>FROM "accounts" AS "a"</c> with <c>"a"."id"</c>; <c>INNER JOIN "accounts" AS "b" ON "b"."id" = "accounts"."id"</c>.
    /// </remarks>
    [Fact]
    public void As_AliasedTable_QualifiesColumnsWithAlias()
    {
        SqlAssert.Compare(
            "SQLite",
            "SELECT \"a\".\"id\" FROM \"accounts\" AS \"a\"",
            Db<SqliteDialect>().From(Account.Table.As("a")).Select(a => a.Id).ToSql()
        );
        SqlAssert.Compare(
            "SQL Server",
            "SELECT [b].[email] FROM [accounts] INNER JOIN [accounts] AS [b] ON [b].[id] = [accounts].[id]",
            Db<SqlServerDialect>().From(Account.Table).InnerJoin(Account.Table.As("b"), (a, b) => Eq(b.Id, a.Id)).Select((a, b) => b.Email).ToSql()
        );
    }

    /// <summary>
    /// A table used twice without an alias is rejected when it is joined, because its columns could not be told apart
    /// (design §5.2, §14.3).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - <see cref="Account"/>.
    ///
    /// Steps:
    /// - Join it to itself without an alias, then with an alias equal to the table's name.
    ///
    /// Expected:
    /// - <see cref="InvalidOperationException"/> both times, naming <c>accounts</c>.
    /// </remarks>
    [Fact]
    public void Join_SameTableWithoutAlias_Throws()
    {
        var plain = Assert.Throws<InvalidOperationException>(() =>
            Db<SqliteDialect>().From(Account.Table).InnerJoin(Account.Table, (a, b) => Eq(b.Id, a.Id))
        );
        var sameName = Assert.Throws<InvalidOperationException>(() =>
            Db<SqliteDialect>().From(Account.Table.As("x")).CrossJoin(Account.Table.As("x"))
        );

        Assert.Contains("accounts", plain.Message, StringComparison.Ordinal);
        Assert.Contains("x", sameName.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// A statement can join up to eight tables, and its lambdas get all eight column sets (design §5.1).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - <see cref="Account"/> under eight aliases.
    ///
    /// Steps:
    /// - Cross join the eight and select a column of the first and the last.
    ///
    /// Expected:
    /// - Seven <c>CROSS JOIN</c>s and the two columns.
    /// </remarks>
    [Fact]
    public void CrossJoin_EightTables_Renders()
    {
        SqlStatement statement = Db<SqliteDialect>()
            .From(Account.Table.As("t1"))
            .CrossJoin(Account.Table.As("t2"))
            .CrossJoin(Account.Table.As("t3"))
            .CrossJoin(Account.Table.As("t4"))
            .CrossJoin(Account.Table.As("t5"))
            .CrossJoin(Account.Table.As("t6"))
            .CrossJoin(Account.Table.As("t7"))
            .CrossJoin(Account.Table.As("t8"))
            .Select((t1, t2, t3, t4, t5, t6, t7, t8) => (t1.Id, t8.Id))
            .ToSql();

        Assert.Equal(
            "SELECT \"t1\".\"id\", \"t8\".\"id\" FROM \"accounts\" AS \"t1\""
                + string.Concat(Enumerable.Range(2, 7).Select(i => $" CROSS JOIN \"accounts\" AS \"t{i}\"")),
            statement.Text
        );
    }

    private static Database<TDialect> Db<TDialect>()
        where TDialect : IDialect => new(new FakeConnection());

    private sealed class Inner : IQueryCase
    {
        public SqlStatement Build<TDialect>(Database<TDialect> db)
            where TDialect : IDialect =>
            db.From(Account.Table).InnerJoin(Membership.Table, (a, m) => Eq(m.AccountId, a.Id)).Select((a, m) => (a.Email, m.Role)).ToSql();
    }

    private sealed class Left : IQueryCase
    {
        public SqlStatement Build<TDialect>(Database<TDialect> db)
            where TDialect : IDialect =>
            db.From(Account.Table)
                .LeftJoin(Membership.Table, (a, m) => And(Eq(m.AccountId, a.Id), Eq(m.Role, "admin")))
                .Where((a, m) => Gt(a.LoginCount, 1))
                .Select((a, m) => a.Id)
                .ToSql();
    }

    private sealed class Right : IQueryCase
    {
        public SqlStatement Build<TDialect>(Database<TDialect> db)
            where TDialect : IDialect =>
            db.From(Account.Table).RightJoin(Membership.Table, (a, m) => Eq(m.AccountId, a.Id)).Select((a, m) => m.Role).ToSql();
    }

    private sealed class Cross : IQueryCase
    {
        public SqlStatement Build<TDialect>(Database<TDialect> db)
            where TDialect : IDialect => db.From(Account.Table).CrossJoin(Membership.Table).Select((a, m) => (a.Id, m.Role)).ToSql();
    }
}
