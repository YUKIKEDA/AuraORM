using System.ComponentModel;

namespace AuraORM;

/// <summary>
/// The base of a generated <c>RowProjection</c> or <c>GroupProjection</c> class: the projection of a result type, one item
/// per member (<c>docs/design.ja.md</c> §3.5).
/// </summary>
/// <typeparam name="TResult">The result type.</typeparam>
/// <typeparam name="TLevel">The evaluation level of every member.</typeparam>
public abstract class ResultProjection<TResult, TLevel> : IProjection<TResult, TLevel>
    where TLevel : EvaluationLevel
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ResultProjection{TResult, TLevel}"/> class.
    /// </summary>
    protected ResultProjection() { }

    /// <summary>
    /// Adds the projection of each member, in declaration order.
    /// </summary>
    /// <param name="items">The receiver.</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    protected abstract void AddItems(ProjectionItems<TLevel> items);

    /// <summary>
    /// Reads the result from the items added by <see cref="AddItems"/>, in the same order.
    /// </summary>
    /// <param name="reader">The reader of the current row.</param>
    /// <returns>The result.</returns>
    [EditorBrowsable(EditorBrowsableState.Never)]
    protected abstract TResult ReadResult(ref RowReader reader);

    void IProjection<TResult, TLevel>.CollectSelectItems(List<SqlNode> items) => AddItems(new ProjectionItems<TLevel>(items));

    TResult IProjection<TResult, TLevel>.Read(ref RowReader reader) => ReadResult(ref reader);
}
