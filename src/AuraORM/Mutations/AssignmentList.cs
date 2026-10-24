using System.Collections.Immutable;

namespace AuraORM;

/// <summary>
/// Checks a list of assignments of <c>UPDATE</c>, <c>DO UPDATE</c>, or <c>ON DUPLICATE KEY UPDATE</c> (<c>docs/design.ja.md</c>
/// §7, §10, §14.3).
/// </summary>
internal static class AssignmentList
{
    /// <summary>
    /// Checks that the list has at least one assignment, each to a different column, and, for an upsert, each to a column
    /// of the table being changed.
    /// </summary>
    /// <param name="list">The assignments.</param>
    /// <param name="statement">The name of the clause, for the messages.</param>
    /// <param name="target">The table that the assignments must assign, or <see langword="null"/> for <c>UPDATE</c>, whose
    /// lambda gets only that table.</param>
    /// <param name="paramName">The name of the parameter that gave the list.</param>
    /// <returns>The checked list.</returns>
    /// <exception cref="ArgumentException">The list is empty, or assigns one column twice.</exception>
    /// <exception cref="InvalidOperationException">An assignment assigns a column that is not of <paramref name="target"/>.</exception>
    public static ImmutableArray<Assignment> Check(Assignment[]? list, string statement, TableAlias? target, string paramName)
    {
        ArgumentNullException.ThrowIfNull(list, paramName);
        if (list.Length == 0)
        {
            throw new ArgumentException(statement + " needs at least one assignment.", paramName);
        }

        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (Assignment assignment in list)
        {
            ArgumentNullException.ThrowIfNull(assignment, paramName);
            if (target is not null && !ReferenceEquals(assignment.Column.Table, target))
            {
                // §10.1: an excluded column has the type of the table's column, so only this check stops it.
                throw new InvalidOperationException(
                    "An assignment of "
                        + statement
                        + " assigns "
                        + assignment.Column.Table.Reference
                        + "."
                        + assignment.Column.Name
                        + ", which is not a column of the existing row. Assign the columns of the first lambda parameter; read excluded only in the values."
                );
            }

            if (!names.Add(assignment.Column.Name))
            {
                throw new ArgumentException("The " + statement + " assigns the column " + assignment.Column.Name + " twice.", paramName);
            }
        }

        return [.. list];
    }
}
