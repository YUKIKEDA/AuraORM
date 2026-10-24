using System.Data.Common;
using static AuraORM.Sql;

namespace AuraORM.Integration.Tests;

/// <summary>
/// DDL (design §13) on each database: tables are created, used, and dropped in the order a user runs them. A derived
/// class per database gives the target and the statements that only some dialects have.
/// </summary>
/// <typeparam name="TDialect">The dialect.</typeparam>
public abstract class DdlTests<TDialect>
    where TDialect : IDialect
{
    private static readonly TimeSpan ClockTolerance = TimeSpan.FromMinutes(5);

    private protected abstract ITestTarget<TDialect> Target { get; }

    // The statements that only some dialects have; null where the dialect does not.
    private protected virtual Func<Database<TDialect>, Task>? CreateUsersIfNotExists => null;

    private protected virtual Func<Database<TDialect>, Task>? CreateOrderIndexesIfNotExists => null;

    private protected virtual Func<Database<TDialect>, Task>? DropOrderIndexesIfExists => null;

    private protected virtual Func<Transaction<TDialect>, Task>? CreateUsersInTransaction => null;

    // Creates the schema of DdlNames.Schema if it does not exist (on SQLite, attaches a database of that name).
    private protected abstract Task CreateSchemaAsync(Database<TDialect> db);

    private protected static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>
    /// The tables of design §13 are created, used, and dropped: the defaults fill the columns left out, the unique
    /// constraint and the unique index reject a duplicate, two NULLs pass the unique index, the foreign key rejects a
    /// missing user, and the drops remove the indexes and the tables (design §13.2–§13.5, §13.8).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - <see cref="DdlUser"/> and <see cref="DdlOrder"/>, dropped if they exist.
    ///
    /// Steps:
    /// - Create both tables and the indexes of the orders; insert a user, two orders without a coupon, and one with
    ///   coupon "x"; insert a duplicate code, a duplicate coupon, and an order of user 999; read the first order and the
    ///   user; drop the indexes and the tables, then drop the users again.
    ///
    /// Expected:
    /// - The duplicates fail with the database's error; the order of user 999 fails where foreign keys are checked
    ///   (always on the servers; on SQLite when <c>PRAGMA foreign_keys</c> is 1).
    /// - The order's total is 0 and both times are the current UTC time; the age is null.
    /// - The second <c>DROP TABLE</c> fails: the table is gone.
    /// </remarks>
    [Fact]
    public async Task Lifecycle_CreateUseDrop_FollowsTheDeclaration()
    {
        Database<TDialect> db = await Target.CreateDatabaseAsync();
        await db.DropTable(DdlOrder.Table).IfExists().ExecuteAsync(Ct);
        await db.DropTable(DdlUser.Table).IfExists().ExecuteAsync(Ct);
        await db.CreateTable(DdlUser.Table).ExecuteAsync(Ct);
        await db.CreateTable(DdlOrder.Table).ExecuteAsync(Ct);
        await db.CreateIndexes(DdlOrder.Table).ExecuteAsync(Ct);

        await db.Insert(DdlUser.Table).Values(new DdlUser.Insert { Name = "Alice" }).ExecuteAsync(Ct);
        await db.Insert(DdlOrder.Table)
            .Values([new DdlOrder.Insert { UserId = 1, Code = "c1" }, new DdlOrder.Insert { UserId = 1, Code = "c2" }])
            .ExecuteAsync(Ct);
        await db.Insert(DdlOrder.Table)
            .Values(
                new DdlOrder.Insert
                {
                    UserId = 1,
                    Code = "c3",
                    Coupon = "x",
                }
            )
            .ExecuteAsync(Ct);
        _ = await Assert.ThrowsAnyAsync<DbException>(() =>
            db.Insert(DdlOrder.Table).Values(new DdlOrder.Insert { UserId = 1, Code = "c1" }).ExecuteAsync(Ct)
        );
        _ = await Assert.ThrowsAnyAsync<DbException>(() =>
            db.Insert(DdlOrder.Table)
                .Values(
                    new DdlOrder.Insert
                    {
                        UserId = 1,
                        Code = "c4",
                        Coupon = "x",
                    }
                )
                .ExecuteAsync(Ct)
        );
        Func<Task<int>> missingUser = () => db.Insert(DdlOrder.Table).Values(new DdlOrder.Insert { UserId = 999, Code = "c5" }).ExecuteAsync(Ct);
        if (await ForeignKeysAreCheckedAsync(db))
        {
            _ = await Assert.ThrowsAnyAsync<DbException>(missingUser);
        }
        else
        {
            _ = await missingUser();
        }

        DdlOrder first = await db.From(DdlOrder.Table).Where(o => Eq(o.Code, "c1")).Select(o => o).FirstAsync(Ct);
        DdlUser user = await db.From(DdlUser.Table).Select(u => u).FirstAsync(Ct);
        Assert.Equal(0m, first.Total);
        Assert.Null(first.Coupon);
        AssertNow(first.CreatedAt);
        AssertNow(user.CreatedAt);
        Assert.Null(user.Age);

        await db.DropIndexes(DdlOrder.Table).ExecuteAsync(Ct);
        await db.DropTable(DdlOrder.Table).ExecuteAsync(Ct);
        await db.DropTable(DdlUser.Table).ExecuteAsync(Ct);
        _ = await Assert.ThrowsAnyAsync<DbException>(() => db.DropTable(DdlUser.Table).ExecuteAsync(Ct));
        await db.DropTable(DdlUser.Table).IfExists().ExecuteAsync(Ct);
    }

    /// <summary>
    /// <c>IF NOT EXISTS</c> and <c>IF EXISTS</c> do nothing a second time, on the dialects that have them (design §2.4,
    /// §13.2, §13.4).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - The statements that the derived class gives for the dialect; none where the dialect lacks the clause.
    ///
    /// Steps:
    /// - Check that the dialect gives each statement exactly when its type has the capability.
    /// - Create the users twice with <c>IfNotExists</c>, the orders, their indexes twice with <c>IfNotExists</c>, and drop
    ///   the indexes twice with <c>IfExists</c>.
    ///
    /// Expected:
    /// - No error.
    /// </remarks>
    [Fact]
    public async Task Clauses_IfNotExistsAndIfExists_DoNothingTheSecondTime()
    {
        Assert.Equal(typeof(ICreateTableIfNotExistsDialect).IsAssignableFrom(typeof(TDialect)), CreateUsersIfNotExists is not null);
        Assert.Equal(typeof(ICreateIndexIfNotExistsDialect).IsAssignableFrom(typeof(TDialect)), CreateOrderIndexesIfNotExists is not null);
        Assert.Equal(typeof(IDropIndexIfExistsDialect).IsAssignableFrom(typeof(TDialect)), DropOrderIndexesIfExists is not null);

        Database<TDialect> db = await Target.CreateDatabaseAsync();
        await db.DropTable(DdlOrder.Table).IfExists().ExecuteAsync(Ct);
        await db.DropTable(DdlUser.Table).IfExists().ExecuteAsync(Ct);
        if (CreateUsersIfNotExists is { } createUsers)
        {
            await createUsers(db);
            await createUsers(db);
        }
        else
        {
            await db.CreateTable(DdlUser.Table).ExecuteAsync(Ct);
        }

        await db.CreateTable(DdlOrder.Table).ExecuteAsync(Ct);
        if (CreateOrderIndexesIfNotExists is { } createIndexes)
        {
            await createIndexes(db);
            await createIndexes(db);
        }
        else
        {
            await db.CreateIndexes(DdlOrder.Table).ExecuteAsync(Ct);
        }

        if (DropOrderIndexesIfExists is { } dropIndexes)
        {
            await dropIndexes(db);
            await dropIndexes(db);
        }

        await db.DropTable(DdlOrder.Table).ExecuteAsync(Ct);
        await db.DropTable(DdlUser.Table).ExecuteAsync(Ct);
    }

    /// <summary>
    /// A rollback undoes the DDL of a transaction on the dialects that have <see cref="ITransactionalDdlDialect"/>
    /// (design §12.5, §13.1).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - The users dropped if they exist.
    ///
    /// Steps:
    /// - Check that the dialect gives the statement exactly when its type has the capability.
    /// - Create the users in a transaction and dispose it without a commit; create the users outside.
    ///
    /// Expected:
    /// - The second <c>CREATE TABLE</c> succeeds: the rollback removed the table.
    /// </remarks>
    [Fact]
    public async Task Transaction_Rollback_UndoesTheDdl()
    {
        Assert.Equal(typeof(ITransactionalDdlDialect).IsAssignableFrom(typeof(TDialect)), CreateUsersInTransaction is not null);
        if (CreateUsersInTransaction is not { } create)
        {
            return;
        }

        Database<TDialect> db = await Target.CreateDatabaseAsync();
        await db.DropTable(DdlOrder.Table).IfExists().ExecuteAsync(Ct);
        await db.DropTable(DdlUser.Table).IfExists().ExecuteAsync(Ct);
        await using (Transaction<TDialect> tx = await db.BeginTransactionAsync(cancellationToken: Ct))
        {
            await create(tx);
        }

        await db.CreateTable(DdlUser.Table).ExecuteAsync(Ct);
        await db.DropTable(DdlUser.Table).ExecuteAsync(Ct);
    }

    /// <summary>
    /// A table of every type of design §3.3, created by DDL, takes a value of each type and returns it (design §3.3,
    /// §13.2).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - <see cref="DdlEvery"/>, dropped if it exists and created.
    ///
    /// Steps:
    /// - Insert a row with a value in each column (null in the nullable one) and read it back.
    ///
    /// Expected:
    /// - Each value as written (the time with offset zero compared as UTC ticks).
    /// </remarks>
    [Fact]
    public async Task EveryType_CreatedByDdl_RoundTripsEachValue()
    {
        Database<TDialect> db = await Target.CreateDatabaseAsync();
        await db.DropTable(DdlEvery.Table).IfExists().ExecuteAsync(Ct);
        await db.CreateTable(DdlEvery.Table).ExecuteAsync(Ct);
        var row = new DdlEvery.Insert
        {
            Id = 1,
            Flag = true,
            Tiny = 255,
            Small = -2,
            Number = -3,
            Big = long.MaxValue,
            Single = 1.5f,
            Real = -2.25,
            Money = 12.3456m,
            Text = "long é text",
            Short = "short",
            Blob = [1, 2, 3],
            Bytes = [4, 5],
            Key = new Guid("0f8fad5b-d9cb-469f-a165-70867728950e"),
            At = new DateTime(2024, 5, 6, 7, 8, 9, DateTimeKind.Unspecified).AddTicks(1_234_560),
            AtOffset = new DateTimeOffset(2024, 5, 6, 7, 8, 9, TimeSpan.Zero).AddTicks(1_234_560),
            Day = new DateOnly(2024, 5, 6),
            Time = new TimeOnly(7, 8, 9).Add(TimeSpan.FromTicks(1_234_560)),
            Status = PersonStatus.Active,
            Missing = null,
        };
        await db.Insert(DdlEvery.Table).Values(row).ExecuteAsync(Ct);
        DdlEvery read = await db.From(DdlEvery.Table).Select(e => e).FirstAsync(Ct);

        Assert.Equal(
            (row.Flag, row.Tiny, row.Small, row.Number, row.Big, row.Single, row.Real, row.Money, row.Text, row.Short),
            (read.Flag, read.Tiny, read.Small, read.Number, read.Big, read.Single, read.Real, read.Money, read.Text, read.Short)
        );
        Assert.Equal(row.Blob, read.Blob);
        Assert.Equal(row.Bytes, read.Bytes);
        Assert.Equal((row.Key, row.At, row.Day, row.Time, row.Status), (read.Key, read.At, read.Day, read.Time, read.Status));
        Assert.Equal(row.AtOffset.UtcTicks, read.AtOffset.UtcTicks);
        Assert.Null(read.Missing);
        await db.DropTable(DdlEvery.Table).ExecuteAsync(Ct);
    }

    /// <summary>
    /// Every kind of default of design §13.3 fills a row that sets no column: each constant as written, null, the current
    /// time in UTC, and raw SQL (design §13.3).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - <see cref="DdlDefault"/>, dropped if it exists and created.
    ///
    /// Steps:
    /// - Insert a row that sets no column, and read it.
    ///
    /// Expected:
    /// - true, false, 255, -2, -1, 0, 1.5, -1.5, 12, "it's é", "x", <see cref="PersonStatus.Active"/>, null, null, the
    ///   current UTC time twice, and 42.
    /// </remarks>
    [Fact]
    public async Task Defaults_RowWithoutValues_TakesEachDefault()
    {
        Database<TDialect> db = await Target.CreateDatabaseAsync();
        await db.DropTable(DdlDefault.Table).IfExists().ExecuteAsync(Ct);
        await db.CreateTable(DdlDefault.Table).ExecuteAsync(Ct);
        await db.Insert(DdlDefault.Table).Values(new DdlDefault.Insert()).ExecuteAsync(Ct);
        DdlDefault read = await db.From(DdlDefault.Table).Select(d => d).FirstAsync(Ct);

        Assert.Equal(
            (true, false, (byte)255, (short)-2, -1, 0L, 1.5f, -1.5, 12m, "it's é", "x", PersonStatus.Active, 42),
            (
                read.Flag,
                read.Off,
                read.Tiny,
                read.Small,
                read.Number,
                read.Big,
                read.Single,
                read.Real,
                read.Money,
                read.Text,
                read.Short,
                read.Status,
                read.Raw
            )
        );
        Assert.Null(read.Missing);
        Assert.Null(read.MaybeNumber);
        AssertNow(read.At);
        AssertNow(read.AtOffset.UtcDateTime);
        await db.DropTable(DdlDefault.Table).ExecuteAsync(Ct);
    }

    /// <summary>
    /// Tables in a schema, a foreign key between them, and an index are created, used, and dropped with qualified names
    /// (design §13.6).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - The schema <see cref="DdlNames.Schema"/> (on SQLite, an attached database of that name), and its tables dropped
    ///   if they exist.
    ///
    /// Steps:
    /// - Create the parent, the child, and the child's index; insert a parent and a child of it, and a child of a missing
    ///   parent; count the children; drop the index and both tables.
    ///
    /// Expected:
    /// - The child of the missing parent fails where foreign keys are checked; one child is counted otherwise two.
    /// </remarks>
    [Fact]
    public async Task Schema_QualifiedTables_CreateUseDrop()
    {
        Database<TDialect> db = await Target.CreateDatabaseAsync();
        await CreateSchemaAsync(db);
        await db.DropTable(DdlScopedChild.Table).IfExists().ExecuteAsync(Ct);
        await db.DropTable(DdlScopedParent.Table).IfExists().ExecuteAsync(Ct);
        await db.CreateTable(DdlScopedParent.Table).ExecuteAsync(Ct);
        await db.CreateTable(DdlScopedChild.Table).ExecuteAsync(Ct);
        await db.CreateIndexes(DdlScopedChild.Table).ExecuteAsync(Ct);

        await db.Insert(DdlScopedParent.Table).Values(new DdlScopedParent.Insert { Id = 1 }).ExecuteAsync(Ct);
        await db.Insert(DdlScopedChild.Table).Values(new DdlScopedChild.Insert { Id = 1, ParentId = 1 }).ExecuteAsync(Ct);
        bool checks = await ForeignKeysAreCheckedAsync(db);
        Func<Task<int>> orphan = () => db.Insert(DdlScopedChild.Table).Values(new DdlScopedChild.Insert { Id = 2, ParentId = 99 }).ExecuteAsync(Ct);
        if (checks)
        {
            _ = await Assert.ThrowsAnyAsync<DbException>(orphan);
        }
        else
        {
            _ = await orphan();
        }

        long children = await db.From(DdlScopedChild.Table).Select(c => Count()).FirstAsync(Ct);
        Assert.Equal(checks ? 1 : 2, children);
        await db.DropIndexes(DdlScopedChild.Table).ExecuteAsync(Ct);
        await db.DropTable(DdlScopedChild.Table).ExecuteAsync(Ct);
        await db.DropTable(DdlScopedParent.Table).ExecuteAsync(Ct);
    }

    // §13.5: the servers check foreign keys; SQLite checks them when PRAGMA foreign_keys is on for the connection.
    private static async Task<bool> ForeignKeysAreCheckedAsync(Database<TDialect> db) =>
        typeof(TDialect) != typeof(SqliteDialect) || await db.QueryScalarAsync<long>($"PRAGMA foreign_keys", Ct) == 1;

    private static void AssertNow(DateTime value)
    {
        TimeSpan difference = (DateTime.SpecifyKind(value, DateTimeKind.Utc) - DateTime.UtcNow).Duration();
        Assert.True(difference < ClockTolerance, $"{value:O} is {difference} from the current UTC time.");
    }
}

