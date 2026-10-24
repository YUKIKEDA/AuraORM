using System.Collections.Immutable;

namespace AuraORM;

/// <summary>
/// An <c>UPDATE</c> with its assignments and no condition yet. It cannot be run: write <see cref="Where(Func{TColumns, Condition{PerRow}})"/>, or
/// <see cref="AllRows"/> to change every row (<c>docs/design.ja.md</c> §7).
/// </summary>
/// <typeparam name="TDialect">The SQL dialect.</typeparam>
/// <typeparam name="TMode">Whether the statement can be run: <see cref="Executable"/> or <see cref="SqlOnly"/>.</typeparam>
/// <typeparam name="TColumns">The column set of the table.</typeparam>
public sealed class UpdateSet<TDialect, TMode, TColumns>
    where TDialect : IDialect
    where TMode : ExecutionMode
    where TColumns : ITableColumns<TColumns>
{
    private readonly IStatementRunner? _session;
    private readonly TableAlias _target;
    private readonly TColumns _columns;
    private readonly ImmutableArray<Assignment> _assignments;

    internal UpdateSet(IStatementRunner? session, TableAlias target, TColumns columns, ImmutableArray<Assignment> assignments)
    {
        _session = session;
        _target = target;
        _columns = columns;
        _assignments = assignments;
    }

    /// <summary>
    /// Changes the rows that meet a condition: <c>WHERE …</c>. A condition that may be empty (the <c>And</c> or <c>Or</c> of a
    /// list) is not accepted, because an empty one would change every row; write <see cref="AllRows"/> for that.
    /// </summary>
    /// <param name="condition">The condition, given the columns of the table.</param>
    /// <returns>The statement.</returns>
    public UpdateStatement<TDialect, TMode, TColumns> Where(Func<TColumns, Condition<PerRow>> condition)
    {
        ArgumentNullException.ThrowIfNull(condition);
        Condition<PerRow> where = condition(_columns);
        ArgumentNullException.ThrowIfNull(where, nameof(condition));
        return new(_session, _target, _columns, _assignments, where.Node);
    }

    /// <summary>Not accepted: a condition that may be empty would change every row. Write <see cref="AllRows"/> for that.</summary>
    /// <param name="condition">The condition.</param>
    /// <returns>Nothing; the call does not compile.</returns>
    [Obsolete(MutationMessages.OptionalCondition, error: true)]
    public UpdateStatement<TDialect, TMode, TColumns> Where(Func<TColumns, OptionalCondition<PerRow>> condition) =>
        throw new InvalidOperationException(MutationMessages.OptionalCondition);

    /// <summary>
    /// Changes every row of the table. It adds nothing to the SQL.
    /// </summary>
    /// <returns>The statement.</returns>
    public UpdateStatement<TDialect, TMode, TColumns> AllRows() => new(_session, _target, _columns, _assignments, null);
}
