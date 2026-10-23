using static AuraORM.Sql;

namespace AuraORM.Tests;

public sealed class IdentifierQuotingTests
{
    /// <summary>
    /// Identifiers are quoted with the dialect's quote, and the closing quote inside a name is doubled (design §5.12).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - A table named <c>a"b`c]d</c> with a column named <c>x"y`z]w</c>: each name holds every dialect's closing quote.
    ///
    /// Steps:
    /// - Render <c>IsNull(column)</c> on each dialect.
    ///
    /// Expected:
    /// - SQLite and PostgreSQL double <c>"</c>; MySQL and MariaDB double <c>`</c>; SQL Server doubles <c>]</c>.
    /// - The other quote characters stay as they are.
    /// </remarks>
    [Fact]
    public void WriteIdentifier_NameWithQuotes_DoublesTheClosingQuote()
    {
        var table = new TableAlias(schema: null, table: "a\"b`c]d", alias: null);
        Column<int?> column = table.Column<int?>("x\"y`z]w");

        SqlAssert.Renders(
            IsNull(column),
            new(
                "\"a\"\"b`c]d\".\"x\"\"y`z]w\" IS NULL",
                "\"a\"\"b`c]d\".\"x\"\"y`z]w\" IS NULL",
                "`a\"b``c]d`.`x\"y``z]w` IS NULL",
                "[a\"b`c]]d].[x\"y`z]]w] IS NULL"
            )
        );
    }

    /// <summary>
    /// A column of an aliased table is qualified by the alias, not by the table name (design §5.2).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - The table <c>users</c> with the alias <c>u</c>.
    ///
    /// Steps:
    /// - Render <c>IsNull</c> of one of its columns.
    ///
    /// Expected:
    /// - The column is written <c>"u"."Age"</c> (in each dialect's quotes).
    /// </remarks>
    [Fact]
    public void ColumnNode_AliasedTable_QualifiedByAlias()
    {
        var table = new TableAlias(schema: null, table: "users", alias: "u");

        SqlAssert.Renders(
            IsNull(table.Column<int?>("Age")),
            new("\"u\".\"Age\" IS NULL", "\"u\".\"Age\" IS NULL", "`u`.`Age` IS NULL", "[u].[Age] IS NULL")
        );
    }
}
