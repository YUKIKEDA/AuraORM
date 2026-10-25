using System.Data.Common;

namespace AuraORM.Tests;

/// <summary>
/// A row of values that do not allow null, for the allocation test of row reading.
/// </summary>
[Table("alloc_people")]
public sealed partial record AllocPerson(
    [PrimaryKey] long Id,
    [MaxLength(50)] string Name,
    int Age,
    [Precision(18, 2)] decimal Balance,
    DateTime CreatedAt,
    bool Active
);

/// <summary>
/// A row of values that allow null, for the allocation test of row reading.
/// </summary>
[Table("alloc_maybe")]
public sealed partial record AllocMaybe(
    [PrimaryKey] long Id,
    string? Nickname,
    int? Age,
    [Precision(18, 2)] decimal? Balance,
    DateTime? Seen,
    bool? Flag
);

public sealed class RowAllocationTests
{
    /// <summary>
    /// Reading rows allocates nothing beyond the result: the allocation that more rows add is the same as that of code that
    /// reads the same reader by hand into the same list (requirement N4, design §16.1).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - A reader whose typed getters return stored values without allocating, with 10 and 1000 rows of six columns
    ///   that do not allow null.
    ///
    /// Steps:
    /// - After three warm-ups, take the least of five measurements of the bytes allocated on the thread by
    ///   <c>ToListAsync</c> of the whole row, and by a hand-written loop that reads each column by ordinal with its typed
    ///   getter into a <see cref="List{T}"/>, for each
    ///   row count.
    ///
    /// Expected:
    /// - The difference between 1000 and 10 rows is the same for both.
    /// </remarks>
    [Fact]
    public void ToListAsync_RowsWithoutNulls_AllocateLikeHandWrittenReading()
    {
        long aura = PerRows(rows => Run(Aura(People(rows))));
        long hand = PerRows(rows => Run(HandPeople(People(rows))));
        Assert.Equal(hand, aura);
    }

    /// <summary>
    /// Reading rows of nullable values, half of them NULL, allocates nothing beyond the result either: no value is boxed
    /// (requirement N4, design §16.1).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - A reader with 10 and 1000 rows of five nullable columns, NULL in every other row.
    ///
    /// Steps:
    /// - Measure as in <see cref="ToListAsync_RowsWithoutNulls_AllocateLikeHandWrittenReading"/>, with a hand-written loop
    ///   that checks <c>IsDBNull</c> before each typed getter.
    ///
    /// Expected:
    /// - The difference between 1000 and 10 rows is the same for both.
    /// </remarks>
    [Fact]
    public void ToListAsync_RowsWithNulls_AllocateLikeHandWrittenReading()
    {
        long aura = PerRows(rows => Run(AuraMaybe(Maybe(rows))));
        long hand = PerRows(rows => Run(HandMaybe(Maybe(rows))));
        Assert.Equal(hand, aura);
    }

    // The allocation that 990 more rows add.
    private static long PerRows(Func<int, Action> run) => Allocated(run(1000)) - Allocated(run(10));

    // The least of five measurements after three warm-ups: a one-time allocation of the runtime (a method recompiled by the
    // tiered JIT) can fall into one measurement, as it did once on macOS (536 bytes on net8.0), and is not a cost per row.
    // An allocation per row is in every measurement.
    private static long Allocated(Action action)
    {
        for (int i = 0; i < 3; i++)
        {
            action();
        }

        long least = long.MaxValue;
        for (int i = 0; i < 5; i++)
        {
            long before = GC.GetAllocatedBytesForCurrentThread();
            action();
            least = Math.Min(least, GC.GetAllocatedBytesForCurrentThread() - before);
        }

        return least;
    }

    // The fake reader completes every call at once, so the whole read runs on this thread.
    private static Action Run<T>(Func<Task<T>> read) => () => _ = read().GetAwaiter().GetResult();

