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

/// <summary>
/// <see cref="FullJoinTests{TDialect}"/> on postgresql.
/// </summary>
[Collection("postgresql")]
public sealed class PostgreSqlJoinTests : FullJoinTests<PostgreSqlDialect>, IAsyncLifetime
{
    private readonly PostgreSqlTarget _target = new();

    private protected override ITestTarget<PostgreSqlDialect> Target => _target;

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public ValueTask DisposeAsync() => _target.DisposeAsync();
}

/// <summary>
/// <see cref="JoinTests{TDialect}"/> on mysql.
/// </summary>
[Collection("mysql")]
public sealed class MySqlJoinTests : JoinTests<MySqlDialect>, IAsyncLifetime
{
    private readonly MySqlTarget _target = new();

    private protected override ITestTarget<MySqlDialect> Target => _target;

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public ValueTask DisposeAsync() => _target.DisposeAsync();
}

/// <summary>
/// <see cref="JoinTests{TDialect}"/> on mariadb.
/// </summary>
[Collection("mariadb")]
public sealed class MariaDbJoinTests : JoinTests<MariaDbDialect>, IAsyncLifetime
{
    private readonly MariaDbTarget _target = new();

    private protected override ITestTarget<MariaDbDialect> Target => _target;

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public ValueTask DisposeAsync() => _target.DisposeAsync();
}

/// <summary>
/// <see cref="FullJoinTests{TDialect}"/> on sqlserver.
/// </summary>
[Collection("sqlserver")]
public sealed class SqlServerJoinTests : FullJoinTests<SqlServerDialect>, IAsyncLifetime
{
    private readonly SqlServerTarget _target = new();

    private protected override ITestTarget<SqlServerDialect> Target => _target;

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public ValueTask DisposeAsync() => _target.DisposeAsync();
}

/// <summary>
/// <see cref="GroupByTests{TDialect}"/> on postgresql.
/// </summary>
[Collection("postgresql")]
public sealed class PostgreSqlGroupByTests : GroupByTests<PostgreSqlDialect>, IAsyncLifetime
{
    private readonly PostgreSqlTarget _target = new();

    private protected override ITestTarget<PostgreSqlDialect> Target => _target;

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public ValueTask DisposeAsync() => _target.DisposeAsync();
}

/// <summary>
/// <see cref="GroupByTests{TDialect}"/> on mysql.
/// </summary>
[Collection("mysql")]
public sealed class MySqlGroupByTests : GroupByTests<MySqlDialect>, IAsyncLifetime
{
    private readonly MySqlTarget _target = new();

    private protected override ITestTarget<MySqlDialect> Target => _target;

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public ValueTask DisposeAsync() => _target.DisposeAsync();
}

/// <summary>
/// <see cref="GroupByTests{TDialect}"/> on mariadb.
/// </summary>
[Collection("mariadb")]
public sealed class MariaDbGroupByTests : GroupByTests<MariaDbDialect>, IAsyncLifetime
{
    private readonly MariaDbTarget _target = new();

    private protected override ITestTarget<MariaDbDialect> Target => _target;

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public ValueTask DisposeAsync() => _target.DisposeAsync();
}

/// <summary>
/// <see cref="GroupByTests{TDialect}"/> on sqlserver.
/// </summary>
[Collection("sqlserver")]
public sealed class SqlServerGroupByTests : GroupByTests<SqlServerDialect>, IAsyncLifetime
{
    private readonly SqlServerTarget _target = new();

    private protected override ITestTarget<SqlServerDialect> Target => _target;

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public ValueTask DisposeAsync() => _target.DisposeAsync();
}

/// <summary>
/// <see cref="LimitInSubqueryTests{TDialect}"/> on postgresql.
/// </summary>
[Collection("postgresql")]
public sealed class PostgreSqlSubqueryTests : LimitInSubqueryTests<PostgreSqlDialect>, IAsyncLifetime
{
    private readonly PostgreSqlTarget _target = new();

    private protected override ITestTarget<PostgreSqlDialect> Target => _target;

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public ValueTask DisposeAsync() => _target.DisposeAsync();
}

/// <summary>
/// <see cref="SubqueryTests{TDialect}"/> on mysql.
/// </summary>
[Collection("mysql")]
public sealed class MySqlSubqueryTests : SubqueryTests<MySqlDialect>, IAsyncLifetime
{
    private readonly MySqlTarget _target = new();

    private protected override ITestTarget<MySqlDialect> Target => _target;

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public ValueTask DisposeAsync() => _target.DisposeAsync();
}

/// <summary>
/// <see cref="SubqueryTests{TDialect}"/> on mariadb.
/// </summary>
[Collection("mariadb")]
public sealed class MariaDbSubqueryTests : SubqueryTests<MariaDbDialect>, IAsyncLifetime
{
    private readonly MariaDbTarget _target = new();

    private protected override ITestTarget<MariaDbDialect> Target => _target;

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public ValueTask DisposeAsync() => _target.DisposeAsync();
}

/// <summary>
/// <see cref="LimitInSubqueryTests{TDialect}"/> on sqlserver.
/// </summary>
[Collection("sqlserver")]
public sealed class SqlServerSubqueryTests : LimitInSubqueryTests<SqlServerDialect>, IAsyncLifetime
{
    private readonly SqlServerTarget _target = new();

    private protected override ITestTarget<SqlServerDialect> Target => _target;

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public ValueTask DisposeAsync() => _target.DisposeAsync();
}

/// <summary>
/// <see cref="SetOperationTests{TDialect}"/> on postgresql.
/// </summary>
[Collection("postgresql")]
public sealed class PostgreSqlSetOperationTests : SetOperationTests<PostgreSqlDialect>, IAsyncLifetime
{
    private readonly PostgreSqlTarget _target = new();

    private protected override ITestTarget<PostgreSqlDialect> Target => _target;

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public ValueTask DisposeAsync() => _target.DisposeAsync();
}

/// <summary>
/// <see cref="SetOperationTests{TDialect}"/> on mysql.
/// </summary>
[Collection("mysql")]
public sealed class MySqlSetOperationTests : SetOperationTests<MySqlDialect>, IAsyncLifetime
{
    private readonly MySqlTarget _target = new();

    private protected override ITestTarget<MySqlDialect> Target => _target;

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public ValueTask DisposeAsync() => _target.DisposeAsync();
}

/// <summary>
/// <see cref="SetOperationTests{TDialect}"/> on mariadb.
/// </summary>
[Collection("mariadb")]
public sealed class MariaDbSetOperationTests : SetOperationTests<MariaDbDialect>, IAsyncLifetime
{
    private readonly MariaDbTarget _target = new();

    private protected override ITestTarget<MariaDbDialect> Target => _target;

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public ValueTask DisposeAsync() => _target.DisposeAsync();
}

/// <summary>
/// <see cref="SetOperationTests{TDialect}"/> on sqlserver.
/// </summary>
[Collection("sqlserver")]
public sealed class SqlServerSetOperationTests : SetOperationTests<SqlServerDialect>, IAsyncLifetime
{
    private readonly SqlServerTarget _target = new();

    private protected override ITestTarget<SqlServerDialect> Target => _target;

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public ValueTask DisposeAsync() => _target.DisposeAsync();
}
