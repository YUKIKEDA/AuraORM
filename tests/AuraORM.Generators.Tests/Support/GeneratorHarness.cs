using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AuraORM.Generators.Tests;

/// <summary>
/// Runs <see cref="SchemaGenerator"/> and the analyzers on a snippet the way a consumer's build does: C# 12, nullable
/// reference types on, against the AuraORM assembly.
/// </summary>
internal static class GeneratorHarness
{
    private static readonly ImmutableArray<MetadataReference> References = LoadReferences();

    /// <summary>
    /// Runs the generator and compiles the result.
    /// </summary>
    /// <param name="source">The consumer source.</param>
    /// <param name="nullable">The nullable context of the compilation.</param>
    /// <returns>The generated sources by hint name, and every diagnostic of the generator and the compiler at warning level or above.</returns>
    public static (IReadOnlyDictionary<string, string> Sources, ImmutableArray<Diagnostic> Diagnostics) Run(
        string source,
        NullableContextOptions nullable = NullableContextOptions.Enable
    )
    {
        var parseOptions = new CSharpParseOptions(LanguageVersion.CSharp12);
        CSharpCompilation compilation = CSharpCompilation.Create(
            "Consumer",
            [CSharpSyntaxTree.ParseText(source, parseOptions, path: "Consumer.cs")],
            References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: nullable)
        );
        GeneratorDriver driver = CSharpGeneratorDriver.Create([new SchemaGenerator().AsSourceGenerator()], parseOptions: parseOptions);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out Compilation output, out ImmutableArray<Diagnostic> generatorDiagnostics);
        GeneratorDriverRunResult result = driver.GetRunResult();
        var sources = result.GeneratedTrees.ToDictionary(t => Path.GetFileName(t.FilePath), t => t.GetText().ToString());
        ImmutableArray<Diagnostic> analyzerDiagnostics = output
            .WithAnalyzers([new NullComparisonAnalyzer(), new RawSqlHoleAnalyzer()])
            .GetAnalyzerDiagnosticsAsync()
            .GetAwaiter()
            .GetResult();
        ImmutableArray<Diagnostic> diagnostics = generatorDiagnostics
            .AddRange(output.GetDiagnostics())
            .AddRange(analyzerDiagnostics)
            .Where(d => d.Severity >= DiagnosticSeverity.Warning)
            .ToImmutableArray();
        return (sources, diagnostics);
    }

    /// <summary>
    /// Asserts that the snippet and its generated code compile with no error and no warning.
    /// </summary>
    /// <param name="source">The consumer source.</param>
    /// <returns>The generated sources.</returns>
    public static IReadOnlyDictionary<string, string> Compiles(string source)
    {
        (IReadOnlyDictionary<string, string> sources, ImmutableArray<Diagnostic> diagnostics) = Run(source);
        Assert.True(diagnostics.IsEmpty, "Unexpected diagnostics:\n" + string.Join("\n", diagnostics));
        return sources;
    }

    /// <summary>
    /// Asserts that the snippet fails with exactly the given diagnostic ids, all errors.
    /// </summary>
    /// <param name="source">The consumer source.</param>
    /// <param name="ids">The expected ids, in any order.</param>
    public static void Fails(string source, params string[] ids)
    {
        (_, ImmutableArray<Diagnostic> diagnostics) = Run(source);
        Assert.Equal(ids.Order().ToArray(), diagnostics.Select(d => d.Id).Distinct().Order().ToArray());
        Assert.All(diagnostics, d => Assert.Equal(DiagnosticSeverity.Error, d.Severity));
    }

    private static ImmutableArray<MetadataReference> LoadReferences()
    {
        string trusted = (string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!;
        IEnumerable<MetadataReference> platform = trusted
            .Split(Path.PathSeparator)
            .Where(p =>
                Path.GetFileName(p).StartsWith("System.", StringComparison.Ordinal) || Path.GetFileName(p) is "netstandard.dll" or "mscorlib.dll"
            )
            .Select(p => MetadataReference.CreateFromFile(p));
        return [.. platform, MetadataReference.CreateFromFile(typeof(TableAttribute).Assembly.Location)];
    }
}
