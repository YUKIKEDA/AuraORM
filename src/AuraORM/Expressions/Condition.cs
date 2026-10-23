namespace AuraORM;

/// <summary>
/// A predicate: a condition for <c>WHERE</c>, <c>ON</c>, <c>HAVING</c>, or <c>CASE WHEN</c>.
/// </summary>
/// <typeparam name="TLevel">The evaluation level: <see cref="PerRow"/> or <see cref="PerGroup"/>.</typeparam>
/// <remarks>
/// A condition is not a value and cannot be selected, because SQL Server cannot use a predicate as a value
/// (<c>docs/design.ja.md</c> §4.1). Combine conditions with <see cref="Sql.And{TLevel}(Condition{TLevel}, Condition{TLevel}, Condition{TLevel}[])"/>
/// and <see cref="Sql.Or{TLevel}(Condition{TLevel}, Condition{TLevel}, Condition{TLevel}[])"/>.
/// </remarks>
public sealed class Condition<TLevel> : ISqlNodeSource
    where TLevel : EvaluationLevel
{
    internal Condition(SqlNode node)
    {
        Node = node;
    }

    internal SqlNode Node { get; }

    SqlNode ISqlNodeSource.SourceNode => Node;
}
