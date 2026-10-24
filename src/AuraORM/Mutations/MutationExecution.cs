namespace AuraORM;

/// <summary>
/// The run method of an <see cref="Executable"/> <c>INSERT</c>, <c>UPDATE</c>, or <c>DELETE</c> statement
/// (<c>docs/design.ja.md</c> §6.2). A <see cref="SqlOnly"/> statement, from a <see cref="SqlBuilder{TDialect}"/>, does not
/// have it, and neither does an <c>UPDATE</c> or <c>DELETE</c> without <c>Where</c> or <c>AllRows()</c>.
/// </summary>
public static class MutationExecution
{
    /// <summary>
    /// Runs the statement and returns the number of rows it affected.
    /// </summary>
    /// <typeparam name="TDialect">The SQL dialect.</typeparam>
    /// <param name="statement">The statement.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of rows, as the provider reports it. Its meaning follows the connection's settings: MySqlConnector
    /// reports the rows that matched the condition by default and only the rows whose values changed with
    /// <c>UseAffectedRows=true</c>; SQL Server reports -1 under <c>SET NOCOUNT ON</c>. AuraORM does not check these settings.</returns>
    /// <exception cref="InvalidOperationException">The statement breaks a rule that is checked when it is rendered
    /// (<c>docs/design.ja.md</c> §14.3).</exception>
    /// <exception cref="ArgumentException">The statement has more parameters than the dialect accepts.</exception>
    public static Task<int> ExecuteAsync<TDialect>(
        this MutationStatement<TDialect, Executable> statement,
        CancellationToken cancellationToken = default
    )
        where TDialect : IDialect
    {
        ArgumentNullException.ThrowIfNull(statement);
        return Executor.ExecuteAsync(statement.Session!, statement.ToSql(), cancellationToken);
    }
}
