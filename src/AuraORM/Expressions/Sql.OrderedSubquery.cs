namespace AuraORM;

/// <content>
/// The subquery functions for a statement that has <c>OrderBy</c> but neither <c>Limit</c> nor <c>Offset</c>. They exist only
/// so that such a call fails with error CS0619 and a message that says how to fix it (<c>docs/design.ja.md</c> §5.11): without
/// them the call fails with CS1503 or CS0411, which do not.
/// </content>
public static partial class Sql
{
    private const string OrderedSubquery =
        "A subquery's ORDER BY has no effect without Limit or Offset, and SQL Server rejects it (error 1033); remove OrderBy, or add Limit.";

    /// <summary>Not accepted: a subquery with <c>OrderBy</c> needs <c>Limit</c> or <c>Offset</c>.</summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <typeparam name="TLevel">The evaluation level of the value.</typeparam>
    /// <typeparam name="TDialect">The dialect of the subquery.</typeparam>
    /// <typeparam name="TMode">Whether the subquery was built to run.</typeparam>
    /// <param name="value">The tested value.</param>
    /// <param name="subquery">The subquery.</param>
    /// <returns>Nothing; the call does not compile.</returns>
    [Obsolete(OrderedSubquery, error: true)]
    public static Condition<TLevel> In<T, TLevel, TDialect, TMode>(IExpr<T, TLevel> value, SelectOrdered<TDialect, TMode, T> subquery)
        where TLevel : EvaluationLevel
        where TDialect : IDialect
        where TMode : ExecutionMode => throw new InvalidOperationException(OrderedSubquery);

    /// <inheritdoc cref="In{T, TLevel, TDialect, TMode}(IExpr{T, TLevel}, SelectOrdered{TDialect, TMode, T})"/>
    [Obsolete(OrderedSubquery, error: true)]
    public static Condition<TLevel> In<T, TLevel, TDialect, TMode>(IExpr<T?, TLevel> value, SelectOrdered<TDialect, TMode, T> subquery)
        where T : struct
        where TLevel : EvaluationLevel
        where TDialect : IDialect
        where TMode : ExecutionMode => throw new InvalidOperationException(OrderedSubquery);

    /// <inheritdoc cref="In{T, TLevel, TDialect, TMode}(IExpr{T, TLevel}, SelectOrdered{TDialect, TMode, T})"/>
    [Obsolete(OrderedSubquery, error: true)]
    public static Condition<TLevel> In<T, TLevel, TDialect, TMode>(IExpr<T, TLevel> value, SelectOrdered<TDialect, TMode, T?> subquery)
        where T : struct
        where TLevel : EvaluationLevel
        where TDialect : IDialect
        where TMode : ExecutionMode => throw new InvalidOperationException(OrderedSubquery);

    /// <inheritdoc cref="In{T, TLevel, TDialect, TMode}(IExpr{T, TLevel}, SelectOrdered{TDialect, TMode, T})"/>
    [Obsolete(OrderedSubquery, error: true)]
    public static Condition<TLevel> NotIn<T, TLevel, TDialect, TMode>(IExpr<T, TLevel> value, SelectOrdered<TDialect, TMode, T> subquery)
        where TLevel : EvaluationLevel
        where TDialect : IDialect
        where TMode : ExecutionMode => throw new InvalidOperationException(OrderedSubquery);

    /// <inheritdoc cref="In{T, TLevel, TDialect, TMode}(IExpr{T, TLevel}, SelectOrdered{TDialect, TMode, T})"/>
    [Obsolete(OrderedSubquery, error: true)]
    public static Condition<TLevel> NotIn<T, TLevel, TDialect, TMode>(IExpr<T?, TLevel> value, SelectOrdered<TDialect, TMode, T> subquery)
        where T : struct
        where TLevel : EvaluationLevel
        where TDialect : IDialect
        where TMode : ExecutionMode => throw new InvalidOperationException(OrderedSubquery);

    /// <inheritdoc cref="In{T, TLevel, TDialect, TMode}(IExpr{T, TLevel}, SelectOrdered{TDialect, TMode, T})"/>
    [Obsolete(OrderedSubquery, error: true)]
    public static Condition<TLevel> NotIn<T, TLevel, TDialect, TMode>(IExpr<T, TLevel> value, SelectOrdered<TDialect, TMode, T?> subquery)
        where T : struct
        where TLevel : EvaluationLevel
        where TDialect : IDialect
        where TMode : ExecutionMode => throw new InvalidOperationException(OrderedSubquery);

