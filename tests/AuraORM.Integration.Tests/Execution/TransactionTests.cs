namespace AuraORM.Integration.Tests;

/// <summary>
/// Transactions (design §12.5) on one database. A derived class per database gives the target.
/// </summary>
/// <typeparam name="TDialect">The dialect.</typeparam>
public abstract class TransactionTests<TDialect>
    where TDialect : IDialect
{
    private protected abstract ITestTarget<TDialect> Target { get; }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>
    /// A committed transaction keeps its rows; one disposed without commit rolls them back.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - An empty <c>people</c> table.
    ///
    /// Steps:
    /// - Insert person 1 in a transaction and commit; insert person 2 in another and dispose it without commit.
    ///
    /// Expected:
    /// - Only person 1 is in the table.
    /// </remarks>
    [Fact]
    public async Task Transaction_CommitAndDispose_KeepsOnlyCommittedRows()
    {
        Database<TDialect> db = await Target.CreateDatabaseAsync();

        await using (Transaction<TDialect> tx = await db.BeginTransactionAsync(cancellationToken: Ct))
        {
            await InsertAsync(tx, 1);
            await tx.CommitAsync(Ct);
        }

        await using (Transaction<TDialect> tx = await db.BeginTransactionAsync(cancellationToken: Ct))
        {
            await InsertAsync(tx, 2);
        }

        long[] ids = await IdsAsync(db);
        Assert.Equal([1L], ids);
    }

    /// <summary>
    /// <c>InTransactionAsync</c> commits when the body returns and rolls back when it throws, rethrowing the body's exception.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - An empty <c>people</c> table.
    ///
    /// Steps:
    /// - Insert person 1 in a body that returns 42; insert person 2 in a body that then throws.
    ///
    /// Expected:
    /// - The first call returns 42; the second throws the body's exception itself.
    /// - Only person 1 is in the table.
    /// </remarks>
    [Fact]
    public async Task InTransactionAsync_ReturnOrThrow_CommitsOrRollsBack()
    {
        Database<TDialect> db = await Target.CreateDatabaseAsync();
        var failure = new InvalidOperationException("body failed");

        int result = await db.InTransactionAsync(
            async tx =>
            {
                await InsertAsync(tx, 1);
                return 42;
            },
            Ct
        );
        InvalidOperationException thrown = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            db.InTransactionAsync(
                async tx =>
                {
                    await InsertAsync(tx, 2);
                    throw failure;
                },
                Ct
            )
        );

        Assert.Equal(42, result);
        Assert.Same(failure, thrown);
        long[] ids = await IdsAsync(db);
        Assert.Equal([1L], ids);
    }

    /// <summary>
    /// A transaction cannot be used after it was committed.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - A committed transaction.
    ///
    /// Steps:
    /// - Commit again, roll back, and run a command on it.
    ///
    /// Expected:
    /// - Each throws <see cref="InvalidOperationException"/>.
    /// </remarks>
    [Fact]
    public async Task Transaction_AfterCommit_Throws()
    {
        Database<TDialect> db = await Target.CreateDatabaseAsync();
        await using Transaction<TDialect> tx = await db.BeginTransactionAsync(cancellationToken: Ct);
        await tx.CommitAsync(Ct);

        await Assert.ThrowsAsync<InvalidOperationException>(() => tx.CommitAsync(Ct));
        await Assert.ThrowsAsync<InvalidOperationException>(() => tx.RollbackAsync(Ct));
        await Assert.ThrowsAsync<InvalidOperationException>(() => InsertAsync(tx, 3));
    }

    private static Task<int> InsertAsync(DatabaseSession<TDialect> session, long id) =>
        session.ExecuteAsync(
            $"INSERT INTO {Person.Table} (id, name, status, balance, birthday) VALUES ({id}, {"p" + id}, {PersonStatus.Active}, {1m}, {new DateOnly(2000, 1, 1)})",
            Ct
        );

    private static async Task<long[]> IdsAsync(Database<TDialect> db) =>
        (await db.QueryAsync<long, int>($"SELECT id, 0 FROM {Person.Table} ORDER BY id", Ct)).Select(r => r.Item1).ToArray();
}

/// <summary>
/// <see cref="TransactionTests{TDialect}"/> on SQLite.
/// </summary>
public sealed class SqliteTransactionTests : TransactionTests<SqliteDialect>, IAsyncLifetime
{
    private readonly SqliteTarget _target = new();

    private protected override ITestTarget<SqliteDialect> Target => _target;

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public ValueTask DisposeAsync() => _target.DisposeAsync();
}
