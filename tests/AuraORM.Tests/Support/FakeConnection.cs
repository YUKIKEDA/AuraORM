using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;

namespace AuraORM.Tests;

/// <summary>
/// An open connection whose transactions can be told to fail on rollback. It runs no commands.
/// </summary>
internal sealed class FakeConnection : DbConnection
{
    public bool FailRollback { get; init; }

    public int RollbackCount { get; private set; }

    [AllowNull]
    public override string ConnectionString { get; set; } = string.Empty;

    public override string Database => "fake";

    public override string DataSource => "fake";

    public override string ServerVersion => "1";

    public override ConnectionState State => ConnectionState.Open;

    public override void ChangeDatabase(string databaseName) => throw new NotSupportedException();

    public override void Close() { }

    public override void Open() { }

    protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => new FakeTransaction(this, isolationLevel);

    protected override DbCommand CreateDbCommand() => throw new NotSupportedException("The fake connection runs no commands.");

    private sealed class FakeTransaction(FakeConnection connection, IsolationLevel isolationLevel) : DbTransaction
    {
        public override IsolationLevel IsolationLevel => isolationLevel;

        protected override DbConnection DbConnection => connection;

        public override void Commit() { }

        public override void Rollback()
        {
            connection.RollbackCount++;
            if (connection.FailRollback)
            {
                throw new InvalidOperationException("rollback failed");
            }
        }
    }
}
