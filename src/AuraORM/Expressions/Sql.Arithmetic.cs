using System.Numerics;

namespace AuraORM;

/// <content>
/// The arithmetic functions for numbers (<c>docs/design.ja.md</c> §4.2). An operand that can be null makes the result nullable.
/// </content>
public static partial class Sql
{
    /// <summary>
    /// Adds two numbers: <c>(a + b)</c>.
    /// </summary>
    /// <typeparam name="T">The numeric type of the operands.</typeparam>
    /// <typeparam name="TLevel">The evaluation level of both operands.</typeparam>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns>The expression.</returns>
    public static IExpr<T, TLevel> Add<T, TLevel>(IExpr<T, TLevel> left, IExpr<T, TLevel> right)
        where T : struct, INumber<T>
        where TLevel : EvaluationLevel => new SqlExpr<T, TLevel>(new BinaryNode(BinaryOperator.Add, left.Node, right.Node));

    /// <inheritdoc cref="Add{T, TLevel}(IExpr{T, TLevel}, IExpr{T, TLevel})"/>
    public static IExpr<T, TLevel> Add<T, TLevel>(IExpr<T, TLevel> left, T right)
        where T : struct, INumber<T>
        where TLevel : EvaluationLevel => new SqlExpr<T, TLevel>(new BinaryNode(BinaryOperator.Add, left.Node, Param(right)));

    /// <inheritdoc cref="Add{T, TLevel}(IExpr{T, TLevel}, IExpr{T, TLevel})"/>
    public static IExpr<T?, TLevel> Add<T, TLevel>(IExpr<T?, TLevel> left, IExpr<T?, TLevel> right)
        where T : struct, INumber<T>
        where TLevel : EvaluationLevel => new SqlExpr<T?, TLevel>(new BinaryNode(BinaryOperator.Add, left.Node, right.Node));

    /// <inheritdoc cref="Add{T, TLevel}(IExpr{T, TLevel}, IExpr{T, TLevel})"/>
    public static IExpr<T?, TLevel> Add<T, TLevel>(IExpr<T?, TLevel> left, IExpr<T, TLevel> right)
        where T : struct, INumber<T>
        where TLevel : EvaluationLevel => new SqlExpr<T?, TLevel>(new BinaryNode(BinaryOperator.Add, left.Node, right.Node));

    /// <inheritdoc cref="Add{T, TLevel}(IExpr{T, TLevel}, IExpr{T, TLevel})"/>
    public static IExpr<T?, TLevel> Add<T, TLevel>(IExpr<T, TLevel> left, IExpr<T?, TLevel> right)
        where T : struct, INumber<T>
        where TLevel : EvaluationLevel => new SqlExpr<T?, TLevel>(new BinaryNode(BinaryOperator.Add, left.Node, right.Node));

    /// <inheritdoc cref="Add{T, TLevel}(IExpr{T, TLevel}, IExpr{T, TLevel})"/>
    public static IExpr<T?, TLevel> Add<T, TLevel>(IExpr<T?, TLevel> left, T right)
        where T : struct, INumber<T>
        where TLevel : EvaluationLevel => new SqlExpr<T?, TLevel>(new BinaryNode(BinaryOperator.Add, left.Node, Param(right)));

    /// <summary>
    /// Subtracts the right number from the left: <c>(a - b)</c>.
    /// </summary>
    /// <typeparam name="T">The numeric type of the operands.</typeparam>
    /// <typeparam name="TLevel">The evaluation level of both operands.</typeparam>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns>The expression.</returns>
    public static IExpr<T, TLevel> Subtract<T, TLevel>(IExpr<T, TLevel> left, IExpr<T, TLevel> right)
        where T : struct, INumber<T>
        where TLevel : EvaluationLevel => new SqlExpr<T, TLevel>(new BinaryNode(BinaryOperator.Subtract, left.Node, right.Node));

    /// <inheritdoc cref="Subtract{T, TLevel}(IExpr{T, TLevel}, IExpr{T, TLevel})"/>
    public static IExpr<T, TLevel> Subtract<T, TLevel>(IExpr<T, TLevel> left, T right)
        where T : struct, INumber<T>
        where TLevel : EvaluationLevel => new SqlExpr<T, TLevel>(new BinaryNode(BinaryOperator.Subtract, left.Node, Param(right)));

