using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace AuraORM.Tests;

/// <summary>
/// Compiles a snippet as a consumer would: C# 12 (the default of net8.0), nullable reference types on, warnings reported,
/// against the AuraORM assembly, in a namespace outside AuraORM.
/// </summary>
internal static class CompileAssert
{
    private static readonly ImmutableArray<MetadataReference> References = LoadReferences();

    /// <summary>
    /// Asserts that the snippet compiles with no error and no warning.
    /// </summary>
    /// <param name="body">Statements of a method body.</param>
    public static void Compiles(string body)
    {
        ImmutableArray<Diagnostic> diagnostics = Compile(body);
        Assert.True(diagnostics.IsEmpty, "Unexpected diagnostics:\n" + string.Join("\n", diagnostics));
    }

    /// <summary>
    /// Asserts that the snippet fails with exactly the given error ids, and no other error or warning.
    /// </summary>
    /// <param name="body">Statements of a method body.</param>
    /// <param name="ids">The expected diagnostic ids, in any order.</param>
    public static void Fails(string body, params string[] ids)
    {
        ImmutableArray<Diagnostic> diagnostics = Compile(body);
        Assert.False(diagnostics.IsEmpty, "The snippet compiled.");
        Assert.Equal(ids.Order().ToArray(), diagnostics.Select(d => d.Id).Distinct().Order().ToArray());
        Assert.All(diagnostics, d => Assert.Equal(DiagnosticSeverity.Error, d.Severity));
    }

    private static ImmutableArray<Diagnostic> Compile(string body)
    {
        string source = $$"""
            using System;
            using System.Collections.Generic;
            using AuraORM;
            using static AuraORM.Sql;

            namespace Consumer;

            public static class Snippet
            {
                public static void Run(IExpr<long, PerRow> id, IExpr<string, PerRow> name, IExpr<string?, PerRow> nickname, IExpr<int?, PerRow> age)
                {
            {{body}}
                }
            }
            """;
        var options = new CSharpParseOptions(LanguageVersion.CSharp12);
        CSharpCompilation compilation = CSharpCompilation.Create(
            "Consumer",
            [CSharpSyntaxTree.ParseText(source, options)],
            References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable)
        );
        return compilation.GetDiagnostics().Where(d => d.Severity >= DiagnosticSeverity.Warning).ToImmutableArray();
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
        return [.. platform, MetadataReference.CreateFromFile(typeof(Sql).Assembly.Location)];
    }
}
