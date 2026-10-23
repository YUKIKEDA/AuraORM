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
    /// Combines the conditions of a list with <c>AND</c>, for filters whose number is known only at run time. Null items are
    /// skipped; two or more give <c>(a AND b …)</c>, one gives that condition, and none give an empty condition, which a
    /// SELECT <c>Where</c> writes as no <c>WHERE</c> (<c>docs/design.ja.md</c> §4.2).
    /// </summary>
    /// <typeparam name="TLevel">The evaluation level of every condition.</typeparam>
    /// <param name="conditions">The conditions; null items are skipped.</param>
    /// <returns>The condition, which may be empty.</returns>
    public static OptionalCondition<TLevel> And<TLevel>(IEnumerable<Condition<TLevel>?> conditions)
        where TLevel : EvaluationLevel => Combine(isAnd: true, conditions);

    /// <summary>
    /// Combines the conditions of a list with <c>OR</c>, for filters whose number is known only at run time. Null items are
    /// skipped; two or more give <c>(a OR b …)</c>, one gives that condition, and none give an empty condition, which a
    /// SELECT <c>Where</c> writes as no <c>WHERE</c> (<c>docs/design.ja.md</c> §4.2).
    /// </summary>
    /// <typeparam name="TLevel">The evaluation level of every condition.</typeparam>
    /// <param name="conditions">The conditions; null items are skipped.</param>
    /// <returns>The condition, which may be empty.</returns>
    public static OptionalCondition<TLevel> Or<TLevel>(IEnumerable<Condition<TLevel>?> conditions)
        where TLevel : EvaluationLevel => Combine(isAnd: false, conditions);

    /// <summary>
    /// Negates a condition: <c>NOT (c)</c>.
    /// </summary>
    /// <typeparam name="TLevel">The evaluation level of the condition.</typeparam>
    /// <param name="condition">The condition.</param>
    /// <returns>The negated condition.</returns>
    public static Condition<TLevel> Not<TLevel>(Condition<TLevel> condition)
        where TLevel : EvaluationLevel => new(new NotNode(condition.Node));

    private static OptionalCondition<TLevel> Combine<TLevel>(bool isAnd, IEnumerable<Condition<TLevel>?> conditions)
        where TLevel : EvaluationLevel
    {
        ArgumentNullException.ThrowIfNull(conditions);
        var nodes = new List<SqlNode>();
        foreach (Condition<TLevel>? condition in conditions)
        {
            if (condition is not null)
            {
                nodes.Add(condition.Node);
            }
        }

        return new OptionalCondition<TLevel>(
            nodes.Count switch
            {
                0 => null,
                1 => nodes[0],
                _ => new LogicalNode(isAnd, [.. nodes]),
            }
        );
    }

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
