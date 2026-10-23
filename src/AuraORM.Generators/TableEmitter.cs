using Microsoft.CodeAnalysis.CSharp;

namespace AuraORM.Generators;

/// <summary>
/// Emits the members of a <c>[Table]</c> type (<c>docs/design.ja.md</c> §3.4). The output compiles with C# 12.
/// </summary>
internal static class TableEmitter
{
    private const string Lib = "global::AuraORM.";

    public static string Emit(TableModel table)
    {
        var w = new CodeWriter();
        int opened = w.BeginTarget(table.Target);
        string row = table.Target.Name;

        EmitTable(w, table, row);
        w.Line();
        EmitColumns(w, table, row);
        w.Line();
        EmitNullableColumns(w, table, row);
        w.Line();
        EmitInsert(w, table);

        w.EndTarget(opened);
        return w.ToString();
    }

    private static void EmitTable(CodeWriter w, TableModel table, string row)
    {
        w.Line("/// <summary>");
        w.Line("/// Gets the reference to the table <c>" + Xml(table.TableName) + "</c>.");
        w.Line("/// </summary>");
        w.Line(
            "public static "
                + Lib
                + "Table<"
                + row
                + ", Columns, NullableColumns, Insert> Table { get; } = new(new "
                + Lib
                + "TableInfo("
                + Literal(table.TableName)
                + ", "
                + Literal(table.Schema)
                + ", new "
                + Lib
                + "ColumnInfo[]"
        );
        w.Line("{");
        foreach (ColumnModel column in table.Columns)
        {
            var settings = new List<string>();
            if (column.Member.IsNullable)
            {
                settings.Add("IsNullable = true");
            }

            if (column.IsPrimaryKey)
            {
                settings.Add("IsPrimaryKey = true");
            }

            if (column.IsAutoIncrement)
            {
                settings.Add("IsAutoIncrement = true");
            }

            if (column.IsUnique)
            {
                settings.Add("IsUnique = true");
            }

            if (column.MaxLength >= 0)
            {
                settings.Add("MaxLength = " + column.MaxLength);
            }

            if (column.Precision >= 0)
            {
                settings.Add("Precision = " + column.Precision + ", Scale = " + column.Scale);
            }

            if (column.DefaultCode is not null)
            {
                settings.Add("Default = " + column.DefaultCode);
            }

            if (column.ReferenceCode is not null)
            {
                settings.Add("References = " + column.ReferenceCode);
            }

            string init = settings.Count == 0 ? string.Empty : " { " + string.Join(", ", settings) + " }";
            w.Line("    new(" + Literal(column.ColumnName) + ", " + Lib + "ColumnType." + column.ColumnType + ")" + init + ",");
        }

        w.Line("}, new " + Lib + "IndexInfo[]");
        w.Line("{");
        foreach (IndexModel index in table.Indexes)
        {
            string columns = string.Join(", ", index.Columns.Select(Literal));
            w.Line("    new(" + Literal(index.Name) + ", new string[] { " + columns + " }, " + (index.IsUnique ? "true" : "false") + "),");
        }

        w.Line("}));");
    }

