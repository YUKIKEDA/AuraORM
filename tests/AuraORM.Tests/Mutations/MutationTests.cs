using static AuraORM.Sql;

namespace AuraORM.Tests;

public sealed class MutationTests
{
    private static readonly Database<SqliteDialect> Sqlite = new(new FakeConnection());
    private static readonly Database<PostgreSqlDialect> PostgreSql = new(new FakeConnection());
    private static readonly Database<MySqlDialect> MySql = new(new FakeConnection());
    private static readonly Database<MariaDbDialect> MariaDb = new(new FakeConnection());
    private static readonly Database<SqlServerDialect> SqlServer = new(new FakeConnection());

    /// <summary>
    /// An insert lists the columns that the row sets, in declaration order, and leaves out the others; several rows are
    /// several parenthesized lists; a schema qualifies the table (design §6.2).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - <see cref="Account"/>, whose <c>DisplayName</c>, <c>CreatedAt</c>, <c>LoginCount</c>, and <c>Balance</c> may be
    ///   left out; <see cref="Membership"/> in the schema <c>app</c>.
    ///
    /// Steps:
    /// - Render an account that sets <c>Status</c>, <c>Email</c>, and <c>LoginCount</c>; two memberships.
    ///
    /// Expected:
    /// - <c>INSERT INTO "accounts" ("email", "status", "login_count") VALUES (@p0, @p1, @p2)</c>, with the enum as its
    ///   underlying <see cref="short"/>.
    /// - <c>INSERT INTO "app"."memberships" ("AccountId", "Role", "joined_on") VALUES (@p0, @p1, @p2), (@p3, @p4, @p5)</c>.
    /// </remarks>
    [Fact]
    public void Insert_WritesTheSetColumnsAndEachRow()
    {
        SqlAssert.Statement(
            new InsertAccount(),
            new(
                "INSERT INTO \"accounts\" (\"email\", \"status\", \"login_count\") VALUES (@p0, @p1, @p2)",
                "INSERT INTO \"accounts\" (\"email\", \"status\", \"login_count\") VALUES ($1, $2, $3)",
                "INSERT INTO `accounts` (`email`, `status`, `login_count`) VALUES (@p0, @p1, @p2)",
                "INSERT INTO [accounts] ([email], [status], [login_count]) VALUES (@p0, @p1, @p2)"
            ),
            "a@example.com",
            (short)1,
            3
        );
        var day = new DateOnly(2026, 1, 2);
        SqlAssert.Statement(
            new InsertMemberships(day),
            new(
                "INSERT INTO \"app\".\"memberships\" (\"AccountId\", \"Role\", \"joined_on\") VALUES (@p0, @p1, @p2), (@p3, @p4, @p5)",
                "INSERT INTO \"app\".\"memberships\" (\"AccountId\", \"Role\", \"joined_on\") VALUES ($1, $2, $3), ($4, $5, $6)",
                "INSERT INTO `app`.`memberships` (`AccountId`, `Role`, `joined_on`) VALUES (@p0, @p1, @p2), (@p3, @p4, @p5)",
                "INSERT INTO [app].[memberships] ([AccountId], [Role], [joined_on]) VALUES (@p0, @p1, @p2), (@p3, @p4, @p5)"
            ),
            1L,
            "admin",
            day,
            2L,
            "member",
            day
        );
    }

