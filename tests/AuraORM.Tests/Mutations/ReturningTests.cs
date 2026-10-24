using static AuraORM.Sql;

namespace AuraORM.Tests;

public sealed class ReturningTests
{
    private static readonly Database<SqliteDialect> Sqlite = new(new FakeConnection());
    private static readonly Database<PostgreSqlDialect> PostgreSql = new(new FakeConnection());
    private static readonly Database<MariaDbDialect> MariaDb = new(new FakeConnection());
    private static readonly Database<SqlServerDialect> SqlServer = new(new FakeConnection());

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>
    /// <c>Returning</c> of an insert writes a trailing <c>RETURNING</c> with unqualified columns on SQLite, PostgreSQL, and
    /// MariaDB, and <c>OUTPUT INSERTED.…</c> before <c>VALUES</c> or <c>DEFAULT VALUES</c> on SQL Server (design §9).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - <see cref="Account"/> and <see cref="Note"/>.
    ///
    /// Steps:
    /// - Render an insert of an account returning its id; an insert of a note that sets nothing returning its id and its
    ///   priority plus one.
    ///
    /// Expected:
    /// - <c>INSERT INTO "accounts" ("email", "status") VALUES (@p0, @p1) RETURNING "id"</c>;
    ///   <c>INSERT INTO [accounts] ([email], [status]) OUTPUT INSERTED.[id] VALUES (@p0, @p1)</c>.
    /// - <c>INSERT INTO "notes" DEFAULT VALUES RETURNING "Id", ("Priority" + @p0)</c>; MariaDB
    ///   <c>INSERT INTO `notes` () VALUES () RETURNING `Id`, (`Priority` + @p0)</c>; SQL Server
    ///   <c>INSERT INTO [notes] OUTPUT INSERTED.[Id], (INSERTED.[Priority] + @p0) DEFAULT VALUES</c>.
    /// </remarks>
    [Fact]
    public void Insert_Returning_WritesTheColumnsOfTheDialect()
    {
        SqlAssert.Compare(
            "SQLite",
            "INSERT INTO \"accounts\" (\"email\", \"status\") VALUES (@p0, @p1) RETURNING \"id\"",
            InsertAccount(Sqlite).Returning(a => a.Id).ToSql(),
            "a@example.com",
            (short)1
        );
        SqlAssert.Compare(
            "PostgreSQL",
            "INSERT INTO \"accounts\" (\"email\", \"status\") VALUES ($1, $2) RETURNING \"id\"",
            InsertAccount(PostgreSql).Returning(a => a.Id).ToSql(),
            "a@example.com",
            (short)1
        );
        SqlAssert.Compare(
            "MariaDB",
            "INSERT INTO `accounts` (`email`, `status`) VALUES (@p0, @p1) RETURNING `id`",
            InsertAccount(MariaDb).Returning(a => a.Id).ToSql(),
            "a@example.com",
            (short)1
        );
        SqlAssert.Compare(
            "SQL Server",
            "INSERT INTO [accounts] ([email], [status]) OUTPUT INSERTED.[id] VALUES (@p0, @p1)",
            InsertAccount(SqlServer).Returning(a => a.Id).ToSql(),
            "a@example.com",
            (short)1
        );

        SqlAssert.Compare(
            "SQLite",
            "INSERT INTO \"notes\" DEFAULT VALUES RETURNING \"Id\", (\"Priority\" + @p0)",
            Sqlite.Insert(Note.Table).Values(new Note.Insert()).Returning(n => (n.Id, Add(n.Priority, 1))).ToSql(),
            1
        );
        SqlAssert.Compare(
            "MariaDB",
            "INSERT INTO `notes` () VALUES () RETURNING `Id`, (`Priority` + @p0)",
            MariaDb.Insert(Note.Table).Values(new Note.Insert()).Returning(n => (n.Id, Add(n.Priority, 1))).ToSql(),
            1
        );
        SqlAssert.Compare(
            "SQL Server",
            "INSERT INTO [notes] OUTPUT INSERTED.[Id], (INSERTED.[Priority] + @p0) DEFAULT VALUES",
            SqlServer.Insert(Note.Table).Values(new Note.Insert()).Returning(n => (n.Id, Add(n.Priority, 1))).ToSql(),
            1
        );
    }