/// <summary>
/// <see cref="DdlTests{TDialect}"/> on SQLite.
/// </summary>
public sealed class SqliteDdlTests : DdlTests<SqliteDialect>, IAsyncLifetime
{
    private readonly SqliteTarget _target = new();

    private protected override ITestTarget<SqliteDialect> Target => _target;

    private protected override Func<Database<SqliteDialect>, Task>? CreateUsersIfNotExists =>
        db => db.CreateTable(DdlUser.Table).IfNotExists().ExecuteAsync(Ct);

    private protected override Func<Database<SqliteDialect>, Task>? CreateOrderIndexesIfNotExists =>
        db => db.CreateIndexes(DdlOrder.Table).IfNotExists().ExecuteAsync(Ct);

    private protected override Func<Database<SqliteDialect>, Task>? DropOrderIndexesIfExists =>
        db => db.DropIndexes(DdlOrder.Table).IfExists().ExecuteAsync(Ct);

    private protected override Func<Transaction<SqliteDialect>, Task>? CreateUsersInTransaction =>
        tx => tx.CreateTable(DdlUser.Table).ExecuteAsync(Ct);

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public ValueTask DisposeAsync() => _target.DisposeAsync();

    private protected override Task CreateSchemaAsync(Database<SqliteDialect> db) =>
#if NET10_0_OR_GREATER
        db.ExecuteAsync($"ATTACH DATABASE ':memory:' AS aura_ddl10", Ct);
#else
        db.ExecuteAsync($"ATTACH DATABASE ':memory:' AS aura_ddl8", Ct);
#endif
}
