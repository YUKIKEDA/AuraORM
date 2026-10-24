namespace AuraORM;

/// <content>
/// Subqueries (<c>docs/design.ja.md</c> §5.11). A subquery is a statement with a projection: after <c>Select</c> or
/// <c>SelectDistinct</c>, after a grouped projection, or after <c>Limit</c> or <c>Offset</c>. A statement that has
/// <c>OrderBy</c> but neither <c>Limit</c> nor <c>Offset</c> is not accepted: its order means nothing to the outer statement,
/// and SQL Server rejects it (error 1033). The subquery must be built with the dialect of the outer statement.
/// </content>
public static partial class Sql
{
    /// <summary>
    /// Tests whether a value is one of the values that a subquery returns: <c>a IN (SELECT …)</c>. The subquery
    /// selects one expression of the value's type. On MySQL and MariaDB, a subquery with a row limit is not accepted
    /// (error 1235).
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <typeparam name="TLevel">The evaluation level of the value, and of the condition.</typeparam>
    /// <typeparam name="TDialect">The dialect of the subquery, which must be the outer statement's.</typeparam>
    /// <typeparam name="TMode">Whether the subquery was built to run; it does not matter here.</typeparam>
    /// <typeparam name="TSubLevel">The evaluation level of the subquery's projection.</typeparam>
    /// <param name="value">The tested value.</param>
    /// <param name="subquery">The subquery.</param>
    /// <returns>The condition.</returns>
    public static Condition<TLevel> In<T, TLevel, TDialect, TMode, TSubLevel>(
        IExpr<T, TLevel> value,
        SelectProjection<TDialect, TMode, T, TSubLevel> subquery
    )
        where TLevel : EvaluationLevel
        where TDialect : IDialect
        where TMode : ExecutionMode
        where TSubLevel : EvaluationLevel => new(new InSubqueryNode(value.Node, Subquery(subquery), negated: false));

    /// <inheritdoc cref="In{T, TLevel, TDialect, TMode, TSubLevel}(IExpr{T, TLevel}, SelectProjection{TDialect, TMode, T, TSubLevel})"/>
    public static Condition<TLevel> In<T, TLevel, TDialect, TMode, TSubLevel>(
        IExpr<T?, TLevel> value,
        SelectProjection<TDialect, TMode, T, TSubLevel> subquery
    )
        where T : struct
        where TLevel : EvaluationLevel
        where TDialect : IDialect
        where TMode : ExecutionMode
        where TSubLevel : EvaluationLevel => new(new InSubqueryNode(value.Node, Subquery(subquery), negated: false));

    /// <inheritdoc cref="In{T, TLevel, TDialect, TMode, TSubLevel}(IExpr{T, TLevel}, SelectProjection{TDialect, TMode, T, TSubLevel})"/>
    public static Condition<TLevel> In<T, TLevel, TDialect, TMode>(IExpr<T, TLevel> value, SelectLimited<TDialect, TMode, T> subquery)
        where TLevel : EvaluationLevel
        where TDialect : ILimitInSubqueryDialect
        where TMode : ExecutionMode => new(new InSubqueryNode(value.Node, Subquery(subquery), negated: false));

    /// <inheritdoc cref="In{T, TLevel, TDialect, TMode, TSubLevel}(IExpr{T, TLevel}, SelectProjection{TDialect, TMode, T, TSubLevel})"/>
    public static Condition<TLevel> In<T, TLevel, TDialect, TMode>(IExpr<T?, TLevel> value, SelectLimited<TDialect, TMode, T> subquery)
        where T : struct
        where TLevel : EvaluationLevel
        where TDialect : ILimitInSubqueryDialect
        where TMode : ExecutionMode => new(new InSubqueryNode(value.Node, Subquery(subquery), negated: false));

    /// <inheritdoc cref="In{T, TLevel, TDialect, TMode, TSubLevel}(IExpr{T, TLevel}, SelectProjection{TDialect, TMode, T, TSubLevel})"/>
    public static Condition<TLevel> In<T, TLevel, TDialect, TMode>(IExpr<T, TLevel> value, SelectOffset<TDialect, TMode, T> subquery)
        where TLevel : EvaluationLevel
        where TDialect : ILimitInSubqueryDialect
        where TMode : ExecutionMode => new(new InSubqueryNode(value.Node, Subquery(subquery), negated: false));

