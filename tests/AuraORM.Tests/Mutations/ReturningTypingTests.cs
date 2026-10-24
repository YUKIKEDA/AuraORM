namespace AuraORM.Tests;

public sealed class ReturningTypingTests
{
    /// <summary>
    /// <c>Returning</c> takes the projections of a SELECT and has the run methods of a SELECT, on the dialects that can return
    /// the rows (design §9, §12.2).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - The consumer snippet of <see cref="CompileAssert"/>.
    ///
    /// Steps:
    /// - Compile an insert returning a column and running <c>ToListAsync</c>; an update returning a tuple of eight items on
    ///   SQL Server; a delete returning the whole row and running <c>FirstAsync</c>; an insert returning a result type's
    ///   projection on PostgreSQL; an insert and a delete returning on MariaDB; and <c>ToSql</c> from a
    ///   <see cref="SqlBuilder{TDialect}"/>.
    ///
    /// Expected:
    /// - No error and no warning.
    /// </remarks>
    [Fact]
    public void Returning_ValidForms_Compile()
    {
        CompileAssert.Compiles(
            """
            var row = new Account.Insert { Email = "a@example.com", Status = AccountStatus.Open };
            System.Threading.Tasks.Task<List<long>> ids = sqlite.Insert(Account.Table).Values([row, row]).Returning(a => a.Id).ToListAsync();
            var eight = mssql.Update(Account.Table).Set(a => [Set(a.LoginCount, 0)]).AllRows()
                .Returning(a => (a.Id, a.Email, a.DisplayName, a.Status, a.CreatedAt, a.LoginCount, a.Balance, Add(a.LoginCount, 1)));
            ReturningQuery<SqlServerDialect, Executable, (long, string, string?, AccountStatus, DateTime, int, decimal?, int)> typed = eight;
            System.Threading.Tasks.Task<Account> deleted = pg.Delete(Account.Table).Where(a => Eq(a.Id, 1L)).Returning(a => a).FirstAsync();
            var counted = pg.Insert(Account.Table).Values(row).Returning(a => new EmailCount.RowProjection { Email = a.Email, Count = a.Id });
            Database<MariaDbDialect> mariadb = null!;
            var inserted = mariadb.Insert(Account.Table).Values(row).Returning(a => a.Id);
            var removed = mariadb.Delete(Account.Table).AllRows().Returning(a => a.Id);
            SqlStatement sql = new SqlBuilder<SqliteDialect>().Delete(Account.Table).AllRows().Returning(a => a.Id).ToSql();
            """
        );
    }

    /// <summary>
    /// <c>Returning</c> does not compile on a dialect that cannot return the rows: MySQL for every statement, MariaDB for an
    /// update (design §2.4, §9, §14.1).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - A MySQL and a MariaDB database.
    ///
    /// Steps:
    /// - Compile <c>Returning</c> on a MySQL insert, update, and delete, and on a MariaDB update.
    ///
    /// Expected:
    /// - CS0311 for each, the dialect not meeting the capability.
    /// </remarks>
    [Theory]
    [InlineData("mysql.Insert(Account.Table).Values(new Account.Insert { Email = \"a\", Status = AccountStatus.Open }).Returning(a => a.Id);")]
    [InlineData("mysql.Update(Account.Table).Set(a => [Set(a.LoginCount, 0)]).AllRows().Returning(a => a.Id);")]
    [InlineData("mysql.Delete(Account.Table).AllRows().Returning(a => a.Id);")]
    [InlineData(
        "Database<MariaDbDialect> mariadb = null!; mariadb.Update(Account.Table).Set(a => [Set(a.LoginCount, 0)]).AllRows().Returning(a => a.Id);"
    )]
    public void Returning_DialectWithout_DoesNotCompile(string body) => CompileAssert.Fails(body, "CS0311");

    /// <summary>
    /// <c>Returning</c> takes only values of each row: an aggregate does not compile; its result is not a subquery; and a
    /// statement from a <see cref="SqlBuilder{TDialect}"/> cannot run (design §9, §12.3, §14.1).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - The consumer snippet of <see cref="CompileAssert"/>.
    ///
    /// Steps:
    /// - Compile <c>Returning(a =&gt; Count())</c>; <c>In(id, …Returning(a =&gt; a.Id))</c>; <c>ToListAsync</c> on a
    ///   <see cref="SqlOnly"/> statement with <c>Returning</c>.
    ///
    /// Expected:
    /// - CS0266 and CS1662 for the aggregate, which is not <see cref="PerRow"/>; CS1503 for the returning statement, which
    ///   is not a SELECT; CS1929 for the statement, which is not <see cref="Executable"/>.
    /// </remarks>
    [Fact]
    public void Returning_InvalidUses_DoNotCompile()
    {
        CompileAssert.Fails("""sqlite.Delete(Account.Table).AllRows().Returning(a => Count());""", "CS0266", "CS1662");
        CompileAssert.Fails("""Condition<PerRow> c = In(id, sqlite.Delete(Account.Table).AllRows().Returning(a => a.Id));""", "CS1503");
        CompileAssert.Fails("""new SqlBuilder<SqliteDialect>().Delete(Account.Table).AllRows().Returning(a => a.Id).ToListAsync();""", "CS1929");
    }
}
