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

/// <summary>
/// An enum of each integer type that a column can have, for the enum branches of the reading (design §3.3).
/// </summary>
public enum AllocByte : byte
{
    A = 1,
}

/// <inheritdoc cref="AllocByte"/>
public enum AllocShort : short
{
    A = 1,
}

/// <inheritdoc cref="AllocByte"/>
public enum AllocInt
{
    A = 1,
}

/// <inheritdoc cref="AllocByte"/>
public enum AllocLong : long
{
    A = 1,
}

/// <summary>
/// The column types of design §3.3 that <see cref="AllocPerson"/> does not have, without null: every other branch of the
/// reading of a value.
/// </summary>
[Table("alloc_every")]
public sealed partial record AllocEvery(
    [PrimaryKey] long Id,
    short Small,
    byte Tiny,
    float Single,
    double Real,
    Guid Key,
    DateTimeOffset At,
    DateOnly Day,
    TimeOnly Time,
    byte[] Data,
    AllocByte E8,
    AllocShort E16,
    AllocInt E32,
    AllocLong E64
);

/// <summary>
/// The column types of design §3.3 that <see cref="AllocMaybe"/> does not have, each allowing null.
/// </summary>
[Table("alloc_every_maybe")]
public sealed partial record AllocEveryMaybe(
    [PrimaryKey] long Id,
    long? Big,
    short? Small,
    byte? Tiny,
    float? Single,
    double? Real,
    Guid? Key,
    DateTimeOffset? At,
    DateOnly? Day,
    TimeOnly? Time,
    byte[]? Data,
    AllocByte? E8,
    AllocShort? E16,
    AllocInt? E32,
    AllocLong? E64
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

    /// <summary>
    /// The other column types of design §3.3 allocate nothing beyond the result either, without null and with null: each
    /// branch of the reading of a value, the enums of every underlying type included (requirement N4).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - A reader with 10 and 1000 rows of <c>short</c>, <c>byte</c>, <c>float</c>, <c>double</c>, <see cref="Guid"/>,
    ///   <see cref="DateTimeOffset"/>, <see cref="DateOnly"/>, <see cref="TimeOnly"/>, <c>byte[]</c>, and an enum of
    ///   <c>byte</c>, <c>short</c>, <c>int</c>, and <c>long</c>; and one of the same types and <c>long</c>, each nullable,
    ///   NULL in every other row.
    ///
    /// Steps:
    /// - Measure as in <see cref="ToListAsync_RowsWithoutNulls_AllocateLikeHandWrittenReading"/>, with a hand-written loop
    ///   that reads each column with the getter that the reading uses for its type.
    ///
    /// Expected:
    /// - The difference between 1000 and 10 rows is the same for both, for each table.
    /// </remarks>
    [Fact]
    public void ToListAsync_EveryOtherType_AllocatesLikeHandWrittenReading()
    {
        Assert.Equal(PerRows(rows => Run(HandEvery(Every(rows)))), PerRows(rows => Run(AuraEvery(Every(rows)))));
        Assert.Equal(PerRows(rows => Run(HandEveryMaybe(EveryMaybe(rows)))), PerRows(rows => Run(AuraEveryMaybe(EveryMaybe(rows)))));
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

    private static Func<Task<List<AllocEvery>>> AuraEvery(FakeConnection connection)
    {
        var db = new Database<SqliteDialect>(connection);
        return () => db.From(AllocEvery.Table).Select(p => p).ToListAsync();
    }

    private static Func<Task<List<AllocEveryMaybe>>> AuraEveryMaybe(FakeConnection connection)
    {
        var db = new Database<SqliteDialect>(connection);
        return () => db.From(AllocEveryMaybe.Table).Select(p => p).ToListAsync();
    }

    private static Func<Task<List<AllocEvery>>> HandEvery(FakeConnection connection) =>
        async () =>
        {
            await using DbCommand command = connection.CreateCommand();
            command.CommandText = "SELECT";
            await using DbDataReader reader = await command.ExecuteReaderAsync();
            var rows = new List<AllocEvery>();
            while (await reader.ReadAsync())
            {
                rows.Add(
                    new AllocEvery(
                        reader.GetInt64(0),
                        reader.GetInt16(1),
                        reader.GetByte(2),
                        reader.GetFloat(3),
                        reader.GetDouble(4),
                        reader.GetGuid(5),
                        reader.GetFieldValue<DateTimeOffset>(6),
                        reader.GetFieldValue<DateOnly>(7),
                        reader.GetFieldValue<TimeOnly>(8),
                        reader.GetFieldValue<byte[]>(9),
                        (AllocByte)reader.GetByte(10),
                        (AllocShort)reader.GetInt16(11),
                        (AllocInt)reader.GetInt32(12),
                        (AllocLong)reader.GetInt64(13)
                    )
                );
            }

            return rows;
        };

    private static Func<Task<List<AllocEveryMaybe>>> HandEveryMaybe(FakeConnection connection) =>
        async () =>
        {
            await using DbCommand command = connection.CreateCommand();
            command.CommandText = "SELECT";
            await using DbDataReader reader = await command.ExecuteReaderAsync();
            var rows = new List<AllocEveryMaybe>();
            while (await reader.ReadAsync())
            {
                rows.Add(
                    new AllocEveryMaybe(
                        reader.GetInt64(0),
                        reader.IsDBNull(1) ? null : reader.GetInt64(1),
                        reader.IsDBNull(2) ? null : reader.GetInt16(2),
                        reader.IsDBNull(3) ? null : reader.GetByte(3),
                        reader.IsDBNull(4) ? null : reader.GetFloat(4),
                        reader.IsDBNull(5) ? null : reader.GetDouble(5),
                        reader.IsDBNull(6) ? null : reader.GetGuid(6),
                        reader.IsDBNull(7) ? null : reader.GetFieldValue<DateTimeOffset>(7),
                        reader.IsDBNull(8) ? null : reader.GetFieldValue<DateOnly>(8),
                        reader.IsDBNull(9) ? null : reader.GetFieldValue<TimeOnly>(9),
                        reader.IsDBNull(10) ? null : reader.GetFieldValue<byte[]>(10),
                        reader.IsDBNull(11) ? null : (AllocByte)reader.GetByte(11),
                        reader.IsDBNull(12) ? null : (AllocShort)reader.GetInt16(12),
                        reader.IsDBNull(13) ? null : (AllocInt)reader.GetInt32(13),
                        reader.IsDBNull(14) ? null : (AllocLong)reader.GetInt64(14)
                    )
                );
            }

            return rows;
        };

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

    private static FakeConnection Every(int rows)
    {
        string[] names = ["Id", "Small", "Tiny", "Single", "Real", "Key", "At", "Day", "Time", "Data", "E8", "E16", "E32", "E64"];
        return new FakeConnection { Reader = () => new ArrayDataReader(names, EveryColumns(rows, withBig: false), new bool[]?[names.Length]) };
    }

    // NULL in every other row of every column but the key; the byte arrays are null there, the other columns are marked.
    private static FakeConnection EveryMaybe(int rows)
    {
        string[] names = ["Id", "Big", "Small", "Tiny", "Single", "Real", "Key", "At", "Day", "Time", "Data", "E8", "E16", "E32", "E64"];
        bool[] nulls = Enumerable.Range(1, rows).Select(i => i % 2 == 0).ToArray();
        Array[] columns = EveryColumns(rows, withBig: true);
        columns[10] = ((byte[][])columns[10]).Select((d, i) => i % 2 == 1 ? null : d).ToArray();
        bool[]?[] map = Enumerable.Repeat<bool[]?>(nulls, names.Length).ToArray();
        map[0] = null;
        map[10] = null;
        return new FakeConnection { Reader = () => new ArrayDataReader(names, columns, map) };
    }

    // The columns of AllocEvery, with a column of long after the key when withBig (AllocEveryMaybe). The byte arrays are
    // made once, so reading them allocates nothing.
    private static Array[] EveryColumns(int rows, bool withBig)
    {
        int[] range = Enumerable.Range(1, rows).ToArray();
        var columns = new List<Array> { range.Select(i => (long)i).ToArray() };
        if (withBig)
        {
            columns.Add(range.Select(i => (long)i * 7).ToArray());
        }

        columns.Add(range.Select(i => (short)i).ToArray());
        columns.Add(range.Select(i => (byte)i).ToArray());
        columns.Add(range.Select(i => i * 0.5f).ToArray());
        columns.Add(range.Select(i => i * 0.25).ToArray());
        columns.Add(range.Select(i => new Guid(i, 0, 0, new byte[8])).ToArray());
        columns.Add(range.Select(i => new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero).AddMinutes(i)).ToArray());
        columns.Add(range.Select(i => new DateOnly(2024, 1, 1).AddDays(i)).ToArray());
        columns.Add(range.Select(i => new TimeOnly(0, 0).Add(TimeSpan.FromSeconds(i))).ToArray());
        columns.Add(range.Select(i => new[] { (byte)i }).ToArray());
        columns.Add(range.Select(_ => (byte)1).ToArray());
        columns.Add(range.Select(_ => (short)1).ToArray());
        columns.Add(range.Select(_ => 1).ToArray());
        columns.Add(range.Select(_ => 1L).ToArray());
        return [.. columns];
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