    /// <inheritdoc cref="In{T, TLevel, TDialect, TMode, TSubLevel}(IExpr{T, TLevel}, SelectProjection{TDialect, TMode, T, TSubLevel})"/>
    public static Condition<TLevel> In<T, TLevel, TDialect, TMode>(IExpr<T?, TLevel> value, SelectOffset<TDialect, TMode, T> subquery)
        where T : struct
        where TLevel : EvaluationLevel
        where TDialect : ILimitInSubqueryDialect
        where TMode : ExecutionMode => new(new InSubqueryNode(value.Node, Subquery(subquery), negated: false));

    /// <summary>
    /// Tests whether a value is none of the values that a subquery returns: <c>a NOT IN (SELECT …)</c>. The subquery
    /// selects one expression of the value's type. A subquery that can return NULL matches no row (<c>docs/design.ja.md</c> §4.4, AURA0203). On MySQL and MariaDB, a subquery with a row limit is not accepted
    /// (error 1235).
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <typeparam name="TLevel">The evaluation level of the value, and of the condition.</typeparam>
    /// <typeparam name="TDialect">The dialect of the subquery, which must be the outer statement's.</typeparam>
    /// <typeparam name="TMode">Whether the subquery was built to run; it does not matter here.</typeparam>
    /// <typeparam name="TSubLevel">The evaluation level of the subquery's projection.</typeparam>
    /// <param name="value">The tested value.</param>
    /// <param name="subquery">The subquery.</param>
    /// <returns>The condition.</returns>
    public static Condition<TLevel> NotIn<T, TLevel, TDialect, TMode, TSubLevel>(
        IExpr<T, TLevel> value,
        SelectProjection<TDialect, TMode, T, TSubLevel> subquery
    )
        where TLevel : EvaluationLevel
        where TDialect : IDialect
        where TMode : ExecutionMode
        where TSubLevel : EvaluationLevel => new(new InSubqueryNode(value.Node, Subquery(subquery), negated: true));

    /// <inheritdoc cref="NotIn{T, TLevel, TDialect, TMode, TSubLevel}(IExpr{T, TLevel}, SelectProjection{TDialect, TMode, T, TSubLevel})"/>
    public static Condition<TLevel> NotIn<T, TLevel, TDialect, TMode, TSubLevel>(
        IExpr<T?, TLevel> value,
        SelectProjection<TDialect, TMode, T, TSubLevel> subquery
    )
        where T : struct
        where TLevel : EvaluationLevel
        where TDialect : IDialect
        where TMode : ExecutionMode
        where TSubLevel : EvaluationLevel => new(new InSubqueryNode(value.Node, Subquery(subquery), negated: true));

    /// <inheritdoc cref="NotIn{T, TLevel, TDialect, TMode, TSubLevel}(IExpr{T, TLevel}, SelectProjection{TDialect, TMode, T, TSubLevel})"/>
    public static Condition<TLevel> NotIn<T, TLevel, TDialect, TMode>(IExpr<T, TLevel> value, SelectLimited<TDialect, TMode, T> subquery)
        where TLevel : EvaluationLevel
        where TDialect : ILimitInSubqueryDialect
        where TMode : ExecutionMode => new(new InSubqueryNode(value.Node, Subquery(subquery), negated: true));

    /// <inheritdoc cref="NotIn{T, TLevel, TDialect, TMode, TSubLevel}(IExpr{T, TLevel}, SelectProjection{TDialect, TMode, T, TSubLevel})"/>
    public static Condition<TLevel> NotIn<T, TLevel, TDialect, TMode>(IExpr<T?, TLevel> value, SelectLimited<TDialect, TMode, T> subquery)
        where T : struct
        where TLevel : EvaluationLevel
        where TDialect : ILimitInSubqueryDialect
        where TMode : ExecutionMode => new(new InSubqueryNode(value.Node, Subquery(subquery), negated: true));

