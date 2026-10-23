using Microsoft.CodeAnalysis.CSharp;

namespace AuraORM.Generators;

/// <summary>
/// Emits the members of a <c>[QueryResult]</c> type (<c>docs/design.ja.md</c> §3.5, §11.3). The output compiles with C# 12.
/// </summary>
internal static class ResultEmitter
{
    private const string Lib = "global::AuraORM.";

    public static string Emit(ResultModel result)
    {
        var w = new CodeWriter();
        string type = result.Target.Name;
        int opened = w.BeginTarget(result.Target, " : " + Lib + "IQueryResult<" + type + ">");
        EmitProjection(w, result, "RowProjection", "PerRow", "evaluated for each row");
        w.Line();
        EmitProjection(w, result, "GroupProjection", "PerGroup", "evaluated once for each group (keys and aggregates)");
        w.Line();
        NamedReading.Emit(w, type, result.Members.Select(m => (m.Member, m.ColumnName)).ToList(), values => Construct(result, values));
        w.EndTarget(opened);
        return w.ToString();
    }

    private static void EmitProjection(CodeWriter w, ResultModel result, string name, string level, string description)
    {
        string type = result.Target.Name;
        w.Line("/// <summary>");
        w.Line("/// The projection of <see cref=\"" + type + "\"/> with members " + description + ". Set every member.");
        w.Line("/// </summary>");
        w.Open("public sealed class " + name + " : " + Lib + "ResultProjection<" + type + ", " + Lib + level + ">");
        foreach (ResultMemberModel member in result.Members)
        {
            w.Line("/// <summary>");
            w.Line("/// Gets the projection of <see cref=\"" + type + "." + member.Member.MemberName + "\"/>.");
            w.Line("/// </summary>");
            w.Line(
                "public required "
                    + Lib
                    + "IProjection<"
                    + member.Member.TypeName
                    + ", "
                    + Lib
                    + level
                    + "> "
                    + member.Member.MemberName
                    + " { get; init; }"
            );
            w.Line();
        }

        w.Line("/// <inheritdoc/>");
        w.Open("protected override void AddItems(" + Lib + "ProjectionItems<" + Lib + level + "> items)");
        foreach (ResultMemberModel member in result.Members)
        {
            w.Line("items.Add(" + member.Member.MemberName + ");");
        }

        w.Close();
        w.Line();
        w.Line("/// <inheritdoc/>");
        w.Open("protected override " + type + " ReadResult(ref " + Lib + "RowReader reader)");
        foreach (ResultMemberModel member in result.Members)
        {
            w.Line("var " + Local(member.Member) + " = reader.Read(" + member.Member.MemberName + ");");
        }

        w.Line("return " + Construct(result, Local) + ";");
        w.Close();
        w.Close();
    }

    private static string Construct(ResultModel result, Func<MemberModel, string> value)
    {
        string arguments = string.Join(", ", result.Members.Where(m => m.Member.IsPositional).Select(m => value(m.Member)));
        List<MemberModel> initialized = result.Members.Where(m => !m.Member.IsPositional).Select(m => m.Member).ToList();
        string initializer =
            initialized.Count == 0 ? string.Empty : " { " + string.Join(", ", initialized.Select(m => m.MemberName + " = " + value(m))) + " }";
        return "new " + result.Target.Name + "(" + (result.UsesConstructor ? arguments : string.Empty) + ")" + initializer;
    }

    private static string Local(MemberModel member) => "@" + char.ToLowerInvariant(member.MemberName[0]) + member.MemberName.Substring(1);
}
