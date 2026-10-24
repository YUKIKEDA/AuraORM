using System.Globalization;

namespace AuraORM;

/// <summary>
/// A value bound as a parameter. Values never reach the SQL text (<c>docs/design.ja.md</c> N6).
/// </summary>
internal abstract class ParameterNode : SqlNode
{
    /// <summary>
    /// Gets the value given to the provider: <see langword="null"/> for SQL <c>NULL</c>, the underlying integer for an enum.
    /// </summary>
    public abstract object? ProviderValue { get; }

    /// <summary>
    /// Gets the CLR type of the value, without <see cref="Nullable{T}"/>.
    /// </summary>
    public abstract Type ValueType { get; }

    /// <summary>
    /// Gets a value indicating whether the value is null.
    /// </summary>
    public abstract bool IsNull { get; }

    public override void Render(SqlRenderContext context) => context.WriteParameter(this);
}

/// <summary>
/// A parameter of a known CLR type.
/// </summary>
/// <typeparam name="T">The type of the value.</typeparam>
internal sealed class ParameterNode<T>(T value) : ParameterNode
{
    // §3.6: a type outside §3.3 is stored through its value converter.
    public override object? ProviderValue => ColumnValue<T>.IsBuiltIn ? ToProviderValue(value) : ValueConversion<T>.ToProvider(value);

    public override Type ValueType =>
        ColumnValue<T>.IsBuiltIn ? Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T) : ValueConversion<T>.ProviderType ?? typeof(T);

    public override bool IsNull => value is null;

    /// <summary>
    /// Returns the value given to the provider for a value of a column type of §3.3.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The value, the underlying integer of an enum, or <see langword="null"/>.</returns>
    internal static object? ToProviderValue(T value)
    {
        if (value is null)
        {
            return null;
        }

        // Providers map CLR enums differently (Npgsql treats them as PostgreSQL enum types), so the underlying integer is sent.
        if (value is Enum e)
        {
            return Convert.ChangeType(e, Enum.GetUnderlyingType(e.GetType()), CultureInfo.InvariantCulture);
        }

        return value;
    }
}
