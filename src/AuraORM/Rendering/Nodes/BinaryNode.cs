namespace AuraORM;

/// <summary>
/// The operators of <see cref="BinaryNode"/>.
/// </summary>
internal enum BinaryOperator
{
    Add,
    Subtract,
    Multiply,
    Divide,
    IntegerDivide,
    Modulo,
}

/// <summary>
/// An arithmetic expression. It is always written in parentheses (<c>docs/design.ja.md</c> §5.12).
/// </summary>
internal sealed class BinaryNode(BinaryOperator op, SqlNode left, SqlNode right) : SqlNode
{
    public BinaryOperator Operator { get; } = op;

    public SqlNode Left { get; } = left;

    public SqlNode Right { get; } = right;

    public override void Render(SqlRenderContext context)
    {
        if (Operator == BinaryOperator.IntegerDivide)
        {
            context.Dialect.RenderIntegerDivide(context, Left, Right);
            return;
        }

        context.Write('(');
        Left.Render(context);
        context.Write(
            Operator switch
            {
                BinaryOperator.Add => " + ",
                BinaryOperator.Subtract => " - ",
                BinaryOperator.Multiply => " * ",
                BinaryOperator.Divide => " / ",
                _ => " % ",
            }
        );
        Right.Render(context);
        context.Write(')');
    }
}
