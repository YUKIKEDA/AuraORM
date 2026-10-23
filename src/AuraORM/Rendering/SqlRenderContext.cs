namespace AuraORM;

/// <summary>
/// The state of rendering one statement: the dialect, the SQL text, and the parameters in the order they were added.
/// </summary>
internal sealed class SqlRenderContext : IDisposable
{
    private readonly List<SqlStatementParameter> _parameters = [];
    private readonly List<IReadOnlyList<TableAlias>> _scopes = [];

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

    /// <summary>
    /// Enters the <c>FROM</c> of a statement: until <see cref="ExitScope"/>, a column must belong to one of these tables or
    /// to a table of an enclosing statement (§14.3).
    /// </summary>
    /// <param name="tables">The tables of the statement.</param>
    public void EnterScope(IReadOnlyList<TableAlias> tables) => _scopes.Add(tables);

    /// <summary>
    /// Leaves the scope that <see cref="EnterScope"/> entered last.
    /// </summary>
    public void ExitScope() => _scopes.RemoveAt(_scopes.Count - 1);

    /// <summary>
    /// Throws when a column of a table that no enclosing statement has in its <c>FROM</c> is written: a column taken out of
    /// the lambda of another query (§14.3). Outside any scope (raw SQL), nothing is checked.
    /// </summary>
    /// <param name="table">The table of the column.</param>
    /// <param name="column">The name of the column.</param>
    public void CheckInScope(TableAlias table, string column)
    {
        if (_scopes.Count == 0)
        {
            return;
        }

        foreach (IReadOnlyList<TableAlias> scope in _scopes)
        {
            foreach (TableAlias candidate in scope)
            {
                // The same occurrence, not the same name: a column of another statement on the same table is out of scope too.
                if (ReferenceEquals(candidate, table))
                {
                    return;
                }
            }
        }

        throw new InvalidOperationException(
            "The column "
                + table.Reference
                + "."
                + column
                + " belongs to a table that is not in this statement's FROM. A column can be used only inside the lambdas of the statement that it came from."
        );
    }

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
        _parameters.Add(new SqlStatementParameter(name, parameter.ProviderValue) { DbType = Dialect.ParameterDbType(parameter) });
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
