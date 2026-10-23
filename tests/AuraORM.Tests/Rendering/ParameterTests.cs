using static AuraORM.Sql;
using static AuraORM.Tests.TestTables;

namespace AuraORM.Tests;

public sealed class ParameterTests
{
    /// <summary>
    /// Parameters are numbered in the order the tree is rendered: from <c>@p0</c>, or from <c>$1</c> on PostgreSQL.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - A condition with 70 values, more than the 64 placeholder names the renderer caches.
    ///
    /// Steps:
    /// - Render it on SQLite and on PostgreSQL.
    ///
    /// Expected:
    /// - SQLite: the placeholders are @p0 … @p69 in order; PostgreSQL: $1 … $70.
    /// - The values follow the same order.
    /// </remarks>
    [Fact]
    public void ParameterName_ManyValues_NumbersInRenderOrder()
    {
        int[] values = Enumerable.Range(100, 70).ToArray();
        Condition<PerRow> condition = In(Score, values);

        SqlStatement sqlite = SqlRenderContext.Render(DialectRenderer.For<SqliteDialect>(), condition.Node);
        SqlStatement postgres = SqlRenderContext.Render(DialectRenderer.For<PostgreSqlDialect>(), condition.Node);

        Assert.Equal("\"users\".\"Score\" IN (" + string.Join(", ", Enumerable.Range(0, 70).Select(i => "@p" + i)) + ")", sqlite.Text);
        Assert.Equal("\"users\".\"Score\" IN (" + string.Join(", ", Enumerable.Range(1, 70).Select(i => "$" + i)) + ")", postgres.Text);
        Assert.Equal(values.Cast<object?>(), sqlite.Parameters.Select(p => p.Value));
        Assert.Equal(Enumerable.Range(0, 70).Select(i => "@p" + i), sqlite.Parameters.Select(p => p.Name));
        Assert.Equal(Enumerable.Range(1, 70).Select(i => "$" + i), postgres.Parameters.Select(p => p.Name));
    }

    /// <summary>
    /// An enum value is sent as its underlying integer, and a null value as null.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - A <c>short</c>-based enum column, and a nullable string column.
    ///
    /// Steps:
    /// - Render a comparison of the enum column with <c>UserStatus.Active</c>, and <c>Coalesce(Nickname, null)</c>.
    ///
    /// Expected:
    /// - The first parameter is <c>(short)1</c>; the second is null.
    /// </remarks>
    [Fact]
    public void ProviderValue_EnumAndNull_UnderlyingIntegerAndNull()
    {
        Condition<PerRow> condition = And(Eq(Status, UserStatus.Active), IsNull(Coalesce(Nickname, (string?)null)));

        SqlStatement statement = SqlRenderContext.Render(DialectRenderer.For<SqliteDialect>(), condition.Node);

        Assert.Equal([(short)1, null], statement.Parameters.Select(p => p.Value));
    }

    /// <summary>
    /// A value that is written twice gets two parameters (design §4.2, the SQL Server expansion of IsNotDistinctFrom).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - <c>IsNotDistinctFrom(Age, 5)</c>.
    ///
    /// Steps:
    /// - Render it on SQL Server.
    ///
    /// Expected:
    /// - Two parameters, @p0 and @p1, both 5.
    /// </remarks>
    [Fact]
    public void ParameterName_ValueWrittenTwice_GetsTwoParameters()
    {
        SqlStatement statement = SqlRenderContext.Render(DialectRenderer.For<SqlServerDialect>(), IsNotDistinctFrom(Age, 5).Node);

        Assert.Equal(["@p0", "@p1"], statement.Parameters.Select(p => p.Name));
        Assert.Equal([5, 5], statement.Parameters.Select(p => p.Value));
    }

    /// <summary>
    /// A statement with more parameters than its dialect accepts throws before it reaches the database (design §12.4).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - The limits: SQLite 32766, PostgreSQL 65535, MySQL and MariaDB 65535, SQL Server 2100.
    ///
    /// Steps:
    /// - Render an IN list with exactly the limit, and one with the limit plus one, on each dialect.
    ///
    /// Expected:
    /// - The list at the limit renders.
    /// - The list over the limit throws <see cref="ArgumentException"/>.
    /// </remarks>
    [Theory]
    [InlineData("SQLite", 32766)]
    [InlineData("PostgreSQL", 65535)]
    [InlineData("MySQL", 65535)]
    [InlineData("MariaDB", 65535)]
    [InlineData("SQL Server", 2100)]
    public void ToStatement_OverParameterLimit_Throws(string dialect, int limit)
    {
        DialectRenderer renderer = dialect switch
        {
            "SQLite" => DialectRenderer.For<SqliteDialect>(),
            "PostgreSQL" => DialectRenderer.For<PostgreSqlDialect>(),
            "MySQL" => DialectRenderer.For<MySqlDialect>(),
            "MariaDB" => DialectRenderer.For<MariaDbDialect>(),
            _ => DialectRenderer.For<SqlServerDialect>(),
        };

        SqlStatement atLimit = SqlRenderContext.Render(renderer, In(Score, new int[limit]).Node);
        Assert.Equal(limit, atLimit.Parameters.Count);

        Assert.Throws<ArgumentException>(() => SqlRenderContext.Render(renderer, In(Score, new int[limit + 1]).Node));
    }
}
