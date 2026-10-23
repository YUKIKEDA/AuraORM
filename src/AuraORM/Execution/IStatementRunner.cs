namespace AuraORM;

/// <summary>
/// What a statement needs from the session that runs it.
/// </summary>
internal interface IStatementRunner
{
    /// <summary>
    /// Gets the renderer of the session's dialect.
    /// </summary>
    DialectRenderer Renderer { get; }

    /// <summary>
    /// Gets the settings of the database.
    /// </summary>
    DatabaseOptions Options { get; }

    /// <summary>
    /// Holds a connection for one command.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The lease. Release it when the command and its reader are done.</returns>
    ValueTask<ConnectionLease> LeaseAsync(CancellationToken cancellationToken);
}
