namespace AuraORM;

/// <summary>
/// A <c>CASE</c> expression that does not have its <c>ELSE</c> yet. Created by <see cref="Sql.Case{T, TLevel}(Condition{TLevel}, IExpr{T, TLevel})"/>.
/// </summary>
/// <typeparam name="T">The type of the result.</typeparam>
/// <typeparam name="TLevel">The evaluation level.</typeparam>
/// <remarks>
/// <c>ELSE</c> is required. For a result that is null when no branch matches, make <typeparamref name="T"/> nullable and
/// write <c>Else(null)</c> (<c>docs/design.ja.md</c> §4.2).
/// </remarks>
public sealed class CaseBuilder<T, TLevel>
    where TLevel : EvaluationLevel
{
    private readonly List<(SqlNode When, SqlNode Then)> _branches = [];

    internal CaseBuilder() { }

    /// <summary>
    /// Adds a branch: <c>WHEN condition THEN result</c>.
    /// </summary>
    /// <param name="condition">The condition.</param>
    /// <param name="result">The result when the condition is true.</param>
    /// <returns>This builder.</returns>
    public CaseBuilder<T, TLevel> When(Condition<TLevel> condition, IExpr<T, TLevel> result)
    {
        ArgumentNullException.ThrowIfNull(condition);
        _branches.Add((condition.Node, result.Node));
        return this;
    }

    /// <inheritdoc cref="When(Condition{TLevel}, IExpr{T, TLevel})"/>
    public CaseBuilder<T, TLevel> When(Condition<TLevel> condition, T result)
    {
        ArgumentNullException.ThrowIfNull(condition);
        _branches.Add((condition.Node, new ParameterNode<T>(result)));
        return this;
    }

    /// <summary>
    /// Finishes the expression with the result when no branch matches: <c>ELSE result END</c>.
    /// </summary>
    /// <param name="result">The result.</param>
    /// <returns>The expression.</returns>
    public IExpr<T, TLevel> Else(IExpr<T, TLevel> result) => new SqlExpr<T, TLevel>(new CaseNode([.. _branches], result.Node));

    /// <inheritdoc cref="Else(IExpr{T, TLevel})"/>
    public IExpr<T, TLevel> Else(T result) => new SqlExpr<T, TLevel>(new CaseNode([.. _branches], new ParameterNode<T>(result)));
}