    /// <summary>
    /// A row that sets no column is <c>DEFAULT VALUES</c> on SQLite, PostgreSQL, and SQL Server and <c>() VALUES ()</c> on
    /// MySQL and MariaDB, which also write several such rows; the others reject several (design §6.2, §14.3).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - <see cref="Note"/>, whose columns all may be left out.
    ///
    /// Steps:
    /// - Render one empty row on each dialect; two empty rows on MySQL and MariaDB; build two empty rows on the others.
    ///
    /// Expected:
    /// - <c>INSERT INTO "notes" DEFAULT VALUES</c>; <c>INSERT INTO `notes` () VALUES ()</c>; <c>… () VALUES (), ()</c>.
    /// - <see cref="ArgumentException"/> for two rows on SQLite, PostgreSQL, and SQL Server, naming <c>DEFAULT VALUES</c>.
    /// </remarks>
    [Fact]
    public void Insert_RowsThatSetNoColumn_FollowTheDialect()
    {
        SqlAssert.Statement(
            new InsertEmptyNote(),
            new(
                "INSERT INTO \"notes\" DEFAULT VALUES",
                "INSERT INTO \"notes\" DEFAULT VALUES",
                "INSERT INTO `notes` () VALUES ()",
                "INSERT INTO [notes] DEFAULT VALUES"
            )
        );
        Note.Insert[] two = [new Note.Insert(), new Note.Insert()];
        Assert.Equal("INSERT INTO `notes` () VALUES (), ()", MySql.Insert(Note.Table).Values(two).ToSql().Text);
        Assert.Equal("INSERT INTO `notes` () VALUES (), ()", MariaDb.Insert(Note.Table).Values(two).ToSql().Text);
        Assert.Contains(
            "DEFAULT VALUES",
            Assert.Throws<ArgumentException>(() => Sqlite.Insert(Note.Table).Values(two)).Message,
            StringComparison.Ordinal
        );
        Assert.Throws<ArgumentException>(() => PostgreSql.Insert(Note.Table).Values(two));
        Assert.Throws<ArgumentException>(() => SqlServer.Insert(Note.Table).Values(two));
    }

    /// <summary>
    /// The rows of one insert are checked when they are given: there is at least one, they set the same columns, none is
    /// null, and SQL Server takes at most 1000; the parameter limit is checked when the statement is rendered (design §6.2,
    /// §12.4, §14.3).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - <see cref="Note"/> and <see cref="Membership"/>.
    ///
    /// Steps:
    /// - Give an empty list; two rows that set different columns; a list with a null row; 1000 and 1001 rows on SQL Server
    ///   and 1001 rows on PostgreSQL; render 701 memberships (2103 parameters) on SQL Server.
    ///
    /// Expected:
    /// - <see cref="ArgumentException"/> for the empty list, for the different columns (naming both sets), and for 1001 rows
    ///   on SQL Server; <see cref="ArgumentNullException"/> for the null row.
    /// - 1000 rows on SQL Server and 1001 on PostgreSQL build; the 2103 parameters throw <see cref="ArgumentException"/> from
    ///   <c>ToSql</c>.
    /// </remarks>
    [Fact]
    public void Insert_InvalidRows_Throw()
    {
        Assert.Throws<ArgumentException>(() => Sqlite.Insert(Note.Table).Values([]));
        ArgumentException mixed = Assert.Throws<ArgumentException>(() =>
            Sqlite.Insert(Note.Table).Values([new Note.Insert { Body = "a" }, new Note.Insert { Priority = 1 }])
        );
        Assert.Contains("sets the columns Priority, but the first row sets Body", mixed.Message, StringComparison.Ordinal);
        Assert.Throws<ArgumentNullException>(() => Sqlite.Insert(Note.Table).Values([new Note.Insert(), null!]));

        Note.Insert[] thousand = [.. Enumerable.Range(0, 1000).Select(i => new Note.Insert { Priority = i })];
        Note.Insert[] tooMany = [.. thousand, new Note.Insert { Priority = 1000 }];
        Assert.Equal(1000, SqlServer.Insert(Note.Table).Values(thousand).ToSql().Parameters.Length);
        Assert.Contains(
            "1000",
            Assert.Throws<ArgumentException>(() => SqlServer.Insert(Note.Table).Values(tooMany)).Message,
            StringComparison.Ordinal
        );
        Assert.Equal(1001, PostgreSql.Insert(Note.Table).Values(tooMany).ToSql().Parameters.Length);

        Membership.Insert[] memberships =
        [
            .. Enumerable
                .Range(0, 701)
                .Select(i => new Membership.Insert
                {
                    AccountId = i,
                    Role = "r",
                    JoinedOn = new DateOnly(2026, 1, 1),
                }),
        ];
        var statement = SqlServer.Insert(Membership.Table).Values(memberships);
        Assert.Throws<ArgumentException>(() => statement.ToSql());
    }