    /// <inheritdoc cref="Subtract{T, TLevel}(IExpr{T, TLevel}, IExpr{T, TLevel})"/>
    public static IExpr<T?, TLevel> Subtract<T, TLevel>(IExpr<T?, TLevel> left, IExpr<T?, TLevel> right)
        where T : struct, INumber<T>
        where TLevel : EvaluationLevel => new SqlExpr<T?, TLevel>(new BinaryNode(BinaryOperator.Subtract, left.Node, right.Node));

    /// <inheritdoc cref="Subtract{T, TLevel}(IExpr{T, TLevel}, IExpr{T, TLevel})"/>
    public static IExpr<T?, TLevel> Subtract<T, TLevel>(IExpr<T?, TLevel> left, IExpr<T, TLevel> right)
        where T : struct, INumber<T>
        where TLevel : EvaluationLevel => new SqlExpr<T?, TLevel>(new BinaryNode(BinaryOperator.Subtract, left.Node, right.Node));

    /// <inheritdoc cref="Subtract{T, TLevel}(IExpr{T, TLevel}, IExpr{T, TLevel})"/>
    public static IExpr<T?, TLevel> Subtract<T, TLevel>(IExpr<T, TLevel> left, IExpr<T?, TLevel> right)
        where T : struct, INumber<T>
        where TLevel : EvaluationLevel => new SqlExpr<T?, TLevel>(new BinaryNode(BinaryOperator.Subtract, left.Node, right.Node));

    /// <inheritdoc cref="Subtract{T, TLevel}(IExpr{T, TLevel}, IExpr{T, TLevel})"/>
    public static IExpr<T?, TLevel> Subtract<T, TLevel>(IExpr<T?, TLevel> left, T right)
        where T : struct, INumber<T>
        where TLevel : EvaluationLevel => new SqlExpr<T?, TLevel>(new BinaryNode(BinaryOperator.Subtract, left.Node, Param(right)));

    /// <summary>
    /// Multiplies two numbers: <c>(a * b)</c>.
    /// </summary>
    /// <typeparam name="T">The numeric type of the operands.</typeparam>
    /// <typeparam name="TLevel">The evaluation level of both operands.</typeparam>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns>The expression.</returns>
    public static IExpr<T, TLevel> Multiply<T, TLevel>(IExpr<T, TLevel> left, IExpr<T, TLevel> right)
        where T : struct, INumber<T>
        where TLevel : EvaluationLevel => new SqlExpr<T, TLevel>(new BinaryNode(BinaryOperator.Multiply, left.Node, right.Node));

    /// <inheritdoc cref="Multiply{T, TLevel}(IExpr{T, TLevel}, IExpr{T, TLevel})"/>
    public static IExpr<T, TLevel> Multiply<T, TLevel>(IExpr<T, TLevel> left, T right)
        where T : struct, INumber<T>
        where TLevel : EvaluationLevel => new SqlExpr<T, TLevel>(new BinaryNode(BinaryOperator.Multiply, left.Node, Param(right)));

    /// <inheritdoc cref="Multiply{T, TLevel}(IExpr{T, TLevel}, IExpr{T, TLevel})"/>
    public static IExpr<T?, TLevel> Multiply<T, TLevel>(IExpr<T?, TLevel> left, IExpr<T?, TLevel> right)
        where T : struct, INumber<T>
        where TLevel : EvaluationLevel => new SqlExpr<T?, TLevel>(new BinaryNode(BinaryOperator.Multiply, left.Node, right.Node));

    /// <inheritdoc cref="Multiply{T, TLevel}(IExpr{T, TLevel}, IExpr{T, TLevel})"/>
    public static IExpr<T?, TLevel> Multiply<T, TLevel>(IExpr<T?, TLevel> left, IExpr<T, TLevel> right)
        where T : struct, INumber<T>
        where TLevel : EvaluationLevel => new SqlExpr<T?, TLevel>(new BinaryNode(BinaryOperator.Multiply, left.Node, right.Node));

