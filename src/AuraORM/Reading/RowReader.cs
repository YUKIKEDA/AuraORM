using System.Data.Common;
using System.Globalization;

namespace AuraORM;

/// <summary>
/// Reads the current row of a result, one column after another. Projections read their items through it; the code that
/// the source generator emits calls <see cref="Read{T, TLevel}(IProjection{T, TLevel})"/>.
/// </summary>
public ref struct RowReader
{
    private readonly DbDataReader _reader;
    private readonly int[]? _ordinals;
    private int _ordinal;

    internal RowReader(DbDataReader reader)
    {
        _reader = reader;
        _ordinals = null;
        _ordinal = 0;
    }

    internal RowReader(DbDataReader reader, int[] ordinals)
    {
        _reader = reader;
        _ordinals = ordinals;
        _ordinal = 0;
    }

    /// <summary>
    /// Reads a projection from the next columns of the row.
    /// </summary>
    /// <typeparam name="T">The type the projection is read as.</typeparam>
    /// <typeparam name="TLevel">The evaluation level of the projection.</typeparam>
    /// <param name="projection">The projection.</param>
    /// <returns>The value.</returns>
    public T Read<T, TLevel>(IProjection<T, TLevel> projection)
        where TLevel : EvaluationLevel => projection.Read(ref this);

    /// <summary>
    /// Reads the value of the next column of the row. Generated code reads a column of a table with it instead of
    /// <see cref="Read{T, TLevel}(IProjection{T, TLevel})"/>: it has one type parameter and no interface call, so a value
    /// type is read by code that the JIT specializes for it (<c>docs/design.ja.md</c> §16.1).
    /// </summary>
    /// <typeparam name="T">The CLR type of the column, including whether it can be null.</typeparam>
    /// <param name="column">The column; it names the type, and its value is not used.</param>
    /// <returns>The value.</returns>
    public T ReadColumn<T>(IExpr<T, PerRow> column) => ReadValue<T>();

    // §16.1: the generated ReadRow reads a column of a type of §3.3 with one of these: a small method with the typed getter
    // that the JIT inlines, without ValueReader<T>'s delegate. An enum and a converted type are read with ReadColumn<T>.

    /// <summary>
    /// Reads the next column as <see cref="bool"/>, with the typed getter of the reader.
    /// </summary>
    /// <returns>The value.</returns>
    public bool ReadBoolean()
    {
        int ordinal = _ordinal++;
        return _reader.GetBoolean(ordinal);
    }

    /// <summary>
    /// Reads the next column as a nullable <see cref="bool"/>: <see langword="null"/> for <c>NULL</c>.
    /// </summary>
    /// <returns>The value.</returns>
    public bool? ReadNullableBoolean()
    {
        int ordinal = _ordinal++;
        return _reader.IsDBNull(ordinal) ? null : _reader.GetBoolean(ordinal);
    }

    /// <summary>
    /// Reads the next column as <see cref="byte"/>, with the typed getter of the reader.
    /// </summary>
    /// <returns>The value.</returns>
    public byte ReadByte()
    {
        int ordinal = _ordinal++;
        return _reader.GetByte(ordinal);
    }

    /// <summary>
    /// Reads the next column as a nullable <see cref="byte"/>: <see langword="null"/> for <c>NULL</c>.
    /// </summary>
    /// <returns>The value.</returns>
    public byte? ReadNullableByte()
    {
        int ordinal = _ordinal++;
        return _reader.IsDBNull(ordinal) ? null : _reader.GetByte(ordinal);
    }

    /// <summary>
    /// Reads the next column as <see cref="short"/>, with the typed getter of the reader.
    /// </summary>
    /// <returns>The value.</returns>
    public short ReadInt16()
    {
        int ordinal = _ordinal++;
        return _reader.GetInt16(ordinal);
    }

    /// <summary>
    /// Reads the next column as a nullable <see cref="short"/>: <see langword="null"/> for <c>NULL</c>.
    /// </summary>
    /// <returns>The value.</returns>
    public short? ReadNullableInt16()
    {
        int ordinal = _ordinal++;
        return _reader.IsDBNull(ordinal) ? null : _reader.GetInt16(ordinal);
    }

    /// <summary>
    /// Reads the next column as <see cref="int"/>, with the typed getter of the reader.
    /// </summary>
    /// <returns>The value.</returns>
    public int ReadInt32()
    {
        int ordinal = _ordinal++;
        return _reader.GetInt32(ordinal);
    }

    /// <summary>
    /// Reads the next column as a nullable <see cref="int"/>: <see langword="null"/> for <c>NULL</c>.
    /// </summary>
    /// <returns>The value.</returns>
    public int? ReadNullableInt32()
    {
        int ordinal = _ordinal++;
        return _reader.IsDBNull(ordinal) ? null : _reader.GetInt32(ordinal);
    }

    /// <summary>
    /// Reads the next column as <see cref="long"/>, with the typed getter of the reader.
    /// </summary>
    /// <returns>The value.</returns>
    public long ReadInt64()
    {
        int ordinal = _ordinal++;
        return _reader.GetInt64(ordinal);
    }

    /// <summary>
    /// Reads the next column as a nullable <see cref="long"/>: <see langword="null"/> for <c>NULL</c>.
    /// </summary>
    /// <returns>The value.</returns>
    public long? ReadNullableInt64()
    {
        int ordinal = _ordinal++;
        return _reader.IsDBNull(ordinal) ? null : _reader.GetInt64(ordinal);
    }

    /// <summary>
    /// Reads the next column as <see cref="float"/>, with the typed getter of the reader.
    /// </summary>
    /// <returns>The value.</returns>
    public float ReadSingle()
    {
        int ordinal = _ordinal++;
        return _reader.GetFloat(ordinal);
    }

    /// <summary>
    /// Reads the next column as a nullable <see cref="float"/>: <see langword="null"/> for <c>NULL</c>.
    /// </summary>
    /// <returns>The value.</returns>
    public float? ReadNullableSingle()
    {
        int ordinal = _ordinal++;
        return _reader.IsDBNull(ordinal) ? null : _reader.GetFloat(ordinal);
    }

    /// <summary>
    /// Reads the next column as <see cref="double"/>, with the typed getter of the reader.
    /// </summary>
    /// <returns>The value.</returns>
    public double ReadDouble()
    {
        int ordinal = _ordinal++;
        return _reader.GetDouble(ordinal);
    }

    /// <summary>
    /// Reads the next column as a nullable <see cref="double"/>: <see langword="null"/> for <c>NULL</c>.
    /// </summary>
    /// <returns>The value.</returns>
    public double? ReadNullableDouble()
    {
        int ordinal = _ordinal++;
        return _reader.IsDBNull(ordinal) ? null : _reader.GetDouble(ordinal);
    }

    /// <summary>
    /// Reads the next column as <see cref="decimal"/>, with the typed getter of the reader.
    /// </summary>
    /// <returns>The value.</returns>
    public decimal ReadDecimal()
    {
        int ordinal = _ordinal++;
        return _reader.GetDecimal(ordinal);
    }

    /// <summary>
    /// Reads the next column as a nullable <see cref="decimal"/>: <see langword="null"/> for <c>NULL</c>.
    /// </summary>
    /// <returns>The value.</returns>
    public decimal? ReadNullableDecimal()
    {
        int ordinal = _ordinal++;
        return _reader.IsDBNull(ordinal) ? null : _reader.GetDecimal(ordinal);
    }

    /// <summary>
    /// Reads the next column as <see cref="Guid"/>, with the typed getter of the reader.
    /// </summary>
    /// <returns>The value.</returns>
    public Guid ReadGuid()
    {
        int ordinal = _ordinal++;
        return _reader.GetGuid(ordinal);
    }

    /// <summary>
    /// Reads the next column as a nullable <see cref="Guid"/>: <see langword="null"/> for <c>NULL</c>.
    /// </summary>
    /// <returns>The value.</returns>
    public Guid? ReadNullableGuid()
    {
        int ordinal = _ordinal++;
        return _reader.IsDBNull(ordinal) ? null : _reader.GetGuid(ordinal);
    }

    /// <summary>
    /// Reads the next column as <see cref="DateTime"/>, with the typed getter of the reader.
    /// </summary>
    /// <returns>The value.</returns>
    public DateTime ReadDateTime()
    {
        int ordinal = _ordinal++;
        return _reader.GetDateTime(ordinal);
    }

    /// <summary>
    /// Reads the next column as a nullable <see cref="DateTime"/>: <see langword="null"/> for <c>NULL</c>.
    /// </summary>
    /// <returns>The value.</returns>
    public DateTime? ReadNullableDateTime()
    {
        int ordinal = _ordinal++;
        return _reader.IsDBNull(ordinal) ? null : _reader.GetDateTime(ordinal);
    }

    /// <summary>
    /// Reads the next column as <see cref="DateTimeOffset"/>, with the typed getter of the reader.
    /// </summary>
    /// <returns>The value.</returns>
    public DateTimeOffset ReadDateTimeOffset()
    {
        int ordinal = _ordinal++;
        return _reader.GetFieldValue<DateTimeOffset>(ordinal);
    }

    /// <summary>
    /// Reads the next column as a nullable <see cref="DateTimeOffset"/>: <see langword="null"/> for <c>NULL</c>.
    /// </summary>
    /// <returns>The value.</returns>
    public DateTimeOffset? ReadNullableDateTimeOffset()
    {
        int ordinal = _ordinal++;
        return _reader.IsDBNull(ordinal) ? null : _reader.GetFieldValue<DateTimeOffset>(ordinal);
    }

    /// <summary>
    /// Reads the next column as <see cref="DateOnly"/>, with the typed getter of the reader.
    /// </summary>
    /// <returns>The value.</returns>
    public DateOnly ReadDateOnly()
    {
        int ordinal = _ordinal++;
        return _reader.GetFieldValue<DateOnly>(ordinal);
    }

    /// <summary>
    /// Reads the next column as a nullable <see cref="DateOnly"/>: <see langword="null"/> for <c>NULL</c>.
    /// </summary>
    /// <returns>The value.</returns>
    public DateOnly? ReadNullableDateOnly()
    {
        int ordinal = _ordinal++;
        return _reader.IsDBNull(ordinal) ? null : _reader.GetFieldValue<DateOnly>(ordinal);
    }

    /// <summary>
    /// Reads the next column as <see cref="TimeOnly"/>, with the typed getter of the reader.
    /// </summary>
    /// <returns>The value.</returns>
    public TimeOnly ReadTimeOnly()
    {
        int ordinal = _ordinal++;
        return _reader.GetFieldValue<TimeOnly>(ordinal);
    }

    /// <summary>
    /// Reads the next column as a nullable <see cref="TimeOnly"/>: <see langword="null"/> for <c>NULL</c>.
    /// </summary>
    /// <returns>The value.</returns>
    public TimeOnly? ReadNullableTimeOnly()
    {
        int ordinal = _ordinal++;
        return _reader.IsDBNull(ordinal) ? null : _reader.GetFieldValue<TimeOnly>(ordinal);
    }

    /// <summary>
    /// Reads the next column as a <see cref="string"/> that the row type declares as not nullable.
    /// </summary>
    /// <returns>The value.</returns>
    /// <exception cref="InvalidOperationException">The column is <c>NULL</c>: the database schema does not match the C# declaration.</exception>
    public string ReadString()
    {
        int ordinal = _ordinal++;
        return _reader.IsDBNull(ordinal) ? throw NullColumn(ordinal) : _reader.GetString(ordinal);
    }

    /// <summary>
    /// Reads the next column as a nullable <see cref="string"/>: <see langword="null"/> for <c>NULL</c>.
    /// </summary>
    /// <returns>The value.</returns>
    public string? ReadNullableString()
    {
        int ordinal = _ordinal++;
        return _reader.IsDBNull(ordinal) ? null : _reader.GetString(ordinal);
    }

    /// <summary>
    /// Reads the next column as a <c>byte[]</c> that the row type declares as not nullable.
    /// </summary>
    /// <returns>The value.</returns>
    /// <exception cref="InvalidOperationException">The column is <c>NULL</c>: the database schema does not match the C# declaration.</exception>
    public byte[] ReadBinary()
    {
        int ordinal = _ordinal++;
        return _reader.IsDBNull(ordinal) ? throw NullColumn(ordinal) : _reader.GetFieldValue<byte[]>(ordinal);
    }

    /// <summary>
    /// Reads the next column as a nullable <c>byte[]</c>: <see langword="null"/> for <c>NULL</c>.
    /// </summary>
    /// <returns>The value.</returns>
    public byte[]? ReadNullableBinary()
    {
        int ordinal = _ordinal++;
        return _reader.IsDBNull(ordinal) ? null : _reader.GetFieldValue<byte[]>(ordinal);
    }

    /// <summary>
    /// Reads a column that the row type declares as not nullable. A <c>NULL</c> there means that the database schema does
    /// not match the C# declaration.
    /// </summary>
    /// <typeparam name="T">The reference type of the column.</typeparam>
    /// <param name="column">The column.</param>
    /// <returns>The value.</returns>
    /// <exception cref="InvalidOperationException">The column is <c>NULL</c>.</exception>
    public T ReadNotNull<T>(IExpr<T, PerRow> column)
        where T : class
    {
        int ordinal = _ordinal;
        T? value = ReadValue<T>();
        return value ?? throw NullColumn(ordinal);
    }

    /// <summary>
    /// Reads the column of a member of an <see cref="IQueryResult{TSelf}"/> type, found by its name when the reading started.
    /// </summary>
    /// <typeparam name="T">The CLR type of the member.</typeparam>
    /// <param name="member">The position of the member in <see cref="IQueryResult{TSelf}.ColumnNames"/>.</param>
    /// <returns>The value.</returns>
    public readonly T ReadNamed<T>(int member) => ValueReader<T>.Read(_reader, Ordinal(member));

    /// <summary>
    /// Reads the column of a member that the type declares as not nullable.
    /// </summary>
    /// <typeparam name="T">The reference type of the member.</typeparam>
    /// <param name="member">The position of the member in <see cref="IQueryResult{TSelf}.ColumnNames"/>.</param>
    /// <returns>The value.</returns>
    /// <exception cref="InvalidOperationException">The column is <c>NULL</c>.</exception>
    public readonly T ReadNamedNotNull<T>(int member)
        where T : class
    {
        int ordinal = Ordinal(member);
        return ValueReader<T>.Read(_reader, ordinal)
            ?? throw new InvalidOperationException(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "The column {0} is NULL, but the type declares it as not nullable.",
                    _reader.GetName(ordinal)
                )
            );
    }

    /// <summary>
    /// Reads the next column as a value of a supported CLR type (<c>docs/design.ja.md</c> §3.3).
    /// </summary>
    /// <typeparam name="T">The CLR type, including whether it can be null.</typeparam>
    /// <returns>The value.</returns>
    internal T ReadValue<T>() => ValueReader<T>.Read(_reader, _ordinal++);

    private readonly InvalidOperationException NullColumn(int ordinal) =>
        new(
            string.Format(
                CultureInfo.InvariantCulture,
                "The column at position {0} ({1}) is NULL, but the row type declares it as not nullable.",
                ordinal,
                _reader.GetName(ordinal)
            )
        );

    private readonly int Ordinal(int member) =>
        _ordinals is null ? throw new InvalidOperationException("The row is not read by column name.") : _ordinals[member];
}
