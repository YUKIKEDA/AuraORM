namespace AuraORM.Tests;

public sealed class TransactionErrorTests
{
    /// <summary>
    /// When the body throws and the rollback throws too, the body's exception is not lost: both come in an
    /// <see cref="AggregateException"/>, the body's first (design §12.5).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - A connection whose rollback throws.
    ///
    /// Steps:
    /// - Run <c>InTransactionAsync</c> with a body that throws.
    ///
    /// Expected:
    /// - <see cref="AggregateException"/> with the body's exception, then the rollback's.
    /// - The rollback was tried once (disposing the transaction does not try again).
    /// </remarks>
    [Fact]
    public async Task InTransactionAsync_RollbackThrowsAfterBody_AggregatesBodyFirst()
    {
        var connection = new FakeConnection { FailRollback = true };
        var db = new Database<SqliteDialect>(connection);
        var bodyError = new FormatException("body failed");

        AggregateException error = await Assert.ThrowsAsync<AggregateException>(() =>
            db.InTransactionAsync(_ => throw bodyError, TestContext.Current.CancellationToken)
        );

        Assert.Equal(2, error.InnerExceptions.Count);
        Assert.Same(bodyError, error.InnerExceptions[0]);
        Assert.Equal("rollback failed", error.InnerExceptions[1].Message);
        Assert.Equal(1, connection.RollbackCount);
    }

    /// <summary>
    /// When the body throws and the rollback succeeds, the body's exception comes out unchanged.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - A connection whose rollback succeeds.
    ///
    /// Steps:
    /// - Run <c>InTransactionAsync</c> with a body that throws.
    ///
    /// Expected:
    /// - The same exception instance; one rollback.
    /// </remarks>
    [Fact]
    public async Task InTransactionAsync_BodyThrows_RethrowsBodyException()
    {
        var connection = new FakeConnection();
        var db = new Database<SqliteDialect>(connection);
        var bodyError = new FormatException("body failed");

        FormatException error = await Assert.ThrowsAsync<FormatException>(() =>
            db.InTransactionAsync(_ => throw bodyError, TestContext.Current.CancellationToken)
        );

        Assert.Same(bodyError, error);
        Assert.Equal(1, connection.RollbackCount);
    }
}
