namespace AuraORM;

/// <summary>
/// The level of an expression that is evaluated once for each group: an aggregate, or a <c>GROUP BY</c> key.
/// </summary>
public sealed class PerGroup : EvaluationLevel
{
    private PerGroup() { }
}