    /// <inheritdoc cref="Multiply{T, TLevel}(IExpr{T, TLevel}, IExpr{T, TLevel})"/>
    public static IExpr<T?, TLevel> Multiply<T, TLevel>(IExpr<T, TLevel> left, IExpr<T?, TLevel> right)
        where T : struct, INumber<T>
        where TLevel : EvaluationLevel => new SqlExpr<T?, TLevel>(new BinaryNode(BinaryOperator.Multiply, left.Node, right.Node));

    /// <inheritdoc cref="Multiply{T, TLevel}(IExpr{T, TLevel}, IExpr{T, TLevel})"/>
    public static IExpr<T?, TLevel> Multiply<T, TLevel>(IExpr<T?, TLevel> left, T right)
        where T : struct, INumber<T>
        where TLevel : EvaluationLevel => new SqlExpr<T?, TLevel>(new BinaryNode(BinaryOperator.Multiply, left.Node, Param(right)));

    /// <summary>
    /// Divides the left number by the right: <c>(a / b)</c>. For integers the result is an integer, and MySQL and MariaDB write <c>(a DIV b)</c>.
    /// </summary>
    /// <typeparam name="T">The numeric type of the operands.</typeparam>
    /// <typeparam name="TLevel">The evaluation level of both operands.</typeparam>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns>The expression.</returns>
    public static IExpr<T, TLevel> Divide<T, TLevel>(IExpr<T, TLevel> left, IExpr<T, TLevel> right)
        where T : struct, INumber<T>
        where TLevel : EvaluationLevel => new SqlExpr<T, TLevel>(new BinaryNode(Divide<T>(), left.Node, right.Node));

    /// <inheritdoc cref="Divide{T, TLevel}(IExpr{T, TLevel}, IExpr{T, TLevel})"/>
    public static IExpr<T, TLevel> Divide<T, TLevel>(IExpr<T, TLevel> left, T right)
        where T : struct, INumber<T>
        where TLevel : EvaluationLevel => new SqlExpr<T, TLevel>(new BinaryNode(Divide<T>(), left.Node, Param(right)));

    /// <inheritdoc cref="Divide{T, TLevel}(IExpr{T, TLevel}, IExpr{T, TLevel})"/>
    public static IExpr<T?, TLevel> Divide<T, TLevel>(IExpr<T?, TLevel> left, IExpr<T?, TLevel> right)
        where T : struct, INumber<T>
        where TLevel : EvaluationLevel => new SqlExpr<T?, TLevel>(new BinaryNode(Divide<T>(), left.Node, right.Node));

    /// <inheritdoc cref="Divide{T, TLevel}(IExpr{T, TLevel}, IExpr{T, TLevel})"/>
    public static IExpr<T?, TLevel> Divide<T, TLevel>(IExpr<T?, TLevel> left, IExpr<T, TLevel> right)
        where T : struct, INumber<T>
        where TLevel : EvaluationLevel => new SqlExpr<T?, TLevel>(new BinaryNode(Divide<T>(), left.Node, right.Node));

    /// <inheritdoc cref="Divide{T, TLevel}(IExpr{T, TLevel}, IExpr{T, TLevel})"/>
    public static IExpr<T?, TLevel> Divide<T, TLevel>(IExpr<T, TLevel> left, IExpr<T?, TLevel> right)
        where T : struct, INumber<T>
        where TLevel : EvaluationLevel => new SqlExpr<T?, TLevel>(new BinaryNode(Divide<T>(), left.Node, right.Node));

    /// <inheritdoc cref="Divide{T, TLevel}(IExpr{T, TLevel}, IExpr{T, TLevel})"/>
    public static IExpr<T?, TLevel> Divide<T, TLevel>(IExpr<T?, TLevel> left, T right)
        where T : struct, INumber<T>
        where TLevel : EvaluationLevel => new SqlExpr<T?, TLevel>(new BinaryNode(Divide<T>(), left.Node, Param(right)));

    /// <summary>
    /// Returns the remainder of the division: <c>(a % b)</c>.
    /// </summary>
    /// <typeparam name="T">The numeric type of the operands.</typeparam>
    /// <typeparam name="TLevel">The evaluation level of both operands.</typeparam>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns>The expression.</returns>
    public static IExpr<T, TLevel> Modulo<T, TLevel>(IExpr<T, TLevel> left, IExpr<T, TLevel> right)
        where T : struct, INumber<T>
        where TLevel : EvaluationLevel => new SqlExpr<T, TLevel>(new BinaryNode(BinaryOperator.Modulo, left.Node, right.Node));