    /// <summary>
    /// An update writes each assignment target unqualified and every other column qualified; <c>AllRows()</c> adds nothing
    /// (design §7).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - <see cref="Account"/> and <see cref="Membership"/>.
    ///
    /// Steps:
    /// - Render an update of the email to a value and the login count to itself plus one, where the id is 1; the same with
    ///   <c>AllRows()</c>; and an update whose condition is a subquery on the memberships.
    ///
    /// Expected:
    /// - <c>UPDATE "accounts" SET "email" = @p0, "login_count" = ("accounts"."login_count" + @p1) WHERE "accounts"."id" = @p2</c>.
    /// - <c>UPDATE "accounts" SET "login_count" = @p0</c>.
    /// - <c>… WHERE "accounts"."id" IN (SELECT "memberships"."AccountId" FROM "app"."memberships")</c> on every dialect,
    ///   MySQL included.
    /// </remarks>
    [Fact]
    public void Update_WritesTheAssignmentsAndTheCondition()
    {
        SqlAssert.Statement(
            new UpdateAccount(allRows: false),
            new(
                "UPDATE \"accounts\" SET \"email\" = @p0, \"login_count\" = (\"accounts\".\"login_count\" + @p1) WHERE \"accounts\".\"id\" = @p2",
                "UPDATE \"accounts\" SET \"email\" = $1, \"login_count\" = (\"accounts\".\"login_count\" + $2) WHERE \"accounts\".\"id\" = $3",
                "UPDATE `accounts` SET `email` = @p0, `login_count` = (`accounts`.`login_count` + @p1) WHERE `accounts`.`id` = @p2",
                "UPDATE [accounts] SET [email] = @p0, [login_count] = ([accounts].[login_count] + @p1) WHERE [accounts].[id] = @p2"
            ),
            "b@example.com",
            1,
            1L
        );
        SqlAssert.Statement(
            new UpdateAccount(allRows: true),
            new(
                "UPDATE \"accounts\" SET \"email\" = @p0, \"login_count\" = (\"accounts\".\"login_count\" + @p1)",
                "UPDATE \"accounts\" SET \"email\" = $1, \"login_count\" = (\"accounts\".\"login_count\" + $2)",
                "UPDATE `accounts` SET `email` = @p0, `login_count` = (`accounts`.`login_count` + @p1)",
                "UPDATE [accounts] SET [email] = @p0, [login_count] = ([accounts].[login_count] + @p1)"
            ),
            "b@example.com",
            1
        );
        SqlAssert.Statement(
            new UpdateMembers(),
            new(
                "UPDATE \"accounts\" SET \"login_count\" = @p0 WHERE \"accounts\".\"id\" IN (SELECT \"memberships\".\"AccountId\" FROM \"app\".\"memberships\")",
                "UPDATE \"accounts\" SET \"login_count\" = $1 WHERE \"accounts\".\"id\" IN (SELECT \"memberships\".\"AccountId\" FROM \"app\".\"memberships\")",
                "UPDATE `accounts` SET `login_count` = @p0 WHERE `accounts`.`id` IN (SELECT `memberships`.`AccountId` FROM `app`.`memberships`)",
                "UPDATE [accounts] SET [login_count] = @p0 WHERE [accounts].[id] IN (SELECT [memberships].[AccountId] FROM [app].[memberships])"
            ),
            0
        );
    }

    /// <summary>
    /// A delete writes its condition, and <c>AllRows()</c> adds nothing (design §8).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - <see cref="Membership"/> in the schema <c>app</c>.
    ///
    /// Steps:
    /// - Render a delete of the role "guest", and a delete of every row.
    ///
    /// Expected:
    /// - <c>DELETE FROM "app"."memberships" WHERE "memberships"."Role" = @p0</c> and <c>DELETE FROM "app"."memberships"</c>.
    /// </remarks>
    [Fact]
    public void Delete_WritesTheCondition()
    {
        SqlAssert.Statement(
            new DeleteMemberships(allRows: false),
            new(
                "DELETE FROM \"app\".\"memberships\" WHERE \"memberships\".\"Role\" = @p0",
                "DELETE FROM \"app\".\"memberships\" WHERE \"memberships\".\"Role\" = $1",
                "DELETE FROM `app`.`memberships` WHERE `memberships`.`Role` = @p0",
                "DELETE FROM [app].[memberships] WHERE [memberships].[Role] = @p0"
            ),
            "guest"
        );
        SqlAssert.Statement(
            new DeleteMemberships(allRows: true),
            new(
                "DELETE FROM \"app\".\"memberships\"",
                "DELETE FROM \"app\".\"memberships\"",
                "DELETE FROM `app`.`memberships`",
                "DELETE FROM [app].[memberships]"
            )
        );
    }

