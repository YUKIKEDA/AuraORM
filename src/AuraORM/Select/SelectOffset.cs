namespace AuraORM;

/// <summary>
/// A SELECT statement with an offset: the last stage.
/// </summary>
/// <typeparam name="TDialect">The SQL dialect.</typeparam>
/// <typeparam name="TMode">Whether the statement can be run: <see cref="Executable"/> or <see cref="SqlOnly"/>.</typeparam>
/// <typeparam name="TResult">The type that each row is read as.</typeparam>
public sealed class SelectOffset<TDialect, TMode, TResult> : SelectQuery<TDialect, TMode, TResult>
    where TDialect : IDialect
    where TMode : ExecutionMode
{
    internal SelectOffset(IStatementRunner? session, SelectState state, RowFactory<TResult> factory)
        : base(session, state, factory) { }
}