    private static Func<Task<List<AllocPerson>>> Aura(FakeConnection connection)
    {
        var db = new Database<SqliteDialect>(connection);
        return () => db.From(AllocPerson.Table).Select(p => p).ToListAsync();
    }

    private static Func<Task<List<AllocMaybe>>> AuraMaybe(FakeConnection connection)
    {
        var db = new Database<SqliteDialect>(connection);
        return () => db.From(AllocMaybe.Table).Select(p => p).ToListAsync();
    }

    private static Func<Task<List<AllocPerson>>> HandPeople(FakeConnection connection) =>
        async () =>
        {
            await using DbCommand command = connection.CreateCommand();
            command.CommandText = "SELECT";
            await using DbDataReader reader = await command.ExecuteReaderAsync();
            var rows = new List<AllocPerson>();
            while (await reader.ReadAsync())
            {
                rows.Add(
                    new AllocPerson(
                        reader.GetInt64(0),
                        reader.GetString(1),
                        reader.GetInt32(2),
                        reader.GetDecimal(3),
                        reader.GetDateTime(4),
                        reader.GetBoolean(5)
                    )
                );
            }

            return rows;
        };

    private static Func<Task<List<AllocMaybe>>> HandMaybe(FakeConnection connection) =>
        async () =>
        {
            await using DbCommand command = connection.CreateCommand();
            command.CommandText = "SELECT";
            await using DbDataReader reader = await command.ExecuteReaderAsync();
            var rows = new List<AllocMaybe>();
            while (await reader.ReadAsync())
            {
                rows.Add(
                    new AllocMaybe(
                        reader.GetInt64(0),
                        reader.IsDBNull(1) ? null : reader.GetString(1),
                        reader.IsDBNull(2) ? null : reader.GetInt32(2),
                        reader.IsDBNull(3) ? null : reader.GetDecimal(3),
                        reader.IsDBNull(4) ? null : reader.GetDateTime(4),
                        reader.IsDBNull(5) ? null : reader.GetBoolean(5)
                    )
                );
            }

            return rows;
        };

    private static FakeConnection People(int rows)
    {
        string[] names = ["Id", "Name", "Age", "Balance", "CreatedAt", "Active"];
        long[] ids = Enumerable.Range(1, rows).Select(i => (long)i).ToArray();
        string[] text = Enumerable.Range(1, rows).Select(i => "name" + i).ToArray();
        int[] ages = Enumerable.Range(1, rows).ToArray();
        decimal[] balances = Enumerable.Range(1, rows).Select(i => i * 1.5m).ToArray();
        DateTime[] times = Enumerable.Range(1, rows).Select(i => new DateTime(2024, 1, 1).AddMinutes(i)).ToArray();
        bool[] active = Enumerable.Range(1, rows).Select(i => i % 2 == 0).ToArray();
        return new FakeConnection { Reader = () => new ArrayDataReader(names, [ids, text, ages, balances, times, active], new bool[]?[6]) };
    }

    private static FakeConnection Maybe(int rows)
    {
        string[] names = ["Id", "Nickname", "Age", "Balance", "Seen", "Flag"];
        bool[] nulls = Enumerable.Range(1, rows).Select(i => i % 2 == 0).ToArray();
        long[] ids = Enumerable.Range(1, rows).Select(i => (long)i).ToArray();
        string?[] text = Enumerable.Range(1, rows).Select(i => i % 2 == 0 ? null : "nick" + i).ToArray();
        int[] ages = Enumerable.Range(1, rows).ToArray();
        decimal[] balances = Enumerable.Range(1, rows).Select(i => i * 1.5m).ToArray();
        DateTime[] times = Enumerable.Range(1, rows).Select(i => new DateTime(2024, 1, 1).AddMinutes(i)).ToArray();
        bool[] flags = Enumerable.Range(1, rows).Select(i => i % 3 == 0).ToArray();
        return new FakeConnection
        {
            Reader = () => new ArrayDataReader(names, [ids, text, ages, balances, times, flags], [null, null, nulls, nulls, nulls, nulls]),
        };
    }
}
