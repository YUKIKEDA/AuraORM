namespace AuraORM;

/// <summary>
/// The state of rendering one statement: the dialect, the SQL text, and the parameters in the order they were added.
/// </summary>
internal sealed class SqlRenderContext : IDisposable
{
    private readonly List<SqlStatementParameter> _parameters = [];
    private readonly Dictionary<ParameterNode, string> _names = new(ReferenceEqualityComparer.Instance);
    private readonly List<IReadOnlyList<TableAlias>> _scopes = [];
    private readonly List<NamedQueryDefinition> _ctes = [];
    private int _statementDepth;
    private int _groupBoundary = -1;
    private int _keyDepth;

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
    /// Gets or sets a value indicating whether the <c>HAVING</c> of the statement being rendered is written now, where a
    /// <see cref="GroupKeyNode"/> may need an aggregate (§5.5).
    /// </summary>
    public bool InHaving { get; set; }

    /// <summary>
    /// Gets the number of columns written so far, and of raw SQL texts, which may name columns (§5.5).
    /// </summary>
    public int ColumnsWritten { get; private set; }

    /// <summary>
    /// Counts a raw SQL text in <see cref="ColumnsWritten"/>, because it may name a column.
    /// </summary>
    public void CountOpaqueSql() => ColumnsWritten++;

    /// <summary>
    /// Gets the number of subqueries written so far (§5.5: a <c>GROUP BY</c> key cannot hold one).
    /// </summary>
    public int SubqueriesWritten { get; private set; }

    /// <summary>
    /// Counts a subquery in <see cref="SubqueriesWritten"/>.
    /// </summary>
    public void CountSubquery() => SubqueriesWritten++;

    /// <summary>
    /// Writes a subquery of <c>GroupScalar</c> or <c>GroupExists</c>: inside it, a column of the statements around it can be
    /// used only through a <c>GROUP BY</c> key (§5.11).
    /// </summary>
    /// <param name="subquery">The subquery.</param>
    public void WriteGroupSubquery(SqlNode subquery)
    {
        int boundary = _groupBoundary;
        int keys = _keyDepth;
        _groupBoundary = _scopes.Count;
        _keyDepth = 0;
        subquery.Render(this);
        _groupBoundary = boundary;
        _keyDepth = keys;
    }

    /// <summary>
    /// Writes a <c>GROUP BY</c> key, inside which the columns of the grouped statement may be used (§5.11).
    /// </summary>
    /// <param name="key">The key.</param>
    public void WriteGroupKey(SqlNode key)
    {
        _keyDepth++;
        key.Render(this);
        _keyDepth--;
    }

    /// <summary>
    /// Gets a value indicating whether a subquery of <c>GroupScalar</c> or <c>GroupExists</c> is being written.
    /// </summary>
    public bool InGroupSubquery => _groupBoundary >= 0;

    // §5.11: a column of an outer statement is written with the same qualifier as a table of the same name in an inner
    // statement, and SQL resolves it to the inner one, so the correlation would compare the inner table with itself.
    private void CheckShadowed(int depth, TableAlias table, string column)
    {
        for (int inner = depth + 1; inner < _scopes.Count; inner++)
        {
            foreach (TableAlias candidate in _scopes[inner])
            {
                if (candidate.Reference == table.Reference)
                {
                    throw new InvalidOperationException(
                        "The column "
                            + table.Reference
                            + "."
                            + column
                            + " of an outer statement is used in a subquery whose FROM has a table of the same name, so SQL would read it as the subquery's column. Give one of the two tables an alias with As(\"...\")."
                    );
                }
            }
        }
    }

    private void CheckGroupSubquery(int depth, TableAlias table, string column)
    {
        if (depth < _groupBoundary && _keyDepth == 0)
        {
            throw new InvalidOperationException(
                "The column "
                    + table.Reference
                    + "."
                    + column
                    + " of the grouped statement is used inside GroupScalar or GroupExists. Only the GROUP BY keys (k) of the grouped statement can be used there."
            );
        }
    }

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
    /// Starts a SELECT statement and returns how deep it is: 1 for the outermost statement.
    /// </summary>
    /// <returns>The depth.</returns>
    public int EnterStatement() => ++_statementDepth;

    /// <summary>
    /// Ends the statement that <see cref="EnterStatement"/> started; the CTEs end with the outermost statement.
    /// </summary>
    public void ExitStatement()
    {
        if (--_statementDepth == 0)
        {
            _ctes.Clear();
        }
    }

