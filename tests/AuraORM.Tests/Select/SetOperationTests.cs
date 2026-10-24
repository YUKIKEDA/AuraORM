using static AuraORM.Sql;

namespace AuraORM.Tests;

public sealed class SetOperationTests
{
    private static readonly (SetOperator Operator, string Sql)[] Others =
    [
        (SetOperator.UnionAll, "UNION ALL"),
        (SetOperator.Intersect, "INTERSECT"),
        (SetOperator.Except, "EXCEPT"),
    ];

    /// <summary>
    /// Each operator writes its keyword between the operands, without parentheses, and the parameters are numbered across
    /// the operands in the order they appear (design §5.10, §5.12).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - <see cref="Account"/> and <see cref="Membership"/>.
    ///
    /// Steps:
    /// - Render three statements combined with <c>Union</c>, with a value in each; render <c>UnionAll</c>, <c>Intersect</c>, and
    ///   <c>Except</c> of two statements; on each dialect.
    ///
    /// Expected:
    /// - <c>SELECT … WHERE … &gt; @p0 UNION SELECT … WHERE … = @p1 UNION SELECT … WHERE … &lt; @p2</c>, and each keyword.
    /// </remarks>
    [Fact]
    public void Operators_WriteTheirKeywords()
    {
        SqlAssert.Statement(
            new ThreeUnions(),
            new(
                "SELECT \"accounts\".\"id\" FROM \"accounts\" WHERE \"accounts\".\"login_count\" > @p0 UNION SELECT \"memberships\".\"AccountId\" FROM \"app\".\"memberships\" WHERE \"memberships\".\"Role\" = @p1 UNION SELECT \"accounts\".\"id\" FROM \"accounts\" WHERE \"accounts\".\"login_count\" < @p2",
                "SELECT \"accounts\".\"id\" FROM \"accounts\" WHERE \"accounts\".\"login_count\" > $1 UNION SELECT \"memberships\".\"AccountId\" FROM \"app\".\"memberships\" WHERE \"memberships\".\"Role\" = $2 UNION SELECT \"accounts\".\"id\" FROM \"accounts\" WHERE \"accounts\".\"login_count\" < $3",
                "SELECT `accounts`.`id` FROM `accounts` WHERE `accounts`.`login_count` > @p0 UNION SELECT `memberships`.`AccountId` FROM `app`.`memberships` WHERE `memberships`.`Role` = @p1 UNION SELECT `accounts`.`id` FROM `accounts` WHERE `accounts`.`login_count` < @p2",
                "SELECT [accounts].[id] FROM [accounts] WHERE [accounts].[login_count] > @p0 UNION SELECT [memberships].[AccountId] FROM [app].[memberships] WHERE [memberships].[Role] = @p1 UNION SELECT [accounts].[id] FROM [accounts] WHERE [accounts].[login_count] < @p2"
            ),
            5,
            "admin",
            1
        );
        foreach ((SetOperator op, string sql) in Others)
        {
            SqlAssert.Statement(
                new TwoOperands(op),
                new(
                    $"SELECT \"accounts\".\"id\" FROM \"accounts\" {sql} SELECT \"memberships\".\"AccountId\" FROM \"app\".\"memberships\"",
                    $"SELECT \"accounts\".\"id\" FROM \"accounts\" {sql} SELECT \"memberships\".\"AccountId\" FROM \"app\".\"memberships\"",
                    $"SELECT `accounts`.`id` FROM `accounts` {sql} SELECT `memberships`.`AccountId` FROM `app`.`memberships`",
                    $"SELECT [accounts].[id] FROM [accounts] {sql} SELECT [memberships].[AccountId] FROM [app].[memberships]"
                )
            );
        }
    }

