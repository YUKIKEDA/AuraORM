namespace AuraORM.Tests;

public sealed class SelectTypingTests
{
    /// <summary>
    /// Valid SELECT chains compile in a consumer project with no warning, and each stage has the type the design names
    /// (design §5.1, §5.9).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - The consumer snippet of <see cref="CompileAssert"/>, with a database of each dialect.
    ///
    /// Steps:
    /// - Compile a whole row, a tuple, an aggregate projection ordered by an aggregate, an unordered limit on SQLite, an
    ///   unordered offset on PostgreSQL, and an ordered limit with an offset on SQL Server.
    ///
    /// Expected:
    /// - No error and no warning.
    /// </remarks>
    [Fact]
    public void Select_ValidChains_Compile()
    {
        CompileAssert.Compiles(
            """
            SelectProjected<SqliteDialect, Account, PerRow, Account.Columns> rows = sqlite.From(Account.Table).Select(a => a);
            var tuple = sqlite.From(Account.Table).Where(a => Gt(a.LoginCount, 1)).Select(a => (a.Id, a.DisplayName));
            SelectQuery<SqliteDialect, (long, string?)> typed = tuple.OrderBy(a => [Desc(a.Id)]).Limit(3).Offset(1);
            var totals = mysql.From(Account.Table).Select(a => (Count(), Max(a.LoginCount))).OrderBy(a => [Desc(Count())]);
            var firstRows = sqlite.From(Account.Table).Select(a => a.Id).Limit(5);
            var skipped = pg.From(Account.Table).Select(a => a.Id).Offset(5);
            var page = mssql.From(Account.Table).Select(a => a.Id).OrderBy(a => [Asc(a.Id)]).Limit(5).Offset(10);
            var named = mysql.From(Account.Table).Select(a => new NameTotal.RowProjection { Name = a.Email, Total = a.Balance });
            """
        );
    }

    /// <summary>
    /// Clauses in the wrong order, or repeated, do not compile, because the stage after a clause has no method for it
    /// (design §5.1).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - The consumer snippet of <see cref="Select_ValidChains_Compile"/>.
    ///
    /// Steps:
    /// - Compile a second <c>Where</c>, <c>OrderBy</c> before <c>Select</c>, <c>Where</c> after <c>Select</c>,
    ///   <c>OrderBy</c> after <c>Limit</c>, a second <c>Limit</c>, and <c>Limit</c> after <c>Offset</c>.
    ///
    /// Expected:
    /// - Each fails with the diagnostic named in the test case, and nothing else.
    /// </remarks>
    [Theory]
    [InlineData("var q = sqlite.From(Account.Table).Where(a => Gt(a.Id, 1L)).Where(a => Gt(a.Id, 2L));", "CS1061")]
    [InlineData("var q = sqlite.From(Account.Table).OrderBy(a => [Asc(a.Id)]);", "CS1061")]
    [InlineData("var q = sqlite.From(Account.Table).Select(a => a.Id).Where(a => Gt(a.Id, 1L));", "CS1061")]
    [InlineData("var q = sqlite.From(Account.Table).Select(a => a.Id).Limit(1).OrderBy(a => [Asc(a.Id)]);", "CS1061")]
    [InlineData("var q = sqlite.From(Account.Table).Select(a => a.Id).OrderBy(a => [Asc(a.Id)]).Limit(1).Limit(2);", "CS1061")]
    [InlineData("var q = mssql.From(Account.Table).Select(a => a.Id).OrderBy(a => [Asc(a.Id)]).Offset(1).Limit(2);", "CS1061")]
    public void Select_ClauseOrder_IsEnforced(string body, string id)
    {
        CompileAssert.Fails(body, id);
    }

    /// <summary>
    /// A limit or an offset that a dialect does not accept in that place does not compile; the library never adds
    /// <c>ORDER BY</c> or a limit for it (design §5.9, D1-8).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - The consumer snippet of <see cref="Select_ValidChains_Compile"/>.
    ///
    /// Steps:
    /// - Compile an unordered limit on SQL Server, an unordered offset on SQL Server and SQLite, and an ordered offset
    ///   without a limit on SQLite and MySQL.
    ///
    /// Expected:
    /// - Each fails with the diagnostic named in the test case, and nothing else.
    /// </remarks>
    [Theory]
    [InlineData("var q = mssql.From(Account.Table).Select(a => a.Id).Limit(1);", "CS0311")]
    [InlineData("var q = mssql.From(Account.Table).Select(a => a.Id).Offset(1);", "CS0311")]
    [InlineData("var q = sqlite.From(Account.Table).Select(a => a.Id).Offset(1);", "CS0311")]
    [InlineData("var q = sqlite.From(Account.Table).Select(a => a.Id).OrderBy(a => [Asc(a.Id)]).Offset(1);", "CS0311")]
    [InlineData("var q = mysql.From(Account.Table).Select(a => a.Id).OrderBy(a => [Asc(a.Id)]).Offset(1);", "CS0311")]
    public void Select_DialectLimits_AreEnforced(string body, string id)
    {
        CompileAssert.Fails(body, id);
    }

    /// <summary>
    /// A projection or an ordering that mixes row values and aggregates does not compile (design §4.5).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - The consumer snippet of <see cref="Select_ValidChains_Compile"/>.
    ///
    /// Steps:
    /// - Compile a tuple of a column and <c>Count()</c>, a <c>PerRow</c> projection ordered by <c>Count()</c>, a
    ///   <c>PerGroup</c> projection ordered by a column, and an aggregate in <c>Where</c>.
    ///
    /// Expected:
    /// - Each fails with the diagnostic named in the test case, and nothing else.
    /// </remarks>
    [Theory]
    [InlineData("var q = sqlite.From(Account.Table).Select(a => (a.Id, Count()));", "CS0411")]
    [InlineData("var q = sqlite.From(Account.Table).Select(a => a.Id).OrderBy(a => [Desc(Count())]);", "CS0029", "CS1662")]
    [InlineData("var q = sqlite.From(Account.Table).Select(a => Count()).OrderBy(a => [Asc(a.Id)]);", "CS0029", "CS1662")]
    [InlineData("var q = sqlite.From(Account.Table).Where(a => Gt(Count(), 1L));", "CS0029", "CS1662")]
    public void Select_MixedLevels_DoNotCompile(string body, params string[] ids)
    {
        CompileAssert.Fails(body, ids);
    }
}
