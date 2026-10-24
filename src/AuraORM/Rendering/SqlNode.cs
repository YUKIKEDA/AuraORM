namespace AuraORM;

/// <summary>
/// A node of the statement tree. The tree does not depend on the dialect; only rendering does.
/// </summary>
internal abstract class SqlNode
{
    /// <summary>
    /// Writes the SQL of this node.
    /// </summary>
    /// <param name="context">The rendering context of the statement.</param>
    public abstract void Render(SqlRenderContext context);

    /// <summary>
    /// Gets the node that this node writes, under the marks that change nothing in the tree's shape (a decimal ordering
    /// check, a <c>GROUP BY</c> key); the node itself for any other node.
    /// </summary>
    public virtual SqlNode Unwrapped => this;
}