    /// <summary>Not accepted: a subquery with <c>OrderBy</c> needs <c>Limit</c> or <c>Offset</c>.</summary>
    /// <typeparam name="TResult">The type of the subquery's rows.</typeparam>
    /// <typeparam name="TDialect">The dialect of the subquery.</typeparam>
    /// <typeparam name="TMode">Whether the subquery was built to run.</typeparam>
    /// <param name="subquery">The subquery.</param>
    /// <returns>Nothing; the call does not compile.</returns>
    [Obsolete(OrderedSubquery, error: true)]
    public static Condition<PerRow> Exists<TResult, TDialect, TMode>(SelectOrdered<TDialect, TMode, TResult> subquery)
        where TDialect : IDialect
        where TMode : ExecutionMode => throw new InvalidOperationException(OrderedSubquery);

    /// <inheritdoc cref="Exists{TResult, TDialect, TMode}(SelectOrdered{TDialect, TMode, TResult})"/>
    [Obsolete(OrderedSubquery, error: true)]
    public static Condition<PerGroup> GroupExists<TResult, TDialect, TMode>(SelectOrdered<TDialect, TMode, TResult> subquery)
        where TDialect : IDialect
        where TMode : ExecutionMode => throw new InvalidOperationException(OrderedSubquery);

    /// <summary>Not accepted: a subquery with <c>OrderBy</c> needs <c>Limit</c> or <c>Offset</c>.</summary>
    /// <typeparam name="T">The type of the selected expression.</typeparam>
    /// <typeparam name="TDialect">The dialect of the subquery.</typeparam>
    /// <typeparam name="TMode">Whether the subquery was built to run.</typeparam>
    /// <param name="subquery">The subquery.</param>
    /// <returns>Nothing; the call does not compile.</returns>
    [Obsolete(OrderedSubquery, error: true)]
    public static IExpr<T?, PerRow> Scalar<T, TDialect, TMode>(SelectOrdered<TDialect, TMode, T> subquery)
        where T : struct
        where TDialect : IDialect
        where TMode : ExecutionMode => throw new InvalidOperationException(OrderedSubquery);

    /// <inheritdoc cref="Scalar{T, TDialect, TMode}(SelectOrdered{TDialect, TMode, T})"/>
    [Obsolete(OrderedSubquery, error: true)]
    public static IExpr<T?, PerRow> Scalar<T, TDialect, TMode>(SelectOrdered<TDialect, TMode, T?> subquery)
        where T : struct
        where TDialect : IDialect
        where TMode : ExecutionMode => throw new InvalidOperationException(OrderedSubquery);

    /// <summary>Not accepted: a subquery with <c>OrderBy</c> needs <c>Limit</c> or <c>Offset</c>.</summary>
    /// <typeparam name="T">The type of the selected expression.</typeparam>
    /// <typeparam name="TDialect">The dialect of the subquery.</typeparam>
    /// <typeparam name="TMode">Whether the subquery was built to run.</typeparam>
    /// <param name="subquery">The subquery.</param>
    /// <param name="overload">Not used; do not pass it.</param>
    /// <returns>Nothing; the call does not compile.</returns>
    [Obsolete(OrderedSubquery, error: true)]
    public static IExpr<T?, PerRow> Scalar<T, TDialect, TMode>(SelectOrdered<TDialect, TMode, T> subquery, object? overload = null)
        where TDialect : IDialect
        where TMode : ExecutionMode => throw new InvalidOperationException(OrderedSubquery);

    /// <inheritdoc cref="Scalar{T, TDialect, TMode}(SelectOrdered{TDialect, TMode, T})"/>
    [Obsolete(OrderedSubquery, error: true)]
    public static IExpr<T?, PerGroup> GroupScalar<T, TDialect, TMode>(SelectOrdered<TDialect, TMode, T> subquery)
        where T : struct
        where TDialect : IDialect
        where TMode : ExecutionMode => throw new InvalidOperationException(OrderedSubquery);

    /// <inheritdoc cref="Scalar{T, TDialect, TMode}(SelectOrdered{TDialect, TMode, T})"/>
    [Obsolete(OrderedSubquery, error: true)]
    public static IExpr<T?, PerGroup> GroupScalar<T, TDialect, TMode>(SelectOrdered<TDialect, TMode, T?> subquery)
        where T : struct
        where TDialect : IDialect
        where TMode : ExecutionMode => throw new InvalidOperationException(OrderedSubquery);

    /// <inheritdoc cref="Scalar{T, TDialect, TMode}(SelectOrdered{TDialect, TMode, T}, object?)"/>
    [Obsolete(OrderedSubquery, error: true)]
    public static IExpr<T?, PerGroup> GroupScalar<T, TDialect, TMode>(SelectOrdered<TDialect, TMode, T> subquery, object? overload = null)
        where TDialect : IDialect
        where TMode : ExecutionMode => throw new InvalidOperationException(OrderedSubquery);
}