    /// <summary>
    /// Writes a CTE of the outermost statement's <c>WITH</c>: <c>"t" AS (SELECT …)</c>. From then on, a reference to the named
    /// query writes its name (§5.11).
    /// </summary>
    /// <param name="query">The named query.</param>
    public void WriteCte(NamedQueryDefinition query)
    {
        WriteIdentifier(query.Name);
        Write(" AS (");
        WriteNamedQueryBody(query);
        Write(')');
        _ctes.Add(query);
    }

    /// <summary>
    /// Writes an occurrence of a named query in <c>FROM</c> or a join: its name (with the alias) when the statement lists it
    /// in <c>With</c>, or the query itself as a derived table (§5.11).
    /// </summary>
    /// <param name="source">The occurrence.</param>
    /// <param name="query">The named query.</param>
    public void WriteNamedQuerySource(TableAlias source, NamedQueryDefinition query)
    {
        foreach (NamedQueryDefinition cte in _ctes)
        {
            if (ReferenceEquals(cte, query))
            {
                WriteIdentifier(query.Name);
                if (source.Alias is not null)
                {
                    Write(" AS ");
                    WriteIdentifier(source.Alias);
                }

                return;
            }
        }

        Write('(');
        WriteNamedQueryBody(query);
        Write(") AS ");
        WriteIdentifier(source.Reference);
    }

    /// <summary>
    /// Throws when a table has the name of a CTE of the statement: SQL would read the name as the CTE (§5.11).
    /// </summary>
    /// <param name="table">The table's occurrence.</param>
    public void CheckCteShadow(TableAlias table)
    {
        if (table.Schema is not null)
        {
            return;
        }

        foreach (NamedQueryDefinition cte in _ctes)
        {
            if (cte.Name == table.Table)
            {
                throw new InvalidOperationException(
                    "The table "
                        + table.Table
                        + " has the name of a CTE of the statement's With, so SQL would read it as the CTE. Give the named query another name."
                );
            }
        }
    }

    // A named query is a statement of its own: it cannot use the columns of the statement around it.
    private void WriteNamedQueryBody(NamedQueryDefinition query)
    {
        if (!ReferenceEquals(query.Dialect, Dialect))
        {
            throw new InvalidOperationException(
                "The named query "
                    + query.Name
                    + " was built for "
                    + query.Dialect.Name
                    + " and is used in a "
                    + Dialect.Name
                    + " statement. Build it with the same dialect."
            );
        }

        IReadOnlyList<TableAlias>[] scopes = [.. _scopes];
        int boundary = _groupBoundary;
        int keys = _keyDepth;
        bool inHaving = InHaving;
        _scopes.Clear();
        _groupBoundary = -1;
        _keyDepth = 0;
        InHaving = false;
        new SelectNode(query.State, query.ColumnNames).Render(this);
        _scopes.Clear();
        _scopes.AddRange(scopes);
        _groupBoundary = boundary;
        _keyDepth = keys;
        InHaving = inHaving;
    }

    /// <summary>
    /// Throws when a column of a table that no enclosing statement has in its <c>FROM</c> is written: a column taken out of
    /// the lambda of another query (§14.3). Outside any scope (raw SQL), nothing is checked.
    /// </summary>
    /// <param name="table">The table of the column.</param>
    /// <param name="column">The name of the column.</param>
    public void CheckInScope(TableAlias table, string column)
    {
        ColumnsWritten++;
        if (_scopes.Count == 0)
        {
            return;
        }

        for (int depth = 0; depth < _scopes.Count; depth++)
        {
            foreach (TableAlias candidate in _scopes[depth])
            {
                // The same occurrence, not the same name: a column of another statement on the same table is out of scope too.
                if (ReferenceEquals(candidate, table))
                {
                    CheckGroupSubquery(depth, table, column);
                    CheckShadowed(depth, table, column);
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
    /// Writes the placeholder of a parameter. Each value given to the builder is its own node and gets its own parameter; a
    /// node written again (the same expression written twice in one statement, such as a GROUP BY key that the select list
    /// repeats) writes the parameter it already has, because PostgreSQL compares a GROUP BY key with the select list by its
    /// parameter numbers (<c>docs/design.ja.md</c> §5.12).
    /// </summary>
    /// <param name="parameter">The parameter node.</param>
    public void WriteParameter(ParameterNode parameter)
    {
        if (!_names.TryGetValue(parameter, out string? name))
        {
            name = Dialect.ParameterName(_parameters.Count);
            _parameters.Add(new SqlStatementParameter(name, parameter.ProviderValue) { DbType = Dialect.ParameterDbType(parameter) });
            _names.Add(parameter, name);
        }

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
