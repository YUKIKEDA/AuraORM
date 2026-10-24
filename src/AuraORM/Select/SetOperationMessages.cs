namespace AuraORM;

/// <summary>
/// The messages of the set operations that do not compile (<c>docs/design.ja.md</c> §5.10). Each is the message of an
/// <see cref="ObsoleteAttribute"/> with <c>error: true</c>, so the call fails with CS0619 and says how to fix it.
/// </summary>
internal static class SetOperationMessages
{
    /// <summary>
    /// The other statement has <c>OrderBy</c>, <c>Limit</c>, or <c>Offset</c>, or is a set operation.
    /// </summary>
    public const string RejectedOperand =
        "A set operation's operand is a statement right after its projection: an operand with OrderBy, Limit, or Offset is not accepted (SQLite rejects them inside a compound SELECT), nor is another set operation (mixed operators read differently on SQLite); order or limit the result of the set operation, or make the operand a derived table.";

    /// <summary>
    /// A set operation of one operator is combined with another operator.
    /// </summary>
    public const string MixedOperators =
        "A chain of set operations holds one operator: mixed operators without parentheses return different rows on SQLite, which evaluates them from left to right, than on the other dialects; make the first set operation a derived table, then combine it.";

    /// <summary>
    /// A statement with <c>OrderBy</c>, <c>Limit</c>, or <c>Offset</c> is the first operand.
    /// </summary>
    public const string OrderedOperand =
        "A set operation's operand cannot be ordered or limited (SQLite rejects it inside a compound SELECT); combine the statements first, then put OrderBy, Limit, and Offset on the result of the set operation.";
}
