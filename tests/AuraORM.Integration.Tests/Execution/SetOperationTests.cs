using static AuraORM.Sql;

namespace AuraORM.Integration.Tests;

/// <summary>
/// Set operations (design §5.10) on one database: people and their pets. A derived class per database gives the target.
/// </summary>
/// <typeparam name="TDialect">The dialect.</typeparam>
public abstract class SetOperationTests<TDialect>
    where TDialect : IDialect
{
    private static readonly (long Id, string Name)[] People = [(1, "Alice"), (2, "Bob"), (3, "Carol"), (4, "Dave")];

    private static readonly (long Id, long? Owner, string Name)[] Pets = [(10, 1, "Rex"), (11, 1, "Tom"), (12, null, "Bob")];

    private protected abstract ITestTarget<TDialect> Target { get; }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>
    /// Each operator returns the rows SQL defines, and <c>OrderBy</c> by position orders the whole result (design §5.10).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - <see cref="SeedAsync"/>: people Alice, Bob, Carol, Dave (ids 1–4); pets Rex and Tom (owner 1) and Bob (no owner).
    ///
    /// Steps:
    /// - Union the people's and the pets' names; union all of them; intersect and except the people's ids with the pets'
    ///   owner ids (null as 0); each ordered by the item.
    ///
    /// Expected:
    /// - Union: Alice, Bob, Carol, Dave, Rex, Tom (one Bob). Union all: 7 names, two Bob. Intersect: 1. Except: 2, 3, 4.
    /// </remarks>
    [Fact]
    public async Task Operators_ReturnTheirRows()
    {
        Database<TDialect> db = await SeedAsync();
        var people = db.From(Person.Table).Select(p => p.Name);
        var pets = db.From(Pet.Table).Select(x => x.Name);
        var ids = db.From(Person.Table).Select(p => p.Id);
        var owners = db.From(Pet.Table).Select(x => Coalesce(x.OwnerId, 0L));

        List<string> union = await people.Union(pets).OrderBy(s => [Asc(s)]).ToListAsync(Ct);
        List<string> unionAll = await people.UnionAll(pets).OrderBy(s => [Asc(s)]).ToListAsync(Ct);
        List<long> intersect = await ids.Intersect(owners).OrderBy(s => [Asc(s)]).ToListAsync(Ct);
        List<long> except = await ids.Except(owners).OrderBy(s => [Asc(s)]).ToListAsync(Ct);

        Assert.Equal(["Alice", "Bob", "Carol", "Dave", "Rex", "Tom"], union);
        Assert.Equal(["Alice", "Bob", "Bob", "Carol", "Dave", "Rex", "Tom"], unionAll);
        Assert.Equal([1L], intersect);
        Assert.Equal([2L, 3L, 4L], except);
    }

    /// <summary>
    /// <c>OrderBy</c> on a tuple's items, <c>Limit</c>, and <c>Offset</c> page the whole result on every database (design §5.9,
    /// §5.10).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - <see cref="SeedAsync"/>.
    ///
    /// Steps:
    /// - Union (name, id) of the people and of the pets, order by the id descending, skip one row, and take two.
    ///
    /// Expected:
    /// - (Tom, 11), (Rex, 10).
    /// </remarks>
    [Fact]
    public async Task OrderByLimitAndOffset_PageTheResult()
    {
        Database<TDialect> db = await SeedAsync();

        List<(string, long)> rows = await db.From(Person.Table)
            .Select(p => (p.Name, p.Id))
            .Union(db.From(Pet.Table).Select(x => (x.Name, x.Id)))
            .OrderBy(s => [Desc(s.Item2)])
            .Limit(2)
            .Offset(1)
            .ToListAsync(Ct);

        Assert.Equal([("Tom", 11L), ("Rex", 10L)], rows);
    }

    /// <summary>
    /// A set operation is a subquery of <c>In</c> and <c>Exists</c>, and a grouped statement is an operand (design §5.10,
    /// §5.11).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - <see cref="SeedAsync"/>.
    ///
    /// Steps:
    /// - Select the people whose name is in the union of the pets' names and "Carol"'s name; select the people for whom a
    ///   pet or a person with a larger id exists; union the number of people with the number of pets per owner.
    ///
    /// Expected:
    /// - Bob, Carol; Alice, Bob, Carol; 1, 2, 4.
    /// </remarks>
    [Fact]
    public async Task SubqueriesAndGroupedOperands_Run()
    {
        Database<TDialect> db = await SeedAsync();

        List<string> named = await db.From(Person.Table)
            .Where(p =>
                In(p.Name, db.From(Pet.Table).Select(x => x.Name).Union(db.From(Person.Table.As("o")).Where(o => Eq(o.Id, 3L)).Select(o => o.Name)))
            )
            .Select(p => p.Name)
            .OrderBy(p => [Asc(p.Id)])
            .ToListAsync(Ct);
        List<string> exists = await db.From(Person.Table)
            .Where(p =>
                Exists(
                    db.From(Pet.Table)
                        .Where(x => Eq(x.OwnerId, p.Id))
                        .Select(x => x.Id)
                        .Union(db.From(Person.Table.As("o")).Where(o => Gt(o.Id, p.Id)).Select(o => o.Id))
                )
            )
            .Select(p => p.Name)
            .OrderBy(p => [Asc(p.Id)])
            .ToListAsync(Ct);
        List<long> counts = await db.From(Person.Table)
            .Select(p => Count())
            .Union(db.From(Pet.Table).GroupBy(x => x.OwnerId).Select((k, x) => Count()))
            .OrderBy(s => [Asc(s)])
            .ToListAsync(Ct);

        Assert.Equal(["Bob", "Carol"], named);
        Assert.Equal(["Alice", "Bob", "Carol"], exists);
        Assert.Equal([1L, 2L, 4L], counts);
    }

    private async Task<Database<TDialect>> SeedAsync()
    {
        Database<TDialect> db = await Target.CreateDatabaseAsync();
        foreach ((long id, string name) in People)
        {
            await db.ExecuteAsync(
                $"INSERT INTO {Person.Table} (id, name, nickname, age, status, balance, birthday) VALUES ({id}, {name}, {(string?)null}, {(int?)null}, {PersonStatus.Active}, {0m}, {new DateOnly(2000, 1, 1)})",
                Ct
            );
        }

        foreach ((long id, long? owner, string name) in Pets)
        {
            await db.ExecuteAsync($"INSERT INTO {Pet.Table} (id, owner_id, name) VALUES ({id}, {owner}, {name})", Ct);
        }

        return db;
    }
}

/// <summary>
/// <see cref="SetOperationTests{TDialect}"/> on SQLite.
/// </summary>
public sealed class SqliteSetOperationTests : SetOperationTests<SqliteDialect>, IAsyncLifetime
{
    private readonly SqliteTarget _target = new();

    private protected override ITestTarget<SqliteDialect> Target => _target;

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public ValueTask DisposeAsync() => _target.DisposeAsync();
}
