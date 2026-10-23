using static AuraORM.Sql;
using static AuraORM.Tests.TestTables;

namespace AuraORM.Tests;

public sealed class RawSqlTests
{
    /// <summary>
    /// A raw fragment writes its literal parts as SQL, columns and expressions as their SQL, tables and identifiers quoted,
    /// <c>Unsafe</c> text unchanged, and every other value as a parameter (design §11.1).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - A fragment with a column, a string value with a quote, the table of <see cref="Account"/>, an identifier with a
    ///   quote, an unsafe text, and a nested expression.
    ///
    /// Steps:
    /// - Render it on each dialect.
    ///
    /// Expected:
    /// - The SQL of the case, and the string and the number as the two parameters.
    /// </remarks>
    [Fact]
    public void Raw_Holes_RenderByKind()
    {
        string name = "O'Brien";
        Condition<PerRow> condition = Raw(
            $"{Name} = {name} AND EXISTS (SELECT 1 FROM {Account.Table} WHERE {Identifier("a\"b`c]")} > {Add(Score, 1)}) {Unsafe("COLLATE x")}"
        );

        SqlAssert.Renders(
            condition,
            new(
                "\"users\".\"Name\" = @p0 AND EXISTS (SELECT 1 FROM \"accounts\" WHERE \"a\"\"b`c]\" > (\"users\".\"Score\" + @p1)) COLLATE x",
                "\"users\".\"Name\" = $1 AND EXISTS (SELECT 1 FROM \"accounts\" WHERE \"a\"\"b`c]\" > (\"users\".\"Score\" + $2)) COLLATE x",
                "`users`.`Name` = @p0 AND EXISTS (SELECT 1 FROM `accounts` WHERE `a\"b``c]` > (`users`.`Score` + @p1)) COLLATE x",
                "[users].[Name] = @p0 AND EXISTS (SELECT 1 FROM [accounts] WHERE [a\"b`c]]] > ([users].[Score] + @p1)) COLLATE x"
            ),
            "O'Brien",
            1
        );
    }

    /// <summary>
    /// A table with a schema is written with its schema in a raw fragment.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - <see cref="Membership"/>, whose schema is <c>app</c>.
    ///
    /// Steps:
    /// - Render <c>Raw&lt;long&gt;($"(SELECT COUNT(*) FROM {Membership.Table})")</c>.
    ///
    /// Expected:
    /// - <c>"app"."memberships"</c> in the dialect's quotes.
    /// </remarks>
    [Fact]
    public void Raw_TableWithSchema_WritesSchemaAndName()
    {
        SqlAssert.Renders(
            Raw<long>($"(SELECT COUNT(*) FROM {Membership.Table})"),
            new(
                "(SELECT COUNT(*) FROM \"app\".\"memberships\")",
                "(SELECT COUNT(*) FROM \"app\".\"memberships\")",
                "(SELECT COUNT(*) FROM `app`.`memberships`)",
                "(SELECT COUNT(*) FROM [app].[memberships])"
            )
        );
    }

    /// <summary>
    /// Raw SQL accepts only interpolated strings, and its holes take no format or alignment (design §11.1, §14.1).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - The consumer snippet of <see cref="CompileAssert"/>.
    ///
    /// Steps:
    /// - Compile a call with a string variable, one with a format, one with an alignment, and a valid one.
    ///
    /// Expected:
    /// - The diagnostic of the case; no diagnostic for the valid call.
    /// </remarks>
    [Theory]
    [InlineData("string sql = \"1 = 1\"; var c = Raw(sql);", "CS1503")]
    [InlineData("var c = Raw($\"{name} = {1.5:N2}\");", "CS1739")]
    [InlineData("var c = Raw($\"{name} = {1,5}\");", "CS1739")]
    [InlineData("var c = Raw($\"{name} = {1}\");", null)]
    public void Raw_NonInterpolatedOrFormatted_DoesNotCompile(string body, string? id)
    {
        if (id is null)
        {
            CompileAssert.Compiles(body);
        }
        else
        {
            CompileAssert.Fails(body, id);
        }
    }
}
