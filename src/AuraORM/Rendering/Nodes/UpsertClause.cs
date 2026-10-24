using System.Collections.Immutable;

namespace AuraORM;

/// <summary>
/// The clause of an <c>INSERT</c> that handles a conflict: <c>ON CONFLICT …</c> or <c>ON DUPLICATE KEY UPDATE …</c>
/// (<c>docs/design.ja.md</c> §10). It is written after the rows.
/// </summary>
/// <param name="excluded">The occurrence of the row proposed for insertion, whose columns the clause may read.</param>
internal abstract class UpsertClause(TableAlias excluded)
{
    /// <summary>
    /// Gets the occurrence of the row proposed for insertion.
    /// </summary>
    private protected TableAlias Excluded { get; } = excluded;

    /// <summary>
    /// Writes the clause, with a space before it.
    /// </summary>
    /// <param name="context">The rendering context, inside the <c>INSERT</c>'s statement and scope.</param>
    /// <param name="target">The table of the <c>INSERT</c>.</param>
    /// <param name="columns">The columns that the rows set.</param>
    public abstract void Render(SqlRenderContext context, TableAlias target, ImmutableArray<string> columns);

    /// <summary>
    /// Throws when the table's name is the name of the proposed row, where the dialect uses that name (§10.1, §10.2).
    /// </summary>
    /// <param name="context">The rendering context.</param>
    /// <param name="target">The table of the <c>INSERT</c>.</param>
    private protected static void CheckExcludedName(SqlRenderContext context, TableAlias target)
    {
        if (context.Dialect.NamesProposedRowExcluded && string.Equals(target.Table, TableAlias.ExcludedName, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "The table is named "
                    + target.Table
                    + ", the name that "
                    + context.Dialect.Name
                    + " gives the row proposed for insertion, so excluded would not name that row (SQLite reads the existing row instead). Rename the table, or write the statement with raw SQL."
            );
        }
    }

    /// <summary>
    /// Writes <c>"a" = …, "b" = …</c>, the targets unqualified, the values evaluated as the dialect does (§7).
    /// </summary>
    /// <param name="context">The rendering context.</param>
    /// <param name="assignments">The assignments, each to a column of the table.</param>
    private protected static void WriteAssignments(SqlRenderContext context, ImmutableArray<Assignment> assignments)
    {
        bool leftToRight = context.Dialect.AssignsLeftToRight;
        var assigned = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < assignments.Length; i++)
        {
            if (i > 0)
            {
                context.Write(", ");
            }

            context.WriteIdentifier(assignments[i].Column.Name);
            context.Write(" = ");
            context.WriteAssignedValue(assignments[i].Value, leftToRight ? assigned : null);
            assigned.Add(assignments[i].Column.Name);
        }
    }
}
