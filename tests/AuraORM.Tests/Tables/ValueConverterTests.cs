using static AuraORM.Sql;

namespace AuraORM.Tests;

public sealed class ValueConverterTests
{
    private static readonly Database<SqliteDialect> Sqlite = new(new FakeConnection());
    private static readonly Database<PostgreSqlDialect> PostgreSql = new(new FakeConnection());

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>
    /// A value of a converted type becomes a parameter of the converted value wherever it appears, and null stays null; the
    /// SQL does not change (design §3.6).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - <see cref="Order"/>: an <see cref="OrderId"/> key, an <see cref="OrderId"/>? parent, a <see cref="Uri"/>? link
    ///   (converted on the assembly), and a <see cref="Price"/>? total.
    ///
    /// Steps:
    /// - Render a select where the id equals <c>OrderId(5)</c> or is in <c>[OrderId(6), OrderId(7)]</c>; an insert of an order
    ///   with a link and no parent; an update that sets the link and the total; a raw condition with an <c>OrderId(3)</c>
    ///   in its hole.
    ///
    /// Expected:
    /// - The parameters 5L, 6L, 7L; 1L, "https://example.com/", and null for the parent left out (the column is not listed);
    ///   "https://example.com/a" and 1.50m, then 1L; 3L.
    /// </remarks>
    [Fact]
    public void Parameters_AreTheConvertedValues()
    {
        SqlStatement select = Sqlite
            .From(Order.Table)
            .Where(o => Or(Eq(o.Id, new OrderId(5)), In(o.Id, [new OrderId(6), new OrderId(7)])))
            .Select(o => o.Id)
            .ToSql();
        Assert.Equal("SELECT \"orders\".\"Id\" FROM \"orders\" WHERE (\"orders\".\"Id\" = @p0 OR \"orders\".\"Id\" IN (@p1, @p2))", select.Text);
        Assert.Equal([5L, 6L, 7L], select.Parameters.Select(p => p.Value));

        SqlStatement insert = Sqlite
            .Insert(Order.Table)
            .Values(new Order.Insert { Id = new OrderId(1), Link = new Uri("https://example.com/") })
            .ToSql();
        Assert.Equal("INSERT INTO \"orders\" (\"Id\", \"Link\") VALUES (@p0, @p1)", insert.Text);
        Assert.Equal([1L, "https://example.com/"], insert.Parameters.Select(p => p.Value));

        SqlStatement update = Sqlite
            .Update(Order.Table)
            .Set(o => [Set(o.Link, new Uri("https://example.com/a")), Set(o.Total, new Price(1.50m)), Set(o.Parent, null)])
            .Where(o => Eq(o.Id, new OrderId(1)))
            .ToSql();
        Assert.Equal(["https://example.com/a", 1.50m, null, 1L], update.Parameters.Select(p => p.Value));

        SqlStatement raw = Sqlite.From(Order.Table).Where(o => Raw($"{o.Id} = {new OrderId(3)}")).Select(o => o.Id).ToSql();
        Assert.Equal([3L], raw.Parameters.Select(p => p.Value));
    }

    /// <summary>
    /// A converted column is read through its converter, and a <c>NULL</c> reads as null without calling it (design §3.6).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Fake connections that return the values 5 and <c>NULL</c>, and the strings of two URIs.
    ///
    /// Steps:
    /// - Read the ids, the parents, and the links of the orders.
    ///
    /// Expected:
    /// - <c>OrderId(5)</c>; <c>OrderId(5)</c> and null; the two URIs.
    /// </remarks>
    [Fact]
    public async Task Results_AreReadThroughTheConverter()
    {
        var ids = new Database<SqliteDialect>(new FakeConnection { Rows = [5L] });
        var parents = new Database<SqliteDialect>(new FakeConnection { Rows = [5L, DBNull.Value] });
        var links = new Database<SqliteDialect>(new FakeConnection { Rows = ["https://a.example/", "https://b.example/"] });

        Assert.Equal([new OrderId(5)], await ids.From(Order.Table).Select(o => o.Id).ToListAsync(Ct));
        Assert.Equal([new OrderId(5), null], await parents.From(Order.Table).Select(o => o.Parent).ToListAsync(Ct));
        Assert.Equal(
            [new Uri("https://a.example/"), new Uri("https://b.example/")],
            await links.From(Order.Table).Select(o => o.Link).ToListAsync(Ct)
        );
    }

