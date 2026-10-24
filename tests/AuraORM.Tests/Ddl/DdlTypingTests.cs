namespace AuraORM.Tests;

public sealed class DdlTypingTests
{
    /// <summary>
    /// Every DDL statement compiles from a <see cref="Database{TDialect}"/> of every dialect, and from a transaction of
    /// the dialects whose DDL a rollback undoes, with the clauses each dialect has (design §2.4, §12.5, §13).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - The consumer snippet of <see cref="CompileAssert"/>, with transactions of SQLite, PostgreSQL, and SQL Server.
    ///
    /// Steps:
    /// - Compile the four statements on a database of each dialect, on each transaction, and with each clause the dialect
    ///   has, run with <c>ExecuteAsync</c>.
    ///
    /// Expected:
    /// - No diagnostic.
    /// </remarks>
    [Fact]
    public void Ddl_SupportedForms_Compile()
    {
        CompileAssert.Compiles(
            """
            Database<MariaDbDialect> mariadb = null!;
            _ = sqlite.CreateTable(ShopUser.Table).IfNotExists().ExecuteAsync();
            _ = pg.CreateTable(ShopUser.Table).IfNotExists().ExecuteAsync();
            _ = mysql.CreateTable(ShopUser.Table).IfNotExists().ExecuteAsync();
            _ = mariadb.CreateTable(ShopUser.Table).IfNotExists().ExecuteAsync();
            _ = mssql.CreateTable(ShopUser.Table).ExecuteAsync();
            _ = sqlite.CreateIndexes(ShopOrder.Table).IfNotExists().ExecuteAsync();
            _ = pg.CreateIndexes(ShopOrder.Table).IfNotExists().ExecuteAsync();
            _ = mariadb.CreateIndexes(ShopOrder.Table).IfNotExists().ExecuteAsync();
            _ = mysql.CreateIndexes(ShopOrder.Table).ExecuteAsync();
            _ = mssql.CreateIndexes(ShopOrder.Table).ExecuteAsync();
            _ = sqlite.DropIndexes(ShopOrder.Table).IfExists().ExecuteAsync();
            _ = pg.DropIndexes(ShopOrder.Table).IfExists().ExecuteAsync();
            _ = mariadb.DropIndexes(ShopOrder.Table).IfExists().ExecuteAsync();
            _ = mssql.DropIndexes(ShopOrder.Table).IfExists().ExecuteAsync();
            _ = mysql.DropIndexes(ShopOrder.Table).ExecuteAsync();
            _ = sqlite.DropTable(ShopUser.Table).IfExists().ExecuteAsync();
            _ = mysql.DropTable(ShopUser.Table).IfExists().ExecuteAsync();
            _ = mssql.DropTable(ShopUser.Table).IfExists().ExecuteAsync();
            Transaction<SqliteDialect> t1 = null!;
            Transaction<PostgreSqlDialect> t2 = null!;
            Transaction<SqlServerDialect> t3 = null!;
            _ = t1.CreateTable(ShopUser.Table).IfNotExists().ExecuteAsync();
            _ = t1.CreateIndexes(ShopOrder.Table).ExecuteAsync();
            _ = t1.DropIndexes(ShopOrder.Table).ExecuteAsync();
            _ = t1.DropTable(ShopUser.Table).ExecuteAsync();
            _ = t2.CreateTable(ShopUser.Table).ExecuteAsync();
            _ = t2.CreateIndexes(ShopOrder.Table).ExecuteAsync();
            _ = t2.DropIndexes(ShopOrder.Table).IfExists().ExecuteAsync();
            _ = t2.DropTable(ShopUser.Table).ExecuteAsync();
            _ = t3.CreateTable(ShopUser.Table).ExecuteAsync();
            _ = t3.CreateIndexes(ShopOrder.Table).ExecuteAsync();
            _ = t3.DropIndexes(ShopOrder.Table).ExecuteAsync();
            _ = t3.DropTable(ShopUser.Table).IfExists().ExecuteAsync();
            SqlStatement s = new SqlBuilder<SqliteDialect>().CreateTable(ShopUser.Table).ToSql();
            IReadOnlyList<SqlStatement> i = new SqlBuilder<SqliteDialect>().CreateIndexes(ShopOrder.Table).ToSql();
            """
        );
    }

