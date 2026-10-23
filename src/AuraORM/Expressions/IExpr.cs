namespace AuraORM;

/// <summary>
/// A SQL expression that has a value.
/// </summary>
/// <typeparam name="T">
/// The CLR type that the value is read as, including whether it can be null: <c>int?</c> for a nullable integer column,
/// <c>string?</c> for a nullable text column.
/// </typeparam>
/// <typeparam name="TLevel">The evaluation level: <see cref="PerRow"/> or <see cref="PerGroup"/>.</typeparam>
/// <remarks>
/// The interface is covariant in <typeparamref name="T"/> so that an expression of <c>string</c> can be used where an
/// expression of <c>string?</c> is expected without a nullability warning (<c>docs/design.ja.md</c> §4.1). Expressions are
/// created by the functions of <see cref="Sql"/> and by the columns that the source generator emits.
/// </remarks>
public interface IExpr<out T, TLevel> : IProjection<T, TLevel>
    where TLevel : EvaluationLevel
{
    /// <summary>
    /// Gets the node of the statement tree that this expression renders.
    /// </summary>
    internal SqlNode Node { get; }

    /// <inheritdoc />
    void IProjection<T, TLevel>.CollectSelectItems(List<SqlNode> items) => items.Add(Node);
}
