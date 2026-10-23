using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace AuraORM.Generators;

/// <summary>
/// Reports a hole of a raw SQL fragment (<c>AuraORM.SqlInterpolatedStringHandler</c>) whose value would become a parameter
/// of a type that is not a column type (AURA0301, <c>docs/design.ja.md</c> §11.1). A hole may hold a column type of §3.3 or
/// its nullable form, an enum, an expression, a condition, a column, a <c>SqlFragment</c>, or a table reference.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class RawSqlHoleAnalyzer : DiagnosticAnalyzer
{
    private const string HandlerType = "AuraORM.SqlInterpolatedStringHandler";

    private static readonly ImmutableHashSet<string> NamedColumnTypes = ImmutableHashSet.Create(
        StringComparer.Ordinal,
        "System.Guid",
        "System.DateTimeOffset",
        "System.DateOnly",
        "System.TimeOnly"
    );

    // What the handler writes as SQL instead of a parameter.
    private static readonly ImmutableHashSet<string> SqlSources = ImmutableHashSet.Create(
        StringComparer.Ordinal,
        "AuraORM.ISqlNodeSource",
        "AuraORM.IExpr<T, TLevel>",
        "AuraORM.SqlFragment",
        "AuraORM.ITableReference"
    );

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Diagnostics.UnsupportedHole);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterOperationAction(Analyze, OperationKind.InterpolatedStringAppendFormatted);
    }

    private static void Analyze(OperationAnalysisContext context)
    {
        var append = (IInterpolatedStringAppendOperation)context.Operation;
        if (
            append.AppendCall is not IInvocationOperation { TargetMethod: { Name: "AppendFormatted", TypeArguments.Length: 1 } method }
            || method.ContainingType?.ToDisplayString() != HandlerType
        )
        {
            return;
        }

        ITypeSymbol type = method.TypeArguments[0];
        if (!IsAllowed(type))
        {
            context.ReportDiagnostic(Diagnostic.Create(Diagnostics.UnsupportedHole, append.Syntax.GetLocation(), type.ToDisplayString()));
        }
    }

    private static bool IsAllowed(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullable)
        {
            return IsColumnValue(nullable.TypeArguments[0]);
        }

        return IsColumnValue(type) || IsSqlSource(type);
    }

    private static bool IsColumnValue(ITypeSymbol type)
    {
        switch (type.SpecialType)
        {
            case SpecialType.System_Boolean:
            case SpecialType.System_Byte:
            case SpecialType.System_Int16:
            case SpecialType.System_Int32:
            case SpecialType.System_Int64:
            case SpecialType.System_Single:
            case SpecialType.System_Double:
            case SpecialType.System_Decimal:
            case SpecialType.System_String:
            case SpecialType.System_DateTime:
                return true;
        }

        if (type is IArrayTypeSymbol { Rank: 1, ElementType.SpecialType: SpecialType.System_Byte })
        {
            return true;
        }

        if (type is INamedTypeSymbol { TypeKind: TypeKind.Enum, EnumUnderlyingType: { } underlying })
        {
            return underlying.SpecialType
                is SpecialType.System_Byte
                    or SpecialType.System_Int16
                    or SpecialType.System_Int32
                    or SpecialType.System_Int64;
        }

        return NamedColumnTypes.Contains(type.ToDisplayString());
    }

    private static bool IsSqlSource(ITypeSymbol type) =>
        SqlSources.Contains(type.OriginalDefinition.ToDisplayString())
        || type.AllInterfaces.Any(i => SqlSources.Contains(i.OriginalDefinition.ToDisplayString()));
}
