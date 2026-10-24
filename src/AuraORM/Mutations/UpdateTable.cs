using System.Collections.Immutable;

namespace AuraORM;

/// <summary>
/// The start of an <c>UPDATE</c>: <c>db.Update(User.Table)</c>. Give it the assignments with <see cref="Set"/>
/// (<c>docs/design.ja.md</c> §7).
/// </summary>
/// <typeparam name="TDialect">The SQL dialect.</typeparam>
/// <typeparam name="TMode">Whether the statement can be run: <see cref="Executable"/> or <see cref="SqlOnly"/>.</typeparam>
/// <typeparam name="TColumns">The column set of the table.</typeparam>
public sealed class UpdateTable<TDialect, TMode, TColumns>
    where TDialect : IDialect
    where TMode : ExecutionMode
    where TColumns : ITableColumns<TColumns>
{
    private readonly IStatementRunner? _session;
    private readonly TableAlias _target;
    private readonly TColumns _columns;

    internal UpdateTable(IStatementRunner? session, TableAlias target, TColumns columns)
    {
        _session = session;
        _target = target;
        _columns = columns;
    }

    /// <summary>
    /// Sets the assignments: <c>SET "a" = …, "b" = …</c>. Write them with <see cref="Sql.Set{T}(Column{T}, T)"/>:
    /// <c>u =&gt; [Set(u.Name, "Bob"), Set(u.Age, Add(u.Age, 1))]</c>. Every value is evaluated on the row before the update.
    /// Write <c>Where</c> or <c>AllRows()</c> next.
    /// </summary>
    /// <param name="assignments">The assignments, given the columns of the table.</param>
    /// <returns>The statement.</returns>
    /// <exception cref="ArgumentException">The list is empty, or assigns one column twice (<c>docs/design.ja.md</c> §14.3).</exception>
    public UpdateSet<TDialect, TMode, TColumns> Set(Func<TColumns, Assignment[]> assignments)
    {
        ArgumentNullException.ThrowIfNull(assignments);
        Assignment[] list = assignments(_columns);
        ArgumentNullException.ThrowIfNull(list, nameof(assignments));
        if (list.Length == 0)
        {
            throw new ArgumentException("UPDATE needs at least one assignment.", nameof(assignments));
        }

        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (Assignment assignment in list)
        {
            ArgumentNullException.ThrowIfNull(assignment, nameof(assignments));
            if (!names.Add(assignment.Column.Name))
            {
                throw new ArgumentException("The UPDATE assigns the column " + assignment.Column.Name + " twice.", nameof(assignments));
            }
        }

        return new UpdateSet<TDialect, TMode, TColumns>(_session, _target, _columns, ImmutableArray.Create(list));
    }
}
