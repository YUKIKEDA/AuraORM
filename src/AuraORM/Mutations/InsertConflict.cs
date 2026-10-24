using System.Collections.Immutable;

namespace AuraORM;

/// <summary>
/// An <c>INSERT … ON CONFLICT (…)</c> without its action yet: write <see cref="DoNothing"/> or <see cref="DoUpdate"/>
/// (<c>docs/design.ja.md</c> §10.1). It cannot be run.
/// </summary>
/// <typeparam name="TDialect">The SQL dialect.</typeparam>
/// <typeparam name="TMode">Whether the statement can be run: <see cref="Executable"/> or <see cref="SqlOnly"/>.</typeparam>
/// <typeparam name="TColumns">The column set of the table.</typeparam>
public sealed class InsertConflict<TDialect, TMode, TColumns>
    where TDialect : IOnConflictDialect
    where TMode : ExecutionMode
    where TColumns : ITableColumns<TColumns>
{
    private readonly InsertStatement<TDialect, TMode, TColumns> _insert;
    private readonly ImmutableArray<string> _target;

    internal InsertConflict(InsertStatement<TDialect, TMode, TColumns> insert, ImmutableArray<string> target)
    {
        _insert = insert;
        _target = target;
    }

    /// <summary>
    /// Skips the rows that conflict on the target: <c>ON CONFLICT ("a") DO NOTHING</c>.
    /// </summary>
    /// <returns>The statement.</returns>
    public UpsertStatement<TDialect, TMode, TColumns> DoNothing() =>
        new(_insert, new ConflictClause(TableAlias.Excluded(), _target, ImmutableArray<Assignment>.Empty, null));

    /// <summary>
    /// Updates the existing row of a conflict: <c>ON CONFLICT ("a") DO UPDATE SET "b" = "excluded"."b"</c>. The assignments
    /// follow the rules of <c>UPDATE</c> (§7); the first lambda parameter is the existing row, whose columns are assigned, and
    /// the second is the row proposed for insertion (<c>excluded</c>), whose column left out of the rows has its default.
    /// </summary>
    /// <param name="assignments">The assignments, given the columns of the existing row and of the proposed row.</param>
    /// <returns>The statement.</returns>
    /// <exception cref="ArgumentException">The list is empty, or assigns one column twice (§14.3).</exception>
    /// <exception cref="InvalidOperationException">An assignment assigns a column of <c>excluded</c> (§14.3).</exception>
    public UpsertUpdate<TDialect, TMode, TColumns> DoUpdate(Func<TColumns, TColumns, Assignment[]> assignments)
    {
        ArgumentNullException.ThrowIfNull(assignments);
        TableAlias excluded = TableAlias.Excluded();
        TColumns excludedColumns = TColumns.Create(excluded);
        ImmutableArray<Assignment> list = AssignmentList.Check(
            assignments(_insert.Table, excludedColumns),
            "DO UPDATE",
            _insert.Target,
            nameof(assignments)
        );
        return new UpsertUpdate<TDialect, TMode, TColumns>(_insert, excluded, excludedColumns, _target, list);
    }
}