    /// <summary>
    /// A converted column that stores a decimal gets the SQLite check of a decimal column: ordering it throws on SQLite and
    /// renders on PostgreSQL (design §3.3, §3.6).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - <see cref="Order"/>'s <see cref="Price"/>? total, stored as a decimal.
    ///
    /// Steps:
    /// - Render a select of the orders ordered by the total, on SQLite and PostgreSQL.
    ///
    /// Expected:
    /// - <see cref="InvalidOperationException"/> on SQLite; the SQL on PostgreSQL.
    /// </remarks>
    [Fact]
    public void DecimalProvider_GetsTheSqliteDecimalCheck()
    {
        Assert.Throws<InvalidOperationException>(() => Sqlite.From(Order.Table).Select(o => o.Id).OrderBy(o => [Asc(o.Total)]).ToSql());
        Assert.EndsWith(
            "ORDER BY \"orders\".\"Total\" ASC",
            PostgreSql.From(Order.Table).Select(o => o.Id).OrderBy(o => [Asc(o.Total)]).ToSql().Text,
            StringComparison.Ordinal
        );
    }

    /// <summary>
    /// A type that is not a column type and has no registered converter throws when it becomes a parameter or is read, and a
    /// type with two registered converters throws whenever it is used, naming both (design §3.6, §14.3).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - <see cref="Unregistered"/>, with no converter; <see cref="Conflicted"/>, registered with two converters here.
    ///
    /// Steps:
    /// - Render <c>Value(new Unregistered(1))</c>; read an <see cref="Unregistered"/> with <c>QueryScalarAsync</c>;
    ///   register two converters for <see cref="Conflicted"/>, then render <c>Value(new Conflicted(1))</c>.
    ///
    /// Expected:
    /// - <see cref="InvalidOperationException"/> naming <c>[ValueConverter]</c> for both <see cref="Unregistered"/> uses.
    /// - <see cref="InvalidOperationException"/> naming both converters for <see cref="Conflicted"/>.
    /// </remarks>
    [Fact]
    public async Task UnregisteredOrConflicted_Throws()
    {
        var parameter = Sqlite.From(Order.Table).Select(o => Value(new Unregistered(1)));
        Assert.Contains("[ValueConverter]", Assert.Throws<InvalidOperationException>(() => parameter.ToSql()).Message, StringComparison.Ordinal);
        var reader = new Database<SqliteDialect>(new FakeConnection { Rows = [1L] });
        await Assert.ThrowsAsync<InvalidOperationException>(() => reader.QueryScalarAsync<Unregistered>($"SELECT value", Ct));

        ValueConverters.RegisterStruct<Conflicted, long, ConflictedFirst>();
        ValueConverters.RegisterStruct<Conflicted, long, ConflictedSecond>();
        var conflicted = Sqlite.From(Order.Table).Select(o => Value(new Conflicted(1)));
        string message = Assert.Throws<InvalidOperationException>(() => conflicted.ToSql()).Message;
        Assert.Contains(nameof(ConflictedFirst), message, StringComparison.Ordinal);
        Assert.Contains(nameof(ConflictedSecond), message, StringComparison.Ordinal);
    }

    /// <summary>
    /// A parameter of a converted type is typed by its stored type: on SQL Server a stored <see cref="DateTime"/> is sent as
    /// <c>datetime2</c>, and a null of a type stored as <see cref="byte"/>[] is sent as binary (design §3.6, §12.4).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - <see cref="Stamp"/>, stored as a <see cref="DateTime"/>, and <see cref="Hash"/>, stored as a <see cref="byte"/> array.
    ///
    /// Steps:
    /// - Render, on SQL Server, a select of <c>Value(new Stamp(…))</c> and of <c>Value((Hash?)null)</c>.
    ///
    /// Expected:
    /// - The parameters' types are <c>DateTime2</c> and <c>Binary</c>.
    /// </remarks>
    [Fact]
    public void Parameters_AreTypedByTheStoredType()
    {
        var db = new Database<SqlServerDialect>(new FakeConnection());
        SqlStatement stamp = db.From(Order.Table).Select(o => Value(new Stamp(new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc)))).ToSql();
        SqlStatement hash = db.From(Order.Table).Select(o => Value((Hash?)null)).ToSql();