    /// <summary>
    /// The table of an insert, an update, or a delete cannot have an alias, and the assignments of an update are checked:
    /// at least one, each column once, and each a column of this statement (design §6.2, §7, §14.3).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - <see cref="Account"/>.
    ///
    /// Steps:
    /// - Start an insert, an update, and a delete on <c>Account.Table.As("a")</c>; give an empty list of assignments, a list
    ///   that assigns <c>LoginCount</c> twice, and an assignment to a column of another update.
    ///
    /// Expected:
    /// - <see cref="ArgumentException"/> for the alias, the empty list, and the column assigned twice.
    /// - <see cref="InvalidOperationException"/> from <c>ToSql</c> for the column of another statement.
    /// </remarks>
    [Fact]
    public void Mutation_AliasOrInvalidAssignments_Throw()
    {
        Assert.Throws<ArgumentException>(() => Sqlite.Insert(Account.Table.As("a")));
        Assert.Throws<ArgumentException>(() => Sqlite.Update(Account.Table.As("a")));
        Assert.Throws<ArgumentException>(() => Sqlite.Delete(Account.Table.As("a")));
        Assert.Throws<ArgumentException>(() => Sqlite.Update(Account.Table).Set(a => []));
        Assert.Contains(
            "login_count twice",
            Assert.Throws<ArgumentException>(() => Sqlite.Update(Account.Table).Set(a => [Set(a.LoginCount, 1), Set(a.LoginCount, 2)])).Message,
            StringComparison.Ordinal
        );
        Column<int>? other = null;
        _ = Sqlite
            .Update(Account.Table)
            .Set(a =>
            {
                other = a.LoginCount;
                return [Set(a.LoginCount, 0)];
            });
        var statement = Sqlite.Update(Account.Table).Set(a => [Set(other!, 1)]).AllRows();
        Assert.Throws<InvalidOperationException>(() => statement.ToSql());
    }

    /// <summary>
    /// MySQL and MariaDB assign from left to right, so a value that reads a column assigned before it, directly or in a
    /// subquery, throws there; a value that reads a column assigned after it, or its own column, does not; the other dialects
    /// accept all of them (design §7, §14.3).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - <see cref="Account"/> and <see cref="Membership"/>.
    ///
    /// Steps:
    /// - Render, on each dialect: <c>Set(a.Email, …), Set(a.DisplayName, a.Email)</c>; the same with the email read in a
    ///   correlated scalar subquery; <c>Set(a.DisplayName, a.Email), Set(a.Email, …)</c>; and
    ///   <c>Set(a.LoginCount, Add(a.LoginCount, 1))</c>.
    ///
    /// Expected:
    /// - The first two throw <see cref="InvalidOperationException"/> naming <c>email</c> on MySQL and MariaDB, and render on
    ///   SQLite, PostgreSQL, and SQL Server.
    /// - The last two render on every dialect.
    /// </remarks>
    [Fact]
    public void Update_ValueReadsAColumnAssignedBefore_ThrowsOnMySqlAndMariaDb()
    {
        foreach (bool inSubquery in (bool[])[false, true])
        {
            var reads = new ReadsAssignedColumn(inSubquery);
            Assert.Contains("email", Assert.Throws<InvalidOperationException>(() => reads.Build(MySql)).Message, StringComparison.Ordinal);
            Assert.Throws<InvalidOperationException>(() => reads.Build(MariaDb));
            _ = reads.Build(Sqlite);
            _ = reads.Build(PostgreSql);
            _ = reads.Build(SqlServer);
        }

        foreach (IQueryCase accepted in (IQueryCase[])[new ReadsLaterColumn(), new UpdateAccount(allRows: false)])
        {
            _ = accepted.Build(MySql);
            _ = accepted.Build(MariaDb);
        }
    }

