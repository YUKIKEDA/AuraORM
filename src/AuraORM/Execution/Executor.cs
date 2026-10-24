using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace AuraORM;

/// <summary>
/// Reads one row of a result.
/// </summary>
/// <typeparam name="T">The type of a row.</typeparam>
/// <param name="reader">The reader of the current row.</param>
/// <returns>The row.</returns>
internal delegate T RowFactory<T>(ref RowReader reader);

/// <summary>
/// Runs rendered statements on a connection of a session: creates the command, binds the parameters, and reads the rows
/// (<c>docs/design.ja.md</c> §12). Errors from the database reach the caller unchanged.
/// </summary>
internal static class Executor
{
    public static async Task<int> ExecuteAsync(IStatementRunner session, SqlStatement statement, CancellationToken cancellationToken)
    {
        ConnectionLease lease = await session.LeaseAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await using DbCommand command = CreateCommand(lease, statement, session);
            return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            await lease.ReleaseAsync().ConfigureAwait(false);
        }
    }

    public static Task<List<T>> ListAsync<T>(
        IStatementRunner session,
        SqlStatement statement,
        RowFactory<T> factory,
        CancellationToken cancellationToken
    ) => ListAsync(session, statement, factory, cancelUnread: true, cancellationToken);

    public static async Task<List<T>> ListAsync<T>(
        IStatementRunner session,
        SqlStatement statement,
        RowFactory<T> factory,
        bool cancelUnread,
        CancellationToken cancellationToken
    )
    {
        var rows = new List<T>();
        await foreach (T row in StreamAsync(session, statement, factory, cancelUnread, cancellationToken).ConfigureAwait(false))
        {
            rows.Add(row);
        }

        return rows;
    }

    public static async IAsyncEnumerable<T> StreamAsync<T>(
        IStatementRunner session,
        SqlStatement statement,
        RowFactory<T> factory,
        bool cancelUnread,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        ConnectionLease lease = await session.LeaseAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await using DbCommand command = CreateCommand(lease, statement, session);
            await using DbDataReader reader = await command.ExecuteReaderAsync(CommandBehavior.Default, cancellationToken).ConfigureAwait(false);
            bool readAll = false;
            try
            {
                while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    yield return Read(reader, factory);
                }

                readAll = true;
            }
            finally
            {
                // §12.2: a SELECT with rows left is cancelled so that SqlClient does not read them; a statement with Returning
                // is not, because a cancel can undo its change (§9).
                if (!readAll && cancelUnread)
                {
                    CancelUnread(command);
                }
            }
        }
        finally
        {
            await lease.ReleaseAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Reads the rows of a raw query by column name (<c>docs/design.ja.md</c> §11.3). The names are matched once, without
    /// regard to case, before the first row.
    /// </summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="session">The session.</param>
    /// <param name="statement">The statement.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The rows.</returns>
    public static async Task<List<T>> ListNamedAsync<T>(IStatementRunner session, SqlStatement statement, CancellationToken cancellationToken)
        where T : IQueryResult<T>
    {
        var rows = new List<T>();
        ConnectionLease lease = await session.LeaseAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await using DbCommand command = CreateCommand(lease, statement, session);
            await using DbDataReader reader = await command.ExecuteReaderAsync(CommandBehavior.Default, cancellationToken).ConfigureAwait(false);
            int[] ordinals = ResolveOrdinals(reader, T.ColumnNames, typeof(T));
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                rows.Add(ReadNamed<T>(reader, ordinals));
            }
        }
        finally
        {
            await lease.ReleaseAsync().ConfigureAwait(false);
        }

        return rows;
    }

    public static async Task<T> ScalarAsync<T>(IStatementRunner session, SqlStatement statement, bool orDefault, CancellationToken cancellationToken)
    {
        ConnectionLease lease = await session.LeaseAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await using DbCommand command = CreateCommand(lease, statement, session);
            await using DbDataReader reader = await command.ExecuteReaderAsync(CommandBehavior.Default, cancellationToken).ConfigureAwait(false);
            if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                T value = ValueReader<T>.Read(reader, 0);
                if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    CancelUnread(command);
                }

                return value;
            }

            return orDefault ? default! : throw new InvalidOperationException("The query returned no row.");
        }
        finally
        {
            await lease.ReleaseAsync().ConfigureAwait(false);
        }
    }

    private static DbCommand CreateCommand(ConnectionLease lease, SqlStatement statement, IStatementRunner session)
    {
        DbCommand command = lease.Connection.CreateCommand();
        command.CommandText = statement.Text;
        command.Transaction = lease.Transaction;
        if (session.Options.CommandTimeout is int timeout)
        {
            command.CommandTimeout = timeout;
        }

        bool positional = session.Renderer.UsesPositionalParameters;
        foreach (SqlStatementParameter parameter in statement.Parameters)
        {
            DbParameter p = command.CreateParameter();
            if (!positional)
            {
                p.ParameterName = parameter.Name;
            }

            p.Value = parameter.Value ?? DBNull.Value;
            if (parameter.DbType is DbType type)
            {
                p.DbType = type;
            }

            command.Parameters.Add(p);
        }

        return command;
    }

    // §12.2: a reader closed with rows left would read them to the end first (SqlClient's Close does). Cancelling first
    // stops that, as Dapper does. A provider that cannot cancel is left to read the rest.
    private static void CancelUnread(DbCommand command)
    {
        try
        {
            command.Cancel();
        }
        catch (DbException)
        {
            // The rows are read to the end instead.
        }
        catch (InvalidOperationException)
        {
            // The rows are read to the end instead.
        }
        catch (NotSupportedException)
        {
            // The rows are read to the end instead.
        }
    }

    private static T Read<T>(DbDataReader reader, RowFactory<T> factory)
    {
        var row = new RowReader(reader);
        return factory(ref row);
    }

    private static T ReadNamed<T>(DbDataReader reader, int[] ordinals)
        where T : IQueryResult<T>
    {
        var row = new RowReader(reader, ordinals);
        return T.ReadNamed(ref row);
    }

    private static int[] ResolveOrdinals(DbDataReader reader, IReadOnlyList<string> names, Type type)
    {
        // A name that two columns share maps to -1, and is an error only when a member needs it (§11.3).
        var columns = new Dictionary<string, int>(reader.FieldCount, StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < reader.FieldCount; i++)
        {
            string name = reader.GetName(i);
            if (!columns.TryAdd(name, i))
            {
                columns[name] = -1;
            }
        }

        int[] ordinals = new int[names.Count];
        for (int i = 0; i < ordinals.Length; i++)
        {
            if (!columns.TryGetValue(names[i], out ordinals[i]))
            {
                throw new InvalidOperationException(
                    string.Format(CultureInfo.InvariantCulture, "The result has no column {0} for a member of {1}.", names[i], type.Name)
                );
            }

            if (ordinals[i] < 0)
            {
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "The result has more than one column named {0} for a member of {1}; give the columns different names with AS.",
                        names[i],
                        type.Name
                    )
                );
            }
        }

        return ordinals;
    }
}
