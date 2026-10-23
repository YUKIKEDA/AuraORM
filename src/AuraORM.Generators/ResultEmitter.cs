namespace AuraORM.Generators;

/// <summary>
/// Emits the members of a <c>[QueryResult]</c> type (<c>docs/design.ja.md</c> §3.5). The output compiles with C# 12.
/// </summary>
internal static class ResultEmitter
{
    private const string Lib = "global::AuraORM.";

    public static string Emit(ResultModel result)
    {
        var w = new CodeWriter();
        int opened = w.BeginTarget(result.Target);
        EmitProjection(w, result, "RowProjection", "PerRow", "evaluated for each row");
        w.Line();
        EmitProjection(w, result, "GroupProjection", "PerGroup", "evaluated once for each group (keys and aggregates)");
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
        foreach (MemberModel member in result.Members)
        {
            w.Line("/// <summary>");
            w.Line("/// Gets the projection of <see cref=\"" + type + "." + GeneratedNames.Id(member.MemberName) + "\"/>.");
            w.Line("/// </summary>");
            w.Line(
                "public required "
                    + Lib
                    + "IProjection<"
                    + member.TypeName
                    + ", "
                    + Lib
                    + level
                    + "> "
                    + GeneratedNames.Id(member.MemberName)
                    + " { get; init; }"
            );
            w.Line();
        }

        w.Line("/// <inheritdoc/>");
        w.Open("protected override void AddItems(" + Lib + "ProjectionItems<" + Lib + level + "> items)");
        foreach (MemberModel member in result.Members)
        {
            w.Line("items.Add(" + GeneratedNames.This(member.MemberName) + ");");
        }

        w.Close();
        w.Line();
        w.Line("/// <inheritdoc/>");
        w.Open("protected override " + type + " ReadResult(ref " + Lib + "RowReader " + GeneratedNames.ReaderParameter + ")");
        for (int i = 0; i < result.Members.Count; i++)
        {
            w.Line(
                "var "
                    + GeneratedNames.Local(i)
                    + " = "
                    + GeneratedNames.ReaderParameter
                    + ".Read("
                    + GeneratedNames.This(result.Members[i].MemberName)
                    + ");"
            );
        }

        IEnumerable<int> all = Enumerable.Range(0, result.Members.Count);
        string arguments = string.Join(", ", all.Where(i => result.Members[i].IsPositional).Select(GeneratedNames.Local));
        List<int> initialized = all.Where(i => !result.Members[i].IsPositional).ToList();
        string initializer =
            initialized.Count == 0
                ? string.Empty
                : " { "
                    + string.Join(", ", initialized.Select(i => GeneratedNames.Id(result.Members[i].MemberName) + " = " + GeneratedNames.Local(i)))
                    + " }";
        w.Line("return new " + type + "(" + (result.UsesConstructor ? arguments : string.Empty) + ")" + initializer + ";");
        w.Close();
        w.Close();
    }
}
