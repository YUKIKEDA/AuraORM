using System.Collections.Immutable;

namespace AuraORM;

/// <summary>
/// An <c>INSERT … ON CONFLICT (…) DO UPDATE SET …</c> without a condition yet (<c>docs/design.ja.md</c> §10.1). Run it as it
/// is, or add <see cref="Where(Func{TColumns, TColumns, Condition{PerRow}})"/>.
/// </summary>
/// <typeparam name="TDialect">The SQL dialect.</typeparam>
/// <typeparam name="TMode">Whether the statement can be run: <see cref="Executable"/> or <see cref="SqlOnly"/>.</typeparam>
/// <typeparam name="TColumns">The column set of the table.</typeparam>
public sealed class UpsertUpdate<TDialect, TMode, TColumns> : UpsertStatement<TDialect, TMode, TColumns>
    where TDialect : IDialect
    where TMode : ExecutionMode
    where TColumns : ITableColumns<TColumns>
{
    private readonly TableAlias _excluded;
    private readonly TColumns _excludedColumns;
    private readonly ImmutableArray<string> _conflictTarget;
    private readonly ImmutableArray<Assignment> _assignments;

    internal UpsertUpdate(
        InsertStatement<TDialect, TMode, TColumns> insert,
        TableAlias excluded,
        TColumns excludedColumns,
        ImmutableArray<string> conflictTarget,
        ImmutableArray<Assignment> assignments
    )
        : base(insert, new ConflictClause(excluded, conflictTarget, assignments, null))
    {
        _excluded = excluded;
        _excludedColumns = excludedColumns;
        _conflictTarget = conflictTarget;
        _assignments = assignments;
    }

    /// <summary>
    /// Updates only the conflicting rows that meet a condition: <c>DO UPDATE SET … WHERE …</c>. The others are left as they
    /// are, and neither counted nor returned. A condition that may be empty (the <c>And</c> or <c>Or</c> of a list) is not
    /// accepted.
    /// </summary>
    /// <param name="condition">The condition, given the columns of the existing row and of the proposed row (<c>excluded</c>).</param>
    /// <returns>The statement.</returns>
    public UpsertStatement<TDialect, TMode, TColumns> Where(Func<TColumns, TColumns, Condition<PerRow>> condition)
    {
        ArgumentNullException.ThrowIfNull(condition);
        Condition<PerRow> where = condition(Table, _excludedColumns);
        ArgumentNullException.ThrowIfNull(where, nameof(condition));
        return new UpsertStatement<TDialect, TMode, TColumns>(Insert, new ConflictClause(_excluded, _conflictTarget, _assignments, where.Node));
    }

    /// <summary>Not accepted: a condition that may be empty. Check the list, and leave out <c>Where</c> to update every conflicting row.</summary>
    /// <param name="condition">The condition.</param>
    /// <returns>Nothing; the call does not compile.</returns>
    [Obsolete(MutationMessages.OptionalUpsertCondition, error: true)]
    public UpsertStatement<TDialect, TMode, TColumns> Where(Func<TColumns, TColumns, OptionalCondition<PerRow>> condition) =>
        throw new InvalidOperationException(MutationMessages.OptionalUpsertCondition);
}
