using Microsoft.CodeAnalysis.CSharp;

namespace AuraORM.Generators;

/// <summary>
/// Emits the implementation of <c>IQueryResult&lt;TSelf&gt;</c>, shared by tables and result types.
/// </summary>
internal static class NamedReading
{
    private const string Lib = "global::AuraORM.";

    private const string ColumnNames = GeneratedNames.ReservedPrefix + "ColumnNames";

    public static void Emit(
        CodeWriter w,
        string type,
        List<(MemberModel Member, string ColumnName)> members,
        Func<Func<int, string>, string> construct
    )
    {
        w.Line(
            "private static readonly string[] " + ColumnNames + " = new string[] { "
                + string.Join(", ", members.Select(m => SymbolDisplay.FormatLiteral(m.ColumnName, quote: true)))
                + " };"
        );
        w.Line();
        w.Line(
            "static global::System.Collections.Generic.IReadOnlyList<string> " + Lib + "IQueryResult<" + type + ">.ColumnNames => " + ColumnNames + ";"
        );
        w.Line();
        w.Open("static " + type + " " + Lib + "IQueryResult<" + type + ">.ReadNamed(ref " + Lib + "RowReader " + GeneratedNames.ReaderParameter + ")");
        for (int i = 0; i < members.Count; i++)
        {
            MemberModel member = members[i].Member;
            string read =
                member.IsReferenceType && !member.IsNullable ? "ReadNamedNotNull<" + member.TypeName + ">" : "ReadNamed<" + member.TypeName + ">";
            w.Line("var " + GeneratedNames.Local(i) + " = " + GeneratedNames.ReaderParameter + "." + read + "(" + i + ");");
        }

        w.Line("return " + construct(GeneratedNames.Local) + ";");
        w.Close();
    }
}