    /// <inheritdoc cref="NotIn{T, TLevel, TDialect, TMode, TSubLevel}(IExpr{T, TLevel}, SelectProjection{TDialect, TMode, T, TSubLevel})"/>
    public static Condition<TLevel> NotIn<T, TLevel, TDialect, TMode>(IExpr<T, TLevel> value, SelectOffset<TDialect, TMode, T> subquery)
        where TLevel : EvaluationLevel
        where TDialect : ILimitInSubqueryDialect
        where TMode : ExecutionMode => new(new InSubqueryNode(value.Node, Subquery(subquery), negated: true));

    /// <inheritdoc cref="NotIn{T, TLevel, TDialect, TMode, TSubLevel}(IExpr{T, TLevel}, SelectProjection{TDialect, TMode, T, TSubLevel})"/>
    public static Condition<TLevel> NotIn<T, TLevel, TDialect, TMode>(IExpr<T?, TLevel> value, SelectOffset<TDialect, TMode, T> subquery)
        where T : struct
        where TLevel : EvaluationLevel
        where TDialect : ILimitInSubqueryDialect
        where TMode : ExecutionMode => new(new InSubqueryNode(value.Node, Subquery(subquery), negated: true));

    /// <summary>
    /// Tests whether a subquery returns a row: <c>EXISTS (SELECT …)</c>. The condition is <see cref="PerRow"/>.
    /// </summary>
    /// <typeparam name="TResult">The type of the subquery's rows.</typeparam>
    /// <typeparam name="TDialect">The dialect of the subquery, which must be the outer statement's.</typeparam>
    /// <typeparam name="TMode">Whether the subquery was built to run; it does not matter here.</typeparam>
    /// <typeparam name="TSubLevel">The evaluation level of the subquery's projection.</typeparam>
    /// <param name="subquery">The subquery.</param>
    /// <returns>The condition.</returns>
    public static Condition<PerRow> Exists<TResult, TDialect, TMode, TSubLevel>(SelectProjection<TDialect, TMode, TResult, TSubLevel> subquery)
        where TDialect : IDialect
        where TMode : ExecutionMode
        where TSubLevel : EvaluationLevel => new(new ExistsNode(Subquery(subquery)));

    /// <inheritdoc cref="Exists{TResult, TDialect, TMode, TSubLevel}(SelectProjection{TDialect, TMode, TResult, TSubLevel})"/>
    public static Condition<PerRow> Exists<TResult, TDialect, TMode>(SelectLimited<TDialect, TMode, TResult> subquery)
        where TDialect : IDialect
        where TMode : ExecutionMode => new(new ExistsNode(Subquery(subquery)));

    /// <inheritdoc cref="Exists{TResult, TDialect, TMode, TSubLevel}(SelectProjection{TDialect, TMode, TResult, TSubLevel})"/>
    public static Condition<PerRow> Exists<TResult, TDialect, TMode>(SelectOffset<TDialect, TMode, TResult> subquery)
        where TDialect : IDialect
        where TMode : ExecutionMode => new(new ExistsNode(Subquery(subquery)));

    /// <summary>
    /// Tests whether a subquery returns a row, as a condition on groups: <c>EXISTS (SELECT …)</c> in <c>HAVING</c>
    /// or after <c>GroupBy</c>. Inside the subquery, the grouped statement's columns can be used only through its
    /// <c>GROUP BY</c> keys (<c>k</c>); another of its columns throws when the statement is built.
    /// </summary>
    /// <typeparam name="TResult">The type of the subquery's rows.</typeparam>
    /// <typeparam name="TDialect">The dialect of the subquery, which must be the outer statement's.</typeparam>
    /// <typeparam name="TMode">Whether the subquery was built to run; it does not matter here.</typeparam>
    /// <typeparam name="TSubLevel">The evaluation level of the subquery's projection.</typeparam>
    /// <param name="subquery">The subquery.</param>
    /// <returns>The condition.</returns>
    public static Condition<PerGroup> GroupExists<TResult, TDialect, TMode, TSubLevel>(SelectProjection<TDialect, TMode, TResult, TSubLevel> subquery)
        where TDialect : IDialect
        where TMode : ExecutionMode
        where TSubLevel : EvaluationLevel => new(new GroupSubqueryNode(new ExistsNode(Subquery(subquery))));

