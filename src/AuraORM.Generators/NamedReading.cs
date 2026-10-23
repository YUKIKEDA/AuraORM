using Microsoft.CodeAnalysis.CSharp;

namespace AuraORM.Generators;

/// <summary>
/// Emits the implementation of <c>IQueryResult&lt;TSelf&gt;</c>, shared by tables and result types.
/// </summary>
internal static class NamedReading
{
    private const string Lib = "global::AuraORM.";

    public static void Emit(
        CodeWriter w,
        string type,
        List<(MemberModel Member, string ColumnName)> members,
        Func<Func<MemberModel, string>, string> construct
    )
    {
        w.Line(
            "private static readonly string[] __AuraColumnNames = new string[] { "
                + string.Join(", ", members.Select(m => SymbolDisplay.FormatLiteral(m.ColumnName, quote: true)))
                + " };"
        );
        w.Line();
        w.Line(
            "static global::System.Collections.Generic.IReadOnlyList<string> " + Lib + "IQueryResult<" + type + ">.ColumnNames => __AuraColumnNames;"
        );
        w.Line();
        w.Open("static " + type + " " + Lib + "IQueryResult<" + type + ">.ReadNamed(ref " + Lib + "RowReader reader)");
        for (int i = 0; i < members.Count; i++)
        {
            MemberModel member = members[i].Member;
            string read =
                member.IsReferenceType && !member.IsNullable ? "ReadNamedNotNull<" + member.TypeName + ">" : "ReadNamed<" + member.TypeName + ">";
            w.Line("var " + Local(member) + " = reader." + read + "(" + i + ");");
        }

        w.Line("return " + construct(Local) + ";");
        w.Close();
    }

    private static string Local(MemberModel member) => "@" + char.ToLowerInvariant(member.MemberName[0]) + member.MemberName.Substring(1);
}
