namespace AuraORM;

/// <summary>
/// The expression that the functions of <see cref="Sql"/> return.
/// </summary>
/// <typeparam name="T">The CLR type of the value.</typeparam>
/// <typeparam name="TLevel">The evaluation level.</typeparam>
internal sealed class SqlExpr<T, TLevel>(SqlNode node) : IExpr<T, TLevel>, ISqlNodeSource
    where TLevel : EvaluationLevel
{
    SqlNode IExpr<T, TLevel>.Node => node;

    SqlNode ISqlNodeSource.SourceNode => node;
}
