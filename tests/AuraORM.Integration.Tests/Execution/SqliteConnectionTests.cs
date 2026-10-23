using System.Data;
using System.Data.Common;
using Microsoft.Data.Sqlite;

namespace AuraORM.Integration.Tests;

/// <summary>
/// How a database uses the connection or data source it was given (design §12.1). The behavior does not depend on the
/// dialect, so SQLite in process shows it.
/// </summary>
public sealed class SqliteConnectionTests : IAsyncLifetime
{
    // A shared-cache in-memory database lives while one connection to it is open; the keeper is that connection.
    private readonly string _connectionString = $"Data Source=aura-{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
    private SqliteConnection _keeper = null!;

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        _keeper = new SqliteConnection(_connectionString);
        await _keeper.OpenAsync(Ct);
        await using SqliteCommand create = _keeper.CreateCommand();
        create.CommandText = SqliteTarget.CreatePeople;
        await create.ExecuteNonQueryAsync(Ct);
    }

    public ValueTask DisposeAsync() => _keeper.DisposeAsync();

    /// <summary>
    /// A closed connection is opened for each command and closed after it.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - A closed connection to the shared database.
    ///
    /// Steps:
    /// - Run a command and a query through a database built on it, checking the state after each.
    ///
    /// Expected:
    /// - Both succeed, and the connection is closed after each.
    /// </remarks>
    [Fact]
    public async Task Database_ClosedConnection_OpensAndClosesPerCommand()
    {
        await using var connection = new SqliteConnection(_connectionString);
        var db = new Database<SqliteDialect>(connection);

        await db.ExecuteAsync($"INSERT INTO {Person.Table} (id, name, status, balance, birthday) VALUES (1, 'a', 1, '0', '2000-01-01')", Ct);
        Assert.Equal(ConnectionState.Closed, connection.State);
        Assert.Equal(1L, await db.QueryScalarAsync<long>($"SELECT COUNT(*) FROM {Person.Table}", Ct));
        Assert.Equal(ConnectionState.Closed, connection.State);
    }

    /// <summary>
    /// An open connection stays open.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - An open connection to the shared database.
    ///
    /// Steps:
    /// - Run a query through a database built on it.
    ///
    /// Expected:
    /// - The connection is still open.
    /// </remarks>
    [Fact]
    public async Task Database_OpenConnection_StaysOpen()
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(Ct);
        var db = new Database<SqliteDialect>(connection);

        await db.QueryScalarAsync<long>($"SELECT COUNT(*) FROM {Person.Table}", Ct);

        Assert.Equal(ConnectionState.Open, connection.State);
    }

    /// <summary>
    /// A transaction on a closed connection opens it for the transaction and closes it at the end.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - A closed connection to the shared database.
    ///
    /// Steps:
    /// - Begin a transaction, check the state, commit, dispose, check again.
    ///
    /// Expected:
    /// - Open during the transaction, closed after it.
    /// </remarks>
    [Fact]
    public async Task Transaction_ClosedConnection_OpenDuringTransactionOnly()
    {
        await using var connection = new SqliteConnection(_connectionString);
        var db = new Database<SqliteDialect>(connection);

        Transaction<SqliteDialect> tx = await db.BeginTransactionAsync(cancellationToken: Ct);
        Assert.Equal(ConnectionState.Open, connection.State);
        await tx.CommitAsync(Ct);
        await tx.DisposeAsync();

        Assert.Equal(ConnectionState.Closed, connection.State);
    }

    /// <summary>
    /// A database built on a data source takes a connection for each command and a transaction keeps one until it ends.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - A data source for the shared database.
    ///
    /// Steps:
    /// - Insert in a transaction, commit, then count.
    ///
    /// Expected:
    /// - The count is 1.
    /// </remarks>
    [Fact]
    public async Task Database_DataSource_RunsCommandsAndTransactions()
    {
        await using DbDataSource dataSource = SqliteFactory.Instance.CreateDataSource(_connectionString);
        var db = new Database<SqliteDialect>(dataSource);

        await db.InTransactionAsync(
            tx => tx.ExecuteAsync($"INSERT INTO {Person.Table} (id, name, status, balance, birthday) VALUES (5, 'e', 1, '0', '2000-01-01')", Ct),
            Ct
        );

        Assert.Equal(1L, await db.QueryScalarAsync<long>($"SELECT COUNT(*) FROM {Person.Table}", Ct));
    }
}
