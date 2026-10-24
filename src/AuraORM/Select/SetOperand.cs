namespace AuraORM;

/// <summary>
/// A statement that a set operation adds after the first one, with the operator before it.
/// </summary>
/// <param name="Operator">The operator.</param>
/// <param name="State">The statement.</param>
internal sealed record SetOperand(SetOperator Operator, SelectState State);