    /// <inheritdoc cref="GroupExists{TResult, TDialect, TMode, TSubLevel}(SelectProjection{TDialect, TMode, TResult, TSubLevel})"/>
    public static Condition<PerGroup> GroupExists<TResult, TDialect, TMode>(SelectLimited<TDialect, TMode, TResult> subquery)
        where TDialect : IDialect
        where TMode : ExecutionMode => new(new GroupSubqueryNode(new ExistsNode(Subquery(subquery))));

    /// <inheritdoc cref="GroupExists{TResult, TDialect, TMode, TSubLevel}(SelectProjection{TDialect, TMode, TResult, TSubLevel})"/>
    public static Condition<PerGroup> GroupExists<TResult, TDialect, TMode>(SelectOffset<TDialect, TMode, TResult> subquery)
        where TDialect : IDialect
        where TMode : ExecutionMode => new(new GroupSubqueryNode(new ExistsNode(Subquery(subquery))));

    /// <summary>
    /// Uses the one value that a subquery returns: <c>(SELECT …)</c>. The subquery selects one expression; the
    /// value is null when it returns no row. A subquery that returns more than one row is an error on every dialect but
    /// SQLite, which returns the first row (<c>docs/design.ja.md</c> §14.2).
    /// </summary>
    /// <typeparam name="T">The type of the selected expression, without its nullability.</typeparam>
    /// <typeparam name="TDialect">The dialect of the subquery, which must be the outer statement's.</typeparam>
    /// <typeparam name="TMode">Whether the subquery was built to run; it does not matter here.</typeparam>
    /// <typeparam name="TSubLevel">The evaluation level of the subquery's projection.</typeparam>
    /// <param name="subquery">The subquery.</param>
    /// <returns>The value.</returns>
    /// <exception cref="ArgumentException">The subquery selects a whole row, a result type, or a tuple.</exception>
    public static IExpr<T?, PerRow> Scalar<T, TDialect, TMode, TSubLevel>(SelectProjection<TDialect, TMode, T, TSubLevel> subquery)
        where T : struct
        where TDialect : IDialect
        where TMode : ExecutionMode
        where TSubLevel : EvaluationLevel => new SqlExpr<T?, PerRow>(ScalarSubquery(subquery));

    /// <inheritdoc cref="Scalar{T, TDialect, TMode, TSubLevel}(SelectProjection{TDialect, TMode, T, TSubLevel})"/>
    public static IExpr<T?, PerRow> Scalar<T, TDialect, TMode, TSubLevel>(SelectProjection<TDialect, TMode, T?, TSubLevel> subquery)
        where T : struct
        where TDialect : IDialect
        where TMode : ExecutionMode
        where TSubLevel : EvaluationLevel => new SqlExpr<T?, PerRow>(ScalarSubquery(subquery));

    /// <summary>
    /// <inheritdoc cref="Scalar{T, TDialect, TMode, TSubLevel}(SelectProjection{TDialect, TMode, T, TSubLevel})" path="/summary"/> This overload is for reference types.
    /// </summary>
    /// <typeparam name="T">The type of the selected expression.</typeparam>
    /// <typeparam name="TDialect">The dialect of the subquery, which must be the outer statement's.</typeparam>
    /// <typeparam name="TMode">Whether the subquery was built to run; it does not matter here.</typeparam>
    /// <typeparam name="TSubLevel">The evaluation level of the subquery's projection.</typeparam>
    /// <param name="subquery">The subquery.</param>
    /// <param name="overload">Not used. It lets this overload, for reference types, exist beside the overloads for value types.</param>
    /// <returns>The value.</returns>
    /// <exception cref="ArgumentException">The subquery selects a whole row, a result type, or a tuple.</exception>
    public static IExpr<T?, PerRow> Scalar<T, TDialect, TMode, TSubLevel>(
        SelectProjection<TDialect, TMode, T, TSubLevel> subquery,
        object? overload = null
    )
        where TDialect : IDialect
        where TMode : ExecutionMode
        where TSubLevel : EvaluationLevel => new SqlExpr<T?, PerRow>(ScalarSubquery(subquery));

    /// <inheritdoc cref="Scalar{T, TDialect, TMode, TSubLevel}(SelectProjection{TDialect, TMode, T, TSubLevel})"/>
    public static IExpr<T?, PerRow> Scalar<T, TDialect, TMode>(SelectLimited<TDialect, TMode, T> subquery)
        where T : struct
        where TDialect : IDialect
        where TMode : ExecutionMode => new SqlExpr<T?, PerRow>(ScalarSubquery(subquery));

