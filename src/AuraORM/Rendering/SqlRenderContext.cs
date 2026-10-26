namespace AuraORM;

/// <summary>
/// The state of rendering one statement: the dialect, the SQL text, and the parameters in the order they were added.
/// </summary>
internal sealed class SqlRenderContext : IDisposable
{
    private readonly List<SqlStatementParameter> _parameters = [];
    private readonly List<IReadOnlyList<TableAlias>> _scopes = [];
    private readonly List<NamedQueryDefinition> _ctes = [];
    private ParameterNode[] _nodes = [];
    private Dictionary<ParameterNode, string>? _names;
    private IReadOnlyList<NamedQueryDefinition> _listedCtes = [];
    private string? _writingCte;
    private int _statementDepth;
    private int _groupBoundary = -1;
    private int _keyDepth;
    private TableAlias? _mutationTarget;
    private HashSet<string>? _assignedBefore;
    private TableAlias? _returningTarget;
    private System.Collections.Immutable.ImmutableArray<string> _insertedColumns;
    private string? _returningPrefix;
    private int _returningDepth;

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
            IReadOnlyList<TableAlias> scope = _scopes[inner];
            for (int i = 0; i < scope.Count; i++)
            {
                if (scope[i].Reference == table.Reference)
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
            _listedCtes = [];
        }
    }

    /// <summary>
    /// Starts an <c>UPDATE</c> or <c>DELETE</c>: its columns are in scope, and a subquery inside it is an inner statement
    /// (§5.11, §7).
    /// </summary>
    /// <param name="target">The table that the statement changes.</param>
    public void EnterMutation(TableAlias target)
    {
        EnterStatement();
        EnterScope([target]);
        _mutationTarget = target;
    }

    /// <summary>
    /// Ends the statement that <see cref="EnterMutation"/> started.
    /// </summary>
    public void ExitMutation()
    {
        _mutationTarget = null;
        ExitScope();
        ExitStatement();
    }

    /// <summary>
    /// Starts the update of an upsert: the columns of the table and of the proposed row are in scope (§10).
    /// </summary>
    /// <param name="target">The table of the <c>INSERT</c>.</param>
    /// <param name="excluded">The occurrence of the proposed row.</param>
    /// <param name="columns">The columns that the rows set, which MySQL's row alias has (§10.2).</param>
    public void EnterUpsert(TableAlias target, TableAlias excluded, System.Collections.Immutable.ImmutableArray<string> columns)
    {
        // The scope of the INSERT has the table; the update's scope replaces it, so that the table is not an outer table of
        // itself.
        ExitScope();
        EnterScope([target, excluded]);
        _insertedColumns = columns;
    }

    /// <summary>
    /// Ends the update that <see cref="EnterUpsert"/> started.
    /// </summary>
    /// <param name="target">The table of the <c>INSERT</c>.</param>
    public void ExitUpsert(TableAlias target)
    {
        _insertedColumns = default;
        ExitScope();
        EnterScope([target]);
    }

    /// <summary>
    /// Returns the name that qualifies a column of the proposed row of the upsert being written: <c>excluded</c>, or on
    /// MariaDB the table's name inside <c>VALUE(…)</c>. Throws when a table of a subquery has that name, which SQL would
    /// read instead (§10.1, §10.2, as §5.11 does for an outer column).
    /// </summary>
    /// <returns>The name.</returns>
    /// <exception cref="InvalidOperationException">A subquery has a table of that name.</exception>
    public string CheckExcludedQualifier()
    {
        TableAlias target = _mutationTarget!;
        string name = Dialect.NamesProposedRowExcluded ? TableAlias.ExcludedName : target.Reference;
        foreach (IReadOnlyList<TableAlias> scope in _scopes)
        {
            for (int i = 0; i < scope.Count; i++)
            {
                TableAlias table = scope[i];
                if (!ReferenceEquals(table, target) && !table.IsExcluded && table.Reference == name)
                {
                    throw new InvalidOperationException(
                        "A column of excluded is read in a subquery whose FROM has a table named "
                            + name
                            + ", which SQL would read instead of the row proposed for insertion. Give that table an alias with As(\"...\")."
                    );
                }
            }
        }

        return name;
    }

    /// <summary>
    /// Gets the columns that the rows of the upsert being written set, for MySQL's row alias (§10.2).
    /// </summary>
    public System.Collections.Immutable.ImmutableArray<string> InsertedColumns => _insertedColumns;

    /// <summary>
    /// Writes the value of an assignment. On MySQL and MariaDB, which assign from left to right, the value cannot read a
    /// column of the target assigned before it (§7).
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="assignedBefore">The columns assigned before this one, or <see langword="null"/> when the dialect assigns
    /// all at once.</param>
    public void WriteAssignedValue(SqlNode value, HashSet<string>? assignedBefore)
    {
        _assignedBefore = assignedBefore;
        value.Render(this);
        _assignedBefore = null;
    }

    /// <summary>
    /// Writes the <c>RETURNING</c> of an <c>INSERT</c>, <c>UPDATE</c>, or <c>DELETE</c>, or SQL Server's <c>OUTPUT</c>, with
    /// a space before it; nothing when there are no items (§9). The target's columns are written unqualified, or after
    /// <c>INSERTED.</c> / <c>DELETED.</c>; inside a subquery they keep their table.
    /// </summary>
    /// <param name="target">The table that the statement changes.</param>
    /// <param name="items">The items.</param>
    /// <param name="outputPrefix">SQL Server's <c>INSERTED</c> or <c>DELETED</c>, or <see langword="null"/> for <c>RETURNING</c>.</param>
    /// <exception cref="InvalidOperationException">An item holds a subquery on SQL Server, or a subquery that reads the target.</exception>
    public void WriteReturning(TableAlias target, System.Collections.Immutable.ImmutableArray<SqlNode> items, string? outputPrefix)
    {
        if (items.IsDefaultOrEmpty)
        {
            return;
        }

        Write(outputPrefix is null ? " RETURNING " : " OUTPUT ");
        _returningTarget = target;
        _returningPrefix = outputPrefix;
        _returningDepth = _statementDepth;
        int subqueries = SubqueriesWritten;
        WriteList(items);
        _returningTarget = null;
        _returningPrefix = null;
        if (outputPrefix is not null && SubqueriesWritten != subqueries)
        {
            throw new InvalidOperationException(
                "Returning holds a subquery, and SQL Server does not accept a subquery in OUTPUT (message 10705). Read the value with a SELECT after the statement."
            );
        }
    }

    /// <summary>
    /// Writes the table of a column of the statement's <c>RETURNING</c> or <c>OUTPUT</c>, if the column needs one: nothing
    /// for <c>RETURNING</c>, <c>INSERTED.</c> or <c>DELETED.</c> for <c>OUTPUT</c> (§9).
    /// </summary>
    /// <param name="table">The column's table.</param>
    /// <returns><see langword="true"/> when the column is a top-level column of the target in <c>RETURNING</c> or
    /// <c>OUTPUT</c>, which then needs only its name.</returns>
    public bool WriteReturningQualifier(TableAlias table)
    {
        if (_returningTarget is null || !ReferenceEquals(table, _returningTarget) || _statementDepth != _returningDepth)
        {
            return false;
        }

        if (_returningPrefix is not null)
        {
            Write(_returningPrefix);
            Write('.');
        }

        return true;
    }

    /// <summary>
    /// Throws when a table in a <c>FROM</c> or a join is the table that the statement changes, where that is not accepted: in
    /// a subquery of <c>Returning</c> on every dialect (§9), and in a subquery of an <c>UPDATE</c> or <c>DELETE</c> on MySQL
    /// (error 1093, §7).
    /// </summary>
    /// <param name="table">The table's occurrence.</param>
    public void CheckTargetRead(TableAlias table)
    {
        if (_returningTarget is not null && table.Table == _returningTarget.Table && table.Schema == _returningTarget.Schema)
        {
            throw new InvalidOperationException(
                "A subquery of Returning reads "
                    + table.Table
                    + ", the table that the statement changes. Databases differ on whether it sees the table before or after the change, and SQLite leaves it undefined; read the value with a SELECT after the statement."
            );
        }

        if (
            _mutationTarget is not null
            && Dialect.RejectsTargetInMutationSubquery
            && table.Table == _mutationTarget.Table
            && table.Schema == _mutationTarget.Schema
        )
        {
            throw new InvalidOperationException(
                "A subquery reads "
                    + table.Table
                    + ", the table that the statement changes, which MySQL rejects (error 1093); a derived table avoids it only when the optimizer materializes it. Read the values with a SELECT first and pass them to In(column, values)."
            );
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
        _writingCte = query.Name;
        WriteNamedQueryBody(query);
        _writingCte = null;
        Write(')');
        _ctes.Add(query);
    }

    /// <summary>
    /// Starts the <c>WITH</c> of the outermost statement with all its CTEs, so that a CTE that reads one listed after it can
    /// be found (§5.11).
    /// </summary>
    /// <param name="ctes">The CTEs, in order.</param>
    public void ListCtes(IReadOnlyList<NamedQueryDefinition> ctes) => _listedCtes = ctes;

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

        for (int i = 0; i < _listedCtes.Count; i++)
        {
            if (ReferenceEquals(_listedCtes[i], query))
            {
                // §5.11, D1-41: a CTE reads only the CTEs before it.
                throw new InvalidOperationException(
                    "The CTE "
                        + _writingCte
                        + " reads "
                        + query.Name
                        + ", which With lists after it. List "
                        + query.Name
                        + " before "
                        + _writingCte
                        + "."
                );
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
        if (_assignedBefore is not null && ReferenceEquals(table, _mutationTarget) && _assignedBefore.Contains(column))
        {
            throw new InvalidOperationException(
                "The value of an assignment reads the column "
                    + column
                    + ", which the same UPDATE assigns before it. "
                    + Dialect.Name
                    + " assigns from left to right, so the value would read the new "
                    + column
                    + " where other databases read the old one. Assign "
                    + column
                    + " after this column, or split the statement."
            );
        }

        if (_scopes.Count == 0)
        {
            return;
        }

        for (int depth = 0; depth < _scopes.Count; depth++)
        {
            IReadOnlyList<TableAlias> scope = _scopes[depth];
            for (int i = 0; i < scope.Count; i++)
            {
                // The same occurrence, not the same name: a column of another statement on the same table is out of scope too.
                if (ReferenceEquals(scope[i], table))
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
        string? name = NameOf(parameter);
        if (name is null)
        {
            name = Dialect.ParameterName(_parameters.Count);
            _parameters.Add(new SqlStatementParameter(name, parameter.ProviderValue) { DbType = Dialect.ParameterDbType(parameter) });
            Remember(parameter, name);
        }

        Writer.Append(name);
    }

    // §16.1: a statement has few parameters, so their nodes are searched in an array; a dictionary is made only for many.
    private string? NameOf(ParameterNode parameter)
    {
        if (_names is not null)
        {
            return _names.TryGetValue(parameter, out string? named) ? named : null;
        }

        for (int i = 0; i < _parameters.Count; i++)
        {
            if (ReferenceEquals(_nodes[i], parameter))
            {
                return _parameters[i].Name;
            }
        }

        return null;
    }

    private void Remember(ParameterNode parameter, string name)
    {
        if (_names is not null)
        {
            _names.Add(parameter, name);
            return;
        }

        int count = _parameters.Count;
        if (count > 16)
        {
            _names = new Dictionary<ParameterNode, string>(count * 2, ReferenceEqualityComparer.Instance);
            for (int i = 0; i < count - 1; i++)
            {
                _names.Add(_nodes[i], _parameters[i].Name);
            }

            _names.Add(parameter, name);
            return;
        }

        if (count > _nodes.Length)
        {
            Array.Resize(ref _nodes, Math.Max(4, _nodes.Length * 2));
        }

        _nodes[count - 1] = parameter;
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
