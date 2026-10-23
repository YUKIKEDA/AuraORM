namespace AuraORM.Integration.Tests;

/// <summary>
/// <see cref="RawQueryTests{TDialect}"/> on postgresql.
/// </summary>
[Collection("postgresql")]
public sealed class PostgreSqlRawQueryTests : RawQueryTests<PostgreSqlDialect>, IAsyncLifetime
{
    private readonly PostgreSqlTarget _target = new();

    private protected override ITestTarget<PostgreSqlDialect> Target => _target;

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public ValueTask DisposeAsync() => _target.DisposeAsync();
}

/// <summary>
/// <see cref="TransactionTests{TDialect}"/> on postgresql.
/// </summary>
[Collection("postgresql")]
public sealed class PostgreSqlTransactionTests : TransactionTests<PostgreSqlDialect>, IAsyncLifetime
{
    private readonly PostgreSqlTarget _target = new();

    private protected override ITestTarget<PostgreSqlDialect> Target => _target;

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public ValueTask DisposeAsync() => _target.DisposeAsync();
}

/// <summary>
/// <see cref="RawQueryTests{TDialect}"/> on mysql.
/// </summary>
[Collection("mysql")]
public sealed class MySqlRawQueryTests : RawQueryTests<MySqlDialect>, IAsyncLifetime
{
    private readonly MySqlTarget _target = new();

    private protected override ITestTarget<MySqlDialect> Target => _target;

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public ValueTask DisposeAsync() => _target.DisposeAsync();
}

/// <summary>
/// <see cref="TransactionTests{TDialect}"/> on mysql.
/// </summary>
[Collection("mysql")]
public sealed class MySqlTransactionTests : TransactionTests<MySqlDialect>, IAsyncLifetime
{
    private readonly MySqlTarget _target = new();

    private protected override ITestTarget<MySqlDialect> Target => _target;

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public ValueTask DisposeAsync() => _target.DisposeAsync();
}

/// <summary>
/// <see cref="RawQueryTests{TDialect}"/> on mariadb.
/// </summary>
[Collection("mariadb")]
public sealed class MariaDbRawQueryTests : RawQueryTests<MariaDbDialect>, IAsyncLifetime
{
    private readonly MariaDbTarget _target = new();

    private protected override ITestTarget<MariaDbDialect> Target => _target;

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public ValueTask DisposeAsync() => _target.DisposeAsync();
}

/// <summary>
/// <see cref="TransactionTests{TDialect}"/> on mariadb.
/// </summary>
[Collection("mariadb")]
public sealed class MariaDbTransactionTests : TransactionTests<MariaDbDialect>, IAsyncLifetime
{
    private readonly MariaDbTarget _target = new();

    private protected override ITestTarget<MariaDbDialect> Target => _target;

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public ValueTask DisposeAsync() => _target.DisposeAsync();
}

/// <summary>
/// <see cref="RawQueryTests{TDialect}"/> on sqlserver.
/// </summary>
[Collection("sqlserver")]
public sealed class SqlServerRawQueryTests : RawQueryTests<SqlServerDialect>, IAsyncLifetime
{
    private readonly SqlServerTarget _target = new();

    private protected override ITestTarget<SqlServerDialect> Target => _target;

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public ValueTask DisposeAsync() => _target.DisposeAsync();
}

/// <summary>
/// <see cref="TransactionTests{TDialect}"/> on sqlserver.
/// </summary>
[Collection("sqlserver")]
public sealed class SqlServerTransactionTests : TransactionTests<SqlServerDialect>, IAsyncLifetime
{
    private readonly SqlServerTarget _target = new();

    private protected override ITestTarget<SqlServerDialect> Target => _target;

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public ValueTask DisposeAsync() => _target.DisposeAsync();
}

/// <summary>
/// <see cref="SelectTests{TDialect}"/> on postgresql.
/// </summary>
[Collection("postgresql")]
public sealed class PostgreSqlSelectTests : SelectTests<PostgreSqlDialect>, IAsyncLifetime
{
    private readonly PostgreSqlTarget _target = new();

    private protected override ITestTarget<PostgreSqlDialect> Target => _target;

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public ValueTask DisposeAsync() => _target.DisposeAsync();
}

/// <summary>
/// <see cref="SelectTests{TDialect}"/> on mysql.
/// </summary>
[Collection("mysql")]
public sealed class MySqlSelectTests : SelectTests<MySqlDialect>, IAsyncLifetime
{
    private readonly MySqlTarget _target = new();

    private protected override ITestTarget<MySqlDialect> Target => _target;

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public ValueTask DisposeAsync() => _target.DisposeAsync();
}

/// <summary>
/// <see cref="SelectTests{TDialect}"/> on mariadb.
/// </summary>
[Collection("mariadb")]
public sealed class MariaDbSelectTests : SelectTests<MariaDbDialect>, IAsyncLifetime
{
    private readonly MariaDbTarget _target = new();

    private protected override ITestTarget<MariaDbDialect> Target => _target;

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public ValueTask DisposeAsync() => _target.DisposeAsync();
}

/// <summary>
/// <see cref="SelectTests{TDialect}"/> on sqlserver.
/// </summary>
[Collection("sqlserver")]
public sealed class SqlServerSelectTests : SelectTests<SqlServerDialect>, IAsyncLifetime
{
    private readonly SqlServerTarget _target = new();

    private protected override ITestTarget<SqlServerDialect> Target => _target;

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public ValueTask DisposeAsync() => _target.DisposeAsync();
}
