namespace AuraORM;

/// <summary>
/// A SELECT statement with an offset: the last stage.
/// </summary>
/// <typeparam name="TDialect">The SQL dialect.</typeparam>
/// <typeparam name="TResult">The type that each row is read as.</typeparam>
public sealed class SelectOffset<TDialect, TResult> : SelectQuery<TDialect, TResult>
    where TDialect : IDialect
{
    internal SelectOffset(IStatementRunner session, SelectState state, RowFactory<TResult> factory)
        : base(session, state, factory) { }
}
