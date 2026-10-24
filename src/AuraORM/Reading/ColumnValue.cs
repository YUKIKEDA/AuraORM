namespace AuraORM;

/// <summary>
/// Whether <typeparamref name="T"/> is a column type of <c>docs/design.ja.md</c> §3.3 (or its nullable form, or an enum),
/// which the provider reads and writes, rather than a type with a value converter (§3.6). The checks run once per type.
/// </summary>
/// <typeparam name="T">The type, including whether it can be null.</typeparam>
internal static class ColumnValue<T>
{
    public static readonly bool IsBuiltIn = Compute();

    private static bool Compute()
    {
        Type type = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
        return type.IsEnum
            || type == typeof(bool)
            || type == typeof(byte)
            || type == typeof(short)
            || type == typeof(int)
            || type == typeof(long)
            || type == typeof(float)
            || type == typeof(double)
            || type == typeof(decimal)
            || type == typeof(string)
            || type == typeof(byte[])
            || type == typeof(Guid)
            || type == typeof(DateTime)
            || type == typeof(DateTimeOffset)
            || type == typeof(DateOnly)
            || type == typeof(TimeOnly);
    }
}
