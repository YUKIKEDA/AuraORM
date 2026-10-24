using System.Data.Common;
using System.Globalization;

namespace AuraORM;

/// <summary>
/// The value converter registered for <typeparamref name="T"/>, a user's type or its nullable form (<c>docs/design.ja.md</c>
/// §3.6). The generated module initializers fill it; parameters and readers look it up, with no reflection.
/// </summary>
/// <typeparam name="T">The type, including whether it can be null.</typeparam>
internal static class ValueConversion<T>
{
    private static readonly object _lock = new();
    private static Func<T, object?>? _toProvider;
    private static Func<DbDataReader, int, T>? _read;
    private static Type? _converter;
    private static Type? _providerType;
    private static string? _conflict;

    /// <summary>
    /// Gets the stored type, or <see langword="null"/> when no converter is registered.
    /// </summary>
    public static Type? ProviderType => _providerType;

    /// <summary>
    /// Registers the converter of the type. The same converter may be registered again; another one is kept as a conflict,
    /// and every later use of the type throws (§3.6).
    /// </summary>
    /// <param name="converter">The converter type.</param>
    /// <param name="providerType">The stored type.</param>
    /// <param name="toProvider">The conversion of a parameter value, null kept as null.</param>
    /// <param name="read">The reading of a column, <c>NULL</c> kept as null.</param>
    public static void Register(Type converter, Type providerType, Func<T, object?> toProvider, Func<DbDataReader, int, T> read)
    {
        lock (_lock)
        {
            if (_converter is null)
            {
                _converter = converter;
                _providerType = providerType;
                _toProvider = toProvider;
                _read = read;
            }
            else if (_converter != converter)
            {
                _conflict ??= converter.FullName;
            }
        }
    }

    /// <summary>
    /// Converts a value to the stored value of a parameter.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The stored value, or <see langword="null"/>.</returns>
    /// <exception cref="InvalidOperationException">No converter is registered, or two were.</exception>
    public static object? ToProvider(T value) => Checked(_toProvider)(value);

    /// <summary>
    /// Reads a column as the type.
    /// </summary>
    /// <param name="reader">The reader.</param>
    /// <param name="ordinal">The column.</param>
    /// <returns>The value.</returns>
    /// <exception cref="InvalidOperationException">No converter is registered, or two were.</exception>
    public static T Read(DbDataReader reader, int ordinal) => Checked(_read)(reader, ordinal);

    private static TDelegate Checked<TDelegate>(TDelegate? conversion)
        where TDelegate : Delegate
    {
        if (_conflict is not null)
        {
            throw new InvalidOperationException(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "Two value converters are registered for {0}: {1} and {2}. A type has one converter; remove one of the [ValueConverter] declarations.",
                    typeof(T).FullName,
                    _converter!.FullName,
                    _conflict
                )
            );
        }

        return conversion
            ?? throw new InvalidOperationException(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "{0} is not a column type of AuraORM, and no value converter is registered for it. Declare one with [ValueConverter]; if it is declared, run code of the assembly that declares it (such as the type's constructor) before the type's values are used.",
                    typeof(T).FullName
                )
            );
    }
}
