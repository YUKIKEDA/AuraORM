namespace AuraORM;

/// <content>
/// The logical operators (<c>docs/design.ja.md</c> §4.2).
/// </content>
public static partial class Sql
{
    /// <summary>
    /// Combines conditions with <c>AND</c>: <c>(a AND b AND …)</c>.
    /// </summary>
    /// <typeparam name="TLevel">The evaluation level of every condition.</typeparam>
    /// <param name="first">The first condition.</param>
    /// <param name="second">The second condition.</param>
    /// <param name="rest">More conditions.</param>
    /// <returns>The condition.</returns>
    public static Condition<TLevel> And<TLevel>(Condition<TLevel> first, Condition<TLevel> second, params Condition<TLevel>[] rest)
        where TLevel : EvaluationLevel => new(new LogicalNode(isAnd: true, Operands(first, second, rest)));

    /// <summary>
    /// Combines conditions with <c>OR</c>: <c>(a OR b OR …)</c>.
    /// </summary>
    /// <typeparam name="TLevel">The evaluation level of every condition.</typeparam>
    /// <param name="first">The first condition.</param>
    /// <param name="second">The second condition.</param>
    /// <param name="rest">More conditions.</param>
    /// <returns>The condition.</returns>
    public static Condition<TLevel> Or<TLevel>(Condition<TLevel> first, Condition<TLevel> second, params Condition<TLevel>[] rest)
        where TLevel : EvaluationLevel => new(new LogicalNode(isAnd: false, Operands(first, second, rest)));

    /// <summary>
    /// Negates a condition: <c>NOT (c)</c>.
    /// </summary>
    /// <typeparam name="TLevel">The evaluation level of the condition.</typeparam>
    /// <param name="condition">The condition.</param>
    /// <returns>The negated condition.</returns>
    public static Condition<TLevel> Not<TLevel>(Condition<TLevel> condition)
        where TLevel : EvaluationLevel => new(new NotNode(condition.Node));

    private static SqlNode[] Operands<TLevel>(Condition<TLevel> first, Condition<TLevel> second, Condition<TLevel>[] rest)
        where TLevel : EvaluationLevel
    {
        ArgumentNullException.ThrowIfNull(rest);
        var nodes = new SqlNode[rest.Length + 2];
        nodes[0] = first.Node;
        nodes[1] = second.Node;
        for (int i = 0; i < rest.Length; i++)
        {
            nodes[i + 2] = rest[i].Node;
        }

        return nodes;
    }
}