    /// <summary>
    /// <c>Returning</c> of an update writes <c>RETURNING</c> after <c>WHERE</c>, or <c>OUTPUT INSERTED.…</c> between
    /// <c>SET</c> and <c>WHERE</c> on SQL Server; of a delete, <c>RETURNING</c> after <c>WHERE</c>, or <c>OUTPUT DELETED.…</c>
    /// between the table and <c>WHERE</c>; a whole row returns every column (design §9).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - <see cref="Account"/> and <see cref="Membership"/>.
    ///
    /// Steps:
    /// - Render an update of the login count where the id is 1, returning the id and the login count; a delete of the role
    ///   "guest" returning the whole membership.
    ///
    /// Expected:
    /// - <c>UPDATE "accounts" SET "login_count" = @p0 WHERE "accounts"."id" = @p1 RETURNING "id", "login_count"</c>;
    ///   <c>UPDATE [accounts] SET [login_count] = @p0 OUTPUT INSERTED.[id], INSERTED.[login_count] WHERE [accounts].[id] = @p1</c>.
    /// - <c>DELETE FROM "app"."memberships" WHERE "memberships"."Role" = @p0 RETURNING "AccountId", "Role", "joined_on"</c>;
    ///   <c>DELETE FROM [app].[memberships] OUTPUT DELETED.[AccountId], DELETED.[Role], DELETED.[joined_on] WHERE …</c>.
    /// </remarks>
    [Fact]
    public void UpdateAndDelete_Returning_WriteTheColumnsOfTheDialect()
    {
        SqlAssert.Compare(
            "SQLite",
            "UPDATE \"accounts\" SET \"login_count\" = @p0 WHERE \"accounts\".\"id\" = @p1 RETURNING \"id\", \"login_count\"",
            Sqlite.Update(Account.Table).Set(a => [Set(a.LoginCount, 0)]).Where(a => Eq(a.Id, 1L)).Returning(a => (a.Id, a.LoginCount)).ToSql(),
            0,
            1L
        );
        SqlAssert.Compare(
            "SQL Server",
            "UPDATE [accounts] SET [login_count] = @p0 OUTPUT INSERTED.[id], INSERTED.[login_count] WHERE [accounts].[id] = @p1",
            SqlServer.Update(Account.Table).Set(a => [Set(a.LoginCount, 0)]).Where(a => Eq(a.Id, 1L)).Returning(a => (a.Id, a.LoginCount)).ToSql(),
            0,
            1L
        );
        SqlAssert.Compare(
            "PostgreSQL",
            "DELETE FROM \"app\".\"memberships\" WHERE \"memberships\".\"Role\" = $1 RETURNING \"AccountId\", \"Role\", \"joined_on\"",
            PostgreSql.Delete(Membership.Table).Where(m => Eq(m.Role, "guest")).Returning(m => m).ToSql(),
            "guest"
        );
        SqlAssert.Compare(
            "MariaDB",
            "DELETE FROM `app`.`memberships` WHERE `memberships`.`Role` = @p0 RETURNING `AccountId`, `Role`, `joined_on`",
            MariaDb.Delete(Membership.Table).Where(m => Eq(m.Role, "guest")).Returning(m => m).ToSql(),
            "guest"
        );
        SqlAssert.Compare(
            "SQL Server",
            "DELETE FROM [app].[memberships] OUTPUT DELETED.[AccountId], DELETED.[Role], DELETED.[joined_on] WHERE [memberships].[Role] = @p0",
            SqlServer.Delete(Membership.Table).Where(m => Eq(m.Role, "guest")).Returning(m => m).ToSql(),
            "guest"
        );
    }

