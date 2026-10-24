using System.Collections.Immutable;

namespace AuraORM;

/// <summary>
/// Starts a SELECT statement for a <see cref="Database{TDialect}"/>, a <see cref="Transaction{TDialect}"/>, or a
/// <see cref="SqlBuilder{TDialect}"/>.
/// </summary>
internal static class SelectStart
{
    public static SelectFrom<TDialect, TMode, TColumns, TNullable> From<TDialect, TMode, TColumns, TNullable>(
        IStatementRunner? session,
        ITableSource<TColumns, TNullable> table,
        ImmutableArray<NamedQueryDefinition> ctes
    )
        where TDialect : IDialect
        where TMode : ExecutionMode
        where TColumns : ITableColumns<TColumns>
        where TNullable : ITableColumns<TNullable>
    {
        ArgumentNullException.ThrowIfNull(table);
        TableAlias alias = table.CreateOccurrence();
        return new SelectFrom<TDialect, TMode, TColumns, TNullable>(session, SelectState.From(alias) with { Ctes = ctes }, TColumns.Create(alias));
    }

    /// <summary>
    /// Checks the named queries of <c>With</c> and returns their definitions (§5.11).
    /// </summary>
    /// <param name="queries">The named queries.</param>
    /// <returns>The definitions, in order.</returns>
    /// <exception cref="ArgumentException">A query is an aliased reference, or two have the same name.</exception>
    public static ImmutableArray<NamedQueryDefinition> Ctes(NamedQuery[] queries)
    {
        ArgumentNullException.ThrowIfNull(queries);
        var definitions = ImmutableArray.CreateBuilder<NamedQueryDefinition>(queries.Length);
        foreach (NamedQuery query in queries)
        {
            ArgumentNullException.ThrowIfNull(query, nameof(queries));
            if (query.Alias is not null)
            {
                throw new ArgumentException(
                    "With takes a named query itself, not an aliased reference to it (As(\"...\") on a named query).",
                    nameof(queries)
                );
            }

            foreach (NamedQueryDefinition defined in definitions)
            {
                if (defined.Name == query.Definition.Name)
                {
                    throw new ArgumentException("Two named queries of With have the name " + defined.Name + ".", nameof(queries));
                }
            }

            definitions.Add(query.Definition);
        }

        return definitions.MoveToImmutable();
    }
}
