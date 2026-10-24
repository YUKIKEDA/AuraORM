namespace AuraORM;

/// <summary>
/// The aggregate functions of <see cref="AggregateNode"/>. Sums and averages of integers are separate kinds because their
/// SQL differs by dialect (<c>docs/design.ja.md</c> §4.2).
/// </summary>
internal enum AggregateFunction
{
    CountAll,
    Count,
    CountDistinct,
    SumInteger,
    Sum,
    SumSingle,
    AverageAsDouble,
    Average,
    Min,
    Max,
}

/// <summary>
/// An aggregate function call.
/// </summary>
internal sealed class AggregateNode(AggregateFunction function, SqlNode? argument) : SqlNode
{
    public AggregateFunction Function { get; } = function;

    /// <summary>
    /// Gets the argument, or <see langword="null"/> for <see cref="AggregateFunction.CountAll"/>.
    /// </summary>
    public SqlNode? Argument { get; } = argument;

    public override void Render(SqlRenderContext context) => context.Dialect.RenderAggregate(context, this);
}
