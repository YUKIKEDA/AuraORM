using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace AuraORM.Generators;

/// <summary>
/// Generates the table references, column sets, <c>INSERT</c> models, and result projections of the types marked with
/// <c>[Table]</c> and <c>[QueryResult]</c> (<c>docs/design.ja.md</c> §3).
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class SchemaGenerator : IIncrementalGenerator
{
    /// <inheritdoc/>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<ParseResult<TableModel>> tables = context.SyntaxProvider.ForAttributeWithMetadataName(
            SchemaReader.TableAttribute,
            static (_, _) => true,
            static (ctx, ct) => SchemaReader.ReadTable(ctx, ct)
        );
        IncrementalValuesProvider<ParseResult<ResultModel>> results = context.SyntaxProvider.ForAttributeWithMetadataName(
            SchemaReader.QueryResultAttribute,
            static (_, _) => true,
            static (ctx, ct) => SchemaReader.ReadResult(ctx, ct)
        );

        // §3.6: the converters declared on types and on the assembly, checked, and those that the tables and results use.
        IncrementalValuesProvider<ParseResult<ConverterSet>> typeConverters = context.SyntaxProvider.ForAttributeWithMetadataName(
            ConverterLookup.AttributeName,
            static (node, _) => node is Microsoft.CodeAnalysis.CSharp.Syntax.TypeDeclarationSyntax,
            static (ctx, _) => ConverterEmitter.ReadType(ctx)
        );
        IncrementalValueProvider<ParseResult<ConverterSet>> assemblyConverters = context.CompilationProvider.Select(
            static (compilation, _) => ConverterEmitter.ReadAssembly(compilation)
        );
        IncrementalValueProvider<DiagnosticInfo?> registrationName = context.CompilationProvider.Select(
            static (compilation, _) => ConverterEmitter.CheckName(compilation)
        );
        IncrementalValueProvider<ImmutableArray<ConverterModel>> usedConverters = tables
            .SelectMany(static (result, _) => result.Model?.Columns.Select(c => c.Converter).OfType<ConverterModel>() ?? [])
            .Collect()
            .Combine(
                results.SelectMany(static (result, _) => result.Model?.Members.Select(m => m.Converter).OfType<ConverterModel>() ?? []).Collect()
            )
            .Select(static (pair, _) => pair.Left.AddRange(pair.Right));
        context.RegisterSourceOutput(
            typeConverters.Collect().Combine(assemblyConverters).Combine(usedConverters).Combine(registrationName),
            static (spc, input) =>
            {
                (
                    ((ImmutableArray<ParseResult<ConverterSet>> declared, ParseResult<ConverterSet> assembly), ImmutableArray<ConverterModel> used),
                    DiagnosticInfo? name
                ) = input;
                var all = new List<ConverterModel>(used);
                foreach (ParseResult<ConverterSet> result in declared.Add(assembly))
                {
                    Report(spc, result.Diagnostics);
                    all.AddRange(result.Model?.Converters ?? default);
                }

                if (ConverterEmitter.Emit(all) is not { } source)
                {
                    return;
                }

                if (name is not null)
                {
                    spc.ReportDiagnostic(name.ToDiagnostic());
                    return;
                }

                spc.AddSource("AuraORM.Generated.__AuraValueConverters.g.cs", SourceText.From(source, Encoding.UTF8));
            }
        );

        context.RegisterSourceOutput(
            tables,
            static (spc, result) =>
            {
                Report(spc, result.Diagnostics);
                if (result.Model is { } table)
                {
                    spc.AddSource(HintName(table.Target, "Table"), SourceText.From(TableEmitter.Emit(table), Encoding.UTF8));
                }
            }
        );
        context.RegisterSourceOutput(
            results,
            static (spc, result) =>
            {
                Report(spc, result.Diagnostics);
                if (result.Model is { } model)
                {
                    spc.AddSource(HintName(model.Target, "QueryResult"), SourceText.From(ResultEmitter.Emit(model), Encoding.UTF8));
                }
            }
        );
    }

    private static void Report(SourceProductionContext context, EquatableArray<DiagnosticInfo> diagnostics)
    {
        foreach (DiagnosticInfo diagnostic in diagnostics)
        {
            context.ReportDiagnostic(diagnostic.ToDiagnostic());
        }
    }

    private static string HintName(TypeTarget target, string kind)
    {
        var name = new StringBuilder();
        if (target.Namespace is not null)
        {
            name.Append(target.Namespace).Append('.');
        }

        foreach (string container in target.Containers)
        {
            name.Append(container.Substring(container.LastIndexOf(' ') + 1)).Append('.');
        }

        return name.Append(target.Name).Append('.').Append(kind).Append(".g.cs").ToString();
    }
}
