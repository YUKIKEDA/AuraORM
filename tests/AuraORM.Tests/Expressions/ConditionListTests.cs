using static AuraORM.Sql;
using static AuraORM.Tests.TestTables;

namespace AuraORM.Tests;

public sealed class ConditionListTests
{
    /// <summary>
    /// The list forms of <c>And</c> and <c>Or</c> skip null items and combine the rest; one condition is written alone, and
    /// none give an empty condition (design §4.2).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Lists with two conditions and a null, one condition and a null, and only nulls.
    ///
    /// Steps:
    /// - Combine each with the list <c>And</c> and <c>Or</c>, and render the non-empty results on SQLite.
    ///
    /// Expected:
    /// - <c>(a AND b)</c> / <c>(a OR b)</c> with the two parameters; the single condition without parentheses; an empty
    ///   condition for the list of nulls.
    /// </remarks>
    [Fact]
    public void AndOr_List_SkipsNullsAndCombinesTheRest()
    {
        Condition<PerRow>? none = null;
        List<Condition<PerRow>?> two = [Gt(Score, 1), none, Lt(Score, 9)];
        List<Condition<PerRow>?> one = [none, Gt(Score, 1)];
        List<Condition<PerRow>?> nulls = [none, none];

        Assert.Equal("(\"users\".\"Score\" > @p0 AND \"users\".\"Score\" < @p1)", Render(And(two)));
        Assert.Equal("(\"users\".\"Score\" > @p0 OR \"users\".\"Score\" < @p1)", Render(Or(two)));
        Assert.Equal("\"users\".\"Score\" > @p0", Render(And(one)));
        Assert.Equal("\"users\".\"Score\" > @p0", Render(Or(one)));
        Assert.True(And(nulls).IsEmpty);
        Assert.True(Or(new List<Condition<PerRow>?>()).IsEmpty);
        Assert.False(And(one).IsEmpty);
    }

    /// <summary>
    /// On SQLite, comparing decimals by size, <c>BETWEEN</c>, and <c>MIN</c> / <c>MAX</c> / <c>SUM</c> / <c>AVG</c> of
    /// decimals throw when the statement is built, because SQLite stores decimals as text; equality is allowed, and the other
    /// dialects write every form (design §3.3, §14.3).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - The decimal column <c>Balance</c>.
    ///
    /// Steps:
    /// - Render <c>Gt</c>, <c>Le</c>, <c>Between</c>, <c>Min</c>, <c>Max</c>, <c>Sum</c>, and <c>Avg</c> of it, and
    ///   <c>Eq</c>, on SQLite and on PostgreSQL.
    ///
    /// Expected:
    /// - SQLite: <see cref="InvalidOperationException"/> for each except <c>Eq</c>. PostgreSQL: SQL for each.
    /// </remarks>
    [Fact]
    public void DecimalOrdering_OnSqlite_Throws()
    {
        object[] ordering = [Gt(Balance, 1m), Le(Balance, 2m), Between(Balance, 1m, 2m), Min(Balance), Max(Balance), Sum(Balance), Avg(Balance)];
        DialectRenderer sqlite = DialectRenderer.For<SqliteDialect>();
        DialectRenderer postgres = DialectRenderer.For<PostgreSqlDialect>();

        foreach (object expression in ordering)
        {
            SqlNode node = ((ISqlNodeSource)expression).SourceNode;
            var error = Assert.Throws<InvalidOperationException>(() => SqlRenderContext.Render(sqlite, node));
            Assert.Contains("decimal", error.Message, StringComparison.Ordinal);
            Assert.NotEmpty(SqlRenderContext.Render(postgres, node).Text);
        }

        Assert.Equal("\"users\".\"Balance\" = @p0", SqlRenderContext.Render(sqlite, Eq(Balance, 1m).Node).Text);
        Assert.Equal("SUM(\"users\".\"Balance\")", SqlRenderContext.Render(postgres, Sum(Balance).Node).Text);
        Assert.Equal("AVG(\"users\".\"Balance\")", SqlRenderContext.Render(postgres, Avg(Balance).Node).Text);
        Assert.Equal("\"users\".\"Score\" > @p0", SqlRenderContext.Render(sqlite, Gt(Score, 1).Node).Text);
    }

    private static string Render(OptionalCondition<PerRow> condition) =>
        SqlRenderContext.Render(DialectRenderer.For<SqliteDialect>(), condition.Node!).Text;
}
