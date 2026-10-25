namespace AuraORM.Tests;

public sealed class ExecutorTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>
    /// Each run method that reads the rows itself returns its rows, throws for the counts that its contract rejects, and
    /// cancels a SELECT only when it closes the reader with rows that it did not read (design §12.2).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - A fake connection that returns 0, 1, or 3 rows.
    ///
    /// Steps:
    /// - Run <c>ToListAsync</c>, <c>FirstAsync</c>, <c>FirstOrDefaultAsync</c>, <c>SingleAsync</c>, and
    ///   <c>SingleOrDefaultAsync</c> of a SELECT for each count.
    ///
    /// Expected:
    /// - <c>ToListAsync</c>: every row, never a cancel.
    /// - <c>FirstAsync</c> and <c>FirstOrDefaultAsync</c>: the first row and one cancel when there is a row (the reader is
    ///   closed after it, without looking for more); with no row, <see cref="InvalidOperationException"/> or the default,
    ///   and no cancel.
    /// - <c>SingleAsync</c> and <c>SingleOrDefaultAsync</c>: the row and no cancel for one row; with no row,
    ///   <see cref="InvalidOperationException"/> or the default, and no cancel; with three rows,
    ///   <see cref="InvalidOperationException"/> and one cancel.
    /// </remarks>
    /// <param name="method">The run method.</param>
    /// <param name="rows">The number of rows.</param>
    /// <param name="expected">The value returned, or null when the method throws.</param>
    /// <param name="cancels">The number of times the command was cancelled.</param>
    [Theory]
    [InlineData("ToList", 0, "", 0)]
    [InlineData("ToList", 1, "1", 0)]
    [InlineData("ToList", 3, "1,2,3", 0)]
    [InlineData("First", 0, null, 0)]
    [InlineData("First", 1, "1", 1)]
    [InlineData("First", 3, "1", 1)]
    [InlineData("FirstOrDefault", 0, "0", 0)]
    [InlineData("FirstOrDefault", 1, "1", 1)]
    [InlineData("FirstOrDefault", 3, "1", 1)]
    [InlineData("Single", 0, null, 0)]
    [InlineData("Single", 1, "1", 0)]
    [InlineData("Single", 3, null, 1)]
    [InlineData("SingleOrDefault", 0, "0", 0)]
    [InlineData("SingleOrDefault", 1, "1", 0)]
    [InlineData("SingleOrDefault", 3, null, 1)]
    public async Task RunMethods_RowCounts_ReturnThrowAndCancelAsDesigned(string method, int rows, string? expected, int cancels)
    {
        var connection = new FakeConnection { Rows = Enumerable.Range(1, rows).Select(i => (object)(long)i).ToArray() };
        SelectProjected<SqliteDialect, Executable, long, PerRow, Account.Columns> query = new Database<SqliteDialect>(connection)
            .From(Account.Table)
            .Select(a => a.Id);
        Func<Task<string>> run = method switch
        {
            "ToList" => async () => string.Join(",", await query.ToListAsync(Ct)),
            "First" => async () => (await query.FirstAsync(Ct)).ToString(System.Globalization.CultureInfo.InvariantCulture),
            "FirstOrDefault" => async () => (await query.FirstOrDefaultAsync(Ct)).ToString(System.Globalization.CultureInfo.InvariantCulture),
            "Single" => async () => (await query.SingleAsync(Ct)).ToString(System.Globalization.CultureInfo.InvariantCulture),
            _ => async () => (await query.SingleOrDefaultAsync(Ct)).ToString(System.Globalization.CultureInfo.InvariantCulture),
        };

        if (expected is null)
        {
            _ = await Assert.ThrowsAsync<InvalidOperationException>(run);
        }
        else
        {
            Assert.Equal(expected, await run());
        }

        Assert.Equal(cancels, connection.Commands.Single().CancelCount);
    }

    /// <summary>
    /// A statement with <c>Returning</c> never cancels its command, whatever run method closes it with rows left (design
    /// §9, §12.2).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - A fake connection that returns three rows.
    ///
    /// Steps:
    /// - Run <c>FirstAsync</c>, <c>FirstOrDefaultAsync</c>, <c>SingleAsync</c>, and <c>SingleOrDefaultAsync</c> of a
    ///   delete with <c>Returning</c>.
    ///
    /// Expected:
    /// - The first row from <c>First…</c>, <see cref="InvalidOperationException"/> from <c>Single…</c>, and no cancel.
    /// </remarks>
    [Fact]
    public async Task RunMethods_ReturningWithRowsLeft_DoNotCancel()
    {
        var connection = new FakeConnection { Rows = [1L, 2L, 3L] };
        var db = new Database<SqliteDialect>(connection);

        Assert.Equal(1L, await db.Delete(Account.Table).AllRows().Returning(a => a.Id).FirstAsync(Ct));
        Assert.Equal(1L, await db.Delete(Account.Table).AllRows().Returning(a => a.Id).FirstOrDefaultAsync(Ct));
        _ = await Assert.ThrowsAsync<InvalidOperationException>(() => db.Delete(Account.Table).AllRows().Returning(a => a.Id).SingleAsync(Ct));
        _ = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            db.Delete(Account.Table).AllRows().Returning(a => a.Id).SingleOrDefaultAsync(Ct)
        );

        Assert.All(connection.Commands, c => Assert.Equal(0, c.CancelCount));
    }
}