    private static void EmitColumns(CodeWriter w, TableModel table, string row)
    {
        w.Line("/// <summary>");
        w.Line("/// The columns of one occurrence of the table <c>" + Xml(table.TableName) + "</c> in a statement.");
        w.Line("/// </summary>");
        w.Open("public sealed class Columns : " + Lib + "TableColumns<" + row + ">, " + Lib + "ITableColumns<Columns>");
        w.Open("private Columns(" + Lib + "TableAlias " + TableParameter + ")");
        foreach (ColumnModel column in table.Columns)
        {
            string factory = column.IsAutoIncrement ? "ReadOnlyColumn" : "Column";
            w.Line(This(column) + " = " + TableParameter + "." + factory + "<" + column.Member.TypeName + ">(" + Literal(column.ColumnName) + ");");
        }

        w.Close();
        foreach (ColumnModel column in table.Columns)
        {
            string type = (column.IsAutoIncrement ? "ReadOnlyColumn<" : "Column<") + column.Member.TypeName + ">";
            w.Line();
            w.Line("/// <summary>");
            w.Line("/// Gets the column <c>" + Xml(column.ColumnName) + "</c>.");
            w.Line("/// </summary>");
            w.Line("public " + Lib + type + " " + Id(column) + " { get; }");
        }

        w.Line();
        w.Line("/// <inheritdoc/>");
        w.Line(
            "public static Columns Create("
                + Lib
                + "TableAlias table) => new(table ?? throw new global::System.ArgumentNullException(nameof(table)));"
        );
        w.Line();
        w.Line("/// <inheritdoc/>");
        w.Line(
            "protected override "
                + Lib
                + "ITableColumn[] GetColumns() => new "
                + Lib
                + "ITableColumn[] { "
                + string.Join(", ", table.Columns.Select(This))
                + " };"
        );
        w.Line();
        w.Line("/// <inheritdoc/>");
        w.Open("protected override " + row + " ReadRow(ref " + Lib + "RowReader " + ReaderParameter + ")");
        for (int i = 0; i < table.Columns.Count; i++)
        {
            ColumnModel column = table.Columns[i];
            string read = column.Member.IsReferenceType && !column.Member.IsNullable ? "ReadNotNull" : "Read";
            w.Line("var " + Local(i) + " = " + ReaderParameter + "." + read + "(" + This(column) + ");");
        }

        w.Line("return " + Construct(table, row, Local) + ";");
        w.Close();
        w.Close();
    }

    private static void EmitNullableColumns(CodeWriter w, TableModel table, string row)
    {
        w.Line("/// <summary>");
        w.Line("/// The columns of the table <c>" + Xml(table.TableName) + "</c> on the nullable side of an outer join.");
        w.Line("/// </summary>");
        w.Open("public sealed class NullableColumns : " + Lib + "NullableTableColumns<" + row + ">, " + Lib + "ITableColumns<NullableColumns>");
        w.Open("private NullableColumns(" + Lib + "TableAlias " + TableParameter + ")");
        foreach (ColumnModel column in table.Columns)
        {
            w.Line(
                This(column) + " = " + TableParameter + ".NullableColumn<" + column.Member.NullableTypeName + ">(" + Literal(column.ColumnName) + ");"
            );
        }

        w.Close();
        foreach (ColumnModel column in table.Columns)
        {
            w.Line();
            w.Line("/// <summary>");
            w.Line("/// Gets the column <c>" + Xml(column.ColumnName) + "</c>, which is null when the join found no row.");
            w.Line("/// </summary>");
            w.Line("public " + Lib + "NullableColumn<" + column.Member.NullableTypeName + "> " + Id(column) + " { get; }");
        }

        w.Line();
        w.Line("/// <inheritdoc/>");
        w.Line(
            "public static NullableColumns Create("
                + Lib
                + "TableAlias table) => new(table ?? throw new global::System.ArgumentNullException(nameof(table)));"
        );
        w.Line();
        w.Line("/// <inheritdoc/>");
        w.Line(
            "protected override "
                + Lib
                + "ITableColumn[] GetColumns() => new "
                + Lib
                + "ITableColumn[] { "
                + string.Join(", ", table.Columns.Select(This))
                + " };"
        );
        w.Line();
        w.Line("/// <inheritdoc/>");
        w.Open("protected override " + row + "? ReadRow(ref " + Lib + "RowReader " + ReaderParameter + ")");
        for (int i = 0; i < table.Columns.Count; i++)
        {
            w.Line("var " + Local(i) + " = " + ReaderParameter + ".Read(" + This(table.Columns[i]) + ");");
        }

        List<int> keys = Enumerable.Range(0, table.Columns.Count).Where(i => table.Columns[i].IsPrimaryKey).ToList();
        IEnumerable<int> tested = keys.Count > 0 ? keys : Enumerable.Range(0, table.Columns.Count);
        w.Open("if (" + string.Join(" && ", tested.Select(i => Local(i) + " is null")) + ")");
        w.Line("return null;");
        w.Close();
        w.Line();
        w.Line("return " + Construct(table, row, i => NonNull(table.Columns[i], i)) + ";");
        w.Close();
        w.Close();
    }

