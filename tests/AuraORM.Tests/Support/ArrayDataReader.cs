using System.Collections;
using System.Data.Common;

namespace AuraORM.Tests;

/// <summary>
/// A reader over columns held in typed arrays. Its typed getters return the stored values without boxing or allocating,
/// so a measurement through it shows only the reading code's own cost. The benchmarks link this file.
/// </summary>
internal sealed class ArrayDataReader : DbDataReader
{
    private static readonly Task<bool> TrueTask = Task.FromResult(true);

    private static readonly Task<bool> FalseTask = Task.FromResult(false);

    private readonly string[] _names;
    private readonly Array[] _columns;
    private readonly bool[][] _nulls;
    private readonly int _rows;
    private int _row = -1;

    /// <summary>
    /// Initializes a new instance of the <see cref="ArrayDataReader"/> class.
    /// </summary>
    /// <param name="names">The column names.</param>
    /// <param name="columns">One array per column, each with one value per row (a reference type's null is NULL).</param>
    /// <param name="nulls">One array per column of the rows where a value type column is NULL, or null for none.</param>
    public ArrayDataReader(string[] names, Array[] columns, bool[]?[] nulls)
    {
        _names = names;
        _columns = columns;
        _rows = columns.Length == 0 ? 0 : columns[0].Length;
        _nulls = nulls.Select(n => n ?? new bool[_rows]).ToArray();
    }

    public override int FieldCount => _columns.Length;

    public override object this[int ordinal] => GetValue(ordinal);

    public override object this[string name] => GetValue(GetOrdinal(name));

    public override int RecordsAffected => -1;

    public override bool HasRows => _rows > 0;

    public override bool IsClosed => false;

    public override int Depth => 0;

    public override bool Read() => ++_row < _rows;

    public override Task<bool> ReadAsync(CancellationToken cancellationToken) => Read() ? TrueTask : FalseTask;

    public override bool NextResult() => false;

    public override string GetName(int ordinal) => _names[ordinal];

    public override int GetOrdinal(string name) => Array.IndexOf(_names, name);

    public override Type GetFieldType(int ordinal) => _columns[ordinal].GetType().GetElementType()!;

    public override string GetDataTypeName(int ordinal) => GetFieldType(ordinal).Name;

    public override bool IsDBNull(int ordinal) => _nulls[ordinal][_row] || (_columns[ordinal] is object?[] values && values[_row] is null);

    public override Task<bool> IsDBNullAsync(int ordinal, CancellationToken cancellationToken) => IsDBNull(ordinal) ? TrueTask : FalseTask;

    public override bool GetBoolean(int ordinal) => ((bool[])_columns[ordinal])[_row];

    public override byte GetByte(int ordinal) => ((byte[])_columns[ordinal])[_row];

    public override short GetInt16(int ordinal) => ((short[])_columns[ordinal])[_row];

    public override int GetInt32(int ordinal) => ((int[])_columns[ordinal])[_row];

    public override long GetInt64(int ordinal) => ((long[])_columns[ordinal])[_row];

    public override float GetFloat(int ordinal) => ((float[])_columns[ordinal])[_row];

    public override double GetDouble(int ordinal) => ((double[])_columns[ordinal])[_row];

    public override decimal GetDecimal(int ordinal) => ((decimal[])_columns[ordinal])[_row];

    public override string GetString(int ordinal) => ((string[])_columns[ordinal])[_row];

    public override Guid GetGuid(int ordinal) => ((Guid[])_columns[ordinal])[_row];

    public override DateTime GetDateTime(int ordinal) => ((DateTime[])_columns[ordinal])[_row];

    public override char GetChar(int ordinal) => throw new NotSupportedException();

    public override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length) => throw new NotSupportedException();

    public override long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length) => throw new NotSupportedException();

    public override T GetFieldValue<T>(int ordinal) => _columns[ordinal] is T[] typed ? typed[_row] : (T)GetValue(ordinal);

    public override object GetValue(int ordinal) => IsDBNull(ordinal) ? DBNull.Value : _columns[ordinal].GetValue(_row)!;

    public override int GetValues(object[] values)
    {
        int count = Math.Min(values.Length, FieldCount);
        for (int i = 0; i < count; i++)
        {
            values[i] = GetValue(i);
        }

        return count;
    }

    public override IEnumerator GetEnumerator() => throw new NotSupportedException();
}
