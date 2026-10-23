namespace AuraORM.Integration.Tests;

/// <summary>
/// Each column type of design §3.3 and its null, written as a parameter and read back, on one database (design §12.4). A
/// derived class per database gives the target.
/// </summary>
/// <typeparam name="TDialect">The dialect.</typeparam>
public abstract class TypedValueTests<TDialect>
    where TDialect : IDialect
{
    private static readonly TypedRow Values = new(
        1,
        true,
        200,
        -300,
        70_000,
        5_000_000_000,
        1.5f,
        2.25,
        1234.5678m,
        "héllo",
        [0, 1, 255],
        new Guid("6f9619ff-8b86-d011-b42d-00c04fc964ff"),
        new DateTime(2024, 5, 6, 7, 8, 9, DateTimeKind.Unspecified).AddTicks(1_234_560),
        new DateTimeOffset(2024, 5, 6, 7, 8, 9, TimeSpan.Zero).AddTicks(1_234_560),
        new DateOnly(2024, 5, 6),
        new TimeOnly(7, 8, 9).Add(TimeSpan.FromTicks(1_234_560)),
        PersonStatus.Active
    );

    // Past the sizes up to which SqlClient infers nvarchar(4000) and varbinary(8000) for an untyped value.
    private static readonly TypedRow Large = new(
        3,
        null,
        null,
        null,
        null,
        null,
        null,
        null,
        null,
        new string('x', 5000),
        [.. Enumerable.Repeat((byte)7, 9000)],
        null,
        null,
        null,
        null,
        null,
        null
    );

    private static readonly TypedRow Nulls = new(2, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null);

    private protected abstract ITestTarget<TDialect> Target { get; }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>
    /// A value of every column type, and a null of every column type, are bound as parameters and read back unchanged.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - An empty <c>typed_values</c> table, with the SQL type of design §3.3 for each column.
    ///
    /// Steps:
    /// - Insert one row of values, one row of nulls, and one row with a 5000-character string and a 9000-byte array, with a
    ///   raw command whose holes are the values.
    /// - Read the rows into <see cref="TypedRow"/>, ordered by id.
    ///
    /// Expected:
    /// - The rows equal what was inserted (the byte array compared by its bytes). A null <see cref="byte"/> array is sent
    ///   with <c>DbType.Binary</c>, so SQL Server accepts it for <c>varbinary</c>, and a <see cref="DateTime"/> keeps its 100 ns
    ///   ticks on SQL Server (<c>DbType.DateTime2</c>).
    /// </remarks>
    [Fact]
    public async Task InsertAndRead_EveryTypeAndNull_RoundTrips()
    {
        Database<TDialect> db = await Target.CreateDatabaseAsync();

        Assert.Equal(1, await InsertAsync(db, Values));
        Assert.Equal(1, await InsertAsync(db, Nulls));
        Assert.Equal(1, await InsertAsync(db, Large));
        List<TypedRow> rows = await db.QueryAsync<TypedRow>(
            $"SELECT id, flag_value, tiny_value, small_value, number_value, big_value, single_value, double_value, money_value, text_value, blob_value, key_value, at_value, at_offset_value, day_value, time_value, state_value FROM {TypedRow.Table} ORDER BY id",
            Ct
        );

        Assert.Equal(3, rows.Count);
        TypedRow read = rows[0];
        Assert.Equal(Values.FlagValue, read.FlagValue);
        Assert.Equal(Values.TinyValue, read.TinyValue);
        Assert.Equal(Values.SmallValue, read.SmallValue);
        Assert.Equal(Values.NumberValue, read.NumberValue);
        Assert.Equal(Values.BigValue, read.BigValue);
        Assert.Equal(Values.SingleValue, read.SingleValue);
        Assert.Equal(Values.DoubleValue, read.DoubleValue);
        Assert.Equal(Values.MoneyValue, read.MoneyValue);
        Assert.Equal(Values.TextValue, read.TextValue);
        Assert.Equal(Values.BlobValue, read.BlobValue);
        Assert.Equal(Values.KeyValue, read.KeyValue);
        Assert.Equal(Values.AtValue?.Ticks, read.AtValue?.Ticks);
        Assert.Equal(Values.AtOffsetValue?.UtcTicks, read.AtOffsetValue?.UtcTicks);
        Assert.Equal(Values.DayValue, read.DayValue);
        Assert.Equal(Values.TimeValue?.Ticks, read.TimeValue?.Ticks);
        Assert.Equal(Values.StateValue, read.StateValue);
        Assert.Equal(Nulls, rows[1]);
        Assert.Equal(Large.TextValue, rows[2].TextValue);
        Assert.Equal(Large.BlobValue, rows[2].BlobValue);
    }

    private static Task<int> InsertAsync(Database<TDialect> db, TypedRow row) =>
        db.ExecuteAsync(
            $"INSERT INTO {TypedRow.Table} (id, flag_value, tiny_value, small_value, number_value, big_value, single_value, double_value, money_value, text_value, blob_value, key_value, at_value, at_offset_value, day_value, time_value, state_value) VALUES ({row.Id}, {row.FlagValue}, {row.TinyValue}, {row.SmallValue}, {row.NumberValue}, {row.BigValue}, {row.SingleValue}, {row.DoubleValue}, {row.MoneyValue}, {row.TextValue}, {row.BlobValue}, {row.KeyValue}, {row.AtValue}, {row.AtOffsetValue}, {row.DayValue}, {row.TimeValue}, {row.StateValue})",
            Ct
        );
}

/// <summary>
/// <see cref="TypedValueTests{TDialect}"/> on SQLite.
/// </summary>
public sealed class SqliteTypedValueTests : TypedValueTests<SqliteDialect>, IAsyncLifetime
{
    private readonly SqliteTarget _target = new();

    private protected override ITestTarget<SqliteDialect> Target => _target;

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public ValueTask DisposeAsync() => _target.DisposeAsync();
}
