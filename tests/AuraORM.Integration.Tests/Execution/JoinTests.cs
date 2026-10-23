using static AuraORM.Sql;

namespace AuraORM.Integration.Tests;

/// <summary>
/// Joins (design §5.2, §5.3) on one database: people and their pets. A derived class per database gives the target.
/// </summary>
/// <typeparam name="TDialect">The dialect.</typeparam>
public abstract class JoinTests<TDialect>
    where TDialect : IDialect
{
    private protected abstract ITestTarget<TDialect> Target { get; }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>
    /// An inner join returns the pairs that match; a left join also returns the people without a pet, with the pet's columns
    /// null and the pet's whole row <see langword="null"/> (design §5.3).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Alice (pets Rex and Tom), Bob, and Carol (no pets); the pets Stray (no owner) and Ghost (an owner who does not
    ///   exist).
    ///
    /// Steps:
    /// - Select (person, pet name) with an inner join, and (person, pet name, pet row) with a left join, ordered by the ids.
    ///
    /// Expected:
    /// - Inner: (Alice, Rex), (Alice, Tom). Left: the same, then (Bob, null, null) and (Carol, null, null).
    /// </remarks>
    [Fact]
    public async Task InnerAndLeftJoin_ReturnMatchesAndNulls()
    {
        Database<TDialect> db = await SeedAsync();

        List<(string, string)> inner = await db.From(Person.Table)
            .InnerJoin(Pet.Table, (p, pet) => Eq(pet.OwnerId, p.Id))
            .Select((p, pet) => (p.Name, pet.Name))
            .OrderBy((p, pet) => [Asc(pet.Id)])
            .ToListAsync(Ct);
        List<(string, string?, Pet?)> left = await db.From(Person.Table)
            .LeftJoin(Pet.Table, (p, pet) => Eq(pet.OwnerId, p.Id))
            .Select((p, pet) => (p.Name, pet.Name, pet))
            .OrderBy((p, pet) => [Asc(p.Id), Asc(pet.Id)])
            .ToListAsync(Ct);

        Assert.Equal([("Alice", "Rex"), ("Alice", "Tom")], inner);
        Assert.Equal([("Alice", "Rex", Rex), ("Alice", "Tom", Tom), ("Bob", (string?)null, (Pet?)null), ("Carol", null, null)], left);
    }

    /// <summary>
    /// A right join returns every row of the joined table, with the earlier table's columns null where nothing matched
    /// (design §5.3).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - The people and pets of <see cref="InnerAndLeftJoin_ReturnMatchesAndNulls"/>.
    ///
    /// Steps:
    /// - Select (pet name, owner name) from people right join pets, ordered by the pet id.
    ///
    /// Expected:
    /// - (Rex, Alice), (Tom, Alice), (Stray, null), (Ghost, null).
    /// </remarks>
    [Fact]
    public async Task RightJoin_KeepsEveryJoinedRow()
    {
        Database<TDialect> db = await SeedAsync();

        List<(string, string?)> rows = await db.From(Person.Table)
            .RightJoin(Pet.Table, (p, pet) => Eq(pet.OwnerId, p.Id))
            .Select((p, pet) => (pet.Name, p.Name))
            .OrderBy((p, pet) => [Asc(pet.Id)])
            .ToListAsync(Ct);

        Assert.Equal([("Rex", "Alice"), ("Tom", "Alice"), ("Stray", (string?)null), ("Ghost", null)], rows);
    }

    /// <summary>
    /// A cross join pairs every row with every row, and an aliased self-join reads each occurrence's own columns
    /// (design §5.2, §5.3).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Three people and four pets.
    ///
    /// Steps:
    /// - Count the rows of people cross join pets; join people to people as <c>other</c> on a greater id.
    ///
    /// Expected:
    /// - 12; (Alice, Bob), (Alice, Carol), (Bob, Carol).
    /// </remarks>
    [Fact]
    public async Task CrossAndSelfJoin_PairRows()
    {
        Database<TDialect> db = await SeedAsync();

        long pairs = await db.From(Person.Table).CrossJoin(Pet.Table).Select((p, pet) => Count()).SingleAsync(Ct);
        List<(string, string)> ordered = await db.From(Person.Table)
            .InnerJoin(Person.Table.As("other"), (p, other) => Gt(other.Id, p.Id))
            .Select((p, other) => (p.Name, other.Name))
            .OrderBy((p, other) => [Asc(p.Id), Asc(other.Id)])
            .ToListAsync(Ct);

        Assert.Equal(12L, pairs);
        Assert.Equal([("Alice", "Bob"), ("Alice", "Carol"), ("Bob", "Carol")], ordered);
    }

    private protected static Pet Rex { get; } = new(10, 1, "Rex");

    private protected static Pet Tom { get; } = new(11, 1, "Tom");

    private protected async Task<Database<TDialect>> SeedAsync()
    {
        Database<TDialect> db = await Target.CreateDatabaseAsync();
        foreach ((long id, string name) in ((long, string)[])[(1, "Alice"), (2, "Bob"), (3, "Carol")])
        {
            await db.ExecuteAsync(
                $"INSERT INTO {Person.Table} (id, name, nickname, age, status, balance, birthday) VALUES ({id}, {name}, {(string?)null}, {(int?)null}, {PersonStatus.Active}, {0m}, {new DateOnly(2000, 1, 1)})",
                Ct
            );
        }

        foreach (Pet pet in (Pet[])[Rex, Tom, new(12, null, "Stray"), new(13, 99, "Ghost")])
        {
            await db.ExecuteAsync($"INSERT INTO {Pet.Table} (id, owner_id, name) VALUES ({pet.Id}, {pet.OwnerId}, {pet.Name})", Ct);
        }

        return db;
    }
}

