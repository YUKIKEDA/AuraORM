namespace AuraORM;

/// <summary>
/// An object that renders as one node of the statement tree: an expression or a condition.
/// </summary>
internal interface ISqlNodeSource
{
    /// <summary>
    /// Gets the node.
    /// </summary>
    SqlNode SourceNode { get; }
}
