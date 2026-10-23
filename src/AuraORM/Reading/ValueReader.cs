using System.Data.Common;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace AuraORM;

/// <summary>
/// Reads one column as <typeparamref name="T"/> with the typed getter of <see cref="DbDataReader"/>. The getter is chosen
/// once per type, with type checks that the JIT and NativeAOT fold (<c>.cursor/rules/perf.mdc</c>); values are not boxed.
/// </summary>
/// <typeparam name="T">A CLR type of <c>docs/design.ja.md</c> §3.3, an enum, or one of their nullable forms.</typeparam>
internal static class ValueReader<T>
{
    public static readonly Func<DbDataReader, int, T> Read = Create();

    private static Func<DbDataReader, int, T> Create()
    {
        if (typeof(T) == typeof(bool))
        {
            return As(static (r, i) => r.GetBoolean(i));
        }

        if (typeof(T) == typeof(bool?))
        {
            return As(static (r, i) => r.IsDBNull(i) ? (bool?)null : r.GetBoolean(i));
        }

        if (typeof(T) == typeof(byte))
        {
            return As(static (r, i) => r.GetByte(i));
        }

        if (typeof(T) == typeof(byte?))
        {
            return As(static (r, i) => r.IsDBNull(i) ? (byte?)null : r.GetByte(i));
        }

        if (typeof(T) == typeof(short))
        {
            return As(static (r, i) => r.GetInt16(i));
        }

        if (typeof(T) == typeof(short?))
        {
            return As(static (r, i) => r.IsDBNull(i) ? (short?)null : r.GetInt16(i));
        }

        if (typeof(T) == typeof(int))
        {
            return As(static (r, i) => r.GetInt32(i));
        }

        if (typeof(T) == typeof(int?))
        {
            return As(static (r, i) => r.IsDBNull(i) ? (int?)null : r.GetInt32(i));
        }

        if (typeof(T) == typeof(long))
        {
            return As(static (r, i) => r.GetInt64(i));
        }

        if (typeof(T) == typeof(long?))
        {
            return As(static (r, i) => r.IsDBNull(i) ? (long?)null : r.GetInt64(i));
        }

        if (typeof(T) == typeof(float))
        {
            return As(static (r, i) => r.GetFloat(i));
        }

        if (typeof(T) == typeof(float?))
        {
            return As(static (r, i) => r.IsDBNull(i) ? (float?)null : r.GetFloat(i));
        }

        if (typeof(T) == typeof(double))
        {
            return As(static (r, i) => r.GetDouble(i));
        }

        if (typeof(T) == typeof(double?))
        {
            return As(static (r, i) => r.IsDBNull(i) ? (double?)null : r.GetDouble(i));
        }

        if (typeof(T) == typeof(decimal))
        {
            return As(static (r, i) => r.GetDecimal(i));
        }

        if (typeof(T) == typeof(decimal?))
        {
            return As(static (r, i) => r.IsDBNull(i) ? (decimal?)null : r.GetDecimal(i));
        }

        // string and string? are the same runtime type, so NULL reads as null; generated code checks non-nullable columns
        // (RowReader.ReadNotNull).
        if (typeof(T) == typeof(string))
        {
            return As(static (r, i) => r.IsDBNull(i) ? null : r.GetString(i));
        }

        if (typeof(T) == typeof(byte[]))
        {
            return As(static (r, i) => r.IsDBNull(i) ? null : r.GetFieldValue<byte[]>(i));
        }

        if (typeof(T) == typeof(Guid))
        {
            return As(static (r, i) => r.GetGuid(i));
        }

        if (typeof(T) == typeof(Guid?))
        {
            return As(static (r, i) => r.IsDBNull(i) ? (Guid?)null : r.GetGuid(i));
        }

        if (typeof(T) == typeof(DateTime))
        {
            return As(static (r, i) => r.GetDateTime(i));
        }

        if (typeof(T) == typeof(DateTime?))
        {
            return As(static (r, i) => r.IsDBNull(i) ? (DateTime?)null : r.GetDateTime(i));
        }

        if (typeof(T) == typeof(DateTimeOffset))
        {
            return As(static (r, i) => r.GetFieldValue<DateTimeOffset>(i));
        }

        if (typeof(T) == typeof(DateTimeOffset?))
        {
            return As(static (r, i) => r.IsDBNull(i) ? (DateTimeOffset?)null : r.GetFieldValue<DateTimeOffset>(i));
        }

        if (typeof(T) == typeof(DateOnly))
        {
            return As(static (r, i) => r.GetFieldValue<DateOnly>(i));
        }

        if (typeof(T) == typeof(DateOnly?))
        {
            return As(static (r, i) => r.IsDBNull(i) ? (DateOnly?)null : r.GetFieldValue<DateOnly>(i));
        }

        if (typeof(T) == typeof(TimeOnly))
        {
            return As(static (r, i) => r.GetFieldValue<TimeOnly>(i));
        }

        if (typeof(T) == typeof(TimeOnly?))
        {
            return As(static (r, i) => r.IsDBNull(i) ? (TimeOnly?)null : r.GetFieldValue<TimeOnly>(i));
        }

        if (typeof(T).IsEnum)
        {
            return CreateEnum(Enum.GetUnderlyingType(typeof(T)), nullable: false);
        }

        if (Nullable.GetUnderlyingType(typeof(T)) is { IsEnum: true } enumType)
        {
            return CreateEnum(Enum.GetUnderlyingType(enumType), nullable: true);
        }

        return static (_, _) =>
            throw new NotSupportedException(
                string.Format(CultureInfo.InvariantCulture, "{0} is not a column type that AuraORM reads.", typeof(T).FullName)
            );
    }

    // An enum and its underlying integer have the same layout, and so do their nullable forms, so the integer that the
    // provider returns is reinterpreted without boxing.
    private static Func<DbDataReader, int, T> CreateEnum(Type underlying, bool nullable) =>
        Type.GetTypeCode(underlying) switch
        {
            TypeCode.Byte => nullable
                ? static (r, i) => FromNullable(r.IsDBNull(i) ? (byte?)null : r.GetByte(i))
                : static (r, i) => From(r.GetByte(i)),
            TypeCode.Int16 => nullable
                ? static (r, i) => FromNullable(r.IsDBNull(i) ? (short?)null : r.GetInt16(i))
                : static (r, i) => From(r.GetInt16(i)),
            TypeCode.Int32 => nullable
                ? static (r, i) => FromNullable(r.IsDBNull(i) ? (int?)null : r.GetInt32(i))
                : static (r, i) => From(r.GetInt32(i)),
            TypeCode.Int64 => nullable
                ? static (r, i) => FromNullable(r.IsDBNull(i) ? (long?)null : r.GetInt64(i))
                : static (r, i) => From(r.GetInt64(i)),
            _ => static (_, _) =>
                throw new NotSupportedException(
                    string.Format(CultureInfo.InvariantCulture, "The underlying type of {0} is not a column type.", typeof(T).FullName)
                ),
        };

    private static T From<TInteger>(TInteger value)
        where TInteger : struct => Unsafe.As<TInteger, T>(ref value);

    private static T FromNullable<TInteger>(TInteger? value)
        where TInteger : struct => Unsafe.As<TInteger?, T>(ref value);

    private static Func<DbDataReader, int, T> As<TValue>(Func<DbDataReader, int, TValue> read) => (Func<DbDataReader, int, T>)(object)read;
}
