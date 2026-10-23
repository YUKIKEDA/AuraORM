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

    public override void Render(SqlRenderContext context) => context.WriteParameter(this);
}

/// <summary>
/// A parameter of a known CLR type.
/// </summary>
/// <typeparam name="T">The type of the value.</typeparam>
internal sealed class ParameterNode<T>(T value) : ParameterNode
{
    public override object? ProviderValue => ToProviderValue(value);

    private static object? ToProviderValue(T value)
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
