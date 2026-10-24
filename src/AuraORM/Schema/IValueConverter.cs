namespace AuraORM;

/// <summary>
/// Converts a value of a user's type to a type that the database stores, and back (<c>docs/design.ja.md</c> §3.6). Declare it
/// on the user's type with <see cref="ValueConverterAttribute"/>, or on the assembly for a type of another library.
/// </summary>
/// <typeparam name="TModel">The user's type, which is not a column type of §3.3.</typeparam>
/// <typeparam name="TProvider">The stored type: a column type of §3.3 that is not nullable and not an enum; <see cref="string"/>
/// and <see cref="byte"/> arrays are allowed.</typeparam>
/// <remarks>
/// A null value is never passed to the converter: a null parameter stays <c>NULL</c>, and a <c>NULL</c> read stays null. An
/// exception that the converter throws reaches the caller as it is. AuraORM does not check that
/// <c>FromProvider(ToProvider(x))</c> gives back <c>x</c>, or that the stored values sort in the order of the user's type.
/// </remarks>
public interface IValueConverter<TModel, TProvider>
    where TModel : notnull
    where TProvider : notnull
{
    /// <summary>
    /// Converts a value of the user's type to the stored type, for a parameter.
    /// </summary>
    /// <param name="value">The value, never null.</param>
    /// <returns>The stored value.</returns>
    static abstract TProvider ToProvider(TModel value);

    /// <summary>
    /// Converts a stored value back to the user's type, for a result.
    /// </summary>
    /// <param name="value">The stored value, never null.</param>
    /// <returns>The value.</returns>
    static abstract TModel FromProvider(TProvider value);
}
