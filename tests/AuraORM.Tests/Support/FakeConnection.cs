using System.Collections;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;

namespace AuraORM.Tests;

/// <summary>
/// An open connection that records the commands it creates and answers them with <see cref="Rows"/>. Its transactions can be
/// told to fail on rollback, and a command to fail when it runs.
/// </summary>
internal sealed class FakeConnection : DbConnection
{
    public bool FailRollback { get; init; }

    /// <summary>
    /// Gets the text of a command that fails with a <see cref="DbException"/> when it runs, or null.
    /// </summary>
    public string? FailingCommand { get; init; }

    public int RollbackCount { get; private set; }

    /// <summary>
    /// Gets the result that every reader returns: one column named <c>value</c>.
    /// </summary>
    public IReadOnlyList<object> Rows { get; init; } = [];

    /// <summary>
    /// Gets the reader that every reader command returns instead of <see cref="Rows"/>, or null.
    /// </summary>
    public Func<DbDataReader>? Reader { get; init; }

    public List<FakeCommand> Commands { get; } = [];

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

    protected override DbCommand CreateDbCommand()
    {
        var command = new FakeCommand(this);
        Commands.Add(command);
        return command;
    }

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

/// <summary>
/// A command of <see cref="FakeConnection"/>. It keeps what was set on it, and whether <see cref="Cancel"/> was called.
/// </summary>
internal sealed class FakeCommand(FakeConnection connection) : DbCommand
{
    private readonly FakeParameterCollection _parameters = new();

    public bool CommandTimeoutSet { get; private set; }

    public int CancelCount { get; private set; }

    [AllowNull]
    public override string CommandText { get; set; } = string.Empty;

    public override int CommandTimeout
    {
        get;
        set
        {
            field = value;
            CommandTimeoutSet = true;
        }
    } = 30;

    public override CommandType CommandType { get; set; } = CommandType.Text;

    public override bool DesignTimeVisible { get; set; }

    public override UpdateRowSource UpdatedRowSource { get; set; }

    public IReadOnlyList<FakeParameter> FakeParameters => _parameters.Items;

    protected override DbConnection? DbConnection { get; set; } = connection;

    protected override DbParameterCollection DbParameterCollection => _parameters;

    protected override DbTransaction? DbTransaction { get; set; }

    public DbTransaction? TransactionSet => DbTransaction;

    public override void Cancel() => CancelCount++;

    public override int ExecuteNonQuery() => CommandText == connection.FailingCommand ? throw new FakeDbException() : 1;

    public override object? ExecuteScalar() => connection.Rows.Count > 0 ? connection.Rows[0] : null;

    public override void Prepare() { }

    protected override DbParameter CreateDbParameter() => new FakeParameter();

    protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
    {
        if (connection.Reader is { } reader)
        {
            return reader();
        }

        var table = new DataTable();
        table.Columns.Add("value", connection.Rows.Count > 0 ? connection.Rows[0].GetType() : typeof(object));
        foreach (object row in connection.Rows)
        {
            table.Rows.Add(row);
        }

        return table.CreateDataReader();
    }
}

/// <summary>
/// A parameter of <see cref="FakeCommand"/>. It records whether <see cref="DbType"/> was set.
/// </summary>
internal sealed class FakeParameter : DbParameter
{
    public bool DbTypeSet { get; private set; }

    public override DbType DbType
    {
        get;
        set
        {
            field = value;
            DbTypeSet = true;
        }
    } = DbType.String;

    public override ParameterDirection Direction { get; set; } = ParameterDirection.Input;

    public override bool IsNullable { get; set; }

    [AllowNull]
    public override string ParameterName { get; set; } = string.Empty;

    public override int Size { get; set; }

    [AllowNull]
    public override string SourceColumn { get; set; } = string.Empty;

    public override bool SourceColumnNullMapping { get; set; }

    public override object? Value { get; set; }

    public override void ResetDbType() => DbTypeSet = false;
}

/// <summary>
/// The parameters of <see cref="FakeCommand"/>, in the order they were added.
/// </summary>
internal sealed class FakeParameterCollection : DbParameterCollection
{
    public List<FakeParameter> Items { get; } = [];

    public override int Count => Items.Count;

    public override object SyncRoot => Items;

    public override int Add(object value)
    {
        Items.Add((FakeParameter)value);
        return Items.Count - 1;
    }

    public override void AddRange(Array values)
    {
        foreach (object value in values)
        {
            Add(value);
        }
    }

    public override void Clear() => Items.Clear();

    public override bool Contains(object value) => Items.Contains((FakeParameter)value);

    public override bool Contains(string value) => Items.Exists(p => p.ParameterName == value);

    public override void CopyTo(Array array, int index) => ((ICollection)Items).CopyTo(array, index);

    public override IEnumerator GetEnumerator() => Items.GetEnumerator();

    public override int IndexOf(object value) => Items.IndexOf((FakeParameter)value);

    public override int IndexOf(string parameterName) => Items.FindIndex(p => p.ParameterName == parameterName);

    public override void Insert(int index, object value) => Items.Insert(index, (FakeParameter)value);

    public override void Remove(object value) => Items.Remove((FakeParameter)value);

    public override void RemoveAt(int index) => Items.RemoveAt(index);

    public override void RemoveAt(string parameterName) => Items.RemoveAt(IndexOf(parameterName));

    protected override DbParameter GetParameter(int index) => Items[index];

    protected override DbParameter GetParameter(string parameterName) => Items[IndexOf(parameterName)];

    protected override void SetParameter(int index, DbParameter value) => Items[index] = (FakeParameter)value;

    protected override void SetParameter(string parameterName, DbParameter value) => Items[IndexOf(parameterName)] = (FakeParameter)value;
}

/// <summary>
/// The error of <see cref="FakeConnection.FailingCommand"/>.
/// </summary>
internal sealed class FakeDbException : DbException
{
    public FakeDbException()
        : base("The fake command failed.") { }
}