    /// <inheritdoc cref="Scalar{T, TDialect, TMode, TSubLevel}(SelectProjection{TDialect, TMode, T, TSubLevel})"/>
    public static IExpr<T?, PerRow> Scalar<T, TDialect, TMode>(SelectLimited<TDialect, TMode, T?> subquery)
        where T : struct
        where TDialect : IDialect
        where TMode : ExecutionMode => new SqlExpr<T?, PerRow>(ScalarSubquery(subquery));

    /// <summary>
    /// <inheritdoc cref="Scalar{T, TDialect, TMode, TSubLevel}(SelectProjection{TDialect, TMode, T, TSubLevel})" path="/summary"/> This overload is for reference types.
    /// </summary>
    /// <typeparam name="T">The type of the selected expression.</typeparam>
    /// <typeparam name="TDialect">The dialect of the subquery, which must be the outer statement's.</typeparam>
    /// <typeparam name="TMode">Whether the subquery was built to run; it does not matter here.</typeparam>
    /// <param name="subquery">The subquery.</param>
    /// <param name="overload">Not used. It lets this overload, for reference types, exist beside the overloads for value types.</param>
    /// <returns>The value.</returns>
    /// <exception cref="ArgumentException">The subquery selects a whole row, a result type, or a tuple.</exception>
    public static IExpr<T?, PerRow> Scalar<T, TDialect, TMode>(SelectLimited<TDialect, TMode, T> subquery, object? overload = null)
        where TDialect : IDialect
        where TMode : ExecutionMode => new SqlExpr<T?, PerRow>(ScalarSubquery(subquery));

    /// <inheritdoc cref="Scalar{T, TDialect, TMode, TSubLevel}(SelectProjection{TDialect, TMode, T, TSubLevel})"/>
    public static IExpr<T?, PerRow> Scalar<T, TDialect, TMode>(SelectOffset<TDialect, TMode, T> subquery)
        where T : struct
        where TDialect : IDialect
        where TMode : ExecutionMode => new SqlExpr<T?, PerRow>(ScalarSubquery(subquery));

    /// <inheritdoc cref="Scalar{T, TDialect, TMode, TSubLevel}(SelectProjection{TDialect, TMode, T, TSubLevel})"/>
    public static IExpr<T?, PerRow> Scalar<T, TDialect, TMode>(SelectOffset<TDialect, TMode, T?> subquery)
        where T : struct
        where TDialect : IDialect
        where TMode : ExecutionMode => new SqlExpr<T?, PerRow>(ScalarSubquery(subquery));

    /// <summary>
    /// <inheritdoc cref="Scalar{T, TDialect, TMode, TSubLevel}(SelectProjection{TDialect, TMode, T, TSubLevel})" path="/summary"/> This overload is for reference types.
    /// </summary>
    /// <typeparam name="T">The type of the selected expression.</typeparam>
    /// <typeparam name="TDialect">The dialect of the subquery, which must be the outer statement's.</typeparam>
    /// <typeparam name="TMode">Whether the subquery was built to run; it does not matter here.</typeparam>
    /// <param name="subquery">The subquery.</param>
    /// <param name="overload">Not used. It lets this overload, for reference types, exist beside the overloads for value types.</param>
    /// <returns>The value.</returns>
    /// <exception cref="ArgumentException">The subquery selects a whole row, a result type, or a tuple.</exception>
    public static IExpr<T?, PerRow> Scalar<T, TDialect, TMode>(SelectOffset<TDialect, TMode, T> subquery, object? overload = null)
        where TDialect : IDialect
        where TMode : ExecutionMode => new SqlExpr<T?, PerRow>(ScalarSubquery(subquery));

