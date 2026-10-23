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

    public static async Task<List<T>> ListAsync<T>(
        IStatementRunner session,
        SqlStatement statement,
        RowFactory<T> factory,
        CancellationToken cancellationToken
    )
    {
        var rows = new List<T>();
        await foreach (T row in StreamAsync(session, statement, factory, cancellationToken).ConfigureAwait(false))
        {
            rows.Add(row);
        }

        return rows;
    }

    public static async IAsyncEnumerable<T> StreamAsync<T>(
        IStatementRunner session,
        SqlStatement statement,
        RowFactory<T> factory,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        ConnectionLease lease = await session.LeaseAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await using DbCommand command = CreateCommand(lease, statement, session);
            await using DbDataReader reader = await command.ExecuteReaderAsync(CommandBehavior.Default, cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                yield return Read(reader, factory);
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
                return ValueReader<T>.Read(reader, 0);
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
            command.Parameters.Add(p);
        }

        return command;
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
        var columns = new Dictionary<string, int>(reader.FieldCount, StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < reader.FieldCount; i++)
        {
            columns.TryAdd(reader.GetName(i), i);
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
        }

        return ordinals;
    }
}