    /// <summary>
    /// The DDL of a transaction does not compile on MySQL and MariaDB, whose DDL commits implicitly (design §12.5, §13.1,
    /// §14.1).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - A transaction of MySQL and one of MariaDB.
    ///
    /// Steps:
    /// - Compile each of the four statements on each.
    ///
    /// Expected:
    /// - CS0311, the dialect not meeting <see cref="ITransactionalDdlDialect"/>.
    /// </remarks>
    /// <param name="body">The snippet.</param>
    [Theory]
    [InlineData("Transaction<MySqlDialect> tx = null!; _ = tx.CreateTable(ShopUser.Table);")]
    [InlineData("Transaction<MySqlDialect> tx = null!; _ = tx.CreateIndexes(ShopOrder.Table);")]
    [InlineData("Transaction<MySqlDialect> tx = null!; _ = tx.DropIndexes(ShopOrder.Table);")]
    [InlineData("Transaction<MySqlDialect> tx = null!; _ = tx.DropTable(ShopUser.Table);")]
    [InlineData("Transaction<MariaDbDialect> tx = null!; _ = tx.CreateTable(ShopUser.Table);")]
    [InlineData("Transaction<MariaDbDialect> tx = null!; _ = tx.CreateIndexes(ShopOrder.Table);")]
    [InlineData("Transaction<MariaDbDialect> tx = null!; _ = tx.DropIndexes(ShopOrder.Table);")]
    [InlineData("Transaction<MariaDbDialect> tx = null!; _ = tx.DropTable(ShopUser.Table);")]
    public void TransactionDdl_MySqlAndMariaDb_DoesNotCompile(string body) => CompileAssert.Fails(body, "CS0311");

    /// <summary>
    /// <c>IF NOT EXISTS</c> and <c>IF EXISTS</c> do not compile on the dialects without them (design §2.4, §13, §14.1).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Databases of MySQL and SQL Server.
    ///
    /// Steps:
    /// - Compile <c>CreateTable(…).IfNotExists()</c> on SQL Server, <c>CreateIndexes(…).IfNotExists()</c> on MySQL and
    ///   SQL Server, and <c>DropIndexes(…).IfExists()</c> on MySQL.
    ///
    /// Expected:
    /// - CS0311 for each.
    /// </remarks>
    /// <param name="body">The snippet.</param>
    [Theory]
    [InlineData("_ = mssql.CreateTable(ShopUser.Table).IfNotExists();")]
    [InlineData("_ = mysql.CreateIndexes(ShopOrder.Table).IfNotExists();")]
    [InlineData("_ = mssql.CreateIndexes(ShopOrder.Table).IfNotExists();")]
    [InlineData("_ = mysql.DropIndexes(ShopOrder.Table).IfExists();")]
    public void DdlClauses_DialectWithout_DoesNotCompile(string body) => CompileAssert.Fails(body, "CS0311");

    /// <summary>
    /// Each clause can be written once, and a statement from a <see cref="SqlBuilder{TDialect}"/> cannot run (design
    /// §12.3, §14.1).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - The consumer snippet of <see cref="CompileAssert"/>.
    ///
    /// Steps:
    /// - Compile each clause written twice, and <c>ExecuteAsync</c> on a statement of <see cref="SqlOnly"/>.
    ///
    /// Expected:
    /// - CS1061 for the second clause; CS1929 for <c>ExecuteAsync</c>.
    /// </remarks>
    /// <param name="body">The snippet.</param>
    /// <param name="id">The diagnostic.</param>
    [Theory]
    [InlineData("_ = sqlite.CreateTable(ShopUser.Table).IfNotExists().IfNotExists();", "CS1061")]
    [InlineData("_ = sqlite.CreateIndexes(ShopOrder.Table).IfNotExists().IfNotExists();", "CS1061")]
    [InlineData("_ = sqlite.DropIndexes(ShopOrder.Table).IfExists().IfExists();", "CS1061")]
    [InlineData("_ = sqlite.DropTable(ShopUser.Table).IfExists().IfExists();", "CS1061")]
    [InlineData("_ = new SqlBuilder<SqliteDialect>().CreateTable(ShopUser.Table).ExecuteAsync();", "CS1929")]
    [InlineData("_ = new SqlBuilder<SqliteDialect>().DropIndexes(ShopOrder.Table).ExecuteAsync();", "CS1929")]
    public void Ddl_InvalidUses_DoNotCompile(string body, string id) => CompileAssert.Fails(body, id);
}