    /// <summary>
    /// On MySQL, a subquery of an update or a delete that reads the table being changed throws, whether it is in the
    /// condition or in a value, reads the table with an alias, in a correlated <c>EXISTS</c>, or through a derived table;
    /// MariaDB and the other dialects render it. A subquery that only uses the target's columns, or reads a table of the
    /// same name in another schema, does not read the target (design §7, §8, §14.3).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - <see cref="Account"/>, <see cref="Membership"/>, and <see cref="ArchivedAccount"/> (<c>archive.accounts</c>).
    ///
    /// Steps:
    /// - Render, on each dialect, an update whose condition is <c>In</c> of the ids of <c>Account.Table.As("x")</c>, an
    ///   update whose value is a scalar subquery on <c>Account.Table.As("x")</c>, a delete with a correlated <c>Exists</c> on
    ///   <c>Account.Table.As("x")</c>, and a delete whose <c>In</c> reads a derived table of the accounts.
    /// - Render, on MySQL, a delete with a correlated <c>Exists</c> on the memberships, and one whose <c>In</c> reads
    ///   <c>archive.accounts</c>.
    ///
    /// Expected:
    /// - <see cref="InvalidOperationException"/> naming error 1093 on MySQL for each of the first four; the SQL on the other
    ///   dialects.
    /// - The last two render on MySQL.
    /// </remarks>
    [Fact]
    public void Mutation_SubqueryReadsTheTarget_ThrowsOnMySql()
    {
        IQueryCase[] cases =
        [
            new UpdateReadsTarget(inValue: false),
            new UpdateReadsTarget(inValue: true),
            new DeleteReadsTarget(derived: false),
            new DeleteReadsTarget(derived: true),
        ];
        foreach (IQueryCase reads in cases)
        {
            Assert.Contains("1093", Assert.Throws<InvalidOperationException>(() => reads.Build(MySql)).Message, StringComparison.Ordinal);
            _ = reads.Build(MariaDb);
            _ = reads.Build(Sqlite);
            _ = reads.Build(PostgreSql);
            _ = reads.Build(SqlServer);
        }

        Assert.Equal(
            "DELETE FROM \"accounts\" WHERE EXISTS (SELECT \"x\".\"id\" FROM \"accounts\" AS \"x\" WHERE \"x\".\"login_count\" > \"accounts\".\"login_count\")",
            new DeleteReadsTarget(derived: false).Build(Sqlite).Text
        );
        _ = MySql
            .Delete(Account.Table)
            .Where(a => Exists(MySql.From(Membership.Table).Where(m => Eq(m.AccountId, a.Id)).Select(m => m.Role)))
            .ToSql();
        Assert.Equal(
            "DELETE FROM `accounts` WHERE `accounts`.`id` IN (SELECT `accounts`.`id` FROM `archive`.`accounts`)",
            MySql.Delete(Account.Table).Where(a => In(a.Id, MySql.From(ArchivedAccount.Table).Select(x => x.Id))).ToSql().Text
        );
    }

    /// <summary>
    /// The checks of §5.11 apply with the target as the outer statement: a subquery on the same table without an alias that
    /// uses the target's columns throws, and so does a subquery with <c>With</c> (design §7, §14.3).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - <see cref="Account"/> and <see cref="Membership"/>.
    ///
    /// Steps:
    /// - Render, on SQLite, an update whose correlated <c>Exists</c> reads <c>Account.Table</c> without an alias, and a
    ///   delete whose <c>In</c> takes a statement with <c>With</c>.
    ///
    /// Expected:
    /// - <see cref="InvalidOperationException"/> for each.
    /// </remarks>
    [Fact]
    public void Mutation_SubqueryChecks_ApplyToTheTarget()
    {
        var shadowed = Sqlite
            .Update(Account.Table)
            .Set(a => [Set(a.LoginCount, 0)])
            .Where(a => Exists(Sqlite.From(Account.Table).Where(x => Eq(x.Email, a.Email)).Select(x => x.Id)));
        Assert.Throws<InvalidOperationException>(() => shadowed.ToSql());
        var t = Sqlite.From(Membership.Table).Select(m => m).As("t");
        var withCte = Sqlite.Delete(Account.Table).Where(a => In(a.Id, Sqlite.With(t).From(t).Select(x => x.AccountId)));
        Assert.Throws<InvalidOperationException>(() => withCte.ToSql());
    }

    private sealed class InsertAccount : IQueryCase
    {
        public SqlStatement Build<TDialect>(Database<TDialect> db)
            where TDialect : IDialect =>
            db.Insert(Account.Table)
                .Values(
                    new Account.Insert
                    {
                        Status = AccountStatus.Open,
                        Email = "a@example.com",
                        LoginCount = 3,
                    }
                )
                .ToSql();
    }

