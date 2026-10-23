using System.ComponentModel;

namespace AuraORM;

/// <summary>
/// The base of a generated <c>Insert</c> class: the values of one row to insert. A column that is not set is left out of
/// the column list, so the database's default applies (<c>docs/design.ja.md</c> §6.1).
/// </summary>
public abstract class InsertModel
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InsertModel"/> class.
    /// </summary>
    protected InsertModel() { }

    /// <summary>
    /// Adds the values that were set, with the position of their column in the table.
    /// </summary>
    /// <param name="values">The receiver of the values.</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    protected abstract void WriteValues(ref InsertValues values);

    internal void Write(ref InsertValues values) => WriteValues(ref values);
}
