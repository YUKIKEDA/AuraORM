namespace AuraORM;

/// <summary>
/// Declares the value converter of a type (<c>docs/design.ja.md</c> §3.6). On a type, it names the converter of that type; on
/// an assembly (<c>[assembly: ValueConverter(typeof(C))]</c>), it names the converter of <c>C</c>'s model type, for a type
/// that another library defines. A type has one converter, used wherever its values appear: parameters, rows, raw SQL holes,
/// and results.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
public sealed class ValueConverterAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ValueConverterAttribute"/> class.
    /// </summary>
    /// <param name="converterType">The converter, which implements <see cref="IValueConverter{TModel, TProvider}"/>.</param>
    public ValueConverterAttribute(Type converterType)
    {
        ConverterType = converterType;
    }

    /// <summary>
    /// Gets the converter.
    /// </summary>
    public Type ConverterType { get; }
}
