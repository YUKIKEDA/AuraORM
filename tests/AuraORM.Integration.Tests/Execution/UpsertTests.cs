using static AuraORM.Sql;

namespace AuraORM.Integration.Tests;

/// <summary>
/// The rows that the upsert tests start from: Alice (1, age 30) and Bob (2, nickname "B").
/// </summary>
internal static class UpsertSeed
{
    private static readonly DateOnly Day = new(2000, 1, 2);

    public static Person.Insert Person(long id, string name, string? nickname = null, int? age = null) =>
        new()
        {
            Id = id,
            Name = name,
            Nickname = nickname,
            Age = age,
            Status = PersonStatus.Active,
            Balance = 1m,
            Birthday = Day,
        };

    public static async Task<Database<TDialect>> SeedAsync<TDialect>(ITestTarget<TDialect> target)
        where TDialect : IDialect
    {
        Database<TDialect> db = await target.CreateDatabaseAsync();
        await db.Insert(AuraORM.Integration.Tests.Person.Table)
            .Values([Person(1, "Alice", age: 30), Person(2, "Bob", nickname: "B")])
            .ExecuteAsync(TestContext.Current.CancellationToken);
        return db;
    }
}

/// <summary>
/// <c>ON CONFLICT</c> (design §10.1) on SQLite or PostgreSQL. A derived class per database gives the target.
/// </summary>
/// <typeparam name="TDialect">The dialect.</typeparam>
public abstract class ConflictUpsertTests<TDialect>
    where TDialect : IOnConflictDialect, IInsertReturningDialect
{
    private protected abstract ITestTarget<TDialect> Target { get; }

    private protected static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>
    /// <c>DO NOTHING</c>, with and without a target, skips the conflicting row and counts it as 0; a row without a conflict
    /// is inserted and counted as 1 (design §10.1, §10.3).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Alice (1) and Bob (2).
    ///
    /// Steps:
    /// - Insert Alice again as "Al" with <c>OnConflict(p =&gt; [p.Id]).DoNothing()</c>; then with <c>OnConflictDoNothing()</c>;
    ///   then insert Carol (3) with <c>OnConflictDoNothing()</c>; select the names.
    ///
    /// Expected:
    /// - 0, 0, 1; Alice, Bob, Carol.
    /// </remarks>
    [Fact]
    public async Task DoNothing_SkipsTheConflict()
    {
        Database<TDialect> db = await UpsertSeed.SeedAsync(Target);

        int target = await db.Insert(Person.Table).Values(UpsertSeed.Person(1, "Al")).OnConflict(p => [p.Id]).DoNothing().ExecuteAsync(Ct);
        int any = await db.Insert(Person.Table).Values(UpsertSeed.Person(1, "Al")).OnConflictDoNothing().ExecuteAsync(Ct);
        int carol = await db.Insert(Person.Table).Values(UpsertSeed.Person(3, "Carol")).OnConflictDoNothing().ExecuteAsync(Ct);
        List<string> names = await db.From(Person.Table).Select(p => p.Name).OrderBy(p => [Asc(p.Id)]).ToListAsync(Ct);

        Assert.Equal((0, 0, 1), (target, any, carol));
        Assert.Equal(["Alice", "Bob", "Carol"], names);
    }

    /// <summary>
    /// <c>DO UPDATE</c> assigns from the proposed row, whose column left out of the rows has its default (null), and its
    /// <c>WHERE</c> skips the rows that do not meet it, which are not counted (design §10.1, §10.3).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Alice (1, age 30) and Bob (2, nickname "B").
    ///
    /// Steps:
    /// - Upsert Bob as "Bobby" without a nickname, setting the name and the nickname from excluded; upsert Alice with age 20,
    ///   setting the age where the existing age is less than the proposed one; select Alice and Bob.
    ///
    /// Expected:
    /// - 1 and 0; Alice keeps 30; Bob is "Bobby" with a null nickname.
    /// </remarks>
    [Fact]
    public async Task DoUpdate_AssignsFromExcluded()
    {
        Database<TDialect> db = await UpsertSeed.SeedAsync(Target);

        int bob = await db.Insert(Person.Table)
            .Values(UpsertSeed.Person(2, "Bobby"))
            .OnConflict(p => [p.Id])
            .DoUpdate((p, excluded) => [Set(p.Name, excluded.Name), Set(p.Nickname, excluded.Nickname)])
            .ExecuteAsync(Ct);
        int alice = await db.Insert(Person.Table)
            .Values(UpsertSeed.Person(1, "Alice", age: 20))
            .OnConflict(p => [p.Id])
            .DoUpdate((p, excluded) => [Set(p.Age, excluded.Age)])
            .Where((p, excluded) => Lt(p.Age, excluded.Age))
            .ExecuteAsync(Ct);
        List<(string, string?, int?)> rows = await db.From(Person.Table)
            .Select(p => (p.Name, p.Nickname, p.Age))
            .OrderBy(p => [Asc(p.Id)])
            .ToListAsync(Ct);

        Assert.Equal((1, 0), (bob, alice));
        Assert.Equal([("Alice", (string?)null, (int?)30), ("Bobby", null, null)], rows);
    }

    /// <summary>
    /// <c>Returning</c> after an upsert returns the inserted and the updated rows, not the skipped ones (design §10.3).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Alice (1) and Bob (2).
    ///
    /// Steps:
    /// - Upsert Alice as "Al" and Carol (3) with <c>DO UPDATE</c> of the name, returning the id and the name; upsert Bob and
    ///   Dave (4) with <c>DO NOTHING</c>, returning the id.
    ///
    /// Expected:
    /// - (1, Al) and (3, Carol); only 4.
    /// </remarks>
    [Fact]
    public async Task Returning_ReadsTheInsertedAndUpdatedRows()
    {
        Database<TDialect> db = await UpsertSeed.SeedAsync(Target);

        List<(long, string)> updated = await db.Insert(Person.Table)
            .Values([UpsertSeed.Person(1, "Al"), UpsertSeed.Person(3, "Carol")])
            .OnConflict(p => [p.Id])
            .DoUpdate((p, excluded) => [Set(p.Name, excluded.Name)])
            .Returning(p => (p.Id, p.Name))
            .ToListAsync(Ct);
        List<long> skipped = await db.Insert(Person.Table)
            .Values([UpsertSeed.Person(2, "B2"), UpsertSeed.Person(4, "Dave")])
            .OnConflict(p => [p.Id])
            .DoNothing()
            .Returning(p => p.Id)
            .ToListAsync(Ct);

        Assert.Equal([(1L, "Al"), (3L, "Carol")], updated.OrderBy(r => r.Item1));
        Assert.Equal([4L], skipped);
    }
}

