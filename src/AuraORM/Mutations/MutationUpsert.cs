using System.Collections.Immutable;

namespace AuraORM;

/// <summary>
/// The UPSERT of an <c>INSERT</c> (<c>docs/design.ja.md</c> §10), in the syntax of each dialect: <c>ON CONFLICT</c> on SQLite
/// and PostgreSQL (<see cref="IOnConflictDialect"/>), <c>ON DUPLICATE KEY UPDATE</c> on MySQL and MariaDB
/// (<see cref="IOnDuplicateKeyDialect"/>). SQL Server has neither; its <c>MERGE</c> is roadmap row U2.
/// </summary>
public static class MutationUpsert
{
    /// <summary>
    /// Skips the rows that conflict on any unique constraint: <c>ON CONFLICT DO NOTHING</c> (§10.1).
    /// </summary>
    /// <typeparam name="TDialect">The SQL dialect.</typeparam>
    /// <typeparam name="TMode">Whether the statement can be run.</typeparam>
    /// <typeparam name="TColumns">The column set of the table.</typeparam>
    /// <param name="insert">The INSERT.</param>
    /// <returns>The statement.</returns>
    /// <exception cref="ArgumentException">The rows set no column on SQLite, which does not accept an upsert after
    /// <c>DEFAULT VALUES</c> (§10.1).</exception>
    public static UpsertStatement<TDialect, TMode, TColumns> OnConflictDoNothing<TDialect, TMode, TColumns>(
        this InsertStatement<TDialect, TMode, TColumns> insert
    )
        where TDialect : IOnConflictDialect
        where TMode : ExecutionMode
        where TColumns : ITableColumns<TColumns>
    {
        ArgumentNullException.ThrowIfNull(insert);
        CheckColumns<TDialect>(insert.SetsNoColumn, nameof(insert));
        return new(insert, new ConflictClause(TableAlias.Excluded(), ImmutableArray<string>.Empty, ImmutableArray<Assignment>.Empty, null));
    }

    /// <summary>
    /// Handles the rows that conflict on a target: <c>ON CONFLICT ("a", "b")</c>, then <c>DoNothing()</c> or
    /// <c>DoUpdate(…)</c> (§10.1). The target is not checked against the unique constraints that the schema declares; the
    /// database rejects a target that no unique constraint matches.
    /// </summary>
    /// <typeparam name="TDialect">The SQL dialect.</typeparam>
    /// <typeparam name="TMode">Whether the statement can be run.</typeparam>
    /// <typeparam name="TColumns">The column set of the table.</typeparam>
    /// <param name="insert">The INSERT.</param>
    /// <param name="target">The columns of the target: <c>u =&gt; [u.Email]</c>.</param>
    /// <returns>The start of the clause.</returns>
    /// <exception cref="ArgumentException">The target is empty or has a column twice, or the rows set no column on SQLite
    /// (§10.1, §14.3).</exception>
    /// <exception cref="InvalidOperationException">The target has a column that is not of the table (§14.3).</exception>
    public static InsertConflict<TDialect, TMode, TColumns> OnConflict<TDialect, TMode, TColumns>(
        this InsertStatement<TDialect, TMode, TColumns> insert,
        Func<TColumns, ITableColumn[]> target
    )
        where TDialect : IOnConflictDialect
        where TMode : ExecutionMode
        where TColumns : ITableColumns<TColumns>
    {
        ArgumentNullException.ThrowIfNull(insert);
        ArgumentNullException.ThrowIfNull(target);
        CheckColumns<TDialect>(insert.SetsNoColumn, nameof(insert));
        ITableColumn[] columns = target(insert.Table);
        ArgumentNullException.ThrowIfNull(columns, nameof(target));
        if (columns.Length == 0)
        {
            throw new ArgumentException("ON CONFLICT needs at least one column of the target.", nameof(target));
        }

        var names = ImmutableArray.CreateBuilder<string>(columns.Length);
        foreach (ITableColumn column in columns)
        {
            ArgumentNullException.ThrowIfNull(column, nameof(target));
            ColumnNode node = column.ColumnNode;
            if (!ReferenceEquals(node.Table, insert.Target))
            {
                throw new InvalidOperationException(
                    "The conflict target has " + node.Table.Reference + "." + node.Name + ", which is not a column of the table of the INSERT."
                );
            }

            if (names.Contains(node.Name))
            {
                throw new ArgumentException("The conflict target has the column " + node.Name + " twice.", nameof(target));
            }

            names.Add(node.Name);
        }

        return new InsertConflict<TDialect, TMode, TColumns>(insert, names.MoveToImmutable());
    }

    /// <summary>
    /// Updates the existing row of a conflict on any unique key: <c>ON DUPLICATE KEY UPDATE "b" = …</c> (§10.2). The proposed
    /// row (<c>excluded</c>) is MySQL's row alias <c>AS `excluded`</c>, or <c>VALUE(`b`)</c> on MariaDB. MySQL and MariaDB
    /// evaluate the assignments from left to right, so a value that reads a column assigned before it throws when the
    /// statement is rendered, as in <c>UPDATE</c> (§7). To leave a conflicting row as it is, assign a column that can be
    /// assigned to itself: <c>(u, excluded) =&gt; [Set(u.Name, u.Name)]</c> (an <c>[AutoIncrement]</c> column cannot be).
    /// </summary>
    /// <typeparam name="TDialect">The SQL dialect.</typeparam>
    /// <typeparam name="TMode">Whether the statement can be run.</typeparam>
    /// <typeparam name="TColumns">The column set of the table.</typeparam>
    /// <param name="insert">The INSERT.</param>
    /// <param name="assignments">The assignments, given the columns of the existing row and of the proposed row.</param>
    /// <returns>The statement.</returns>
    /// <exception cref="ArgumentException">The list is empty, or assigns one column twice (§14.3).</exception>
    /// <exception cref="InvalidOperationException">An assignment assigns a column of <c>excluded</c> (§14.3).</exception>
    public static UpsertStatement<TDialect, TMode, TColumns> OnDuplicateKeyUpdate<TDialect, TMode, TColumns>(
        this InsertStatement<TDialect, TMode, TColumns> insert,
        Func<TColumns, TColumns, Assignment[]> assignments
    )
        where TDialect : IOnDuplicateKeyDialect
        where TMode : ExecutionMode
        where TColumns : ITableColumns<TColumns>
    {
        ArgumentNullException.ThrowIfNull(insert);
        ArgumentNullException.ThrowIfNull(assignments);
        TableAlias excluded = TableAlias.Excluded();
        ImmutableArray<Assignment> list = AssignmentList.Check(
            assignments(insert.Table, TColumns.Create(excluded)),
            "ON DUPLICATE KEY UPDATE",
            insert.Target,
            nameof(assignments)
        );
        return new(insert, new DuplicateKeyClause(excluded, list));
    }

    // §10.1: "The upsert-clause is not supported after DEFAULT VALUES" (SQLite's lang_insert.html).
    private static void CheckColumns<TDialect>(bool setsNoColumn, string paramName)
        where TDialect : IDialect
    {
        if (setsNoColumn && !DialectRenderer.For<TDialect>().AcceptsUpsertAfterDefaultValues)
        {
            throw new ArgumentException(
                "The rows set no column, and SQLite does not accept ON CONFLICT after DEFAULT VALUES. Set a column in the rows.",
                paramName
            );
        }
    }
}
