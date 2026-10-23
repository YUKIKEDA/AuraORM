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
        SelectFrom<SqliteDialect, Executable, Account.Columns, Account.NullableColumns> from = Db<SqliteDialect>().From(Account.Table);

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
        SelectProjected<SqliteDialect, Executable, long, PerRow, Account.Columns> projected = Db<SqliteDialect>()
            .From(Account.Table)
            .Select(a => a.Id);

        Assert.Throws<ArgumentOutOfRangeException>(() => projected.OrderBy(a => [Asc(a.Id)]).Limit(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => projected.Limit(1).Offset(-1));
        Assert.Throws<ArgumentException>(() => projected.OrderBy(a => []));
    }

    /// <summary>
    /// <c>Limit(0)</c> is rejected on SQL Server before any SQL is written, because <c>FETCH NEXT 0 ROWS</c> fails there; the
    /// other dialects write <c>LIMIT 0</c> (design §5.9, §14.3).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - An ordered statement on SQL Server and on SQLite.
    ///
    /// Steps:
    /// - Call <c>Limit(0)</c> on each.
    ///
    /// Expected:
    /// - SQL Server: <see cref="ArgumentOutOfRangeException"/>. SQLite: <c>LIMIT @p0</c> with the parameter 0.
    /// </remarks>
    [Fact]
    public void Limit_ZeroOnSqlServer_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Db<SqlServerDialect>().From(Account.Table).Select(a => a.Id).OrderBy(a => [Asc(a.Id)]).Limit(0)
        );

        SqlAssert.Compare(
            "SQLite",
            "SELECT \"accounts\".\"id\" FROM \"accounts\" ORDER BY \"accounts\".\"id\" ASC LIMIT @p0",
            Db<SqliteDialect>().From(Account.Table).Select(a => a.Id).OrderBy(a => [Asc(a.Id)]).Limit(0).ToSql(),
            0
        );
    }

    /// <summary>
    /// A column taken out of the lambda of one query and used in a query of another table is rejected when the statement is
    /// built, not sent as SQL that names a table outside <c>FROM</c> (design §14.3).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - The <c>Id</c> column of <see cref="Account"/>, kept in a variable from the lambda of a query on accounts.
    ///
    /// Steps:
    /// - Use it in the projection of a query on <see cref="Membership"/>, then in its <c>WHERE</c>, then in another query on
    ///   <see cref="Account"/> (the same table, another occurrence).
    ///
    /// Expected:
    /// - <see cref="InvalidOperationException"/> from <c>ToSql()</c> in all three, naming the column.
    /// </remarks>
    [Fact]
    public void ToSql_ColumnOfAnotherQuery_Throws()
    {
        ReadOnlyColumn<long>? leaked = null;
        _ = Db<SqliteDialect>()
            .From(Account.Table)
            .Select(a =>
            {
                leaked = a.Id;
                return a.Id;
            })
            .ToSql();

        var inSelect = Assert.Throws<InvalidOperationException>(() => Db<SqliteDialect>().From(Membership.Table).Select(m => leaked!).ToSql());
        var inWhere = Assert.Throws<InvalidOperationException>(() =>
            Db<SqliteDialect>().From(Membership.Table).Where(m => Eq(leaked!, 1L)).Select(m => m.Role).ToSql()
        );

        var sameTable = Assert.Throws<InvalidOperationException>(() => Db<SqliteDialect>().From(Account.Table).Select(a => leaked!).ToSql());

        Assert.Contains("accounts.id", sameTable.Message, StringComparison.Ordinal);
        Assert.Contains("accounts.id", inSelect.Message, StringComparison.Ordinal);
        Assert.Contains("accounts.id", inWhere.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// <c>FirstAsync</c> reads one row and cancels the command before it closes the reader when rows are left;
    /// <c>ToListAsync</c> reads every row and does not cancel (design §12.2).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - A fake connection that returns three rows.
    ///
    /// Steps:
    /// - Run the same statement with <c>FirstAsync</c> and with <c>ToListAsync</c>.
    ///
    /// Expected:
    /// - 1 and [1, 2, 3]; <c>Cancel</c> called once for the first command, never for the second.
    /// </remarks>
    [Fact]
    public async Task FirstAsync_RowsLeft_CancelsBeforeClosing()
    {
        var connection = new FakeConnection { Rows = [1L, 2L, 3L] };
        SelectProjected<SqliteDialect, Executable, long, PerRow, Account.Columns> query = new Database<SqliteDialect>(connection)
            .From(Account.Table)
            .Select(a => a.Id);
        CancellationToken ct = TestContext.Current.CancellationToken;

        Assert.Equal(1L, await query.FirstAsync(ct));
        Assert.Equal([1L, 2L, 3L], await query.ToListAsync(ct));

        Assert.Equal([1, 0], connection.Commands.Select(c => c.CancelCount));
    }

    /// <summary>
    /// <c>Where</c> with the list form of <c>And</c> writes the conditions that are present, and no <c>WHERE</c> when none
    /// is (design §4.2, §5.4).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Filters built from two optional values, as a search form would.
    ///
    /// Steps:
    /// - Render the statement with both values, with one, and with none.
    ///
    /// Expected:
    /// - <c>WHERE (a AND b)</c>, <c>WHERE a</c>, and no <c>WHERE</c>; only the present values are parameters.
    /// </remarks>
    [Fact]
    public void Where_ListOfOptionalConditions_WritesOnlyPresentOnes()
    {
        static SqlStatement Search(string? email, int? minLogins) =>
            Db<SqliteDialect>()
                .From(Account.Table)
                .Where(a => And([email is null ? null : Like(a.Email, email), minLogins is null ? null : Ge(a.LoginCount, minLogins.Value)]))
                .Select(a => a.Id)
                .ToSql();

        SqlAssert.Compare(
            "SQLite",
            "SELECT \"accounts\".\"id\" FROM \"accounts\" WHERE (\"accounts\".\"email\" LIKE @p0 AND \"accounts\".\"login_count\" >= @p1)",
            Search("a%", 3),
            "a%",
            3
        );
        SqlAssert.Compare("SQLite", "SELECT \"accounts\".\"id\" FROM \"accounts\" WHERE \"accounts\".\"login_count\" >= @p0", Search(null, 3), 3);
        SqlAssert.Compare("SQLite", "SELECT \"accounts\".\"id\" FROM \"accounts\"", Search(null, null));
    }

    /// <summary>
    /// The <c>OrderBy</c> of a <c>SelectDistinct</c> names the selected items by their position in the select list, so the keys
    /// are items of the list on every dialect, even when an item has a parameter (design §5.7).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - A distinct expression with a parameter, and a distinct tuple of two columns.
    ///
    /// Steps:
    /// - Order the first by its item, and the second by its second item descending, then its first.
    ///
    /// Expected:
    /// - <c>ORDER BY 1 ASC</c> with one parameter; <c>ORDER BY 2 DESC, 1 ASC</c>.
    /// </remarks>
    [Fact]
    public void SelectDistinct_OrderBy_NamesItemsByPosition()
    {
        SqlAssert.Compare(
            "PostgreSQL",
            "SELECT DISTINCT (\"accounts\".\"login_count\" + $1) FROM \"accounts\" ORDER BY 1 ASC",
            Db<PostgreSqlDialect>().From(Account.Table).SelectDistinct(a => Add(a.LoginCount, 1)).OrderBy(s => [Asc(s)]).ToSql(),
            1
        );
        SqlAssert.Compare(
            "SQL Server",
            "SELECT DISTINCT [accounts].[status], [accounts].[email] FROM [accounts] ORDER BY 2 DESC, 1 ASC",
            Db<SqlServerDialect>().From(Account.Table).SelectDistinct(a => (a.Status, a.Email)).OrderBy(s => [Desc(s.Item2), Asc(s.Item1)]).ToSql()
        );
    }

    /// <summary>
    /// An <c>ORDER BY</c> key of decimals is rejected on SQLite, which stores decimals as text, and written on the other
    /// dialects (design §3.3, §14.3).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - The decimal column <c>Balance</c>.
    ///
    /// Steps:
    /// - Order by it on SQLite and on PostgreSQL.
    ///
    /// Expected:
    /// - SQLite: <see cref="InvalidOperationException"/> from <c>ToSql()</c>. PostgreSQL: <c>ORDER BY "accounts"."balance" ASC</c>.
    /// </remarks>
    [Fact]
    public void OrderBy_DecimalOnSqlite_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            Db<SqliteDialect>().From(Account.Table).Select(a => a.Id).OrderBy(a => [Asc(a.Balance)]).ToSql()
        );

        SqlAssert.Compare(
            "PostgreSQL",
            "SELECT \"accounts\".\"id\" FROM \"accounts\" ORDER BY \"accounts\".\"balance\" ASC",
            Db<PostgreSqlDialect>().From(Account.Table).Select(a => a.Id).OrderBy(a => [Asc(a.Balance)]).ToSql()
        );
    }

    /// <summary>
    /// A <see cref="SqlBuilder{TDialect}"/> renders the same SQL as a database, with no connection or provider (design §12.3).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - A builder and a database on SQL Server.
    ///
    /// Steps:
    /// - Render the same paged statement from both.
    ///
    /// Expected:
    /// - The same text and parameters.
    /// </remarks>
    [Fact]
    public void SqlBuilder_Statement_RendersLikeDatabase()
    {
        SqlStatement built = new SqlBuilder<SqlServerDialect>()
            .From(Account.Table)
            .Where(a => Gt(a.LoginCount, 2))
            .Select(a => a.Email)
            .OrderBy(a => [Asc(a.Id)])
            .Limit(5)
            .ToSql();
        SqlStatement run = Db<SqlServerDialect>()
            .From(Account.Table)
            .Where(a => Gt(a.LoginCount, 2))
            .Select(a => a.Email)
            .OrderBy(a => [Asc(a.Id)])
            .Limit(5)
            .ToSql();

        Assert.Equal(run.Text, built.Text);
        Assert.Equal(run.Parameters, built.Parameters);
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
