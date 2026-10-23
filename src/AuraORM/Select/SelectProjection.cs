namespace AuraORM;

/// <summary>
/// A SELECT statement right after its projection, before <c>ORDER BY</c>. The row limit and the offset of the dialects that
/// accept them here are extension methods (<see cref="SelectExtensions"/>, <c>docs/design.ja.md</c> §5.9).
/// </summary>
/// <typeparam name="TDialect">The SQL dialect.</typeparam>
/// <typeparam name="TResult">The type that each row is read as.</typeparam>
/// <typeparam name="TLevel">The evaluation level of the projection, which <c>ORDER BY</c> keys must share.</typeparam>
public abstract class SelectProjection<TDialect, TResult, TLevel> : SelectQuery<TDialect, TResult>
    where TDialect : IDialect
    where TLevel : EvaluationLevel
{
    private protected SelectProjection(IStatementRunner session, SelectState state, RowFactory<TResult> factory)
        : base(session, state, factory) { }

    internal SelectLimited<TDialect, TResult> LimitCore(int count) =>
        new(Session, State with { Limit = SelectState.RowLimit<TDialect>(count, nameof(count)) }, Factory);

    internal SelectOffset<TDialect, TResult> OffsetCore(int count) =>
        new(Session, State with { Offset = SelectState.RowCount(count, nameof(count)) }, Factory);

    private protected static RowFactory<TResult> FactoryOf(IProjection<TResult, TLevel> projection) =>
        (ref RowReader reader) => reader.Read(projection);
}
