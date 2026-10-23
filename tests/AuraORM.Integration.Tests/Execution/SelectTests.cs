using static AuraORM.Sql;

namespace AuraORM.Integration.Tests;

/// <summary>
/// SELECT statements of the builder (design §5) on one database. A derived class per database gives the target.
/// </summary>
/// <typeparam name="TDialect">The dialect.</typeparam>
public abstract class SelectTests<TDialect>
    where TDialect : IDialect
{
    private static readonly Person Alice = new(1, "Alice", "Al", 30, PersonStatus.Active, 12.50m, new DateOnly(1996, 4, 1));
    private static readonly Person Bob = new(2, "Bob", null, null, PersonStatus.Inactive, 0m, new DateOnly(2000, 1, 2));
    private static readonly Person Carol = new(3, "Carol", "Cee", 41, PersonStatus.Active, 7.25m, new DateOnly(1985, 12, 31));
    private static readonly Person Dave = new(4, "Dave", null, 30, PersonStatus.Active, 3m, new DateOnly(1990, 6, 15));

    private protected abstract ITestTarget<TDialect> Target { get; }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>
    /// A whole row is read back into the table type, filtered by <c>WHERE</c> and ordered by <c>ORDER BY</c>.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Alice, Bob, Carol, and Dave in <c>people</c>.
    ///
    /// Steps:
    /// - Select the active people, ordered by age descending and then id.
    ///
    /// Expected:
    /// - Carol, Alice, Dave, with every column read back.
    /// </remarks>
    [Fact]
    public async Task Select_WholeRow_FiltersAndOrders()
    {
        Database<TDialect> db = await SeedAsync();

        List<Person> rows = await db.From(Person.Table)
            .Where(p => Eq(p.Status, PersonStatus.Active))
            .Select(p => p)
            .OrderBy(p => [Desc(p.Age), Asc(p.Id)])
            .ToListAsync(Ct);

        Assert.Equal([Carol, Alice, Dave], rows);
    }

    /// <summary>
    /// A limit and an offset after <c>ORDER BY</c> return one page, on every dialect.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - The four people.
    ///
    /// Steps:
    /// - Select the ids ordered by id, with a limit of 2 and an offset of 1.
    ///
    /// Expected:
    /// - 2 and 3.
    /// </remarks>
    [Fact]
    public async Task Select_LimitOffset_ReturnsPage()
    {
        Database<TDialect> db = await SeedAsync();

        List<long> ids = await db.From(Person.Table).Select(p => p.Id).OrderBy(p => [Asc(p.Id)]).Limit(2).Offset(1).ToListAsync(Ct);

        Assert.Equal([2L, 3L], ids);
    }

    /// <summary>
    /// A tuple reads its items by position, nulls included, and <c>SelectDistinct</c> removes duplicate rows.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - The four people; Alice and Dave are both 30 and active.
    ///
    /// Steps:
    /// - Select the distinct (status, age) pairs, ordered by status and age.
    ///
    /// Expected:
    /// - (Inactive, null), (Active, 30), (Active, 41); NULL sorts first in ascending order on every supported dialect.
    /// </remarks>
    [Fact]
    public async Task SelectDistinct_Tuple_RemovesDuplicates()
    {
        Database<TDialect> db = await SeedAsync();

        List<(PersonStatus, int?)> rows = await db.From(Person.Table)
            .SelectDistinct(p => (p.Status, p.Age))
            .OrderBy(p => [Asc(p.Status), Asc(p.Age)])
            .ToListAsync(Ct);

        Assert.Equal([(PersonStatus.Inactive, null), (PersonStatus.Active, 30), (PersonStatus.Active, (int?)41)], rows);
    }

    /// <summary>
    /// A result type's projection reads each member from its own column.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - The four people.
    ///
    /// Steps:
    /// - Select the name and the age of people with a nickname into <see cref="NameAndAge"/>, ordered by id.
    ///
    /// Expected:
    /// - Alice (30) and Carol (41).
    /// </remarks>
    [Fact]
    public async Task Select_ResultProjection_ReadsMembers()
    {
        Database<TDialect> db = await SeedAsync();

        List<NameAndAge> rows = await db.From(Person.Table)
            .Where(p => IsNotNull(p.Nickname))
            .Select(p => new NameAndAge.RowProjection { Name = p.Name, Age = p.Age })
            .OrderBy(p => [Asc(p.Id)])
            .ToListAsync(Ct);

        Assert.Equal([new NameAndAge("Alice", 30), new NameAndAge("Carol", 41)], rows);
    }

    /// <summary>
    /// Aggregates without <c>GROUP BY</c> return one row for the whole table, and on an empty table <c>MAX</c> is null.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - An empty table, then the four people (a server target reuses one table, so the empty case comes first).
    ///
    /// Steps:
    /// - Select <c>Count()</c> and <c>Max(Age)</c> with <c>SingleAsync</c> on each.
    ///
    /// Expected:
    /// - (0, null), then (4, 41).
    /// </remarks>
    [Fact]
    public async Task Select_Aggregates_ReturnOneRow()
    {
        Database<TDialect> empty = await Target.CreateDatabaseAsync();
        (long, int?) none = await empty.From(Person.Table).Select(p => (Count(), Max(p.Age))).SingleAsync(Ct);
        Database<TDialect> db = await SeedAsync();
        (long, int?) full = await db.From(Person.Table).Select(p => (Count(), Max(p.Age))).SingleAsync(Ct);

        Assert.Equal((4L, (int?)41), full);
        Assert.Equal((0L, (int?)null), none);
    }

    /// <summary>
    /// <c>First</c> reads the first row of an unlimited result; <c>Single</c> throws for more than one row; the
    /// <c>OrDefault</c> forms return the default for no row (design §12.2, D1-7).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - The four people.
    ///
    /// Steps:
    /// - Call each method on the ids ordered by id, and on a filter that matches nothing.
    ///
    /// Expected:
    /// - <c>FirstAsync</c> is 1; <c>SingleAsync</c> on four rows and <c>FirstAsync</c> on none throw
    ///   <see cref="InvalidOperationException"/>; <c>FirstOrDefaultAsync</c> and <c>SingleOrDefaultAsync</c> on none are 0.
    /// </remarks>
    [Fact]
    public async Task FirstAndSingle_RowCounts_FollowLinqRules()
    {
        Database<TDialect> db = await SeedAsync();
        SelectOrdered<TDialect, long> all = db.From(Person.Table).Select(p => p.Id).OrderBy(p => [Asc(p.Id)]);
        SelectProjected<TDialect, long, PerRow, Person.Columns> none = db.From(Person.Table).Where(p => Gt(p.Id, 100L)).Select(p => p.Id);

        Assert.Equal(1L, await all.FirstAsync(Ct));
        await Assert.ThrowsAsync<InvalidOperationException>(() => all.SingleAsync(Ct));
        await Assert.ThrowsAsync<InvalidOperationException>(() => none.FirstAsync(Ct));
        Assert.Equal(0L, await none.FirstOrDefaultAsync(Ct));
        Assert.Equal(0L, await none.SingleOrDefaultAsync(Ct));
    }

    /// <summary>
    /// <c>AsAsyncEnumerable</c> streams the rows in order.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - The four people.
    ///
    /// Steps:
    /// - Stream the names ordered by id descending.
    ///
    /// Expected:
    /// - Dave, Carol, Bob, Alice.
    /// </remarks>
    [Fact]
    public async Task AsAsyncEnumerable_StreamsRows()
    {
        Database<TDialect> db = await SeedAsync();
        var names = new List<string>();

        await foreach (string name in db.From(Person.Table).Select(p => p.Name).OrderBy(p => [Desc(p.Id)]).AsAsyncEnumerable(Ct))
        {
            names.Add(name);
        }

        Assert.Equal(["Dave", "Carol", "Bob", "Alice"], names);
    }

    /// <summary>
    /// <c>Limit(0)</c> returns no row where the dialect accepts <c>LIMIT 0</c>, and throws before the statement is sent on SQL
    /// Server, whose <c>FETCH NEXT 0 ROWS</c> fails (design §5.9, §14.3).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - The four people.
    ///
    /// Steps:
    /// - Select the ids ordered by id with <c>Limit(0)</c>.
    ///
    /// Expected:
    /// - An empty list, or <see cref="ArgumentOutOfRangeException"/> on SQL Server.
    /// </remarks>
    [Fact]
    public async Task Limit_Zero_ReturnsNoRowOrThrowsOnSqlServer()
    {
        Database<TDialect> db = await SeedAsync();
        SelectOrdered<TDialect, long> ordered = db.From(Person.Table).Select(p => p.Id).OrderBy(p => [Asc(p.Id)]);

        if (typeof(TDialect) == typeof(SqlServerDialect))
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => ordered.Limit(0));
        }
        else
        {
            Assert.Empty(await ordered.Limit(0).ToListAsync(Ct));
        }
    }

    /// <summary>
    /// After <c>FirstAsync</c> stops reading a result with rows left (and cancels the command), the database runs the next
    /// statement normally (design §12.2).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - The four people.
    ///
    /// Steps:
    /// - Read the first id of all people, then count the people, on the same database.
    ///
    /// Expected:
    /// - 1, then 4.
    /// </remarks>
    [Fact]
    public async Task FirstAsync_ThenAnotherStatement_Runs()
    {
        Database<TDialect> db = await SeedAsync();

        long first = await db.From(Person.Table).Select(p => p.Id).OrderBy(p => [Asc(p.Id)]).FirstAsync(Ct);
        long count = await db.From(Person.Table).Select(p => Count()).SingleAsync(Ct);

        Assert.Equal(1L, first);
        Assert.Equal(4L, count);
    }

    private async Task<Database<TDialect>> SeedAsync()
    {
        Database<TDialect> db = await Target.CreateDatabaseAsync();
        foreach (Person person in (Person[])[Alice, Bob, Carol, Dave])
        {
            await db.ExecuteAsync(
                $"INSERT INTO {Person.Table} (id, name, nickname, age, status, balance, birthday) VALUES ({person.Id}, {person.Name}, {person.Nickname}, {person.Age}, {person.Status}, {person.Balance}, {person.Birthday})",
                Ct
            );
        }

        return db;
    }
}

/// <summary>
/// <see cref="SelectTests{TDialect}"/> on SQLite.
/// </summary>
public sealed class SqliteSelectTests : SelectTests<SqliteDialect>, IAsyncLifetime
{
    private readonly SqliteTarget _target = new();

    private protected override ITestTarget<SqliteDialect> Target => _target;

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public ValueTask DisposeAsync() => _target.DisposeAsync();
}
