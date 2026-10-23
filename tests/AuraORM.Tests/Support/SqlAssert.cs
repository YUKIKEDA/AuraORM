namespace AuraORM.Tests;

/// <summary>
/// Golden assertions: the exact SQL text and parameter values of a node, for every dialect.
/// </summary>
internal static class SqlAssert
{
    public static void Renders<T, TLevel>(IExpr<T, TLevel> expression, DialectSql expected, params object?[] parameters)
        where TLevel : EvaluationLevel => Renders(expression.Node, expected, parameters);

    public static void Renders<TLevel>(Condition<TLevel> condition, DialectSql expected, params object?[] parameters)
        where TLevel : EvaluationLevel => Renders(condition.Node, expected, parameters);

    public static void Renders(object source, DialectSql expected, params object?[] parameters) =>
        Renders(((ISqlNodeSource)source).SourceNode, expected, parameters);

    public static void Renders(SqlNode node, DialectSql expected, params object?[] parameters)
    {
        foreach ((string dialect, DialectRenderer renderer, string sql) in expected.Cases())
        {
            SqlStatement statement = SqlRenderContext.Render(renderer, node);
            Assert.True(sql == statement.Text, $"{dialect}\nexpected: {sql}\nactual:   {statement.Text}");
            object?[] expectedParameters = dialect == "SQL Server" ? expected.SqlServerParameters ?? parameters : parameters;
            Assert.Equal(expectedParameters, statement.Parameters.Select(p => p.Value).ToArray());
        }
    }
}
