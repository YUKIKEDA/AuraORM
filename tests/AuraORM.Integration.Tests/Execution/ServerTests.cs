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

/// <summary>
/// <see cref="NamedQueryTests{TDialect}"/> on postgresql.
/// </summary>
[Collection("postgresql")]
public sealed class PostgreSqlNamedQueryTests : NamedQueryTests<PostgreSqlDialect>, IAsyncLifetime
{
    private readonly PostgreSqlTarget _target = new();

    private protected override ITestTarget<PostgreSqlDialect> Target => _target;

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public ValueTask DisposeAsync() => _target.DisposeAsync();
}

/// <summary>
/// <see cref="NamedQueryTests{TDialect}"/> on mysql.
/// </summary>
[Collection("mysql")]
public sealed class MySqlNamedQueryTests : NamedQueryTests<MySqlDialect>, IAsyncLifetime
{
    private readonly MySqlTarget _target = new();

    private protected override ITestTarget<MySqlDialect> Target => _target;

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public ValueTask DisposeAsync() => _target.DisposeAsync();
}

/// <summary>
/// <see cref="NamedQueryTests{TDialect}"/> on mariadb.
/// </summary>
[Collection("mariadb")]
public sealed class MariaDbNamedQueryTests : NamedQueryTests<MariaDbDialect>, IAsyncLifetime
{
    private readonly MariaDbTarget _target = new();

    private protected override ITestTarget<MariaDbDialect> Target => _target;

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public ValueTask DisposeAsync() => _target.DisposeAsync();
}

/// <summary>
/// <see cref="NamedQueryTests{TDialect}"/> on sqlserver.
/// </summary>
[Collection("sqlserver")]
public sealed class SqlServerNamedQueryTests : NamedQueryTests<SqlServerDialect>, IAsyncLifetime
{
    private readonly SqlServerTarget _target = new();

    private protected override ITestTarget<SqlServerDialect> Target => _target;

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public ValueTask DisposeAsync() => _target.DisposeAsync();
}

/// <summary>
/// <see cref="TargetSubqueryMutationTests{TDialect}"/> on postgresql.
/// </summary>
[Collection("postgresql")]
public sealed class PostgreSqlMutationTests : TargetSubqueryMutationTests<PostgreSqlDialect>, IAsyncLifetime
{
    private readonly PostgreSqlTarget _target = new();

    private protected override ITestTarget<PostgreSqlDialect> Target => _target;

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public ValueTask DisposeAsync() => _target.DisposeAsync();
}

/// <summary>
/// <see cref="MutationTests{TDialect}"/> on mysql.
/// </summary>
[Collection("mysql")]
public sealed class MySqlMutationTests : MutationTests<MySqlDialect>, IAsyncLifetime
{
    private readonly MySqlTarget _target = new();

    private protected override ITestTarget<MySqlDialect> Target => _target;

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public ValueTask DisposeAsync() => _target.DisposeAsync();
}

/// <summary>
/// <see cref="TargetSubqueryMutationTests{TDialect}"/> on mariadb.
/// </summary>
[Collection("mariadb")]
public sealed class MariaDbMutationTests : TargetSubqueryMutationTests<MariaDbDialect>, IAsyncLifetime
{
    private readonly MariaDbTarget _target = new();

    private protected override ITestTarget<MariaDbDialect> Target => _target;

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public ValueTask DisposeAsync() => _target.DisposeAsync();
}

/// <summary>
/// <see cref="TargetSubqueryMutationTests{TDialect}"/> on sqlserver.
/// </summary>
[Collection("sqlserver")]
public sealed class SqlServerMutationTests : TargetSubqueryMutationTests<SqlServerDialect>, IAsyncLifetime
{
    private readonly SqlServerTarget _target = new();

    private protected override ITestTarget<SqlServerDialect> Target => _target;

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public ValueTask DisposeAsync() => _target.DisposeAsync();
}

/// <summary>
/// <see cref="UpdateReturningTests{TDialect}"/> on postgresql.
/// </summary>
[Collection("postgresql")]
public sealed class PostgreSqlReturningTests : UpdateReturningTests<PostgreSqlDialect>, IAsyncLifetime
{
    private readonly PostgreSqlTarget _target = new();

    private protected override ITestTarget<PostgreSqlDialect> Target => _target;

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public ValueTask DisposeAsync() => _target.DisposeAsync();
}

/// <summary>
/// <see cref="ReturningTests{TDialect}"/> on mariadb.
/// </summary>
[Collection("mariadb")]
public sealed class MariaDbReturningTests : ReturningTests<MariaDbDialect>, IAsyncLifetime
{
    private readonly MariaDbTarget _target = new();

    private protected override ITestTarget<MariaDbDialect> Target => _target;

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public ValueTask DisposeAsync() => _target.DisposeAsync();
}

/// <summary>
/// <see cref="UpdateReturningTests{TDialect}"/> on sqlserver.
/// </summary>
[Collection("sqlserver")]
public sealed class SqlServerReturningTests : UpdateReturningTests<SqlServerDialect>, IAsyncLifetime
{
    private readonly SqlServerTarget _target = new();

    private protected override ITestTarget<SqlServerDialect> Target => _target;

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public ValueTask DisposeAsync() => _target.DisposeAsync();
}

