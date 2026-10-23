namespace AuraORM;

/// <summary>
/// The state of rendering one statement: the dialect, the SQL text, and the parameters in the order they were added.
/// </summary>
internal sealed class SqlRenderContext : IDisposable
{
    private readonly List<SqlStatementParameter> _parameters = [];

    public SqlRenderContext(DialectRenderer dialect)
    {
        Dialect = dialect;
    }

    public DialectRenderer Dialect { get; }

    /// <summary>
    /// Renders one node as a whole statement.
    /// </summary>
    /// <param name="dialect">The dialect.</param>
    /// <param name="node">The node.</param>
    /// <returns>The SQL text and the parameters.</returns>
    public static SqlStatement Render(DialectRenderer dialect, SqlNode node)
    {
        using var context = new SqlRenderContext(dialect);
        node.Render(context);
        return context.ToStatement();
    }

    public SqlWriter Writer { get; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether column references are written without their table (<c>SET</c> targets,
    /// <c>RETURNING</c>).
    /// </summary>
    public bool UnqualifiedColumns { get; set; }

    public void Write(string sql) => Writer.Append(sql);

    public void Write(char sql) => Writer.Append(sql);

    public void WriteIdentifier(string name) => Dialect.WriteIdentifier(Writer, name);

    /// <summary>
    /// Writes the placeholder of a new parameter. A value gets a new parameter each time it is written.
    /// </summary>
    /// <param name="parameter">The parameter node.</param>
    public void WriteParameter(ParameterNode parameter)
    {
        int index = _parameters.Count;
        string name = Dialect.ParameterName(index);
        _parameters.Add(new SqlStatementParameter(name, parameter.ProviderValue));
        Writer.Append(name);
    }

    public void WriteList(IReadOnlyList<SqlNode> nodes)
    {
        for (int i = 0; i < nodes.Count; i++)
        {
            if (i > 0)
            {
                Writer.Append(", ");
            }

            nodes[i].Render(this);
        }
    }

    public SqlStatement ToStatement()
    {
        if (_parameters.Count > Dialect.MaxParameters)
        {
            ThrowHelper.ThrowTooManyParameters(_parameters.Count, Dialect.MaxParameters, Dialect.Name);
        }

        return new SqlStatement(Writer.ToString(), [.. _parameters]);
    }

    public void Dispose() => Writer.Dispose();
}
