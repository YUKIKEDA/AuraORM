using static AuraORM.Sql;

namespace AuraORM.Integration.Tests;

/// <summary>
/// Derived tables and CTEs (design §5.11) on one database: people and their pets. A derived class per database gives the
/// target.
/// </summary>
/// <typeparam name="TDialect">The dialect.</typeparam>
public abstract class NamedQueryTests<TDialect>
    where TDialect : IDialect
{
    private static readonly (long Id, string Name)[] People = [(1, "Alice"), (2, "Bob"), (3, "Carol"), (4, "Dave")];

    private static readonly (long Id, long? Owner, string Name)[] Pets = [(10, 1, "Rex"), (11, 1, "Tom"), (12, null, "Bob")];

    private protected abstract ITestTarget<TDialect> Target { get; }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>
    /// The same named query returns the same rows as a derived table and as a CTE (design §5.11).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - <see cref="SeedAsync"/>: Alice (1) owns Rex and Tom; Bob the pet has no owner.
    ///
    /// Steps:
    /// - Name the number of pets per owner, and select the owners with more than one pet from it, without and with
    ///   <c>With</c>.
    ///
    /// Expected:
    /// - 1 both times.
    /// </remarks>
    [Fact]
    public async Task DerivedTableAndCte_ReturnTheSameRows()
    {
        Database<TDialect> db = await SeedAsync();
        var counts = db.From(Pet.Table)
            .GroupBy(x => x.OwnerId)
            .Select((k, x) => new OwnerPets.GroupProjection { Owner = k, Count = Count() })
            .As("c");

        List<long?> derived = await db.From(counts).Where(c => Gt(c.Count, 1L)).Select(c => c.Owner).ToListAsync(Ct);
        List<long?> cte = await db.With(counts).From(counts).Where(c => Gt(c.Count, 1L)).Select(c => c.Owner).ToListAsync(Ct);

        Assert.Equal([(long?)1], derived);
        Assert.Equal([(long?)1], cte);
    }

    /// <summary>
    /// A left join with a named query reads its whole row, which is <see langword="null"/> where nothing matched (design §5.3,
    /// §5.11).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - <see cref="SeedAsync"/>.
    ///
    /// Steps:
    /// - People left join the number of pets per owner, as a CTE; select the name and the whole row, ordered by the id.
    ///
    /// Expected:
    /// - (Alice, (1, 2)), (Bob, null), (Carol, null), (Dave, null).
    /// </remarks>
    [Fact]
    public async Task LeftJoin_NamedQuery_ReadsANullableRow()
    {
        Database<TDialect> db = await SeedAsync();
        var counts = db.From(Pet.Table)
            .GroupBy(x => x.OwnerId)
            .Select((k, x) => new OwnerPets.GroupProjection { Owner = k, Count = Count() })
            .As("c");

        List<(string, OwnerPets?)> rows = await db.With(counts)
            .From(Person.Table)
            .LeftJoin(counts, (p, c) => Eq(c.Owner, p.Id))
            .Select((p, c) => (p.Name, c))
            .OrderBy((p, c) => [Asc(p.Id)])
            .ToListAsync(Ct);

        Assert.Equal([("Alice", new OwnerPets(1, 2)), ("Bob", (OwnerPets?)null), ("Carol", null), ("Dave", null)], rows);
    }

    /// <summary>
    /// A later CTE reads an earlier one, and whole rows of a table go through both (design §5.11).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - <see cref="SeedAsync"/>.
    ///
    /// Steps:
    /// - <c>a</c>: the people with an id over 1; <c>b</c>: the rows of <c>a</c> with an id under 4. Select the whole rows of
    ///   <c>b</c> ordered by the id, with <c>With(a, b)</c>.
    ///
    /// Expected:
    /// - Bob and Carol, read as <see cref="Person"/>.
    /// </remarks>
    [Fact]
    public async Task TwoCtes_TheSecondReadsTheFirst()
    {
        Database<TDialect> db = await SeedAsync();
        var a = db.From(Person.Table).Where(p => Gt(p.Id, 1L)).Select(p => p).As("a");
        var b = db.From(a).Where(x => Lt(x.Id, 4L)).Select(x => x).As("b");

        List<Person> rows = await db.With(a, b).From(b).Select(x => x).OrderBy(x => [Asc(x.Id)]).ToListAsync(Ct);

        Assert.Equal(["Bob", "Carol"], rows.Select(r => r.Name));
    }

    /// <summary>
    /// A set operation is a CTE whose operands all name their items, and a second reference to it has an alias (design
    /// §5.11).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - <see cref="SeedAsync"/>.
    ///
    /// Steps:
    /// - Name the union of (name, id) of the people and of the pets as <c>u</c>; join <c>u</c> with <c>u.As("u2")</c> on the next
    ///   value; select both names, ordered by the value.
    ///
    /// Expected:
    /// - (Alice, Bob), (Bob, Carol), (Carol, Dave), (Rex, Tom), (Tom, Bob).
    /// </remarks>
    [Fact]
    public async Task SetOperationCte_JoinedWithItself()
    {
        Database<TDialect> db = await SeedAsync();
        var u = db.From(Person.Table)
            .Select(p => new NamedValue.RowProjection { Name = p.Name, Value = p.Id })
            .Union(db.From(Pet.Table).Select(x => new NamedValue.RowProjection { Name = x.Name, Value = x.Id }))
            .As("u");

        List<(string, string)> rows = await db.With(u)
            .From(u)
            .InnerJoin(u.As("u2"), (x, y) => Eq(y.Value, Add(x.Value, 1L)))
            .Select((x, y) => (x.Name, y.Name))
            .OrderBy((x, y) => [Asc(x.Value)])
            .ToListAsync(Ct);

        Assert.Equal([("Alice", "Bob"), ("Bob", "Carol"), ("Carol", "Dave"), ("Rex", "Tom"), ("Tom", "Bob")], rows);
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
/// <see cref="NamedQueryTests{TDialect}"/> on SQLite.
/// </summary>
public sealed class SqliteNamedQueryTests : NamedQueryTests<SqliteDialect>, IAsyncLifetime
{
    private readonly SqliteTarget _target = new();

    private protected override ITestTarget<SqliteDialect> Target => _target;

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public ValueTask DisposeAsync() => _target.DisposeAsync();
}