    /// <summary>
    /// <c>OrderBy</c>, <c>Limit</c>, and <c>Offset</c> after a set operation apply to the whole compound SELECT, and the keys are
    /// the positions of the items (design §5.10).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - <see cref="Account"/> and <see cref="Membership"/>, each selecting (an email or a role, an id).
    ///
    /// Steps:
    /// - Render the union ordered by the second item descending and the first ascending, limited to 10 rows after 5, on each
    ///   dialect.
    ///
    /// Expected:
    /// - <c>… UNION … ORDER BY 2 DESC, 1 ASC LIMIT @p0 OFFSET @p1</c>, and <c>OFFSET @p0 ROWS FETCH NEXT @p1 ROWS ONLY</c> on SQL
    ///   Server.
    /// </remarks>
    [Fact]
    public void OrderByAndLimit_ApplyToTheWholeSetOperation()
    {
        SqlAssert.Statement(
            new OrderedUnion(),
            new(
                "SELECT \"accounts\".\"email\", \"accounts\".\"id\" FROM \"accounts\" UNION SELECT \"memberships\".\"Role\", \"memberships\".\"AccountId\" FROM \"app\".\"memberships\" ORDER BY 2 DESC, 1 ASC LIMIT @p0 OFFSET @p1",
                "SELECT \"accounts\".\"email\", \"accounts\".\"id\" FROM \"accounts\" UNION SELECT \"memberships\".\"Role\", \"memberships\".\"AccountId\" FROM \"app\".\"memberships\" ORDER BY 2 DESC, 1 ASC LIMIT $1 OFFSET $2",
                "SELECT `accounts`.`email`, `accounts`.`id` FROM `accounts` UNION SELECT `memberships`.`Role`, `memberships`.`AccountId` FROM `app`.`memberships` ORDER BY 2 DESC, 1 ASC LIMIT @p0 OFFSET @p1",
                "SELECT [accounts].[email], [accounts].[id] FROM [accounts] UNION SELECT [memberships].[Role], [memberships].[AccountId] FROM [app].[memberships] ORDER BY 2 DESC, 1 ASC OFFSET @p0 ROWS FETCH NEXT @p1 ROWS ONLY"
            )
            {
                SqlServerParameters = [5, 10],
            },
            10,
            5
        );
    }

    /// <summary>
    /// Grouped and <c>SelectDistinct</c> statements are operands too, and a set operation is a subquery of <c>In</c>
    /// (design §5.10, §5.11).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - <see cref="Account"/> and <see cref="Membership"/>.
    ///
    /// Steps:
    /// - Render a grouped count unioned with an ungrouped count; a <c>SelectDistinct</c> excepted from a projection; and
    ///   accounts whose id is in a union of two subqueries; on PostgreSQL.
    ///
    /// Expected:
    /// - The operands as written, and <c>IN (SELECT … UNION SELECT …)</c>.
    /// </remarks>
    [Fact]
    public void GroupedDistinctAndSubquery_Operands()
    {
        var db = new Database<PostgreSqlDialect>(new FakeConnection());

        SqlStatement grouped = db.From(Account.Table)
            .GroupBy(a => a.Status)
            .Select((k, a) => Count())
            .Union(db.From(Membership.Table).Select(m => Count()))
            .ToSql();
        SqlStatement distinct = db.From(Account.Table).SelectDistinct(a => a.Email).Except(db.From(Membership.Table).Select(m => m.Role)).ToSql();
        SqlStatement inUnion = db.From(Account.Table)
            .Where(a => In(a.Id, db.From(Membership.Table).Select(m => m.AccountId).Union(db.From(Account.Table.As("o")).Select(o => o.Id))))
            .Select(a => a.Email)
            .ToSql();

        Assert.Equal(
            "SELECT COUNT(*) FROM \"accounts\" GROUP BY \"accounts\".\"status\" UNION SELECT COUNT(*) FROM \"app\".\"memberships\"",
            grouped.Text
        );
        Assert.Equal(
            "SELECT DISTINCT \"accounts\".\"email\" FROM \"accounts\" EXCEPT SELECT \"memberships\".\"Role\" FROM \"app\".\"memberships\"",
            distinct.Text
        );
        Assert.Equal(
            "SELECT \"accounts\".\"email\" FROM \"accounts\" WHERE \"accounts\".\"id\" IN (SELECT \"memberships\".\"AccountId\" FROM \"app\".\"memberships\" UNION SELECT \"o\".\"id\" FROM \"accounts\" AS \"o\")",
            inUnion.Text
        );
    }

