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
/// <see cref="TypedValueTests{TDialect}"/> on postgresql.
/// </summary>
[Collection("postgresql")]
public sealed class PostgreSqlTypedValueTests : TypedValueTests<PostgreSqlDialect>, IAsyncLifetime
{
    private readonly PostgreSqlTarget _target = new();

    private protected override ITestTarget<PostgreSqlDialect> Target => _target;

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public ValueTask DisposeAsync() => _target.DisposeAsync();
}

/// <summary>
/// <see cref="TypedValueTests{TDialect}"/> on mysql.
/// </summary>
[Collection("mysql")]
public sealed class MySqlTypedValueTests : TypedValueTests<MySqlDialect>, IAsyncLifetime
{
    private readonly MySqlTarget _target = new();

    private protected override ITestTarget<MySqlDialect> Target => _target;

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public ValueTask DisposeAsync() => _target.DisposeAsync();
}

/// <summary>
/// <see cref="TypedValueTests{TDialect}"/> on mariadb.
/// </summary>
[Collection("mariadb")]
public sealed class MariaDbTypedValueTests : TypedValueTests<MariaDbDialect>, IAsyncLifetime
{
    private readonly MariaDbTarget _target = new();

    private protected override ITestTarget<MariaDbDialect> Target => _target;

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public ValueTask DisposeAsync() => _target.DisposeAsync();
}

/// <summary>
/// <see cref="TypedValueTests{TDialect}"/> on sqlserver.
/// </summary>
[Collection("sqlserver")]
public sealed class SqlServerTypedValueTests : TypedValueTests<SqlServerDialect>, IAsyncLifetime
{
    private readonly SqlServerTarget _target = new();

    private protected override ITestTarget<SqlServerDialect> Target => _target;

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public ValueTask DisposeAsync() => _target.DisposeAsync();
}