/// <summary>
/// <see cref="ConflictUpsertTests{TDialect}"/> on postgresql, and an empty row with <c>DEFAULT VALUES</c> (design §10.1).
/// </summary>
[Collection("postgresql")]
public sealed class PostgreSqlUpsertTests : ConflictUpsertTests<PostgreSqlDialect>, IAsyncLifetime
{
    private readonly PostgreSqlTarget _target = new();

    private protected override ITestTarget<PostgreSqlDialect> Target => _target;

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public ValueTask DisposeAsync() => _target.DisposeAsync();

    /// <summary>
    /// PostgreSQL takes <c>ON CONFLICT</c> after <c>DEFAULT VALUES</c>, which inserts the row (design §10.1).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - An empty <c>notes</c> table.
    ///
    /// Steps:
    /// - Insert an empty note with <c>OnConflictDoNothing()</c>.
    ///
    /// Expected:
    /// - 1.
    /// </remarks>
    [Fact]
    public async Task EmptyRow_TakesOnConflict()
    {
        Database<PostgreSqlDialect> db = await Target.CreateDatabaseAsync();

        Assert.Equal(1, await db.Insert(Note.Table).Values(new Note.Insert()).OnConflictDoNothing().ExecuteAsync(Ct));
    }
}

/// <summary>
/// <see cref="DuplicateKeyUpsertTests{TDialect}"/> on mysql.
/// </summary>
[Collection("mysql")]
public sealed class MySqlUpsertTests : DuplicateKeyUpsertTests<MySqlDialect>, IAsyncLifetime
{
    private readonly MySqlTarget _target = new();

    private protected override ITestTarget<MySqlDialect> Target => _target;

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public ValueTask DisposeAsync() => _target.DisposeAsync();
}

/// <summary>
/// <see cref="DuplicateKeyUpsertTests{TDialect}"/> on mariadb, and what only MariaDB has: <c>Returning</c> after the clause
/// and <c>VALUE(a)</c> of a column the rows do not set (design §10.2, §10.3).
/// </summary>
[Collection("mariadb")]
public sealed class MariaDbUpsertTests : DuplicateKeyUpsertTests<MariaDbDialect>, IAsyncLifetime
{
    private readonly MariaDbTarget _target = new();

    private protected override ITestTarget<MariaDbDialect> Target => _target;

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public ValueTask DisposeAsync() => _target.DisposeAsync();

    /// <summary>
    /// <c>Returning</c> after <c>ON DUPLICATE KEY UPDATE</c> returns the updated and the inserted rows, and a proposed column
    /// that the rows do not set reads its default (null) (design §10.2, §10.3).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Alice (1) and Bob (2, nickname "B").
    ///
    /// Steps:
    /// - Upsert Bob as "Bobby" without a nickname and Carol (3), setting the name and the nickname from excluded, returning
    ///   the id, the name, and the nickname.
    ///
    /// Expected:
    /// - (2, Bobby, null) and (3, Carol, null).
    /// </remarks>
    [Fact]
    public async Task Returning_ReadsTheUpdatedAndInsertedRows()
    {
        Database<MariaDbDialect> db = await UpsertSeed.SeedAsync(Target);

        List<(long, string, string?)> rows = await db.Insert(Person.Table)
            .Values([UpsertSeed.Person(2, "Bobby"), UpsertSeed.Person(3, "Carol")])
            .OnDuplicateKeyUpdate((p, excluded) => [Sql.Set(p.Name, excluded.Name), Sql.Set(p.Nickname, excluded.Nickname)])
            .Returning(p => (p.Id, p.Name, p.Nickname))
            .ToListAsync(Ct);

        Assert.Equal([(2L, "Bobby", (string?)null), (3L, "Carol", null)], rows.OrderBy(r => r.Item1));
    }
}

/// <summary>
/// <see cref="ValueConverterTests{TDialect}"/> on postgresql.
/// </summary>
[Collection("postgresql")]
public sealed class PostgreSqlValueConverterTests : ValueConverterTests<PostgreSqlDialect>, IAsyncLifetime
{
    private readonly PostgreSqlTarget _target = new();

    private protected override ITestTarget<PostgreSqlDialect> Target => _target;

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public ValueTask DisposeAsync() => _target.DisposeAsync();
}

/// <summary>
/// <see cref="ValueConverterTests{TDialect}"/> on mysql.
/// </summary>
[Collection("mysql")]
public sealed class MySqlValueConverterTests : ValueConverterTests<MySqlDialect>, IAsyncLifetime
{
    private readonly MySqlTarget _target = new();

    private protected override ITestTarget<MySqlDialect> Target => _target;

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public ValueTask DisposeAsync() => _target.DisposeAsync();
}

/// <summary>
/// <see cref="ValueConverterTests{TDialect}"/> on mariadb.
/// </summary>
[Collection("mariadb")]
public sealed class MariaDbValueConverterTests : ValueConverterTests<MariaDbDialect>, IAsyncLifetime
{
    private readonly MariaDbTarget _target = new();

    private protected override ITestTarget<MariaDbDialect> Target => _target;

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public ValueTask DisposeAsync() => _target.DisposeAsync();
}

/// <summary>
/// <see cref="ValueConverterTests{TDialect}"/> on sqlserver.
/// </summary>
[Collection("sqlserver")]
public sealed class SqlServerValueConverterTests : ValueConverterTests<SqlServerDialect>, IAsyncLifetime
{
    private readonly SqlServerTarget _target = new();

    private protected override ITestTarget<SqlServerDialect> Target => _target;

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public ValueTask DisposeAsync() => _target.DisposeAsync();
}