    /// <summary>
    /// A subquery in <c>Returning</c> that does not read the target is written with its own columns qualified, the target's
    /// too; one that reads the target throws on every dialect, and any subquery throws on SQL Server (design §9, §14.3).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - <see cref="Account"/> and <see cref="Membership"/>.
    ///
    /// Steps:
    /// - Render a delete of an account returning the id and a scalar subquery of its memberships' role, on PostgreSQL and on
    ///   SQL Server; render one returning a count of <c>Account.Table.As("x")</c>, on SQLite, PostgreSQL, MariaDB, and SQL
    ///   Server.
    ///
    /// Expected:
    /// - PostgreSQL: <c>… RETURNING "id", (SELECT "memberships"."Role" FROM "app"."memberships" WHERE
    ///   "memberships"."AccountId" = "accounts"."id")</c>.
    /// - SQL Server: <see cref="InvalidOperationException"/> naming message 10705.
    /// - The count of the target: <see cref="InvalidOperationException"/> on each dialect.
    /// </remarks>
    [Fact]
    public void Returning_Subqueries_FollowTheRules()
    {
        Assert.Equal(
            "DELETE FROM \"accounts\" WHERE \"accounts\".\"id\" = $1 RETURNING \"id\", (SELECT \"memberships\".\"Role\" FROM \"app\".\"memberships\" WHERE \"memberships\".\"AccountId\" = \"accounts\".\"id\")",
            ReturnsRole(PostgreSql).ToSql().Text
        );
        Assert.Contains("10705", Assert.Throws<InvalidOperationException>(() => ReturnsRole(SqlServer).ToSql()).Message, StringComparison.Ordinal);

        Assert.Throws<InvalidOperationException>(() => ReturnsCount(Sqlite).ToSql());
        Assert.Throws<InvalidOperationException>(() => ReturnsCount(PostgreSql).ToSql());
        Assert.Throws<InvalidOperationException>(() => ReturnsCount(MariaDb).ToSql());
        Assert.Throws<InvalidOperationException>(() => ReturnsCount(SqlServer).ToSql());
    }

    /// <summary>
    /// A statement with <c>Returning</c> that is closed with rows left does not cancel its command, unlike a SELECT (design
    /// §9, §12.2).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Fake connections that return three rows.
    ///
    /// Steps:
    /// - Run <c>FirstAsync</c> on a delete with <c>Returning</c>, and on a SELECT.
    ///
    /// Expected:
    /// - The first row on both; <c>Cancel</c> never called for the delete, once for the SELECT.
    /// </remarks>
    [Fact]
    public async Task Returning_ClosedWithRowsLeft_DoesNotCancel()
    {
        var deleted = new FakeConnection { Rows = [1L, 2L, 3L] };
        var selected = new FakeConnection { Rows = [1L, 2L, 3L] };

        Assert.Equal(1L, await new Database<SqliteDialect>(deleted).Delete(Account.Table).AllRows().Returning(a => a.Id).FirstAsync(Ct));
        Assert.Equal(1L, await new Database<SqliteDialect>(selected).From(Account.Table).Select(a => a.Id).FirstAsync(Ct));

        Assert.Equal(0, deleted.Commands.Single().CancelCount);
        Assert.Equal(1, selected.Commands.Single().CancelCount);
    }

    private static InsertStatement<TDialect, Executable, Account.Columns> InsertAccount<TDialect>(Database<TDialect> db)
        where TDialect : IDialect => db.Insert(Account.Table).Values(new Account.Insert { Email = "a@example.com", Status = AccountStatus.Open });

    private static ReturningQuery<TDialect, Executable, (long, string?)> ReturnsRole<TDialect>(Database<TDialect> db)
        where TDialect : IDeleteReturningDialect =>
        db.Delete(Account.Table)
            .Where(a => Eq(a.Id, 1L))
            .Returning(a => (a.Id, Scalar(db.From(Membership.Table).Where(m => Eq(m.AccountId, a.Id)).Select(m => m.Role))));

    private static ReturningQuery<TDialect, Executable, long?> ReturnsCount<TDialect>(Database<TDialect> db)
        where TDialect : IDeleteReturningDialect =>
        db.Delete(Account.Table).Where(a => Eq(a.Id, 1L)).Returning(a => Scalar(db.From(Account.Table.As("x")).Select(x => Count())));
}
