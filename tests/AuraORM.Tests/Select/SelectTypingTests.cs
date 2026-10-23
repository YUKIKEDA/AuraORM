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
            SelectProjected<SqliteDialect, Executable, Account, PerRow, Account.Columns> rows = sqlite.From(Account.Table).Select(a => a);
            var tuple = sqlite.From(Account.Table).Where(a => Gt(a.LoginCount, 1)).Select(a => (a.Id, a.DisplayName));
            SelectQuery<SqliteDialect, Executable, (long, string?)> typed = tuple.OrderBy(a => [Desc(a.Id)]).Limit(3).Offset(1);
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
    /// <c>ORDER BY</c> or a limit for it (design §5.9, D1-12).
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
    /// A statement from a <see cref="SqlBuilder{TDialect}"/> has no run methods, and the <c>OrderBy</c> of a
    /// <c>SelectDistinct</c> takes only the selected items, and does not exist for a whole row (design §5.7, §12.3).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - The consumer snippet of <see cref="Select_ValidChains_Compile"/>.
    ///
    /// Steps:
    /// - Compile <c>ToListAsync</c> and <c>FirstAsync</c> on builder statements, and a distinct <c>OrderBy</c> by a table column,
    ///   by a function of an item, and on a distinct whole row.
    ///
    /// Expected:
    /// - Each fails with the diagnostic named in the test case, and nothing else.
    /// </remarks>
    [Theory]
    [InlineData("var q = new SqlBuilder<SqliteDialect>().From(Account.Table).Select(a => a.Id).ToListAsync();", "CS1929")]
    [InlineData("var q = new SqlBuilder<SqliteDialect>().From(Account.Table).Select(a => a.Id).OrderBy(a => [Asc(a.Id)]).FirstAsync();", "CS1929")]
    [InlineData("var q = sqlite.From(Account.Table).SelectDistinct(a => a.Status).OrderBy(s => [Asc(s.Id)]);", "CS1061")]
    [InlineData("var q = sqlite.From(Account.Table).SelectDistinct(a => a.Email).OrderBy(s => [Asc(Lower(s))]);", "CS0411")]
    [InlineData("var q = sqlite.From(Account.Table).SelectDistinct(a => a).OrderBy(a => [Asc(a.Id)]);", "CS1061")]
    public void Select_BuilderAndDistinct_AreEnforced(string body, string id)
    {
        CompileAssert.Fails(body, id);
    }

    /// <summary>
    /// The valid forms of a builder statement, of a distinct ordering, and of a list of optional conditions compile
    /// (design §4.2, §5.7, §12.3).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - The consumer snippet of <see cref="Select_ValidChains_Compile"/>.
    ///
    /// Steps:
    /// - Compile <c>ToSql()</c> on a builder statement, a distinct tuple ordered by its items, and <c>Where</c> with the list
    ///   form of <c>And</c>.
    ///
    /// Expected:
    /// - No error and no warning.
    /// </remarks>
    [Fact]
    public void Select_BuilderDistinctAndOptionalWhere_Compile()
    {
        CompileAssert.Compiles(
            """
            SqlStatement s = new SqlBuilder<PostgreSqlDialect>().From(Account.Table).Select(a => a.Id).Offset(5).ToSql();
            var ordered = mysql.From(Account.Table).SelectDistinct(a => (a.Status, a.LoginCount)).OrderBy(x => [Asc(x.Item1), Desc(x.Item2)]);
            string? email = null;
            var filtered = sqlite.From(Account.Table).Where(a => And([email is null ? null : Eq(a.Email, email), Gt(a.LoginCount, 1)])).Select(a => a.Id);
            """
        );
    }

    /// <summary>
    /// Joins compile with the column sets the design gives each lambda: the joined table's ordinary columns in <c>ON</c>, and
    /// nullable column sets for the nullable side of an outer join afterwards (design §5.3).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - The consumer snippet of <see cref="Select_ValidChains_Compile"/>.
    ///
    /// Steps:
    /// - Compile an inner join read as non-nullable values, a left join whose <c>ON</c> uses the joined table's columns as
    ///   non-nullable and whose projection reads the joined side as nullable, a right join, a full join on PostgreSQL and
    ///   SQL Server, and an aliased self-join.
    ///
    /// Expected:
    /// - No error and no warning.
    /// </remarks>
    [Fact]
    public void Join_ValidChains_Compile()
    {
        CompileAssert.Compiles(
            """
            SelectProjected<SqliteDialect, Executable, (string, string), PerRow, Account.Columns, Membership.Columns> inner =
                sqlite.From(Account.Table).InnerJoin(Membership.Table, (a, m) => Eq(m.AccountId, a.Id)).Select((a, m) => (a.Email, m.Role));
            var left = sqlite.From(Account.Table)
                .LeftJoin(Membership.Table, (a, m) => And(Eq(m.AccountId, a.Id), Eq(m.Role, "admin")))
                .Select((a, m) => (a.Email, m.Role, m));
            SelectQuery<SqliteDialect, Executable, (string, string?, Membership?)> leftRows = left;
            SelectQuery<MySqlDialect, Executable, (string?, string)> right =
                mysql.From(Account.Table).RightJoin(Membership.Table, (a, m) => Eq(m.AccountId, a.Id)).Select((a, m) => (a.Email, m.Role));
            var full = pg.From(Account.Table).FullJoin(Membership.Table, (a, m) => Eq(m.AccountId, a.Id)).Select((a, m) => (a.Id, m.Role));
            var fullServer = mssql.From(Account.Table).FullJoin(Membership.Table, (a, m) => Eq(m.AccountId, a.Id)).Select((a, m) => a);
            var self = sqlite.From(Account.Table).InnerJoin(Account.Table.As("b"), (a, b) => Eq(b.Id, a.Id)).Where((a, b) => Gt(b.LoginCount, 1)).Select((a, b) => b.Email);
            """
        );
    }

    /// <summary>
    /// Joins that the design rejects do not compile: <c>FULL JOIN</c> where the dialect has none, a nullable side read as
    /// non-nullable, a join after <c>WHERE</c>, and a ninth table (design §5.1, §5.3, §14.1).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - The consumer snippet of <see cref="Select_ValidChains_Compile"/>.
    ///
    /// Steps:
    /// - Compile each statement of the test case: a full join on MySQL, a nullable side read as a non-nullable value type,
    ///   a join after <c>WHERE</c>, and a ninth table.
    ///
    /// Expected:
    /// - The diagnostic of the test case, and no other error.
    /// </remarks>
    /// <param name="body">The statement.</param>
    /// <param name="id">The expected diagnostic id.</param>
    [Theory]
    [InlineData("var q = mysql.From(Account.Table).FullJoin(Membership.Table, (a, m) => Eq(m.AccountId, a.Id));", "CS0311")]
    [InlineData(
        "SelectQuery<SqliteDialect, Executable, long> q = sqlite.From(Account.Table).LeftJoin(Membership.Table, (a, m) => Eq(m.AccountId, a.Id)).Select((a, m) => m.AccountId);",
        "CS0029"
    )]
    [InlineData(
        "SelectQuery<SqliteDialect, Executable, long> q = sqlite.From(Account.Table).RightJoin(Membership.Table, (a, m) => Eq(m.AccountId, a.Id)).Select((a, m) => a.Id);",
        "CS0029"
    )]
    [InlineData(
        "var q = sqlite.From(Account.Table).Where(a => Gt(a.Id, 1L)).InnerJoin(Membership.Table, (a, m) => Eq(m.AccountId, a.Id));",
        "CS1061"
    )]
    [InlineData(
        "var q = sqlite.From(Account.Table.As(\"t1\")).CrossJoin(Account.Table.As(\"t2\")).CrossJoin(Account.Table.As(\"t3\")).CrossJoin(Account.Table.As(\"t4\")).CrossJoin(Account.Table.As(\"t5\")).CrossJoin(Account.Table.As(\"t6\")).CrossJoin(Account.Table.As(\"t7\")).CrossJoin(Account.Table.As(\"t8\")).CrossJoin(Account.Table.As(\"t9\"));",
        "CS1061"
    )]
    public void Join_InvalidChains_DoNotCompile(string body, string id)
    {
        CompileAssert.Fails(body, id);
    }

    /// <summary>
    /// A string column or a whole row of the nullable side of an outer join read as non-nullable is a nullable warning (CS8619):
    /// a reference type and its nullable form are one type at run time, so only the compiler's nullable analysis can see it,
    /// and a consumer that treats warnings as errors gets an error (design §5.3, §14.1).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - The consumer snippet of <see cref="Select_ValidChains_Compile"/>, with nullable reference types on.
    ///
    /// Steps:
    /// - Compile a left join whose joined string column, and whose joined whole row, is assigned to a statement read as the
    ///   non-nullable type.
    ///
    /// Expected:
    /// - The warning CS8619.
    /// </remarks>
    [Theory]
    [InlineData(
        "SelectQuery<SqliteDialect, Executable, string> q = sqlite.From(Account.Table).LeftJoin(Membership.Table, (a, m) => Eq(m.AccountId, a.Id)).Select((a, m) => m.Role);"
    )]
    [InlineData(
        "SelectQuery<SqliteDialect, Executable, Membership> q = sqlite.From(Account.Table).LeftJoin(Membership.Table, (a, m) => Eq(m.AccountId, a.Id)).Select((a, m) => m);"
    )]
    public void Join_NullableReferenceReadAsNonNullable_Warns(string body)
    {
        CompileAssert.Warns(body, "CS8619");
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
