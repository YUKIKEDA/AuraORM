namespace AuraORM;

/// <summary>
/// The start of a <c>DELETE</c>: <c>db.Delete(User.Table)</c>. It cannot be run: write <see cref="Where(Func{TColumns, Condition{PerRow}})"/>, or
/// <see cref="AllRows"/> to delete every row (<c>docs/design.ja.md</c> §8).
/// </summary>
/// <typeparam name="TDialect">The SQL dialect.</typeparam>
/// <typeparam name="TMode">Whether the statement can be run: <see cref="Executable"/> or <see cref="SqlOnly"/>.</typeparam>
/// <typeparam name="TColumns">The column set of the table.</typeparam>
public sealed class DeleteFrom<TDialect, TMode, TColumns>
    where TDialect : IDialect
    where TMode : ExecutionMode
    where TColumns : ITableColumns<TColumns>
{
    private readonly IStatementRunner? _session;
    private readonly TableAlias _target;
    private readonly TColumns _columns;

    internal DeleteFrom(IStatementRunner? session, TableAlias target, TColumns columns)
    {
        _session = session;
        _target = target;
        _columns = columns;
    }

    /// <summary>
    /// Deletes the rows that meet a condition: <c>WHERE …</c>. A condition that may be empty (the <c>And</c> or <c>Or</c> of a
    /// list) is not accepted, because an empty one would delete every row; write <see cref="AllRows"/> for that.
    /// </summary>
    /// <param name="condition">The condition, given the columns of the table.</param>
    /// <returns>The statement.</returns>
    public DeleteStatement<TDialect, TMode, TColumns> Where(Func<TColumns, Condition<PerRow>> condition)
    {
        ArgumentNullException.ThrowIfNull(condition);
        Condition<PerRow> where = condition(_columns);
        ArgumentNullException.ThrowIfNull(where, nameof(condition));
        return new(_session, _target, where.Node);
    }

    /// <summary>Not accepted: a condition that may be empty would change every row. Write <see cref="AllRows"/> for that.</summary>
    /// <param name="condition">The condition.</param>
    /// <returns>Nothing; the call does not compile.</returns>
    [Obsolete(MutationMessages.OptionalCondition, error: true)]
    public DeleteStatement<TDialect, TMode, TColumns> Where(Func<TColumns, OptionalCondition<PerRow>> condition) =>
        throw new InvalidOperationException(MutationMessages.OptionalCondition);

    /// <summary>
    /// Deletes every row of the table. It adds nothing to the SQL.
    /// </summary>
    /// <returns>The statement.</returns>
    public DeleteStatement<TDialect, TMode, TColumns> AllRows() => new(_session, _target, null);
}