    private sealed class InsertMemberships(DateOnly day) : IQueryCase
    {
        public SqlStatement Build<TDialect>(Database<TDialect> db)
            where TDialect : IDialect =>
            db.Insert(Membership.Table)
                .Values([
                    new Membership.Insert
                    {
                        AccountId = 1,
                        Role = "admin",
                        JoinedOn = day,
                    },
                    new Membership.Insert
                    {
                        AccountId = 2,
                        Role = "member",
                        JoinedOn = day,
                    },
                ])
                .ToSql();
    }

    private sealed class InsertEmptyNote : IQueryCase
    {
        public SqlStatement Build<TDialect>(Database<TDialect> db)
            where TDialect : IDialect => db.Insert(Note.Table).Values(new Note.Insert()).ToSql();
    }

    private sealed class UpdateAccount(bool allRows) : IQueryCase
    {
        public SqlStatement Build<TDialect>(Database<TDialect> db)
            where TDialect : IDialect
        {
            var set = db.Update(Account.Table).Set(a => [Set(a.Email, "b@example.com"), Set(a.LoginCount, Add(a.LoginCount, 1))]);
            return allRows ? set.AllRows().ToSql() : set.Where(a => Eq(a.Id, 1L)).ToSql();
        }
    }

    private sealed class UpdateMembers : IQueryCase
    {
        public SqlStatement Build<TDialect>(Database<TDialect> db)
            where TDialect : IDialect =>
            db.Update(Account.Table)
                .Set(a => [Set(a.LoginCount, 0)])
                .Where(a => In(a.Id, db.From(Membership.Table).Select(m => m.AccountId)))
                .ToSql();
    }

    private sealed class DeleteMemberships(bool allRows) : IQueryCase
    {
        public SqlStatement Build<TDialect>(Database<TDialect> db)
            where TDialect : IDialect
        {
            var delete = db.Delete(Membership.Table);
            return allRows ? delete.AllRows().ToSql() : delete.Where(m => Eq(m.Role, "guest")).ToSql();
        }
    }

    private sealed class ReadsAssignedColumn(bool inSubquery) : IQueryCase
    {
        public SqlStatement Build<TDialect>(Database<TDialect> db)
            where TDialect : IDialect =>
            db.Update(Account.Table)
                .Set(a =>
                    [
                        Set(a.Email, "b@example.com"),
                        inSubquery
                            ? Set(a.DisplayName, Scalar(db.From(Membership.Table).Where(m => Eq(m.Role, a.Email)).Select(m => m.Role)))
                            : Set(a.DisplayName, a.Email),
                    ]
                )
                .AllRows()
                .ToSql();
    }

    private sealed class ReadsLaterColumn : IQueryCase
    {
        public SqlStatement Build<TDialect>(Database<TDialect> db)
            where TDialect : IDialect =>
            db.Update(Account.Table).Set(a => [Set(a.DisplayName, a.Email), Set(a.Email, "b@example.com")]).AllRows().ToSql();
    }

    private sealed class UpdateReadsTarget(bool inValue) : IQueryCase
    {
        public SqlStatement Build<TDialect>(Database<TDialect> db)
            where TDialect : IDialect =>
            inValue
                ? db.Update(Account.Table)
                    .Set(a => [Set(a.LoginCount, Coalesce(Scalar(db.From(Account.Table.As("x")).Select(x => Max(x.LoginCount))), 0))])
                    .AllRows()
                    .ToSql()
                : db.Update(Account.Table)
                    .Set(a => [Set(a.LoginCount, 0)])
                    .Where(a => In(a.Id, db.From(Account.Table.As("x")).Where(x => Gt(x.LoginCount, 1)).Select(x => x.Id)))
                    .ToSql();
    }

    private sealed class DeleteReadsTarget(bool derived) : IQueryCase
    {
        public SqlStatement Build<TDialect>(Database<TDialect> db)
            where TDialect : IDialect
        {
            if (derived)
            {
                var busy = db.From(Account.Table).Where(x => Gt(x.LoginCount, 1)).Select(x => x).As("busy");
                return db.Delete(Account.Table).Where(a => In(a.Id, db.From(busy).Select(b => b.Id))).ToSql();
            }

            return db.Delete(Account.Table)
                .Where(a => Exists(db.From(Account.Table.As("x")).Where(x => Gt(x.LoginCount, a.LoginCount)).Select(x => x.Id)))
                .ToSql();
        }
    }
}
