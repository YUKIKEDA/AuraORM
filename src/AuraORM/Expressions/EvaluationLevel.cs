namespace AuraORM;

/// <summary>
/// The base of the evaluation levels: <see cref="PerRow"/> and <see cref="PerGroup"/>.
/// </summary>
/// <remarks>
/// An expression carries its level as a type argument so that row values and aggregates cannot be mixed
/// (<c>docs/design.ja.md</c> §4.5). The level types have no instances.
/// </remarks>
public abstract class EvaluationLevel
{
    private protected EvaluationLevel() { }
}