    /// <summary>
    /// Uses the one value that a subquery returns, as a value of groups: <c>(SELECT …)</c> in <c>HAVING</c> or after
    /// <c>GroupBy</c>. Inside the subquery, the grouped statement's columns can be used only through its <c>GROUP BY</c>
    /// keys (<c>k</c>); another of its columns throws when the statement is built. The value is null when the subquery
    /// returns no row.
    /// </summary>
    /// <typeparam name="T">The type of the selected expression, without its nullability.</typeparam>
    /// <typeparam name="TDialect">The dialect of the subquery, which must be the outer statement's.</typeparam>
    /// <typeparam name="TMode">Whether the subquery was built to run; it does not matter here.</typeparam>
    /// <typeparam name="TSubLevel">The evaluation level of the subquery's projection.</typeparam>
    /// <param name="subquery">The subquery.</param>
    /// <returns>The value.</returns>
    /// <exception cref="ArgumentException">The subquery selects a whole row, a result type, or a tuple.</exception>
    public static IExpr<T?, PerGroup> GroupScalar<T, TDialect, TMode, TSubLevel>(SelectProjection<TDialect, TMode, T, TSubLevel> subquery)
        where T : struct
        where TDialect : IDialect
        where TMode : ExecutionMode
        where TSubLevel : EvaluationLevel => new SqlExpr<T?, PerGroup>(new GroupSubqueryNode(ScalarSubquery(subquery)));

    /// <inheritdoc cref="GroupScalar{T, TDialect, TMode, TSubLevel}(SelectProjection{TDialect, TMode, T, TSubLevel})"/>
    public static IExpr<T?, PerGroup> GroupScalar<T, TDialect, TMode, TSubLevel>(SelectProjection<TDialect, TMode, T?, TSubLevel> subquery)
        where T : struct
        where TDialect : IDialect
        where TMode : ExecutionMode
        where TSubLevel : EvaluationLevel => new SqlExpr<T?, PerGroup>(new GroupSubqueryNode(ScalarSubquery(subquery)));

    /// <summary>
    /// <inheritdoc cref="GroupScalar{T, TDialect, TMode, TSubLevel}(SelectProjection{TDialect, TMode, T, TSubLevel})" path="/summary"/> This overload is for reference types.
    /// </summary>
    /// <typeparam name="T">The type of the selected expression.</typeparam>
    /// <typeparam name="TDialect">The dialect of the subquery, which must be the outer statement's.</typeparam>
    /// <typeparam name="TMode">Whether the subquery was built to run; it does not matter here.</typeparam>
    /// <typeparam name="TSubLevel">The evaluation level of the subquery's projection.</typeparam>
    /// <param name="subquery">The subquery.</param>
    /// <param name="overload">Not used. It lets this overload, for reference types, exist beside the overloads for value types.</param>
    /// <returns>The value.</returns>
    /// <exception cref="ArgumentException">The subquery selects a whole row, a result type, or a tuple.</exception>
    public static IExpr<T?, PerGroup> GroupScalar<T, TDialect, TMode, TSubLevel>(
        SelectProjection<TDialect, TMode, T, TSubLevel> subquery,
        object? overload = null
    )
        where TDialect : IDialect
        where TMode : ExecutionMode
        where TSubLevel : EvaluationLevel => new SqlExpr<T?, PerGroup>(new GroupSubqueryNode(ScalarSubquery(subquery)));

    /// <inheritdoc cref="GroupScalar{T, TDialect, TMode, TSubLevel}(SelectProjection{TDialect, TMode, T, TSubLevel})"/>
    public static IExpr<T?, PerGroup> GroupScalar<T, TDialect, TMode>(SelectLimited<TDialect, TMode, T> subquery)
        where T : struct
        where TDialect : IDialect
        where TMode : ExecutionMode => new SqlExpr<T?, PerGroup>(new GroupSubqueryNode(ScalarSubquery(subquery)));

    /// <inheritdoc cref="GroupScalar{T, TDialect, TMode, TSubLevel}(SelectProjection{TDialect, TMode, T, TSubLevel})"/>
    public static IExpr<T?, PerGroup> GroupScalar<T, TDialect, TMode>(SelectLimited<TDialect, TMode, T?> subquery)
        where T : struct
        where TDialect : IDialect
        where TMode : ExecutionMode => new SqlExpr<T?, PerGroup>(new GroupSubqueryNode(ScalarSubquery(subquery)));

