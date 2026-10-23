using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace AuraORM.Generators;

/// <summary>
/// Reports a comparison function of <c>AuraORM.Sql</c> whose value argument is a null constant (AURA0201) or may be null at
/// run time (AURA0202): in SQL, <c>a = NULL</c> is never true, so the query would match no row (<c>docs/design.ja.md</c>
/// §4.4). A reference type may be null when the compiler's flow analysis says so; a nullable value type always may.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NullComparisonAnalyzer : DiagnosticAnalyzer
{
    private const string SqlType = "AuraORM.Sql";

    // The functions of the comparison table of design §4.2.
    private static readonly ImmutableHashSet<string> Comparisons = ImmutableHashSet.Create(
        StringComparer.Ordinal,
        "Eq",
        "Ne",
        "Gt",
        "Ge",
        "Lt",
        "Le",
        "Between",
        "In",
        "NotIn",
        "Like",
        "NotLike"
    );

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Diagnostics.NullLiteralComparison, Diagnostics.NullableComparison);

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterOperationAction(Analyze, OperationKind.Invocation);
    }

    private static void Analyze(OperationAnalysisContext context)
    {
        var invocation = (IInvocationOperation)context.Operation;
        IMethodSymbol method = invocation.TargetMethod;
        if (!Comparisons.Contains(method.Name) || method.ContainingType?.ToDisplayString() != SqlType)
        {
            return;
        }

        foreach (IArgumentOperation argument in invocation.Arguments)
        {
            if (argument.Parameter is not { } parameter)
            {
                continue;
            }

            ITypeSymbol declared = parameter.OriginalDefinition.Type;
            if (declared is ITypeParameterSymbol || declared.SpecialType == SpecialType.System_String)
            {
                CheckValue(context, method.Name, argument);
            }
            else if (
                IsListOfTypeParameter(declared)
                && parameter.Type is INamedTypeSymbol { TypeArguments.Length: 1 } list
                && MayBeNull(list.TypeArguments[0])
            )
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        Diagnostics.NullableComparison,
                        argument.Syntax.GetLocation(),
                        method.Name,
                        list.TypeArguments[0].ToDisplayString()
                    )
                );
            }
        }
    }

    private static void CheckValue(OperationAnalysisContext context, string function, IArgumentOperation argument)
    {
        IOperation value = Unwrap(argument.Value);
        if (IsNullConstant(value))
        {
            context.ReportDiagnostic(Diagnostic.Create(Diagnostics.NullLiteralComparison, argument.Syntax.GetLocation(), function));
            return;
        }

        if (value.Type is not { } type)
        {
            return;
        }

        bool mayBeNull =
            type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T
            || (
                type.IsReferenceType
                && value.SemanticModel?.GetTypeInfo(value.Syntax, context.CancellationToken).Nullability.FlowState == NullableFlowState.MaybeNull
            );
        if (mayBeNull)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(Diagnostics.NullableComparison, argument.Syntax.GetLocation(), function, type.ToDisplayString())
            );
        }
    }

    // The value as written: the implicit conversions that the call adds (to T?, to object) are skipped.
    private static IOperation Unwrap(IOperation operation)
    {
        while (operation is IConversionOperation { IsImplicit: true } conversion)
        {
            operation = conversion.Operand;
        }

        return operation;
    }

    private static bool IsNullConstant(IOperation operation) =>
        (operation.ConstantValue.HasValue && operation.ConstantValue.Value is null)
        || (
            operation is IDefaultValueOperation { Type: { } type }
            && (type.IsReferenceType || type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
        )
        || (operation is IConversionOperation { Operand: var operand } && IsNullConstant(operand));

    private static bool IsListOfTypeParameter(ITypeSymbol type) =>
        type is INamedTypeSymbol { Name: "IReadOnlyList", TypeArguments.Length: 1 } list && list.TypeArguments[0] is ITypeParameterSymbol;

    private static bool MayBeNull(ITypeSymbol element) =>
        element.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T
        || (element.IsReferenceType && element.NullableAnnotation == NullableAnnotation.Annotated);
}
