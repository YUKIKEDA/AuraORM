using Microsoft.CodeAnalysis.CSharp;

namespace AuraORM.Generators;

/// <summary>
/// Emits the <c>As</c> extension methods for the queries whose rows are a <c>[Table]</c> row or a <c>[QueryResult]</c> type
/// (<c>docs/design.ja.md</c> §3.5, §5.11). C# does not infer a type argument from a constraint, so the column set of the
/// named query has to come from a method written for the row type. The class goes at the namespace level, because an
/// extension method cannot be in a nested class.
/// </summary>
internal static class NamedQueryEmitter
{
    private const string Lib = "global::AuraORM.";

    public static void Emit(CodeWriter w, TypeTarget target, IReadOnlyList<string> columnNames)
    {
        string type = target.QualifiedName;
        string columns = type + ".Columns";
        string nullable = type + ".NullableColumns";
        string named = Lib + "NamedQuery<" + columns + ", " + nullable + ">";
        string names = GeneratedNames.Field(0);
        string create =
            Lib + "SelectNamedQueries.Create<" + columns + ", " + nullable + ", TDialect, TMode, " + type + ">(query, name, " + names + ")";
        w.Line();
        w.Line("/// <summary>");
        w.Line("/// <c>As</c> for the queries whose rows are <see cref=\"" + type + "\"/>: it names the query, which is then a derived table");
        w.Line("/// where it is used, or a CTE when the statement lists it in <c>With</c>.");
        w.Line("/// </summary>");
        w.Open((target.IsPublic ? "public" : "internal") + " static class " + target.FlatName + "NamedQueries");
        w.Line("private static readonly string[] " + names + " = new string[] { " + string.Join(", ", columnNames.Select(Literal)) + " };");
        EmitAs(w, named, create, "SelectProjection<TDialect, TMode, " + type + ", TLevel>", "the projection", withLevel: true);
        EmitAs(w, named, create, "SelectLimited<TDialect, TMode, " + type + ">", "a limit", withLevel: false);
        EmitAs(w, named, create, "SelectOffset<TDialect, TMode, " + type + ">", "an offset", withLevel: false);
        w.Close();
    }

    private static void EmitAs(CodeWriter w, string named, string create, string receiver, string stage, bool withLevel)
    {
        w.Line();
        w.Line("/// <summary>");
        w.Line("/// Names the query right after " + stage + ": <c>(SELECT …) AS \"name\"</c>, or <c>WITH \"name\" AS (SELECT …)</c>.");
        w.Line("/// </summary>");
        w.Line("/// <typeparam name=\"TDialect\">The SQL dialect.</typeparam>");
        w.Line("/// <typeparam name=\"TMode\">Whether the query was built to run; it does not matter here.</typeparam>");
        if (withLevel)
        {
            w.Line("/// <typeparam name=\"TLevel\">The evaluation level of the projection.</typeparam>");
        }

        w.Line("/// <param name=\"query\">The query.</param>");
        w.Line("/// <param name=\"name\">The name.</param>");
        w.Line("/// <returns>The named query.</returns>");
        w.Line(
            "public static "
                + named
                + " As<TDialect, TMode"
                + (withLevel ? ", TLevel" : string.Empty)
                + ">(this "
                + Lib
                + receiver
                + " query, string name)"
        );
        w.Line("    where TDialect : " + Lib + "IDialect");
        w.Line("    where TMode : " + Lib + "ExecutionMode");
        if (withLevel)
        {
            w.Line("    where TLevel : " + Lib + "EvaluationLevel");
        }

        w.Line("    => " + create + ";");
    }

    private static string Literal(string value) => SymbolDisplay.FormatLiteral(value, quote: true);
}
