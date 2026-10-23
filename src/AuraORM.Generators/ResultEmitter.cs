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
            w.Line("/// Gets the projection of <see cref=\"" + type + "." + member.MemberName + "\"/>.");
            w.Line("/// </summary>");
            w.Line("public required " + Lib + "IProjection<" + member.TypeName + ", " + Lib + level + "> " + member.MemberName + " { get; init; }");
            w.Line();
        }

        w.Line("/// <inheritdoc/>");
        w.Open("protected override void AddItems(" + Lib + "ProjectionItems<" + Lib + level + "> items)");
        foreach (MemberModel member in result.Members)
        {
            w.Line("items.Add(" + member.MemberName + ");");
        }

        w.Close();
        w.Line();
        w.Line("/// <inheritdoc/>");
        w.Open("protected override " + type + " ReadResult(ref " + Lib + "RowReader reader)");
        foreach (MemberModel member in result.Members)
        {
            w.Line("var @" + Camel(member.MemberName) + " = reader.Read(" + member.MemberName + ");");
        }

        string arguments = string.Join(", ", result.Members.Where(m => m.IsPositional).Select(m => "@" + Camel(m.MemberName)));
        List<MemberModel> initialized = result.Members.Where(m => !m.IsPositional).ToList();
        string initializer =
            initialized.Count == 0
                ? string.Empty
                : " { " + string.Join(", ", initialized.Select(m => m.MemberName + " = @" + Camel(m.MemberName))) + " }";
        w.Line("return new " + type + "(" + (result.UsesConstructor ? arguments : string.Empty) + ")" + initializer + ";");
        w.Close();
        w.Close();
    }

    private static string Camel(string name) => char.ToLowerInvariant(name[0]) + name.Substring(1);
}
