namespace AuraORM.Tests;

public sealed class UpsertTypingTests
{
    /// <summary>
    /// The upserts of each dialect compile with no warning, run with <c>ExecuteAsync</c>, and take <c>Returning</c> where the
    /// dialect returns rows (design §10).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - The consumer snippet of <see cref="CompileAssert"/>.
    ///
    /// Steps:
    /// - Compile <c>OnConflictDoNothing()</c>, <c>OnConflict(…).DoNothing()</c>, and <c>OnConflict(…).DoUpdate(…)</c> with and
    ///   without <c>Where</c> on SQLite and PostgreSQL, with <c>ExecuteAsync</c> and <c>Returning</c>;
    ///   <c>OnDuplicateKeyUpdate</c> on MySQL with <c>ExecuteAsync</c> and on MariaDB with <c>Returning</c>; and an upsert from
    ///   a <see cref="SqlBuilder{TDialect}"/> with <c>ToSql</c>.
    ///
    /// Expected:
    /// - No error and no warning.
    /// </remarks>
    [Fact]
    public void Upsert_ValidForms_Compile()
    {
        CompileAssert.Compiles(
            """
            var row = new Account.Insert { Email = "a@example.com", Status = AccountStatus.Open };
            System.Threading.Tasks.Task<int> skip = sqlite.Insert(Account.Table).Values(row).OnConflictDoNothing().ExecuteAsync();
            System.Threading.Tasks.Task<int> skipTarget = pg.Insert(Account.Table).Values(row).OnConflict(a => [a.Email]).DoNothing().ExecuteAsync();
            System.Threading.Tasks.Task<int> update = sqlite.Insert(Account.Table).Values(row).OnConflict(a => [a.Email, a.Id])
                .DoUpdate((a, excluded) => [Set(a.DisplayName, excluded.DisplayName)]).ExecuteAsync();
            System.Threading.Tasks.Task<List<long>> returned = pg.Insert(Account.Table).Values([row, row]).OnConflict(a => [a.Email])
                .DoUpdate((a, excluded) => [Set(a.LoginCount, Add(a.LoginCount, 1))])
                .Where((a, excluded) => Lt(a.LoginCount, excluded.LoginCount))
                .Returning(a => a.Id)
                .ToListAsync();
            System.Threading.Tasks.Task<int> duplicate = mysql.Insert(Account.Table).Values(row)
                .OnDuplicateKeyUpdate((a, excluded) => [Set(a.Email, excluded.Email)]).ExecuteAsync();
            Database<MariaDbDialect> mariadb = null!;
            var mariadbReturned = mariadb.Insert(Account.Table).Values(row).OnDuplicateKeyUpdate((a, excluded) => [Set(a.Email, a.Email)]).Returning(a => a.Id);
            SqlStatement sql = new SqlBuilder<PostgreSqlDialect>().Insert(Account.Table).Values(row).OnConflictDoNothing().ToSql();
            """
        );
    }

    /// <summary>
    /// An upsert does not compile on a dialect without its syntax: SQL Server has none, MySQL and MariaDB have no
    /// <c>ON CONFLICT</c>, SQLite and PostgreSQL no <c>ON DUPLICATE KEY UPDATE</c>, and MySQL no <c>Returning</c> (design §10,
    /// §14.1).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - The consumer snippet of <see cref="CompileAssert"/>.
    ///
    /// Steps:
    /// - Compile each upsert on a dialect that lacks it, and <c>Returning</c> after a MySQL upsert.
    ///
    /// Expected:
    /// - CS0311 for each.
    /// </remarks>
    [Theory]
    [InlineData("mssql.Insert(Account.Table).Values(new Account.Insert { Email = \"a\", Status = AccountStatus.Open }).OnConflictDoNothing();")]
    [InlineData(
        "mssql.Insert(Account.Table).Values(new Account.Insert { Email = \"a\", Status = AccountStatus.Open }).OnDuplicateKeyUpdate((a, x) => [Set(a.Email, a.Email)]);"
    )]
    [InlineData("mysql.Insert(Account.Table).Values(new Account.Insert { Email = \"a\", Status = AccountStatus.Open }).OnConflict(a => [a.Email]);")]
    [InlineData(
        "sqlite.Insert(Account.Table).Values(new Account.Insert { Email = \"a\", Status = AccountStatus.Open }).OnDuplicateKeyUpdate((a, x) => [Set(a.Email, a.Email)]);"
    )]
    [InlineData(
        "mysql.Insert(Account.Table).Values(new Account.Insert { Email = \"a\", Status = AccountStatus.Open }).OnDuplicateKeyUpdate((a, x) => [Set(a.Email, a.Email)]).Returning(a => a.Id);"
    )]
    public void Upsert_DialectWithout_DoesNotCompile(string body) => CompileAssert.Fails(body, "CS0311");

    /// <summary>
    /// <c>OnConflict(…)</c> without <c>DoNothing()</c> or <c>DoUpdate(…)</c> cannot run or render; <c>Where</c> follows only
    /// <c>DoUpdate</c>, and does not take a condition that may be empty (design §10.1, §14.1).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - The consumer snippet of <see cref="CompileAssert"/>.
    ///
    /// Steps:
    /// - Compile <c>ExecuteAsync</c> and <c>ToSql</c> right after <c>OnConflict</c>; <c>Where</c> after <c>DoNothing()</c>;
    ///   <c>Where</c> after <c>DoUpdate</c> with the <c>And</c> of a list.
    ///
    /// Expected:
    /// - CS1061 for the first three; CS0619 naming <c>Where</c> for the last.
    /// </remarks>
    [Fact]
    public void Upsert_IncompleteOrOptionalCondition_DoesNotCompile()
    {
        const string start =
            """sqlite.Insert(Account.Table).Values(new Account.Insert { Email = "a", Status = AccountStatus.Open }).OnConflict(a => [a.Email])""";
        CompileAssert.Fails(start + ".ExecuteAsync();", "CS1061");
        CompileAssert.Fails(start + ".ToSql();", "CS1061");
        CompileAssert.Fails(start + ".DoNothing().Where((a, x) => Eq(a.Id, 1L));", "CS1061");
        CompileAssert.FailsWith(
            start + ".DoUpdate((a, x) => [Set(a.Email, a.Email)]).Where((a, x) => And(new List<Condition<PerRow>> { Eq(a.Id, 1L) }));",
            "CS0619",
            "leave out Where"
        );
    }
}
