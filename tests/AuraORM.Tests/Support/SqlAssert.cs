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

    /// <summary>
    /// Asserts the SQL of a statement that is built the same way on every dialect.
    /// </summary>
    /// <param name="query">The statement.</param>
    /// <param name="expected">The SQL of each dialect.</param>
    /// <param name="parameters">The parameter values, the same on every dialect unless <see cref="DialectSql.SqlServerParameters"/> says otherwise.</param>
    public static void Statement(IQueryCase query, DialectSql expected, params object?[] parameters)
    {
        Compare("SQLite", expected.Sqlite, query.Build(new Database<SqliteDialect>(new FakeConnection())), parameters);
        Compare("PostgreSQL", expected.PostgreSql, query.Build(new Database<PostgreSqlDialect>(new FakeConnection())), parameters);
        Compare("MySQL", expected.MySql, query.Build(new Database<MySqlDialect>(new FakeConnection())), parameters);
        Compare("MariaDB", expected.MariaDb ?? expected.MySql, query.Build(new Database<MariaDbDialect>(new FakeConnection())), parameters);
        Compare(
            "SQL Server",
            expected.SqlServer,
            query.Build(new Database<SqlServerDialect>(new FakeConnection())),
            expected.SqlServerParameters ?? parameters
        );
    }

    /// <summary>
    /// Asserts the SQL and the parameters of one statement.
    /// </summary>
    /// <param name="dialect">The dialect's name, for the message.</param>
    /// <param name="sql">The expected SQL.</param>
    /// <param name="statement">The statement.</param>
    /// <param name="parameters">The expected parameter values.</param>
    public static void Compare(string dialect, string sql, SqlStatement statement, params object?[] parameters)
    {
        Assert.True(sql == statement.Text, $"{dialect}\nexpected: {sql}\nactual:   {statement.Text}");
        Assert.Equal(parameters, statement.Parameters.Select(p => p.Value).ToArray());
    }

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