    /// <summary>
    /// <c>OrderBy</c> after a set operation whose projection holds a whole row throws when it is called, because the row spreads
    /// over several columns and its items have no positions (design §5.10, §14.3).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - <see cref="Account"/>.
    ///
    /// Steps:
    /// - Union two whole rows and order by the row; union two tuples of (row, id) and order by the id.
    ///
    /// Expected:
    /// - <see cref="InvalidOperationException"/> for both.
    /// </remarks>
    [Fact]
    public void OrderBy_ProjectionWithARow_Throws()
    {
        var db = new Database<SqliteDialect>(new FakeConnection());

        Assert.Throws<InvalidOperationException>(() =>
            db.From(Account.Table).Select(a => a).Union(db.From(Account.Table).Select(a => a)).OrderBy(s => [Asc(s)])
        );
        Assert.Throws<InvalidOperationException>(() =>
            db.From(Account.Table).Select(a => (a, a.Id)).Union(db.From(Account.Table).Select(a => (a, a.Id))).OrderBy(s => [Asc(s.Item2)])
        );
    }

    /// <summary>
    /// SQLite combines at most 500 statements in one compound SELECT, so a set operation of more throws when it is built; the
    /// other dialects have no such check (design §5.10, §12.4, §14.3).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - <see cref="Account"/>.
    ///
    /// Steps:
    /// - Union 500 and 501 statements on SQLite, and 501 on PostgreSQL.
    ///
    /// Expected:
    /// - 500 renders; 501 throws <see cref="ArgumentException"/> on SQLite and renders on PostgreSQL.
    /// </remarks>
    [Fact]
    public void ManyOperands_SqliteLimit_Throws()
    {
        Assert.Equal(499, CountUnions(Union(new Database<SqliteDialect>(new FakeConnection()), 500).ToSql().Text));
        Assert.Throws<ArgumentException>(() => Union(new Database<SqliteDialect>(new FakeConnection()), 501).ToSql());
        Assert.Equal(500, CountUnions(Union(new Database<PostgreSqlDialect>(new FakeConnection()), 501).ToSql().Text));
    }

    private static SelectUnion<TDialect, Executable, long, PerRow> Union<TDialect>(Database<TDialect> db, int operands)
        where TDialect : IDialect
    {
        SelectUnion<TDialect, Executable, long, PerRow> union = db.From(Account.Table)
            .Select(a => a.Id)
            .Union(db.From(Account.Table).Select(a => a.Id));
        for (int i = 2; i < operands; i++)
        {
            union = union.Union(db.From(Account.Table).Select(a => a.Id));
        }

        return union;
    }

    private static int CountUnions(string sql) => sql.Split(" UNION ").Length - 1;

    private sealed class ThreeUnions : IQueryCase
    {
        public SqlStatement Build<TDialect>(Database<TDialect> db)
            where TDialect : IDialect =>
            db.From(Account.Table)
                .Where(a => Gt(a.LoginCount, 5))
                .Select(a => a.Id)
                .Union(db.From(Membership.Table).Where(m => Eq(m.Role, "admin")).Select(m => m.AccountId))
                .Union(db.From(Account.Table).Where(a => Lt(a.LoginCount, 1)).Select(a => a.Id))
                .ToSql();
    }

    private sealed class TwoOperands(SetOperator op) : IQueryCase
    {
        public SqlStatement Build<TDialect>(Database<TDialect> db)
            where TDialect : IDialect
        {
            var left = db.From(Account.Table).Select(a => a.Id);
            var right = db.From(Membership.Table).Select(m => m.AccountId);
            return op switch
            {
                SetOperator.UnionAll => left.UnionAll(right).ToSql(),
                SetOperator.Intersect => left.Intersect(right).ToSql(),
                _ => left.Except(right).ToSql(),
            };
        }
    }

    private sealed class OrderedUnion : IQueryCase
    {
        public SqlStatement Build<TDialect>(Database<TDialect> db)
            where TDialect : IDialect =>
            db.From(Account.Table)
                .Select(a => (a.Email, a.Id))
                .Union(db.From(Membership.Table).Select(m => (m.Role, m.AccountId)))
                .OrderBy(s => [Desc(s.Item2), Asc(s.Item1)])
                .Limit(10)
                .Offset(5)
                .ToSql();
    }
}
