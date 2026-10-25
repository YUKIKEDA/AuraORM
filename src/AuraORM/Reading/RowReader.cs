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
        return value
            ?? throw new InvalidOperationException(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "The column at position {0} ({1}) is NULL, but the row type declares it as not nullable.",
                    ordinal,
                    _reader.GetName(ordinal)
                )
            );
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

    private readonly int Ordinal(int member) =>
        _ordinals is null ? throw new InvalidOperationException("The row is not read by column name.") : _ordinals[member];
}