    /// <summary>
    /// <inheritdoc cref="GroupScalar{T, TDialect, TMode, TSubLevel}(SelectProjection{TDialect, TMode, T, TSubLevel})" path="/summary"/> This overload is for reference types.
    /// </summary>
    /// <typeparam name="T">The type of the selected expression.</typeparam>
    /// <typeparam name="TDialect">The dialect of the subquery, which must be the outer statement's.</typeparam>
    /// <typeparam name="TMode">Whether the subquery was built to run; it does not matter here.</typeparam>
    /// <param name="subquery">The subquery.</param>
    /// <param name="overload">Not used. It lets this overload, for reference types, exist beside the overloads for value types.</param>
    /// <returns>The value.</returns>
    /// <exception cref="ArgumentException">The subquery selects a whole row, a result type, or a tuple.</exception>
    public static IExpr<T?, PerGroup> GroupScalar<T, TDialect, TMode>(SelectLimited<TDialect, TMode, T> subquery, object? overload = null)
        where TDialect : IDialect
        where TMode : ExecutionMode => new SqlExpr<T?, PerGroup>(new GroupSubqueryNode(ScalarSubquery(subquery)));

    /// <inheritdoc cref="GroupScalar{T, TDialect, TMode, TSubLevel}(SelectProjection{TDialect, TMode, T, TSubLevel})"/>
    public static IExpr<T?, PerGroup> GroupScalar<T, TDialect, TMode>(SelectOffset<TDialect, TMode, T> subquery)
        where T : struct
        where TDialect : IDialect
        where TMode : ExecutionMode => new SqlExpr<T?, PerGroup>(new GroupSubqueryNode(ScalarSubquery(subquery)));

    /// <inheritdoc cref="GroupScalar{T, TDialect, TMode, TSubLevel}(SelectProjection{TDialect, TMode, T, TSubLevel})"/>
    public static IExpr<T?, PerGroup> GroupScalar<T, TDialect, TMode>(SelectOffset<TDialect, TMode, T?> subquery)
        where T : struct
        where TDialect : IDialect
        where TMode : ExecutionMode => new SqlExpr<T?, PerGroup>(new GroupSubqueryNode(ScalarSubquery(subquery)));

    /// <summary>
    /// <inheritdoc cref="GroupScalar{T, TDialect, TMode, TSubLevel}(SelectProjection{TDialect, TMode, T, TSubLevel})" path="/summary"/> This overload is for reference types.
    /// </summary>
    /// <typeparam name="T">The type of the selected expression.</typeparam>
    /// <typeparam name="TDialect">The dialect of the subquery, which must be the outer statement's.</typeparam>
    /// <typeparam name="TMode">Whether the subquery was built to run; it does not matter here.</typeparam>
    /// <param name="subquery">The subquery.</param>
    /// <param name="overload">Not used. It lets this overload, for reference types, exist beside the overloads for value types.</param>
    /// <returns>The value.</returns>
    /// <exception cref="ArgumentException">The subquery selects a whole row, a result type, or a tuple.</exception>
    public static IExpr<T?, PerGroup> GroupScalar<T, TDialect, TMode>(SelectOffset<TDialect, TMode, T> subquery, object? overload = null)
        where TDialect : IDialect
        where TMode : ExecutionMode => new SqlExpr<T?, PerGroup>(new GroupSubqueryNode(ScalarSubquery(subquery)));

    private static SubqueryNode Subquery<TDialect, TMode, TResult>(SelectQuery<TDialect, TMode, TResult> subquery)
        where TDialect : IDialect
        where TMode : ExecutionMode
    {
        ArgumentNullException.ThrowIfNull(subquery);
        return new SubqueryNode(subquery.State, DialectRenderer.For<TDialect>());
    }

    // §5.11: the result type alone lets a whole row through, so the projection is checked here.
    private static SubqueryNode ScalarSubquery<TDialect, TMode, TResult>(SelectQuery<TDialect, TMode, TResult> subquery)
        where TDialect : IDialect
        where TMode : ExecutionMode
    {
        ArgumentNullException.ThrowIfNull(subquery);
        if (!subquery.State.SingleExpression)
        {
            throw new ArgumentException("A scalar subquery selects one expression, not a whole row, a result type, or a tuple.", nameof(subquery));
        }

        return new SubqueryNode(subquery.State, DialectRenderer.For<TDialect>());
    }
}
