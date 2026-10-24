using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace AuraORM.Tests;

/// <summary>
/// Compiles a snippet as a consumer would: C# 12 (the default of net8.0), nullable reference types on, warnings reported,
/// against the AuraORM assembly and the test schema (<see cref="Account"/>), in a namespace outside AuraORM.
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
    /// Asserts that the snippet fails with exactly the given error ids, and no other error. Warnings that follow from the
    /// errors (such as nullability warnings on the expression that failed) are ignored.
    /// </summary>
    /// <param name="body">Statements of a method body.</param>
    /// <param name="ids">The expected error ids, in any order.</param>
    public static void Fails(string body, params string[] ids)
    {
        string[] errors = Compile(body).Where(d => d.Severity == DiagnosticSeverity.Error).Select(d => d.Id).Distinct().Order().ToArray();
        Assert.False(errors.Length == 0, "The snippet compiled.");
        Assert.Equal(ids.Order().ToArray(), errors);
    }

    /// <summary>
    /// Asserts that the snippet fails with exactly one error id, and that an error of that id has a message containing the
    /// given text (the fix that an <c>[Obsolete(error: true)]</c> member names).
    /// </summary>
    /// <param name="body">Statements of a method body.</param>
    /// <param name="id">The expected error id.</param>
    /// <param name="message">Text that the error's message contains.</param>
    public static void FailsWith(string body, string id, string message)
    {
        Fails(body, id);
        Assert.Contains(
            Compile(body),
            d => d.Id == id && d.GetMessage(System.Globalization.CultureInfo.InvariantCulture).Contains(message, StringComparison.Ordinal)
        );
    }

    /// <summary>
    /// Asserts that the snippet compiles with no error and reports the given warning.
    /// </summary>
    /// <param name="body">Statements of a method body.</param>
    /// <param name="id">The expected warning id.</param>
    public static void Warns(string body, string id)
    {
        ImmutableArray<Diagnostic> diagnostics = Compile(body);
        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error);
        Assert.Contains(diagnostics, d => d.Id == id && d.Severity == DiagnosticSeverity.Warning);
    }

    /// <summary>
    /// Compiles the snippet, asserts that it has no error and no warning, runs it, and returns the string it returns.
    /// </summary>
    /// <param name="body">Statements of a method body that end with <c>return</c> of a string.</param>
    /// <returns>The value the snippet returned.</returns>
    public static string Evaluate(string body)
    {
        CSharpCompilation compilation = Compilation(
            $$"""
            using System;
            using System.Collections.Generic;
            using AuraORM;
            using AuraORM.Tests;
            using static AuraORM.Sql;

            namespace Consumer;

            public static class Snippet
            {
                public static string Run()
                {
            {{body}}
                }
            }
            """
        );
        using var image = new MemoryStream();
        Microsoft.CodeAnalysis.Emit.EmitResult result = compilation.Emit(image);
        ImmutableArray<Diagnostic> diagnostics = [.. result.Diagnostics.Where(d => d.Severity >= DiagnosticSeverity.Warning)];
        Assert.True(result.Success && diagnostics.IsEmpty, "Unexpected diagnostics:\n" + string.Join("\n", diagnostics));
        image.Position = 0;
        var context = new AssemblyLoadContext("Snippet", isCollectible: true);
        try
        {
            Assembly assembly = context.LoadFromStream(image);
            MethodInfo run = assembly.GetType("Consumer.Snippet", throwOnError: true)!.GetMethod("Run")!;
            return (string)run.Invoke(null, null)!;
        }
        finally
        {
            context.Unload();
        }
    }

    private static ImmutableArray<Diagnostic> Compile(string body)
    {
        string source = $$"""
            using System;
            using System.Collections.Generic;
            using AuraORM;
            using AuraORM.Tests;
            using static AuraORM.Sql;

            namespace Consumer;

            public static class Snippet
            {
                public static void Run(IExpr<long, PerRow> id, IExpr<string, PerRow> name, IExpr<string?, PerRow> nickname, IExpr<int?, PerRow> age,
                    Database<SqliteDialect> sqlite, Database<PostgreSqlDialect> pg, Database<MySqlDialect> mysql, Database<SqlServerDialect> mssql)
                {
            {{body}}
                }
            }
            """;
        return Compilation(source).GetDiagnostics().Where(d => d.Severity >= DiagnosticSeverity.Warning).ToImmutableArray();
    }

    private static CSharpCompilation Compilation(string source) =>
        CSharpCompilation.Create(
            "Consumer",
            [CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.CSharp12))],
            References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable)
        );

    private static ImmutableArray<MetadataReference> LoadReferences()
    {
        string trusted = (string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!;
        IEnumerable<MetadataReference> platform = trusted
            .Split(Path.PathSeparator)
            .Where(p =>
                Path.GetFileName(p).StartsWith("System.", StringComparison.Ordinal) || Path.GetFileName(p) is "netstandard.dll" or "mscorlib.dll"
            )
            .Select(p => MetadataReference.CreateFromFile(p));
        return
        [
            .. platform,
            MetadataReference.CreateFromFile(typeof(Sql).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Account).Assembly.Location),
        ];
    }
}
