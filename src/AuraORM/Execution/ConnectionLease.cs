using System.Data.Common;

namespace AuraORM;

/// <summary>
/// A connection held for one command, and what to do with it afterwards (<c>docs/design.ja.md</c> §12.1).
/// </summary>
internal readonly struct ConnectionLease
{
    private readonly Ownership _ownership;

    public ConnectionLease(DbConnection connection, DbTransaction? transaction, Ownership ownership)
    {
        Connection = connection;
        Transaction = transaction;
        _ownership = ownership;
    }

    /// <summary>
    /// What the lease does with the connection when it ends.
    /// </summary>
    public enum Ownership
    {
        /// <summary>
        /// Nothing: the caller or the transaction owns it.
        /// </summary>
        Borrowed,

        /// <summary>
        /// Close it: it was closed before the command.
        /// </summary>
        CloseAfter,

        /// <summary>
        /// Dispose it: it came from a <see cref="DbDataSource"/>.
        /// </summary>
        DisposeAfter,
    }

    public DbConnection Connection { get; }

    public DbTransaction? Transaction { get; }

    public async ValueTask ReleaseAsync()
    {
        switch (_ownership)
        {
            case Ownership.CloseAfter:
                await Connection.CloseAsync().ConfigureAwait(false);
                break;
            case Ownership.DisposeAfter:
                await Connection.DisposeAsync().ConfigureAwait(false);
                break;
        }
    }
}