    /// <inheritdoc cref="Modulo{T, TLevel}(IExpr{T, TLevel}, IExpr{T, TLevel})"/>
    public static IExpr<T, TLevel> Modulo<T, TLevel>(IExpr<T, TLevel> left, T right)
        where T : struct, INumber<T>
        where TLevel : EvaluationLevel => new SqlExpr<T, TLevel>(new BinaryNode(BinaryOperator.Modulo, left.Node, Param(right)));

    /// <inheritdoc cref="Modulo{T, TLevel}(IExpr{T, TLevel}, IExpr{T, TLevel})"/>
    public static IExpr<T?, TLevel> Modulo<T, TLevel>(IExpr<T?, TLevel> left, IExpr<T?, TLevel> right)
        where T : struct, INumber<T>
        where TLevel : EvaluationLevel => new SqlExpr<T?, TLevel>(new BinaryNode(BinaryOperator.Modulo, left.Node, right.Node));

    /// <inheritdoc cref="Modulo{T, TLevel}(IExpr{T, TLevel}, IExpr{T, TLevel})"/>
    public static IExpr<T?, TLevel> Modulo<T, TLevel>(IExpr<T?, TLevel> left, IExpr<T, TLevel> right)
        where T : struct, INumber<T>
        where TLevel : EvaluationLevel => new SqlExpr<T?, TLevel>(new BinaryNode(BinaryOperator.Modulo, left.Node, right.Node));

    /// <inheritdoc cref="Modulo{T, TLevel}(IExpr{T, TLevel}, IExpr{T, TLevel})"/>
    public static IExpr<T?, TLevel> Modulo<T, TLevel>(IExpr<T, TLevel> left, IExpr<T?, TLevel> right)
        where T : struct, INumber<T>
        where TLevel : EvaluationLevel => new SqlExpr<T?, TLevel>(new BinaryNode(BinaryOperator.Modulo, left.Node, right.Node));

    /// <inheritdoc cref="Modulo{T, TLevel}(IExpr{T, TLevel}, IExpr{T, TLevel})"/>
    public static IExpr<T?, TLevel> Modulo<T, TLevel>(IExpr<T?, TLevel> left, T right)
        where T : struct, INumber<T>
        where TLevel : EvaluationLevel => new SqlExpr<T?, TLevel>(new BinaryNode(BinaryOperator.Modulo, left.Node, Param(right)));

    /// <summary>
    /// Negates a number: <c>(-a)</c>.
    /// </summary>
    /// <typeparam name="T">The numeric type of the operand.</typeparam>
    /// <typeparam name="TLevel">The evaluation level of the operand.</typeparam>
    /// <param name="value">The operand.</param>
    /// <returns>The expression.</returns>
    public static IExpr<T, TLevel> Negate<T, TLevel>(IExpr<T, TLevel> value)
        where T : struct, INumber<T>
        where TLevel : EvaluationLevel => new SqlExpr<T, TLevel>(new NegateNode(value.Node));

    /// <inheritdoc cref="Negate{T, TLevel}(IExpr{T, TLevel})"/>
    public static IExpr<T?, TLevel> Negate<T, TLevel>(IExpr<T?, TLevel> value)
        where T : struct, INumber<T>
        where TLevel : EvaluationLevel => new SqlExpr<T?, TLevel>(new NegateNode(value.Node));

    // The JIT and NativeAOT fold these type checks for each T.
    private static BinaryOperator Divide<T>() => IsInteger<T>() ? BinaryOperator.IntegerDivide : BinaryOperator.Divide;

    private static bool IsInteger<T>() =>
        typeof(T) == typeof(int)
        || typeof(T) == typeof(long)
        || typeof(T) == typeof(short)
        || typeof(T) == typeof(byte)
        || typeof(T) == typeof(sbyte)
        || typeof(T) == typeof(ushort)
        || typeof(T) == typeof(uint)
        || typeof(T) == typeof(ulong)
        || typeof(T) == typeof(Int128)
        || typeof(T) == typeof(UInt128)
        || typeof(T) == typeof(nint)
        || typeof(T) == typeof(nuint)
        || typeof(T) == typeof(BigInteger);
}
