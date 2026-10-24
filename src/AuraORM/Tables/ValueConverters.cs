using System.ComponentModel;

namespace AuraORM;

/// <summary>
/// Registers value converters (<c>docs/design.ja.md</c> §3.6). The module initializers that the source generator emits call
/// it; user code does not.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class ValueConverters
{
    /// <summary>
    /// Registers the converter of a reference type, which is also the converter of its nullable form.
    /// </summary>
    /// <typeparam name="TModel">The user's type.</typeparam>
    /// <typeparam name="TProvider">The stored type.</typeparam>
    /// <typeparam name="TConverter">The converter.</typeparam>
    public static void RegisterClass<TModel, TProvider, TConverter>()
        where TModel : class
        where TProvider : notnull
        where TConverter : IValueConverter<TModel, TProvider>
    {
        ValueConversion<TModel>.Register(
            typeof(TConverter),
            typeof(TProvider),
            static value => value is null ? null : ParameterNode<TProvider>.ToProviderValue(TConverter.ToProvider(value)),
            static (reader, ordinal) => reader.IsDBNull(ordinal) ? null! : TConverter.FromProvider(ValueReader<TProvider>.Read(reader, ordinal))
        );
    }

    /// <summary>
    /// Registers the converter of a value type and of its nullable form.
    /// </summary>
    /// <typeparam name="TModel">The user's type.</typeparam>
    /// <typeparam name="TProvider">The stored type.</typeparam>
    /// <typeparam name="TConverter">The converter.</typeparam>
    public static void RegisterStruct<TModel, TProvider, TConverter>()
        where TModel : struct
        where TProvider : notnull
        where TConverter : IValueConverter<TModel, TProvider>
    {
        ValueConversion<TModel>.Register(
            typeof(TConverter),
            typeof(TProvider),
            static value => ParameterNode<TProvider>.ToProviderValue(TConverter.ToProvider(value)),
            static (reader, ordinal) => TConverter.FromProvider(ValueReader<TProvider>.Read(reader, ordinal))
        );
        ValueConversion<TModel?>.Register(
            typeof(TConverter),
            typeof(TProvider),
            static value => value is { } v ? ParameterNode<TProvider>.ToProviderValue(TConverter.ToProvider(v)) : null,
            static (reader, ordinal) => reader.IsDBNull(ordinal) ? null : TConverter.FromProvider(ValueReader<TProvider>.Read(reader, ordinal))
        );
    }
}