/// <summary>
/// <c>ON DUPLICATE KEY UPDATE</c> (design §10.2) on MySQL or MariaDB. A derived class per database gives the target.
/// </summary>
/// <typeparam name="TDialect">The dialect.</typeparam>
public abstract class DuplicateKeyUpsertTests<TDialect>
    where TDialect : IOnDuplicateKeyDialect
{
    private protected abstract ITestTarget<TDialect> Target { get; }

    private protected static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>
    /// An upsert counts an inserted row as 1, an updated row as 2, and, with MySqlConnector's default, a row assigned its own
    /// values as 1, which leaves it as it is (design §10.2, §10.3).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Alice (1) and Bob (2).
    ///
    /// Steps:
    /// - Upsert Alice as "Al" with the name from excluded; upsert Carol (3) the same way; upsert Bob as "B2" assigning the
    ///   name to itself; select the names.
    ///
    /// Expected:
    /// - 2, 1, 1; Al, Bob, Carol.
    /// </remarks>
    [Fact]
    public async Task OnDuplicateKeyUpdate_CountsAndUpdates()
    {
        Database<TDialect> db = await UpsertSeed.SeedAsync(Target);

        int updated = await db.Insert(Person.Table)
            .Values(UpsertSeed.Person(1, "Al"))
            .OnDuplicateKeyUpdate((p, excluded) => [Set(p.Name, excluded.Name)])
            .ExecuteAsync(Ct);
        int inserted = await db.Insert(Person.Table)
            .Values(UpsertSeed.Person(3, "Carol"))
            .OnDuplicateKeyUpdate((p, excluded) => [Set(p.Name, excluded.Name)])
            .ExecuteAsync(Ct);
        int unchanged = await db.Insert(Person.Table)
            .Values(UpsertSeed.Person(2, "B2"))
            .OnDuplicateKeyUpdate((p, excluded) => [Set(p.Name, p.Name)])
            .ExecuteAsync(Ct);
        List<string> names = await db.From(Person.Table).Select(p => p.Name).OrderBy(p => [Asc(p.Id)]).ToListAsync(Ct);

        Assert.Equal((2, 1, 1), (updated, inserted, unchanged));
        Assert.Equal(["Al", "Bob", "Carol"], names);
    }

    /// <summary>
    /// A row that sets no column is written <c>() VALUES ()</c> before the clause, and is inserted (design §10.2).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - An empty <c>notes</c> table, whose id the database assigns.
    ///
    /// Steps:
    /// - Upsert an empty note twice, adding one to the priority on a conflict; count the notes.
    ///
    /// Expected:
    /// - 1 for each (no conflict); two notes.
    /// </remarks>
    [Fact]
    public async Task EmptyRow_IsInserted()
    {
        Database<TDialect> db = await Target.CreateDatabaseAsync();

        int first = await db.Insert(Note.Table)
            .Values(new Note.Insert())
            .OnDuplicateKeyUpdate((n, x) => [Set(n.Priority, Add(n.Priority, 1))])
            .ExecuteAsync(Ct);
        int second = await db.Insert(Note.Table)
            .Values(new Note.Insert())
            .OnDuplicateKeyUpdate((n, x) => [Set(n.Priority, Add(n.Priority, 1))])
            .ExecuteAsync(Ct);

        Assert.Equal((1, 1), (first, second));
        Assert.Equal(2, await db.From(Note.Table).Select(n => Count()).SingleAsync(Ct));
    }
}

/// <summary>
/// <see cref="ConflictUpsertTests{TDialect}"/> on SQLite in memory.
/// </summary>
public sealed class SqliteUpsertTests : ConflictUpsertTests<SqliteDialect>, IAsyncLifetime
{
    private readonly SqliteTarget _target = new();

    private protected override ITestTarget<SqliteDialect> Target => _target;

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public ValueTask DisposeAsync() => _target.DisposeAsync();
}
