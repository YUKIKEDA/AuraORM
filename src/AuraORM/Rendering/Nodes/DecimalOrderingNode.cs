namespace AuraORM;

/// <summary>
/// Marks an expression that orders or aggregates <see cref="decimal"/> values: a comparison by size, <c>BETWEEN</c>, an
/// <c>ORDER BY</c> key, or <c>MIN</c> / <c>MAX</c> / <c>SUM</c> / <c>AVG</c>. SQLite stores a decimal as text, so it would
/// order such values as strings; its renderer rejects the expression (<c>docs/design.ja.md</c> §3.3, §14.3). The other
/// dialects write the expression unchanged.
/// </summary>
internal sealed class DecimalOrderingNode(SqlNode inner, string operation) : SqlNode
{
    public SqlNode Inner { get; } = inner;

    public override SqlNode Unwrapped => Inner.Unwrapped;

    public override void Render(SqlRenderContext context)
    {
        context.Dialect.CheckDecimalOrdering(operation);
        Inner.Render(context);
    }

    /// <summary>
    /// Wraps the node when <typeparamref name="T"/> is <see cref="decimal"/> or <see cref="decimal"/>?.
    /// </summary>
    /// <typeparam name="T">The type of the ordered or aggregated values.</typeparam>
    /// <param name="node">The node.</param>
    /// <param name="operation">What the node does, for the message.</param>
    /// <returns>The node, wrapped or not.</returns>
    public static SqlNode For<T>(SqlNode node, string operation) =>
        typeof(T) == typeof(decimal) || typeof(T) == typeof(decimal?) ? new DecimalOrderingNode(node, operation) : node;
}
