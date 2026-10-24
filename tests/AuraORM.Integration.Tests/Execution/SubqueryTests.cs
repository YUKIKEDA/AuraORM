using static AuraORM.Sql;

namespace AuraORM.Integration.Tests;

/// <summary>
/// Subqueries (design §5.11) on one database: people and their pets. A derived class per database gives the target.
/// </summary>
/// <typeparam name="TDialect">The dialect.</typeparam>
public abstract class SubqueryTests<TDialect>
    where TDialect : IDialect
{
    private static readonly (long Id, string Name, int? Age)[] People = [(1, "Alice", 30), (2, "Bob", 30), (3, "Carol", 40), (4, "Dave", null)];

    private static readonly (long Id, long? Owner, string Name)[] Pets = [(10, 1, "Rex"), (11, 1, "Tom"), (12, null, "Stray"), (13, 99, "Ghost")];

    private protected abstract ITestTarget<TDialect> Target { get; }

    private protected static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>
    /// <c>In</c> keeps the people who own a pet, with a subquery of the nullable owner id; <c>NotIn</c> with a subquery that
    /// returns no null keeps the others (design §5.11).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - <see cref="SeedAsync"/>: Alice owns Rex and Tom; Stray has no owner; Ghost's owner (99) does not exist.
    ///
    /// Steps:
    /// - Select the names of the people whose id is in the pets' owner ids, and of those whose id is not in
    ///   <c>Coalesce(owner_id, 0)</c>, ordered by the id.
    ///
    /// Expected:
    /// - Alice; Bob, Carol, Dave.
    /// </remarks>
    [Fact]
    public async Task InAndNotIn_FilterByASubquery()
    {
        Database<TDialect> db = await SeedAsync();

        List<string> owners = await db.From(Person.Table)
            .Where(p => In(p.Id, db.From(Pet.Table).Select(x => x.OwnerId)))
            .Select(p => p.Name)
            .OrderBy(p => [Asc(p.Id)])
            .ToListAsync(Ct);
        List<string> others = await db.From(Person.Table)
            .Where(p => NotIn(p.Id, db.From(Pet.Table).Select(x => Coalesce(x.OwnerId, 0L))))
            .Select(p => p.Name)
            .OrderBy(p => [Asc(p.Id)])
            .ToListAsync(Ct);

        Assert.Equal(["Alice"], owners);
        Assert.Equal(["Bob", "Carol", "Dave"], others);
    }

    /// <summary>
    /// A correlated <c>Exists</c> filters by the outer row, and a correlated <c>Scalar</c> reads a count and a value that is
    /// null when the subquery has no row (design §5.11).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - <see cref="SeedAsync"/>.
    ///
    /// Steps:
    /// - Select the people who have a pet named Tom; select each person's name, number of pets, and largest pet name.
    ///
    /// Expected:
    /// - Alice; (Alice, 2, Tom), (Bob, 0, null), (Carol, 0, null), (Dave, 0, null).
    /// </remarks>
    [Fact]
    public async Task ExistsAndScalar_Correlated_ReadPerRow()
    {
        Database<TDialect> db = await SeedAsync();

        List<string> withTom = await db.From(Person.Table)
            .Where(p => Exists(db.From(Pet.Table).Where(x => And(Eq(x.OwnerId, p.Id), Eq(x.Name, "Tom"))).Select(x => x.Id)))
            .Select(p => p.Name)
            .ToListAsync(Ct);
        List<(string, long?, string?)> counts = await db.From(Person.Table)
            .Select(p =>
                (
                    p.Name,
                    Scalar(db.From(Pet.Table).Where(x => Eq(x.OwnerId, p.Id)).Select(x => Count())),
                    Scalar(db.From(Pet.Table).Where(x => Eq(x.OwnerId, p.Id)).Select(x => Max(x.Name)))
                )
            )
            .OrderBy(p => [Asc(p.Id)])
            .ToListAsync(Ct);

        Assert.Equal(["Alice"], withTom);
        Assert.Equal([("Alice", (long?)2, (string?)"Tom"), ("Bob", 0, null), ("Carol", 0, null), ("Dave", 0, null)], counts);
    }

    /// <summary>
    /// <c>GroupScalar</c> in <c>HAVING</c> and <c>GroupExists</c> in a grouped projection correlate with the grouped
    /// statement through <c>Outer(k)</c> on every database, also from a subquery nested inside them (design §5.11).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - <see cref="SeedAsync"/>.
    ///
    /// Steps:
    /// - Group the people by id, keep the groups whose count is at most the person's number of pets, and select the id and
    ///   whether the person has a pet named Rex; then group them by age and keep the ages for which a pet's owner is person 1
    ///   of that age, found by a <c>Scalar</c> nested in <c>GroupExists</c> that uses <c>Outer(k)</c>.
    ///
    /// Expected:
    /// - (1, true); 30.
    /// </remarks>
    [Fact]
    public async Task GroupScalarAndGroupExists_CorrelateThroughOuter()
    {
        Database<TDialect> db = await SeedAsync();

        List<(long, bool)> rows = await db.From(Person.Table)
            .GroupBy(p => p.Id)
            .Having((k, p) => Le(Count(), GroupScalar(db.From(Pet.Table).Where(x => Eq(x.OwnerId, Outer(k))).Select(x => Count()))))
            .Select(
                (k, p) =>
                    (
                        k,
                        Case(GroupExists(db.From(Pet.Table).Where(x => And(Eq(x.OwnerId, Outer(k)), Eq(x.Name, "Rex"))).Select(x => x.Id)), true)
                            .Else(false)
                    )
            )
            .ToListAsync(Ct);
        List<int?> ages = await db.From(Person.Table)
            .GroupBy(p => p.Age)
            .Having(
                (k, p) =>
                    GroupExists(
                        db.From(Pet.Table)
                            .Where(x =>
                                Eq(
                                    x.OwnerId,
                                    Scalar(db.From(Person.Table.As("o")).Where(o => And(Eq(o.Age, Outer(k)), Eq(o.Id, 1L))).Select(o => o.Id))
                                )
                            )
                            .Select(x => x.Id)
                    )
            )
            .Select((k, p) => k)
            .ToListAsync(Ct);

        Assert.Equal([(1L, true)], rows);
        Assert.Equal([(int?)30], ages);
    }

    /// <summary>
    /// A self-correlated subquery with an alias on the inner table compares the inner rows with the outer row on every
    /// database; without the alias it throws before it is sent, because SQL would read both sides as the inner table
    /// (design §5.11).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - <see cref="SeedAsync"/>: Alice (1) and Bob (2) are 30, Carol (3) is 40, Dave (4) has no age.
    ///
    /// Steps:
    /// - Select the people for whom someone of the same age has a larger id, with <c>As("o")</c> on the inner table; build
    ///   the same without the alias.
    ///
    /// Expected:
    /// - Alice; <see cref="InvalidOperationException"/>.
    /// </remarks>
    [Fact]
    public async Task SelfCorrelation_NeedsAnAlias()
    {
        Database<TDialect> db = await SeedAsync();

        List<string> rows = await db.From(Person.Table)
            .Where(p => Exists(db.From(Person.Table.As("o")).Where(o => And(Eq(o.Age, p.Age), Gt(o.Id, p.Id))).Select(o => o.Id)))
            .Select(p => p.Name)
            .ToListAsync(Ct);

        Assert.Equal(["Alice"], rows);
        Assert.Throws<InvalidOperationException>(() =>
            db.From(Person.Table)
                .Where(p => Exists(db.From(Person.Table).Where(o => And(Eq(o.Age, p.Age), Gt(o.Id, p.Id))).Select(o => o.Id)))
                .Select(p => p.Name)
                .ToSql()
        );
    }

    /// <summary>
    /// <c>Exists</c> and <c>Scalar</c> accept a subquery with a row limit on every database, MySQL and MariaDB included
    /// (design §5.11).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - <see cref="SeedAsync"/>.
    ///
    /// Steps:
    /// - Select the people who have a pet, with an ordered, limited <c>Exists</c>; select the name of Alice's first pet by
    ///   name with an ordered, limited <c>Scalar</c>.
    ///
    /// Expected:
    /// - Alice; Rex.
    /// </remarks>
    [Fact]
    public async Task ExistsAndScalar_WithLimit_RunEverywhere()
    {
        Database<TDialect> db = await SeedAsync();

        List<string> owners = await db.From(Person.Table)
            .Where(p => Exists(db.From(Pet.Table).Where(x => Eq(x.OwnerId, p.Id)).Select(x => x.Id).OrderBy(x => [Asc(x.Id)]).Limit(1)))
            .Select(p => p.Name)
            .ToListAsync(Ct);
        string? first = await db.From(Person.Table)
            .Where(p => Eq(p.Id, 1L))
            .Select(p => Scalar(db.From(Pet.Table).Where(x => Eq(x.OwnerId, p.Id)).Select(x => x.Name).OrderBy(x => [Asc(x.Name)]).Limit(1)))
            .SingleAsync(Ct);

        Assert.Equal(["Alice"], owners);
        Assert.Equal("Rex", first);
    }

    private protected async Task<Database<TDialect>> SeedAsync()
    {
        Database<TDialect> db = await Target.CreateDatabaseAsync();
        foreach ((long id, string name, int? age) in People)
        {
            await db.ExecuteAsync(
                $"INSERT INTO {Person.Table} (id, name, nickname, age, status, balance, birthday) VALUES ({id}, {name}, {(string?)null}, {age}, {PersonStatus.Active}, {0m}, {new DateOnly(2000, 1, 1)})",
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
/// <see cref="SubqueryTests{TDialect}"/> and a row limit in an <c>IN</c> subquery, for the dialects that accept it
/// (<see cref="ILimitInSubqueryDialect"/>).
/// </summary>
/// <typeparam name="TDialect">The dialect.</typeparam>
public abstract class LimitInSubqueryTests<TDialect> : SubqueryTests<TDialect>
    where TDialect : ILimitInSubqueryDialect
{
    /// <summary>
    /// <c>In</c> with an ordered, limited subquery keeps only the limited values (design §5.11).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - <see cref="SubqueryTests{TDialect}.SeedAsync"/>.
    ///
    /// Steps:
    /// - Select the names of the people whose id is among the two smallest ids.
    ///
    /// Expected:
    /// - Alice, Bob.
    /// </remarks>
    [Fact]
    public async Task In_WithLimit_KeepsTheLimitedValues()
    {
        Database<TDialect> db = await SeedAsync();

        List<string> rows = await db.From(Person.Table)
            .Where(p => In(p.Id, db.From(Person.Table.As("o")).Select(o => o.Id).OrderBy(o => [Asc(o.Id)]).Limit(2)))
            .Select(p => p.Name)
            .OrderBy(p => [Asc(p.Id)])
            .ToListAsync(Ct);

        Assert.Equal(["Alice", "Bob"], rows);
    }
}

/// <summary>
/// <see cref="LimitInSubqueryTests{TDialect}"/> on SQLite.
/// </summary>
public sealed class SqliteSubqueryTests : LimitInSubqueryTests<SqliteDialect>, IAsyncLifetime
{
    private readonly SqliteTarget _target = new();

    private protected override ITestTarget<SqliteDialect> Target => _target;

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public ValueTask DisposeAsync() => _target.DisposeAsync();
}
