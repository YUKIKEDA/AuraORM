using System.Data;

namespace AuraORM.Tests;

public sealed class CommandBindingTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>
    /// Parameters carry the placeholder name and the value only, with no type, except on PostgreSQL, where they are positional
    /// and nameless (design §12.4).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - A fake connection that records commands, on SQLite and on PostgreSQL.
    ///
    /// Steps:
    /// - Run a raw command with two values on each.
    ///
    /// Expected:
    /// - SQLite: the names <c>@p0</c> and <c>@p1</c>; PostgreSQL: empty names. The values in order, and no <c>DbType</c> set.
    /// </remarks>
    [Fact]
    public async Task ExecuteAsync_Values_BindsNameAndValueOnly()
    {
        var sqlite = new FakeConnection();
        var postgres = new FakeConnection();

        await new Database<SqliteDialect>(sqlite).ExecuteAsync($"UPDATE t SET a = {1}, b = {"x"}", Ct);
        await new Database<PostgreSqlDialect>(postgres).ExecuteAsync($"UPDATE t SET a = {1}, b = {"x"}", Ct);

        IReadOnlyList<FakeParameter> named = sqlite.Commands.Single().FakeParameters;
        IReadOnlyList<FakeParameter> positional = postgres.Commands.Single().FakeParameters;
        Assert.Equal(["@p0", "@p1"], named.Select(p => p.ParameterName));
        Assert.Equal([string.Empty, string.Empty], positional.Select(p => p.ParameterName));
        Assert.Equal([1, "x"], named.Select(p => p.Value));
        Assert.Equal([1, "x"], positional.Select(p => p.Value));
        Assert.All(named.Concat(positional), p => Assert.False(p.DbTypeSet));
    }

    /// <summary>
    /// A null is sent as <see cref="DBNull.Value"/>; only a null <see cref="byte"/> array also gets
    /// <see cref="DbType.Binary"/> (design §12.4).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - A fake connection that records commands.
    ///
    /// Steps:
    /// - Run a raw command with a null string, a null byte array, and a byte array.
    ///
    /// Expected:
    /// - The values <see cref="DBNull.Value"/>, <see cref="DBNull.Value"/>, and the array; <c>DbType</c> is set only on the
    ///   null array, to <see cref="DbType.Binary"/>.
    /// </remarks>
    [Fact]
    public async Task ExecuteAsync_Nulls_BindDbNullAndTypeOnlyNullBinary()
    {
        var connection = new FakeConnection();
        string? text = null;
        byte[]? none = null;
        byte[] some = [1, 2];

        await new Database<SqlServerDialect>(connection).ExecuteAsync($"UPDATE t SET a = {text}, b = {none}, c = {some}", Ct);

        IReadOnlyList<FakeParameter> parameters = connection.Commands.Single().FakeParameters;
        Assert.Equal([DBNull.Value, DBNull.Value, some], parameters.Select(p => p.Value));
        Assert.Equal([false, true, false], parameters.Select(p => p.DbTypeSet));
        Assert.Equal(DbType.Binary, parameters[1].DbType);
    }

    /// <summary>
    /// The command timeout is set only when <see cref="DatabaseOptions.CommandTimeout"/> is given, and a statement in a
    /// transaction runs in it (design §12.1, §12.5).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Two fake connections, one with a timeout of 7 seconds in the options.
    ///
    /// Steps:
    /// - Run a command on each; run another inside a transaction of the first.
    ///
    /// Expected:
    /// - The timeout is set to 7 only with the option; the command in the transaction has that transaction, the others none.
    /// </remarks>
    [Fact]
    public async Task ExecuteAsync_OptionsAndTransaction_AreSetOnCommand()
    {
        var plain = new FakeConnection();
        var timed = new FakeConnection();
        var db = new Database<SqliteDialect>(plain);

        await db.ExecuteAsync($"DELETE FROM t", Ct);
        await new Database<SqliteDialect>(timed, new DatabaseOptions { CommandTimeout = 7 }).ExecuteAsync($"DELETE FROM t", Ct);
        await db.InTransactionAsync(tx => tx.ExecuteAsync($"DELETE FROM t", Ct), Ct);

        Assert.False(plain.Commands[0].CommandTimeoutSet);
        Assert.True(timed.Commands[0].CommandTimeoutSet);
        Assert.Equal(7, timed.Commands[0].CommandTimeout);
        Assert.Null(plain.Commands[0].TransactionSet);
        Assert.NotNull(plain.Commands[1].TransactionSet);
    }

    /// <summary>
    /// A scalar query that leaves rows unread cancels the command before it closes the reader; one that reads every row does
    /// not (design §12.2).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - A fake connection that returns three rows, and one that returns one row.
    ///
    /// Steps:
    /// - Run <c>QueryScalarAsync</c> on each.
    ///
    /// Expected:
    /// - The first value on both; <c>Cancel</c> called once with rows left, never with none left.
    /// </remarks>
    [Fact]
    public async Task QueryScalarAsync_RowsLeft_CancelsBeforeClosing()
    {
        var many = new FakeConnection { Rows = [1L, 2L, 3L] };
        var one = new FakeConnection { Rows = [1L] };

        Assert.Equal(1L, await new Database<SqliteDialect>(many).QueryScalarAsync<long>($"SELECT value FROM t", Ct));
        Assert.Equal(1L, await new Database<SqliteDialect>(one).QueryScalarAsync<long>($"SELECT value FROM t", Ct));

        Assert.Equal(1, many.Commands.Single().CancelCount);
        Assert.Equal(0, one.Commands.Single().CancelCount);
    }
}
