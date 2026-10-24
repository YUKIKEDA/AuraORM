using static AuraORM.Sql;

namespace AuraORM.Integration.Tests;

/// <summary>
/// GROUP BY, HAVING, and the aggregates (design §4.2, §4.5, §5.5) on one database. A derived class per database gives the
/// target.
/// </summary>
/// <typeparam name="TDialect">The dialect.</typeparam>
public abstract class GroupByTests<TDialect>
    where TDialect : IDialect
{
    // Two rows of the Active group with every value set, one Inactive row with every value null, and one row with no state.
    private static readonly TypedRow[] Rows =
    [
        new(
            1,
            true,
            1,
            10,
            100,
            1000,
            1.5f,
            2.5,
            1.25m,
            "b",
            null,
            null,
            new DateTime(2024, 1, 2, 3, 4, 5),
            new DateTimeOffset(2024, 1, 2, 3, 4, 5, TimeSpan.Zero),
            new DateOnly(2024, 1, 2),
            new TimeOnly(3, 4, 5),
            PersonStatus.Active
        ),
        new(
            2,
            false,
            3,
            30,
            300,
            3000,
            2.5f,
            4.5,
            2.5m,
            "a",
            null,
            null,
            new DateTime(2024, 2, 3, 4, 5, 6),
            new DateTimeOffset(2024, 2, 3, 4, 5, 6, TimeSpan.Zero),
            new DateOnly(2024, 2, 3),
            new TimeOnly(4, 5, 6),
            PersonStatus.Active
        ),
        new(3, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, PersonStatus.Inactive),
        new(4, null, null, null, 7, null, null, null, null, null, null, null, null, null, null, null, null),
    ];

    // The people of the tests on keys: (Active, 30) twice, (Inactive, 40), and (Active, null).
    private static readonly (long Id, string Name, int? Age, PersonStatus Status)[] People =
    [
        (1, "Alice", 30, PersonStatus.Active),
        (2, "Bob", 30, PersonStatus.Active),
        (3, "Carol", 40, PersonStatus.Inactive),
        (4, "Dave", null, PersonStatus.Active),
    ];

    private protected abstract ITestTarget<TDialect> Target { get; }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>
    /// The counts and the integer sums of each group, the group of null keys among them, read as <c>long</c> and
    /// <c>long?</c> (design §4.2, §5.5).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - <see cref="Rows"/>: Active (two rows with values), Inactive (one row of nulls), and a null state (one row with
    ///   <c>number_value</c> 7).
    ///
    /// Steps:
    /// - Group by the state; select the key, <c>Count()</c>, <c>Count</c> and <c>CountDistinct</c> of a column, and
    ///   <c>Sum</c> of the byte, short, int, and long columns.
    ///
    /// Expected:
    /// - Active: 2, 2, 2, 4, 40, 400, 4000. Inactive: 1, 0, 0, and null sums. The null state: 1, 1, 0, null, null, 7, null.
    /// </remarks>
    [Fact]
    public async Task GroupBy_CountsAndIntegerSums_PerGroup()
    {
        Database<TDialect> db = await SeedTypedAsync();

        var rows = await db.From(TypedRow.Table)
            .GroupBy(t => t.StateValue)
            .Select(
                (k, t) =>
                    (
                        k,
                        Count(),
                        Count(t.NumberValue),
                        CountDistinct(t.TextValue),
                        Sum(t.TinyValue),
                        Sum(t.SmallValue),
                        Sum(t.NumberValue),
                        Sum(t.BigValue)
                    )
            )
            .ToListAsync(Ct);

        Assert.Equal(
            [
                ((PersonStatus?)null, 1L, 1L, 0L, (long?)null, (long?)null, (long?)7, (long?)null),
                (PersonStatus.Inactive, 1L, 0L, 0L, null, null, null, null),
                (PersonStatus.Active, 2L, 2L, 2L, 4L, 40L, 400L, 4000L),
            ],
            rows.OrderBy(r => r.Item1 is null ? -1 : (int)r.Item1)
        );
    }

    /// <summary>
    /// The floating-point sums and the averages of each group, read as <c>float?</c> and <c>double?</c>, and null for a
    /// group with no value (design §4.2).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - <see cref="Rows"/>.
    ///
    /// Steps:
    /// - Group the rows that have a state by the state; select the key, <c>Sum</c> of the float and double columns, and
    ///   <c>Avg</c> of the int, double, and byte columns, ordered by the key.
    ///
    /// Expected:
    /// - Inactive: all null. Active: 4, 7, 200, 3.5, 2.
    /// </remarks>
    [Fact]
    public async Task GroupBy_FloatingSumsAndAverages_PerGroup()
    {
        Database<TDialect> db = await SeedTypedAsync();

        var rows = await db.From(TypedRow.Table)
            .Where(t => IsNotNull(t.StateValue))
            .GroupBy(t => t.StateValue)
            .Select((k, t) => (k, Sum(t.SingleValue), Sum(t.DoubleValue), Avg(t.NumberValue), Avg(t.DoubleValue), Avg(t.TinyValue)))
            .OrderBy((k, t) => [Asc(k)])
            .ToListAsync(Ct);

        Assert.Equal(
            [
                ((PersonStatus?)PersonStatus.Inactive, (float?)null, (double?)null, (double?)null, (double?)null, (double?)null),
                (PersonStatus.Active, 4f, 7.0, 200.0, 3.5, 2.0),
            ],
            rows
        );
    }

    /// <summary>
    /// <c>Min</c> and <c>Max</c> of numbers and text read as the column's type (design §4.2).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - <see cref="Rows"/>.
    ///
    /// Steps:
    /// - Group the Active rows by the state; select the key and <c>Min</c> or <c>Max</c> of the byte, short, int, long,
    ///   float, double, and text columns.
    ///
    /// Expected:
    /// - 1, 30, 100, 3000, 1.5, 4.5, "a".
    /// </remarks>
    [Fact]
    public async Task GroupBy_MinAndMaxOfNumbersAndText_ReadAsTheColumnType()
    {
        Database<TDialect> db = await SeedTypedAsync();

        var row = await db.From(TypedRow.Table)
            .Where(t => Eq(t.StateValue, PersonStatus.Active))
            .GroupBy(t => t.StateValue)
            .Select(
                (k, t) =>
                    (
                        k,
                        Min(t.TinyValue),
                        Max(t.SmallValue),
                        Min(t.NumberValue),
                        Max(t.BigValue),
                        Min(t.SingleValue),
                        Max(t.DoubleValue),
                        Min(t.TextValue)
                    )
            )
            .SingleAsync(Ct);

        Assert.Equal(((PersonStatus?)PersonStatus.Active, (byte?)1, (short?)30, (int?)100, (long?)3000, (float?)1.5f, (double?)4.5, "a"), row);
    }

    /// <summary>
    /// <c>Min</c> and <c>Max</c> of dates, times, and an enum read as the column's type (design §4.2).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - <see cref="Rows"/>.
    ///
    /// Steps:
    /// - Group the Active rows by the state; select the key and <c>Min</c> or <c>Max</c> of the DateTime, DateTimeOffset,
    ///   DateOnly, TimeOnly, and enum columns.
    ///
    /// Expected:
    /// - 2024-01-02 03:04:05, 2024-02-03 04:05:06 +00:00, 2024-01-02, 04:05:06, Active.
    /// </remarks>
    [Fact]
    public async Task GroupBy_MinAndMaxOfDatesAndEnums_ReadAsTheColumnType()
    {
        Database<TDialect> db = await SeedTypedAsync();

        var row = await db.From(TypedRow.Table)
            .Where(t => Eq(t.StateValue, PersonStatus.Active))
            .GroupBy(t => t.StateValue)
            .Select((k, t) => (Min(t.AtValue), Max(t.AtOffsetValue), Min(t.DayValue), Max(t.TimeValue), Max(t.StateValue)))
            .SingleAsync(Ct);

        Assert.Equal(new DateTime(2024, 1, 2, 3, 4, 5).Ticks, row.Item1?.Ticks);
        Assert.Equal(new DateTimeOffset(2024, 2, 3, 4, 5, 6, TimeSpan.Zero).UtcTicks, row.Item2?.UtcTicks);
        Assert.Equal(new DateOnly(2024, 1, 2), row.Item3);
        Assert.Equal(new TimeOnly(4, 5, 6), row.Item4);
        Assert.Equal(PersonStatus.Active, row.Item5);
    }

    /// <summary>
    /// The decimal aggregates read as <c>decimal?</c>; SQLite, which stores decimals as text, rejects them before the
    /// statement runs (design §3.3, §4.2).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - <see cref="Rows"/>.
    ///
    /// Steps:
    /// - Group the Active rows by the state; select <c>Sum</c>, <c>Avg</c>, <c>Min</c>, and <c>Max</c> of the decimal
    ///   column. On SQLite, render the statement.
    ///
    /// Expected:
    /// - 3.75, 1.875, 1.25, 2.5; on SQLite, <see cref="InvalidOperationException"/>.
    /// </remarks>
    [Fact]
    public async Task GroupBy_DecimalAggregates_ReadAsDecimal()
    {
        Database<TDialect> db = await SeedTypedAsync();

        var query = db.From(TypedRow.Table)
            .Where(t => Eq(t.StateValue, PersonStatus.Active))
            .GroupBy(t => t.StateValue)
            .Select((k, t) => (Sum(t.MoneyValue), Avg(t.MoneyValue), Min(t.MoneyValue), Max(t.MoneyValue)));
        if (typeof(TDialect) == typeof(SqliteDialect))
        {
            Assert.Throws<InvalidOperationException>(() => query.ToSql());
            return;
        }

        var row = await query.SingleAsync(Ct);

        Assert.Equal(((decimal?)3.75m, (decimal?)1.875m, (decimal?)1.25m, (decimal?)2.5m), row);
    }

    /// <summary>
    /// A key with a value runs on every database, PostgreSQL included, because the key is one parameter wherever it is
    /// written: in the select list, <c>GROUP BY</c>, <c>HAVING</c>, and <c>ORDER BY</c> (design §5.5, §5.12).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - <see cref="SeedPeopleAsync"/>: ages 30, 30, 40, and null.
    ///
    /// Steps:
    /// - Group by <c>Add(p.Age, 1)</c>, keep the keys greater than 30, select the key and the count, ordered by the key.
    ///
    /// Expected:
    /// - (31, 2), (41, 1).
    /// </remarks>
    [Fact]
    public async Task GroupBy_KeyWithValue_RunsOnEveryDatabase()
    {
        Database<TDialect> db = await SeedPeopleAsync();

        List<(int?, long)> rows = await db.From(Person.Table)
            .GroupBy(p => Add(p.Age, 1))
            .Having((k, p) => Gt(k, 30))
            .Select((k, p) => (k, Count()))
            .OrderBy((k, p) => [Asc(k)])
            .ToListAsync(Ct);

        Assert.Equal([((int?)31, 2L), (41, 1L)], rows);
    }

    /// <summary>
    /// A tuple of keys with <c>HAVING</c> on an aggregate, <c>ORDER BY</c> on aggregates, and a limit; and <c>Having</c> with
    /// an empty condition list, which keeps every group (design §4.2, §5.5, §5.8).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - <see cref="SeedPeopleAsync"/>: (Active, 30) twice, (Inactive, 40), (Active, null).
    ///
    /// Steps:
    /// - Group by (status, age), keep the groups with at least one row, select both keys, the count, and the largest name,
    ///   order by the count descending and the largest name, and take one row.
    /// - Group by the status with an empty <c>Having</c> list and count the groups.
    ///
    /// Expected:
    /// - (Active, 30, 2, "Bob").
    /// - Two groups.
    /// </remarks>
    [Fact]
    public async Task GroupBy_TupleKeysHavingOrderAndLimit()
    {
        Database<TDialect> db = await SeedPeopleAsync();

        List<(PersonStatus, int?, long, string?)> top = await db.From(Person.Table)
            .GroupBy(p => (p.Status, p.Age))
            .Having((k, p) => Ge(Count(), 1L))
            .Select((k, p) => (k.Item1, k.Item2, Count(), Max(p.Name)))
            .OrderBy((k, p) => [Desc(Count()), Asc(Max(p.Name))])
            .Limit(1)
            .ToListAsync(Ct);
        List<PersonStatus> all = await db.From(Person.Table)
            .GroupBy(p => p.Status)
            .Having((k, p) => And(new List<Condition<PerGroup>?> { null }))
            .Select((k, p) => k)
            .ToListAsync(Ct);

        Assert.Equal([(PersonStatus.Active, (int?)30, 2L, "Bob")], top);
        Assert.Equal(2, all.Count);
    }

    /// <summary>
    /// A grouped left join counts the matches of the nullable side, 0 for a person without a pet (design §5.3, §5.5).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - <see cref="SeedPeopleAsync"/>: Alice has the pets Rex and Tom; Bob, Carol, and Dave have none.
    ///
    /// Steps:
    /// - People left join pets, grouped by the person's name; select the name and the count of pet ids, ordered by the name.
    ///
    /// Expected:
    /// - (Alice, 2), (Bob, 0), (Carol, 0), (Dave, 0).
    /// </remarks>
    [Fact]
    public async Task GroupBy_LeftJoin_CountsTheNullableSide()
    {
        Database<TDialect> db = await SeedPeopleAsync();

        List<(string, long)> rows = await db.From(Person.Table)
            .LeftJoin(Pet.Table, (p, pet) => Eq(pet.OwnerId, p.Id))
            .GroupBy((p, pet) => p.Name)
            .Select((k, p, pet) => (k, Count(pet.Id)))
            .OrderBy((k, p, pet) => [Asc(k)])
            .ToListAsync(Ct);

        Assert.Equal([("Alice", 2L), ("Bob", 0L), ("Carol", 0L), ("Dave", 0L)], rows);
    }

    /// <summary>
    /// An expression of values and a function as <c>ORDER BY</c> keys are expressions on every database, not column
    /// positions, so the next key decides the order (design §5.8).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - <see cref="SeedPeopleAsync"/>: ids 1 to 4.
    ///
    /// Steps:
    /// - Select the ids ordered by <c>Add(Value(1), Value(0))</c>, then <c>CurrentTimestamp()</c>, then the id descending.
    ///
    /// Expected:
    /// - 4, 3, 2, 1. A key read as column position 1 would order by the id ascending.
    /// </remarks>
    [Fact]
    public async Task OrderBy_ValueExpressionAndFunction_AreNotColumnPositions()
    {
        Database<TDialect> db = await SeedPeopleAsync();

        List<long> ids = await db.From(Person.Table)
            .Select(p => p.Id)
            .OrderBy(p => [Asc(Add(Value(1), Value(0))), Asc(CurrentTimestamp()), Desc(p.Id)])
            .ToListAsync(Ct);

        Assert.Equal([4L, 3L, 2L, 1L], ids);
    }

    /// <summary>
    /// On an empty table, an aggregate projection without <c>GroupBy</c> returns one row (a count of 0, null for the
    /// others), and a grouped statement returns no row (design §4.5).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - An empty <c>pets</c> table.
    ///
    /// Steps:
    /// - Select <c>Count()</c>, <c>Sum</c> of the owner ids, and <c>Max</c> of the names; then the same grouped by the name.
    ///
    /// Expected:
    /// - (0, null, null); an empty list.
    /// </remarks>
    [Fact]
    public async Task Aggregate_EmptyTable_OneRowWithoutGroupByAndNoneWithIt()
    {
        Database<TDialect> db = await Target.CreateDatabaseAsync();

        (long, long?, string?) whole = await db.From(Pet.Table).Select(p => (Count(), Sum(p.OwnerId), Max(p.Name))).SingleAsync(Ct);
        List<(string, long)> grouped = await db.From(Pet.Table).GroupBy(p => p.Name).Select((k, p) => (k, Count())).ToListAsync(Ct);

        Assert.Equal((0L, (long?)null, (string?)null), whole);
        Assert.Empty(grouped);
    }

    private async Task<Database<TDialect>> SeedTypedAsync()
    {
        Database<TDialect> db = await Target.CreateDatabaseAsync();
        foreach (TypedRow row in Rows)
        {
            await TypedRows.InsertAsync(db, row, Ct);
        }

        return db;
    }

    private async Task<Database<TDialect>> SeedPeopleAsync()
    {
        Database<TDialect> db = await Target.CreateDatabaseAsync();
        foreach ((long id, string name, int? age, PersonStatus status) in People)
        {
            await db.ExecuteAsync(
                $"INSERT INTO {Person.Table} (id, name, nickname, age, status, balance, birthday) VALUES ({id}, {name}, {(string?)null}, {age}, {status}, {0m}, {new DateOnly(2000, 1, 1)})",
                Ct
            );
        }

        foreach ((long id, string name) in ((long, string)[])[(10, "Rex"), (11, "Tom")])
        {
            await db.ExecuteAsync($"INSERT INTO {Pet.Table} (id, owner_id, name) VALUES ({id}, {1L}, {name})", Ct);
        }

        return db;
    }
}

/// <summary>
/// <see cref="GroupByTests{TDialect}"/> on SQLite.
/// </summary>
public sealed class SqliteGroupByTests : GroupByTests<SqliteDialect>, IAsyncLifetime
{
    private readonly SqliteTarget _target = new();

    private protected override ITestTarget<SqliteDialect> Target => _target;

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public ValueTask DisposeAsync() => _target.DisposeAsync();
}
