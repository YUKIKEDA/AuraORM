namespace AuraORM;

/// <summary>
/// A query with a name (<c>docs/design.ja.md</c> §5.11), created by the <c>As</c> that the source generator writes for each
/// result type and table. It is a derived table where it is used, or a CTE when the statement lists it in <c>With</c>.
/// </summary>
public abstract class NamedQuery
{
    private protected NamedQuery(NamedQueryDefinition definition, string? alias)
    {
        Definition = definition;
        Alias = alias;
    }

    /// <summary>
    /// Gets the query and its name.
    /// </summary>
    internal NamedQueryDefinition Definition { get; }

    /// <summary>
    /// Gets the alias of this reference (<c>t.As("t2")</c>), or <see langword="null"/>.
    /// </summary>
    internal string? Alias { get; }
}

/// <summary>
/// A query with a name whose rows have the column set <typeparamref name="TColumns"/> (<c>docs/design.ja.md</c> §5.11): pass
/// it to <c>From</c> or a join, and to <c>With</c> to write it as a CTE.
/// </summary>
/// <typeparam name="TColumns">The column set of its rows.</typeparam>
/// <typeparam name="TNullable">The column set on the nullable side of an outer join.</typeparam>
public sealed class NamedQuery<TColumns, TNullable> : NamedQuery, ITableSource<TColumns, TNullable>
    where TColumns : ITableColumns<TColumns>
    where TNullable : ITableColumns<TNullable>
{
    private NamedQuery(NamedQueryDefinition definition, string? alias)
        : base(definition, alias) { }

    internal NamedQuery(NamedQueryDefinition definition)
        : this(definition, alias: null) { }

    /// <summary>
    /// Creates another reference to this named query under an alias, to use it twice in one statement:
    /// <c>FROM "t" INNER JOIN "t" AS "t2"</c> for a CTE, the query written again as <c>(SELECT …) AS "t2"</c> for a derived
    /// table.
    /// </summary>
    /// <param name="alias">The alias.</param>
    /// <returns>The reference.</returns>
    public NamedQuery<TColumns, TNullable> As(string alias)
    {
        ArgumentException.ThrowIfNullOrEmpty(alias);
        return new(Definition, alias);
    }

    TableAlias ITableSource<TColumns, TNullable>.CreateOccurrence() => new(Definition, Alias);
}
