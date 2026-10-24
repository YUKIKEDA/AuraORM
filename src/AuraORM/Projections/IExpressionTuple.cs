namespace AuraORM;

/// <summary>
/// A tuple projection that says whether every item is an expression (<c>docs/design.ja.md</c> §5.10).
/// </summary>
internal interface IExpressionTuple
{
    /// <summary>
    /// Gets a value indicating whether every item is an expression, not a whole row or a result type.
    /// </summary>
    bool AllExpressions { get; }
}