    private static void EmitInsert(CodeWriter w, TableModel table)
    {
        w.Line("/// <summary>");
        w.Line("/// The values of one row to insert into <c>" + Xml(table.TableName) + "</c>. A column that is not set is left out, so its");
        w.Line("/// database default applies.");
        w.Line("/// </summary>");
        w.Open("public sealed class Insert : " + Lib + "InsertModel");
        for (int i = 0; i < table.Columns.Count; i++)
        {
            ColumnModel column = table.Columns[i];
            if (column.IsAutoIncrement)
            {
                continue;
            }

            string field = Field(i);
            string required = column.IsRequiredOnInsert ? "required " : string.Empty;
            w.Line("private " + column.Member.TypeName + " " + field + " = default!;");
            w.Line("private bool " + field + "Set;");
            w.Line();
            w.Line("/// <summary>");
            w.Line("/// Gets the value of the column <c>" + Xml(column.ColumnName) + "</c>.");
            w.Line("/// </summary>");
            w.Line(
                "public "
                    + required
                    + column.Member.TypeName
                    + " "
                    + Id(column)
                    + " { get => "
                    + field
                    + "; init { "
                    + field
                    + " = value; "
                    + field
                    + "Set = true; } }"
            );
            w.Line();
        }

        w.Line("/// <inheritdoc/>");
        w.Open("protected override void WriteValues(ref " + Lib + "InsertValues values)");
        for (int i = 0; i < table.Columns.Count; i++)
        {
            ColumnModel column = table.Columns[i];
            if (column.IsAutoIncrement)
            {
                continue;
            }

            string field = Field(i);
            w.Open("if (" + field + "Set)");
            w.Line("values.Add(" + i + ", " + field + ");");
            w.Close();
        }

        w.Close();
        w.Close();
    }

    private static string Construct(TableModel table, string row, Func<int, string> value)
    {
        IEnumerable<int> all = Enumerable.Range(0, table.Columns.Count);
        string arguments = string.Join(", ", all.Where(i => table.Columns[i].Member.IsPositional).Select(value));
        List<int> initialized = all.Where(i => !table.Columns[i].Member.IsPositional).ToList();
        string initializer =
            initialized.Count == 0
                ? string.Empty
                : " { " + string.Join(", ", initialized.Select(i => Id(table.Columns[i]) + " = " + value(i))) + " }";
        return table.UsesConstructor ? "new " + row + "(" + arguments + ")" + initializer : "new " + row + "()" + initializer;
    }

    private static string NonNull(ColumnModel column, int index)
    {
        if (column.Member.IsNullable)
        {
            return Local(index);
        }

        return column.Member.IsReferenceType ? Local(index) + "!" : Local(index) + "!.Value";
    }

    // A member name is always written verbatim (@name), so a member named like a keyword compiles, and is reached through
    // this.@name, so a parameter cannot hide it. Generated locals, fields, and parameters are numbered and start with
    // __aura, which a member name cannot (AURA0104).
    private static string Id(ColumnModel column) => GeneratedNames.Id(column.Member.MemberName);

    private static string This(ColumnModel column) => GeneratedNames.This(column.Member.MemberName);

    private static string Local(int index) => GeneratedNames.Local(index);

    private static string Field(int index) => GeneratedNames.Field(index);

    private const string TableParameter = GeneratedNames.TableParameter;

    private const string ReaderParameter = GeneratedNames.ReaderParameter;

    private static string Literal(string? value) => value is null ? "null" : SymbolDisplay.FormatLiteral(value, quote: true);

    private static string Xml(string text) => text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
}
