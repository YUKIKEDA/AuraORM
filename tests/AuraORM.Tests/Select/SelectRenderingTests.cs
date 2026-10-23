using static AuraORM.Sql;

namespace AuraORM.Tests;

public sealed class SelectRenderingTests
{
    private const string AccountColumns =
        "\"accounts\".\"id\", \"accounts\".\"email\", \"accounts\".\"display_name\", \"accounts\".\"status\", \"accounts\".\"created_at\", \"accounts\".\"login_count\", \"accounts\".\"balance\"";

    /// <summary>
    /// A whole row selects every column in declaration order; <c>SELECT *</c> is never written (design §5.6).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - <see cref="Account"/>, with seven columns.
    ///
    /// Steps:
    /// - Render <c>From(Account.Table).Select(a =&gt; a)</c> on each dialect.
    ///
    /// Expected:
    /// - The seven qualified columns, then <c>FROM</c> the table.
    /// </remarks>
    [Fact]
    public void Select_WholeRow_ListsEveryColumn()
    {
        SqlAssert.Statement(
            new WholeRow(),
            new(
                $"SELECT {AccountColumns} FROM \"accounts\"",
                $"SELECT {AccountColumns} FROM \"accounts\"",
                $"SELECT {AccountColumns.Replace('"', '`')} FROM `accounts`",
                $"SELECT {Brackets(AccountColumns)} FROM [accounts]"
            )
        );
    }

    /// <summary>
    /// <c>WHERE</c>, the projection, <c>ORDER BY</c>, the limit, and the offset are written in that order, with the limit and
    /// the offset as parameters, numbered in the order they appear (design §5.4, §5.8, §5.9, §5.12).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - A chain with a compound condition, one column, two ordering keys, a limit of 10, and an offset of 20.
    ///
    /// Steps:
    /// - Render it on each dialect.
    ///
    /// Expected:
    /// - <c>LIMIT … OFFSET …</c> on SQLite, PostgreSQL, MySQL, and MariaDB, with the parameters 3, 10, 20.
    /// - <c>OFFSET … ROWS FETCH NEXT … ROWS ONLY</c> on SQL Server, with the parameters 3, 20, 10.
    /// </remarks>
    [Fact]
    public void Select_WhereOrderLimitOffset_WritesClausesInStandardOrder()
    {
        SqlAssert.Statement(
            new Paged(),
            new(
                "SELECT \"accounts\".\"email\" FROM \"accounts\" WHERE (\"accounts\".\"login_count\" > @p0 AND \"accounts\".\"display_name\" IS NOT NULL) ORDER BY \"accounts\".\"created_at\" DESC, \"accounts\".\"id\" ASC LIMIT @p1 OFFSET @p2",
                "SELECT \"accounts\".\"email\" FROM \"accounts\" WHERE (\"accounts\".\"login_count\" > $1 AND \"accounts\".\"display_name\" IS NOT NULL) ORDER BY \"accounts\".\"created_at\" DESC, \"accounts\".\"id\" ASC LIMIT $2 OFFSET $3",
                "SELECT `accounts`.`email` FROM `accounts` WHERE (`accounts`.`login_count` > @p0 AND `accounts`.`display_name` IS NOT NULL) ORDER BY `accounts`.`created_at` DESC, `accounts`.`id` ASC LIMIT @p1 OFFSET @p2",
                "SELECT [accounts].[email] FROM [accounts] WHERE ([accounts].[login_count] > @p0 AND [accounts].[display_name] IS NOT NULL) ORDER BY [accounts].[created_at] DESC, [accounts].[id] ASC OFFSET @p1 ROWS FETCH NEXT @p2 ROWS ONLY"
            )
            {
                SqlServerParameters = [3, 20, 10],
            },
            3,
            10,
            20
        );
    }

    /// <summary>
    /// <c>SelectDistinct</c> writes <c>SELECT DISTINCT</c>, and a tuple selects its items in order (design §5.6, §5.7).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - A tuple of the status and the display name.
    ///
    /// Steps:
    /// - Render <c>SelectDistinct</c> of it on each dialect.
    ///
    /// Expected:
    /// - <c>SELECT DISTINCT</c> with the two columns.
    /// </remarks>
    [Fact]
    public void SelectDistinct_Tuple_WritesDistinctItems()
    {
        SqlAssert.Statement(
            new DistinctTuple(),
            new(
                "SELECT DISTINCT \"accounts\".\"status\", \"accounts\".\"display_name\" FROM \"accounts\"",
                "SELECT DISTINCT \"accounts\".\"status\", \"accounts\".\"display_name\" FROM \"accounts\"",
                "SELECT DISTINCT `accounts`.`status`, `accounts`.`display_name` FROM `accounts`",
                "SELECT DISTINCT [accounts].[status], [accounts].[display_name] FROM [accounts]"
            )
        );
    }

    /// <summary>
    /// A result type's projection selects its members in declaration order, without aliases (design §5.6).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - <see cref="NameTotal.RowProjection"/> of the email and the balance.
    ///
    /// Steps:
    /// - Render it on each dialect.
    ///
    /// Expected:
    /// - The two columns, in member order.
    /// </remarks>
    [Fact]
    public void Select_ResultProjection_SelectsMembersInOrder()
    {
        SqlAssert.Statement(
            new ResultType(),
            new(
                "SELECT \"accounts\".\"email\", \"accounts\".\"balance\" FROM \"accounts\"",
                "SELECT \"accounts\".\"email\", \"accounts\".\"balance\" FROM \"accounts\"",
                "SELECT `accounts`.`email`, `accounts`.`balance` FROM `accounts`",
                "SELECT [accounts].[email], [accounts].[balance] FROM [accounts]"
            )
        );
    }

    /// <summary>
    /// A group-level projection without <c>GROUP BY</c> aggregates the whole table (design §4.5).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - A tuple of <c>Count()</c> and <c>Max(LoginCount)</c>.
    ///
    /// Steps:
    /// - Render it on each dialect.
    ///
    /// Expected:
    /// - The two aggregates and no <c>GROUP BY</c>; SQL Server counts with <c>COUNT_BIG</c>.
    /// </remarks>
    [Fact]
    public void Select_GroupLevelProjection_AggregatesWholeTable()
    {
        SqlAssert.Statement(
            new WholeTableAggregate(),
            new(
                "SELECT COUNT(*), MAX(\"accounts\".\"login_count\") FROM \"accounts\"",
                "SELECT COUNT(*), MAX(\"accounts\".\"login_count\") FROM \"accounts\"",
                "SELECT COUNT(*), MAX(`accounts`.`login_count`) FROM `accounts`",
                "SELECT COUNT_BIG(*), MAX([accounts].[login_count]) FROM [accounts]"
            )
        );
    }

    /// <summary>
    /// A table with a schema is written <c>"schema"."table"</c> in <c>FROM</c>, and its columns are qualified by the table name
    /// (design §5.2).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - <see cref="Membership"/>, in the schema <c>app</c>.
    ///
    /// Steps:
    /// - Render a projection of its role on each dialect.
    ///
    /// Expected:
    /// - <c>FROM "app"."memberships"</c> and the column <c>"memberships"."Role"</c>.
    /// </remarks>
    [Fact]
    public void From_TableWithSchema_WritesSchema()
    {
        SqlAssert.Statement(
            new WithSchema(),
            new(
                "SELECT \"memberships\".\"Role\" FROM \"app\".\"memberships\"",
                "SELECT \"memberships\".\"Role\" FROM \"app\".\"memberships\"",
                "SELECT `memberships`.`Role` FROM `app`.`memberships`",
                "SELECT [memberships].[Role] FROM [app].[memberships]"
            )
        );
    }

    /// <summary>
    /// A limit without <c>ORDER BY</c> is written <c>LIMIT n</c> on the dialects that accept it (design §5.9).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - SQLite, PostgreSQL, MySQL, and MariaDB, which implement <see cref="IUnorderedLimitDialect"/>.
    ///
    /// Steps:
    /// - Render <c>Select(a =&gt; a.Id).Limit(5)</c> on each.
    ///
    /// Expected:
    /// - <c>LIMIT</c> with the parameter 5.
    /// </remarks>
    [Fact]
    public void Limit_WithoutOrderBy_WritesLimit()
    {
        const string Tail = " FROM \"accounts\" LIMIT ";
        SqlAssert.Compare(
            "SQLite",
            "SELECT \"accounts\".\"id\"" + Tail + "@p0",
            Db<SqliteDialect>().From(Account.Table).Select(a => a.Id).Limit(5).ToSql(),
            5
        );
        SqlAssert.Compare(
            "PostgreSQL",
            "SELECT \"accounts\".\"id\"" + Tail + "$1",
            Db<PostgreSqlDialect>().From(Account.Table).Select(a => a.Id).Limit(5).ToSql(),
            5
        );
        SqlAssert.Compare(
            "MySQL",
            "SELECT `accounts`.`id` FROM `accounts` LIMIT @p0",
            Db<MySqlDialect>().From(Account.Table).Select(a => a.Id).Limit(5).ToSql(),
            5
        );
        SqlAssert.Compare(
            "MariaDB",
            "SELECT `accounts`.`id` FROM `accounts` LIMIT @p0",
            Db<MariaDbDialect>().From(Account.Table).Select(a => a.Id).Limit(5).ToSql(),
            5
        );
    }

    /// <summary>
    /// An offset without a limit is written <c>OFFSET m</c> on PostgreSQL, and <c>OFFSET m ROWS</c> after <c>ORDER BY</c> on SQL
    /// Server; SQL Server writes a limit after <c>ORDER BY</c> with <c>OFFSET 0 ROWS</c> (design §5.9).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - PostgreSQL and SQL Server.
    ///
    /// Steps:
    /// - Render an offset of 7 without a limit on both, ordered and unordered on PostgreSQL, and a limit of 5 after
    ///   <c>ORDER BY</c> on SQL Server.
    ///
    /// Expected:
    /// - The SQL of each case, with the count as the only parameter.
    /// </remarks>
    [Fact]
    public void Offset_WithoutLimit_WritesOffsetOnly()
    {
        SqlAssert.Compare(
            "PostgreSQL",
            "SELECT \"accounts\".\"id\" FROM \"accounts\" OFFSET $1",
            Db<PostgreSqlDialect>().From(Account.Table).Select(a => a.Id).Offset(7).ToSql(),
            7
        );
        SqlAssert.Compare(
            "PostgreSQL",
            "SELECT \"accounts\".\"id\" FROM \"accounts\" ORDER BY \"accounts\".\"id\" ASC OFFSET $1",
            Db<PostgreSqlDialect>().From(Account.Table).Select(a => a.Id).OrderBy(a => [Asc(a.Id)]).Offset(7).ToSql(),
            7
        );
        SqlAssert.Compare(
            "SQL Server",
            "SELECT [accounts].[id] FROM [accounts] ORDER BY [accounts].[id] ASC OFFSET @p0 ROWS",
            Db<SqlServerDialect>().From(Account.Table).Select(a => a.Id).OrderBy(a => [Asc(a.Id)]).Offset(7).ToSql(),
            7
        );
        SqlAssert.Compare(
            "SQL Server",
            "SELECT [accounts].[id] FROM [accounts] ORDER BY [accounts].[id] ASC OFFSET 0 ROWS FETCH NEXT @p0 ROWS ONLY",
            Db<SqlServerDialect>().From(Account.Table).Select(a => a.Id).OrderBy(a => [Asc(a.Id)]).Limit(5).ToSql(),
            5
        );
    }

    /// <summary>
    /// A stage can be reused: each call makes a new statement and leaves the stage as it was.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - One <c>From</c> stage.
    ///
    /// Steps:
    /// - Build a statement with <c>WHERE</c> from it, then one without.
    ///
    /// Expected:
    /// - The second statement has no <c>WHERE</c> and no parameter.
    /// </remarks>
    [Fact]
    public void From_ReusedStage_KeepsStatementsApart()
    {
        SelectFrom<SqliteDialect, Account.Columns, Account.NullableColumns> from = Db<SqliteDialect>().From(Account.Table);

        SqlStatement filtered = from.Where(a => Eq(a.Id, 1L)).Select(a => a.Id).ToSql();
        SqlStatement plain = from.Select(a => a.Id).ToSql();

        Assert.Equal("SELECT \"accounts\".\"id\" FROM \"accounts\" WHERE \"accounts\".\"id\" = @p0", filtered.Text);
        Assert.Equal("SELECT \"accounts\".\"id\" FROM \"accounts\"", plain.Text);
        Assert.Empty(plain.Parameters);
    }

    /// <summary>
    /// A negative row count, and <c>ORDER BY</c> without keys, throw before any SQL is written (design §14.3).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - A projected statement on SQLite.
    ///
    /// Steps:
    /// - Call <c>Limit(-1)</c> after <c>ORDER BY</c>, <c>Offset(-1)</c> after a limit, and <c>OrderBy</c> with an empty array.
    ///
    /// Expected:
    /// - <see cref="ArgumentOutOfRangeException"/> for the counts, <see cref="ArgumentException"/> for the empty keys.
    /// </remarks>
    [Fact]
    public void Builder_InvalidArguments_Throw()
    {
        SelectProjected<SqliteDialect, long, PerRow, Account.Columns> projected = Db<SqliteDialect>().From(Account.Table).Select(a => a.Id);

        Assert.Throws<ArgumentOutOfRangeException>(() => projected.OrderBy(a => [Asc(a.Id)]).Limit(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => projected.Limit(1).Offset(-1));
        Assert.Throws<ArgumentException>(() => projected.OrderBy(a => []));
    }

    private static Database<TDialect> Db<TDialect>()
        where TDialect : IDialect => new(new FakeConnection());

    private static string Brackets(string quoted)
    {
        var builder = new System.Text.StringBuilder(quoted.Length);
        bool open = true;
        foreach (char c in quoted)
        {
            if (c == '"')
            {
                builder.Append(open ? '[' : ']');
                open = !open;
            }
            else
            {
                builder.Append(c);
            }
        }

        return builder.ToString();
    }

    private sealed class WholeRow : IQueryCase
    {
        public SqlStatement Build<TDialect>(Database<TDialect> db)
            where TDialect : IDialect => db.From(Account.Table).Select(a => a).ToSql();
    }

    private sealed class Paged : IQueryCase
    {
        public SqlStatement Build<TDialect>(Database<TDialect> db)
            where TDialect : IDialect =>
            db.From(Account.Table)
                .Where(a => And(Gt(a.LoginCount, 3), IsNotNull(a.DisplayName)))
                .Select(a => a.Email)
                .OrderBy(a => [Desc(a.CreatedAt), Asc(a.Id)])
                .Limit(10)
                .Offset(20)
                .ToSql();
    }

    private sealed class DistinctTuple : IQueryCase
    {
        public SqlStatement Build<TDialect>(Database<TDialect> db)
            where TDialect : IDialect => db.From(Account.Table).SelectDistinct(a => (a.Status, a.DisplayName)).ToSql();
    }

    private sealed class ResultType : IQueryCase
    {
        public SqlStatement Build<TDialect>(Database<TDialect> db)
            where TDialect : IDialect =>
            db.From(Account.Table).Select(a => new NameTotal.RowProjection { Name = a.Email, Total = a.Balance }).ToSql();
    }

    private sealed class WholeTableAggregate : IQueryCase
    {
        public SqlStatement Build<TDialect>(Database<TDialect> db)
            where TDialect : IDialect => db.From(Account.Table).Select(a => (Count(), Max(a.LoginCount))).ToSql();
    }

    private sealed class WithSchema : IQueryCase
    {
        public SqlStatement Build<TDialect>(Database<TDialect> db)
            where TDialect : IDialect => db.From(Membership.Table).Select(m => m.Role).ToSql();
    }
}
