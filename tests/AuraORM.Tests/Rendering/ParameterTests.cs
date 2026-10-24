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
    /// A value that the rendering writes more than once is one parameter, used at each place (design §4.2, the SQL Server
    /// expansion of IsNotDistinctFrom; §5.12).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - <c>IsNotDistinctFrom(Age, 5)</c>.
    ///
    /// Steps:
    /// - Render it on SQL Server.
    ///
    /// Expected:
    /// - The SQL uses @p0 three times.
    /// - The statement has one parameter, @p0 = 5.
    /// </remarks>
    [Fact]
    public void ParameterName_ValueWrittenThreeTimes_GetsOneParameter()
    {
        SqlStatement statement = SqlRenderContext.Render(DialectRenderer.For<SqlServerDialect>(), IsNotDistinctFrom(Age, 5).Node);

        Assert.Equal(3, statement.Text.Split("@p0").Length - 1);
        Assert.Equal(["@p0"], statement.Parameters.Select(p => p.Name));
        Assert.Equal([5], statement.Parameters.Select(p => p.Value));
    }

    /// <summary>
    /// The same expression written twice in one statement reuses its parameter, and an equal value written separately gets
    /// its own parameter (design §5.12).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - <c>key = Add(Score, 1)</c>.
    ///
    /// Steps:
    /// - Render <c>And(Gt(key, 0), Lt(key, 9), Eq(Add(Score, 1), 2))</c> on PostgreSQL and on SQLite.
    ///
    /// Expected:
    /// - <c>key</c> is <c>$1</c> at both places; the separately written <c>Add(Score, 1)</c> gets its own parameter.
    /// - The parameters are numbered in order of first use: 1, 0, 9, 1, 2.
    /// </remarks>
    [Fact]
    public void ParameterName_SameExpressionTwice_ReusesItsParameter()
    {
        IExpr<int, PerRow> key = Add(Score, 1);
        Condition<PerRow> condition = And(Gt(key, 0), Lt(key, 9), Eq(Add(Score, 1), 2));

        SqlStatement postgres = SqlRenderContext.Render(DialectRenderer.For<PostgreSqlDialect>(), condition.Node);
        SqlStatement sqlite = SqlRenderContext.Render(DialectRenderer.For<SqliteDialect>(), condition.Node);

        Assert.Equal("((\"users\".\"Score\" + $1) > $2 AND (\"users\".\"Score\" + $1) < $3 AND (\"users\".\"Score\" + $4) = $5)", postgres.Text);
        Assert.Equal([1, 0, 9, 1, 2], postgres.Parameters.Select(p => p.Value));
        Assert.Equal(["@p0", "@p1", "@p2", "@p3", "@p4"], sqlite.Parameters.Select(p => p.Name));
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
        Assert.Equal(limit, atLimit.Parameters.Length);

        Assert.Throws<ArgumentException>(() => SqlRenderContext.Render(renderer, In(Score, new int[limit + 1]).Node));
    }
}
