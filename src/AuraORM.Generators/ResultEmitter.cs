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
            w.Line("/// Gets the projection of <see cref=\"" + type + "." + GeneratedNames.Id(member.Member.MemberName) + "\"/>.");
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
                    + GeneratedNames.Id(member.Member.MemberName)
                    + " { get; init; }"
            );
            w.Line();
        }

        w.Line("/// <inheritdoc/>");
        w.Open("protected override void AddItems(" + Lib + "ProjectionItems<" + Lib + level + "> items)");
        foreach (ResultMemberModel member in result.Members)
        {
            w.Line("items.Add(" + GeneratedNames.This(member.Member.MemberName) + ");");
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
                    + GeneratedNames.This(result.Members[i].Member.MemberName)
                    + ");"
            );
        }

        w.Line("return " + Construct(result, GeneratedNames.Local) + ";");
        w.Close();
        w.Close();
    }

    private static string Construct(ResultModel result, Func<int, string> value)
    {
        IEnumerable<int> all = Enumerable.Range(0, result.Members.Count);
        string arguments = string.Join(", ", all.Where(i => result.Members[i].Member.IsPositional).Select(value));
        List<int> initialized = all.Where(i => !result.Members[i].Member.IsPositional).ToList();
        string initializer =
            initialized.Count == 0
                ? string.Empty
                : " { " + string.Join(", ", initialized.Select(i => GeneratedNames.Id(result.Members[i].Member.MemberName) + " = " + value(i))) + " }";
        return "new " + result.Target.Name + "(" + (result.UsesConstructor ? arguments : string.Empty) + ")" + initializer;
    }
}