/// <summary>
/// <c>FULL JOIN</c> (design §5.3) on a database that has it. A derived class per database gives the target.
/// </summary>
/// <typeparam name="TDialect">A dialect with <c>FULL JOIN</c>.</typeparam>
public abstract class FullJoinTests<TDialect> : JoinTests<TDialect>
    where TDialect : IFullJoinDialect
{
    /// <summary>
    /// A full join returns the matches, the people without a pet, and the pets without an owner, each with the other side's
    /// columns null (design §5.3).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - The people and pets of <see cref="JoinTests{TDialect}.InnerAndLeftJoin_ReturnMatchesAndNulls"/>.
    ///
    /// Steps:
    /// - Select (person id, pet id) of people full join pets.
    ///
    /// Expected:
    /// - (1, 10), (1, 11), (2, null), (3, null), (null, 12), (null, 13), in any order.
    /// </remarks>
    [Fact]
    public async Task FullJoin_KeepsRowsOfBothSides()
    {
        Database<TDialect> db = await SeedAsync();

        List<(long?, long?)> rows = await db.From(Person.Table)
            .FullJoin(Pet.Table, (p, pet) => Eq(pet.OwnerId, p.Id))
            .Select((p, pet) => (p.Id, pet.Id))
            .ToListAsync(TestContext.Current.CancellationToken);

        Assert.Equal(
            [(1L, 10L), (1L, 11L), (2L, (long?)null), (3L, null), (null, 12L), (null, 13L)],
            rows.OrderBy(r => r.Item1 ?? long.MaxValue).ThenBy(r => r.Item2 ?? long.MaxValue).ToList()
        );
    }
}

/// <summary>
/// <see cref="FullJoinTests{TDialect}"/> on SQLite.
/// </summary>
public sealed class SqliteJoinTests : FullJoinTests<SqliteDialect>, IAsyncLifetime
{
    private readonly SqliteTarget _target = new();

    private protected override ITestTarget<SqliteDialect> Target => _target;

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public ValueTask DisposeAsync() => _target.DisposeAsync();
}
