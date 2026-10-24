namespace AuraORM;

/// <summary>
/// A <c>SELECT DISTINCT</c> of a whole row, a result type's projection, or a tuple with a whole row (<c>docs/design.ja.md</c>
/// §5.7). It has no <c>OrderBy</c>: its <c>ORDER BY</c> keys could not be limited to the select list, which PostgreSQL and
/// SQL Server require. Select the columns to order by as a tuple of expressions instead.
/// </summary>
/// <typeparam name="TDialect">The SQL dialect.</typeparam>
/// <typeparam name="TMode">Whether the statement can be run: <see cref="Executable"/> or <see cref="SqlOnly"/>.</typeparam>
/// <typeparam name="TResult">The type that each row is read as.</typeparam>
/// <typeparam name="TLevel">The evaluation level of the projection.</typeparam>
public sealed class SelectDistinctRows<TDialect, TMode, TResult, TLevel> : SelectOperand<TDialect, TMode, TResult, TLevel>
    where TDialect : IDialect
    where TMode : ExecutionMode
    where TLevel : EvaluationLevel
{
    internal SelectDistinctRows(IStatementRunner? session, SelectState state, IProjection<TResult, TLevel> projection)
        : base(session, state, FactoryOf(projection)) { }
}
