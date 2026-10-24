using Microsoft.CodeAnalysis;

namespace AuraORM.Generators;

/// <summary>
/// Finds and checks the value converters of <c>docs/design.ja.md</c> §3.6: a <c>[ValueConverter(typeof(C))]</c> on a type,
/// or <c>[assembly: ValueConverter(typeof(C))]</c> in this compilation or a referenced assembly.
/// </summary>
internal static class ConverterLookup
{
    public const string AttributeName = "AuraORM.ValueConverterAttribute";

    private const string InterfaceName = "AuraORM.IValueConverter";

    private static readonly SymbolDisplayFormat TypeFormat = SymbolDisplayFormat.FullyQualifiedFormat.WithMiscellaneousOptions(
        SymbolDisplayFormat.FullyQualifiedFormat.MiscellaneousOptions | SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier
    );

    /// <summary>
    /// Resolves the converter of a column's or member's type that is not a column type of §3.3.
    /// </summary>
    /// <param name="type">The type, nullable or not.</param>
    /// <param name="compilation">The compilation.</param>
    /// <param name="converter">The converter, when there is exactly one valid one.</param>
    /// <param name="error">The diagnostic and its arguments when there is none, an invalid one, or several.</param>
    /// <returns><see langword="true"/> when the converter was found.</returns>
    public static bool TryResolve(
        ITypeSymbol type,
        Compilation compilation,
        out ConverterModel? converter,
        out (DiagnosticDescriptor Descriptor, string[] Arguments)? error
    )
    {
        converter = null;
        error = null;
        ITypeSymbol model = Unwrap(type);
        List<INamedTypeSymbol> declared = Declared(model, compilation);
        if (declared.Count == 0)
        {
            return false;
        }

        if (declared.Count > 1)
        {
            error = (Diagnostics.DuplicateConverter, [model.ToDisplayString(), string.Join(", ", declared.Select(d => d.ToDisplayString()))]);
            return false;
        }

        if (Check(declared[0], model, out converter) is { } message)
        {
            error = (Diagnostics.InvalidConverter, [declared[0].ToDisplayString(), message]);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Returns whether a type has exactly one valid converter, for the raw SQL holes of §11.1.
    /// </summary>
    /// <param name="type">The type, nullable or not.</param>
    /// <param name="compilation">The compilation.</param>
    /// <returns><see langword="true"/> when the type can be a parameter through its converter.</returns>
    public static bool HasConverter(ITypeSymbol type, Compilation compilation) => TryResolve(type, compilation, out _, out _);

    /// <summary>
    /// Checks a declared converter: it implements <c>IValueConverter&lt;TModel, TProvider&gt;</c>, its model is the annotated
    /// type (when it is on a type) and not a column type, and its provider is a column type that is not nullable and not an
    /// enum (AURA0105).
    /// </summary>
    /// <param name="converter">The converter type.</param>
    /// <param name="annotated">The type that the attribute is on, or <see langword="null"/> for an assembly attribute.</param>
    /// <param name="model">The checked converter.</param>
    /// <returns>The reason it is invalid, or <see langword="null"/>.</returns>
    public static string? Check(INamedTypeSymbol converter, ITypeSymbol? annotated, out ConverterModel? model)
    {
        model = null;
        INamedTypeSymbol? contract = Interface(converter);
        if (contract is null)
        {
            return "it does not implement AuraORM.IValueConverter<TModel, TProvider>";
        }

        // One converter is one conversion: with two, which one applies would depend on the order of the declaration.
        if (converter.AllInterfaces.Count(IsContract) > 1)
        {
            return "it implements AuraORM.IValueConverter<TModel, TProvider> more than once";
        }

        ITypeSymbol modelType = contract.TypeArguments[0];
        ITypeSymbol provider = contract.TypeArguments[1];
        if (annotated is not null && !SymbolEqualityComparer.Default.Equals(modelType.OriginalDefinition, annotated.OriginalDefinition))
        {
            return "its model type " + modelType.ToDisplayString() + " is not the type " + annotated.ToDisplayString() + " that the attribute is on";
        }

        if (SchemaReader.ColumnTypeOf(modelType) is not null)
        {
            return "its model type " + modelType.ToDisplayString() + " is already a column type of docs/design.ja.md section 3.3";
        }

        string? providerColumn = SchemaReader.ColumnTypeOf(provider);
        if (
            providerColumn is null
            || provider.TypeKind == TypeKind.Enum
            || provider is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T }
            || provider.NullableAnnotation == NullableAnnotation.Annotated
        )
        {
            return "its provider type " + provider.ToDisplayString() + " is not a column type of section 3.3 that is not nullable and not an enum";
        }

        model = new ConverterModel(
            modelType.WithNullableAnnotation(NullableAnnotation.NotAnnotated).ToDisplayString(TypeFormat),
            provider.WithNullableAnnotation(NullableAnnotation.NotAnnotated).ToDisplayString(TypeFormat),
            converter.ToDisplayString(TypeFormat),
            modelType.IsValueType,
            providerColumn,
            IsAccessible(converter) && IsAccessible(modelType)
        );
        return null;
    }

    /// <summary>
    /// Returns the converter types named by an attribute's argument.
    /// </summary>
    /// <param name="attribute">A <c>[ValueConverter]</c> attribute.</param>
    /// <returns>The converter, or <see langword="null"/> when the argument is not a type.</returns>
    public static INamedTypeSymbol? ConverterOf(AttributeData attribute) =>
        attribute.ConstructorArguments.Length == 1 && attribute.ConstructorArguments[0].Value is INamedTypeSymbol converter ? converter : null;

    private static List<INamedTypeSymbol> Declared(ITypeSymbol model, Compilation compilation)
    {
        var found = new List<INamedTypeSymbol>();
        foreach (AttributeData attribute in model.GetAttributes())
        {
            if (attribute.AttributeClass?.ToDisplayString() == AttributeName && ConverterOf(attribute) is { } converter)
            {
                Add(found, converter);
            }
        }

        AddAssembly(found, compilation.Assembly, model);
        foreach (IAssemblySymbol assembly in compilation.SourceModule.ReferencedAssemblySymbols)
        {
            AddAssembly(found, assembly, model);
        }

        return found;
    }

    private static void AddAssembly(List<INamedTypeSymbol> found, IAssemblySymbol assembly, ITypeSymbol model)
    {
        foreach (AttributeData attribute in assembly.GetAttributes())
        {
            if (
                attribute.AttributeClass?.ToDisplayString() == AttributeName
                && ConverterOf(attribute) is { } converter
                && Interface(converter) is { } contract
                && SymbolEqualityComparer.Default.Equals(contract.TypeArguments[0].OriginalDefinition, model.OriginalDefinition)
            )
            {
                Add(found, converter);
            }
        }
    }

    private static void Add(List<INamedTypeSymbol> found, INamedTypeSymbol converter)
    {
        if (!found.Any(f => SymbolEqualityComparer.Default.Equals(f, converter)))
        {
            found.Add(converter);
        }
    }

    private static INamedTypeSymbol? Interface(INamedTypeSymbol converter) => converter.AllInterfaces.FirstOrDefault(IsContract);

    private static bool IsContract(INamedTypeSymbol type) =>
        type.TypeArguments.Length == 2 && type.OriginalDefinition.ToDisplayString().StartsWith(InterfaceName + "<", StringComparison.Ordinal);

    // The generated registration names the types, so they must be public, or internal and in this compilation.
    private static bool IsAccessible(ITypeSymbol type)
    {
        for (INamedTypeSymbol? named = type as INamedTypeSymbol; named is not null; named = named.ContainingType)
        {
            bool reachable =
                named.DeclaredAccessibility == Accessibility.Public
                || (named.DeclaredAccessibility == Accessibility.Internal && named.Locations.Any(l => l.IsInSource));
            if (!reachable)
            {
                return false;
            }
        }

        return true;
    }

    private static ITypeSymbol Unwrap(ITypeSymbol type) =>
        type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullable
            ? nullable.TypeArguments[0]
            : type.WithNullableAnnotation(NullableAnnotation.NotAnnotated);
}