        Assert.Equal(System.Data.DbType.DateTime2, stamp.Parameters.Single().DbType);
        Assert.Equal(System.Data.DbType.Binary, hash.Parameters.Single().DbType);
    }

    /// <summary>
    /// An exception that a converter throws reaches the caller as it is, from a parameter and from a read (design §3.6,
    /// D1-69).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - <see cref="Faulty"/>, whose converter throws <see cref="FaultyException"/> both ways.
    ///
    /// Steps:
    /// - Render a select of <c>Value(new Faulty(1))</c>; read a <see cref="Faulty"/> with <c>QueryScalarAsync</c> from a fake
    ///   connection that returns 1.
    ///
    /// Expected:
    /// - <see cref="FaultyException"/> from both, not wrapped.
    /// </remarks>
    [Fact]
    public async Task ConverterException_ReachesTheCaller()
    {
        var parameter = Sqlite.From(Order.Table).Select(o => Value(new Faulty(1)));
        Assert.Throws<FaultyException>(() => parameter.ToSql());
        var reader = new Database<SqliteDialect>(new FakeConnection { Rows = [1L] });
        await Assert.ThrowsAsync<FaultyException>(() => reader.QueryScalarAsync<Faulty>($"SELECT value", Ct));
    }

    /// <summary>
    /// A value whose converter throws.
    /// </summary>
    /// <param name="Value">The value.</param>
    [ValueConverter(typeof(FaultyConverter))]
    public readonly record struct Faulty(long Value);

    /// <summary>
    /// Throws from both conversions.
    /// </summary>
    public sealed class FaultyConverter : IValueConverter<Faulty, long>
    {
        public static long ToProvider(Faulty value) => throw new FaultyException();

        public static Faulty FromProvider(long value) => throw new FaultyException();
    }

    /// <summary>
    /// The exception of <see cref="FaultyConverter"/>.
    /// </summary>
    public sealed class FaultyException : Exception
    {
        public FaultyException()
            : base("faulty") { }
    }

    /// <summary>
    /// A moment stored as a <see cref="DateTime"/>.
    /// </summary>
    /// <param name="Value">The stored value.</param>
    [ValueConverter(typeof(StampConverter))]
    public readonly record struct Stamp(DateTime Value);

    /// <summary>
    /// Converts <see cref="Stamp"/> to <see cref="DateTime"/>.
    /// </summary>
    public sealed class StampConverter : IValueConverter<Stamp, DateTime>
    {
        public static DateTime ToProvider(Stamp value) => value.Value;

        public static Stamp FromProvider(DateTime value) => new(value);
    }

    /// <summary>
    /// A hash stored as a <see cref="byte"/> array.
    /// </summary>
    /// <param name="Bytes">The stored value.</param>
    [ValueConverter(typeof(HashConverter))]
    public sealed record Hash(byte[] Bytes);

    /// <summary>
    /// Converts <see cref="Hash"/> to a <see cref="byte"/> array.
    /// </summary>
    public sealed class HashConverter : IValueConverter<Hash, byte[]>
    {
        public static byte[] ToProvider(Hash value) => value.Bytes;

        public static Hash FromProvider(byte[] value) => new(value);
    }

    /// <summary>
    /// A type with no value converter.
    /// </summary>
    /// <param name="Value">The value.</param>
    public readonly record struct Unregistered(long Value);

    /// <summary>
    /// A type that this test registers with two converters.
    /// </summary>
    /// <param name="Value">The value.</param>
    public readonly record struct Conflicted(long Value);

    /// <summary>
    /// The first converter of <see cref="Conflicted"/>.
    /// </summary>
    public sealed class ConflictedFirst : IValueConverter<Conflicted, long>
    {
        public static long ToProvider(Conflicted value) => value.Value;

        public static Conflicted FromProvider(long value) => new(value);
    }

    /// <summary>
    /// The second converter of <see cref="Conflicted"/>.
    /// </summary>
    public sealed class ConflictedSecond : IValueConverter<Conflicted, long>
    {
        public static long ToProvider(Conflicted value) => value.Value + 1;

        public static Conflicted FromProvider(long value) => new(value - 1);
    }
}
