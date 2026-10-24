namespace AuraORM.Tests;

public sealed class SetOperationTypingTests
{
    /// <summary>
    /// Valid set operations compile with no warning, and each stage has the type the design names (design §5.10, §5.11).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - The consumer snippet of <see cref="CompileAssert"/>, with a database of each dialect.
    ///
    /// Steps:
    /// - Compile a chain of each operator, a grouped and a <c>SelectDistinct</c> operand, an operand from a
    ///   <c>SqlBuilder</c>, <c>OrderBy</c> on one item and on a tuple, an unordered <c>Limit</c> on SQLite, an ordered
    ///   <c>Limit</c> on SQL Server, an unordered <c>Offset</c> on PostgreSQL, and a set operation as the subquery of
    ///   <c>Exists</c>, <c>Scalar</c>, and a limited <c>In</c> on SQLite.
    ///
    /// Expected:
    /// - No error and no warning.
    /// </remarks>
    [Fact]
    public void SetOperation_ValidForms_Compile()
    {
        CompileAssert.Compiles(
            """
            var ids = sqlite.From(Account.Table).Select(a => a.Id);
            var other = sqlite.From(Membership.Table).Select(m => m.AccountId);
            SelectUnion<SqliteDialect, Executable, long, PerRow> union = ids.Union(other).Union(other);
            SelectUnionAll<SqliteDialect, Executable, long, PerRow> unionAll = ids.UnionAll(other).UnionAll(other);
            SelectIntersect<SqliteDialect, Executable, long, PerRow> intersect = ids.Intersect(other);
            SelectExcept<SqliteDialect, Executable, long, PerRow> except = ids.Except(other);
            var counts = sqlite.From(Account.Table).GroupBy(a => a.Status).Select((k, a) => Count()).Union(sqlite.From(Membership.Table).Select(m => Count()));
            var distinct = sqlite.From(Account.Table).SelectDistinct(a => a.Email).Except(new SqlBuilder<SqliteDialect>().From(Membership.Table).Select(m => m.Role));
            SelectOrdered<SqliteDialect, Executable, long> ordered = union.OrderBy(s => [Desc(s)]);
            SelectOrdered<SqliteDialect, Executable, (string, long)> tuple = sqlite.From(Account.Table)
                .Select(a => (a.Email, a.Id))
                .Union(sqlite.From(Membership.Table).Select(m => (m.Role, m.AccountId)))
                .OrderBy(s => [Asc(s.Item1), Desc(s.Item2)]);
            SelectLimited<SqliteDialect, Executable, long> limited = union.Limit(3);
            var page = mssql.From(Account.Table).Select(a => a.Id).Union(mssql.From(Membership.Table).Select(m => m.AccountId)).OrderBy(s => [Asc(s)]).Limit(3).Offset(1);
            var skipped = pg.From(Account.Table).Select(a => a.Id).Union(pg.From(Membership.Table).Select(m => m.AccountId)).Offset(2);
            Condition<PerRow> exists = Exists(union);
            IExpr<long?, PerRow> scalar = Scalar(ids.Union(other));
            Condition<PerRow> inLimited = In(id, union.Limit(2));
            """
        );
    }

    /// <summary>
    /// Invalid set operations do not compile: mixed operators, operands of another result type or dialect, an operand with
    /// <c>OrderBy</c> or <c>Limit</c> or a set operation as an operand (CS0619 with the fix), an unordered <c>Limit</c> on SQL
    /// Server, and a limited set operation in <c>In</c> on MySQL (design §5.10, §5.11, §14.1).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - The consumer snippet of <see cref="SetOperation_ValidForms_Compile"/>.
    ///
    /// Steps:
    /// - Compile each case.
    ///
    /// Expected:
    /// - Each fails with the diagnostics named in the test case, and nothing else.
    /// </remarks>
    [Theory]
    [InlineData(
        "var q = sqlite.From(Account.Table).Select(a => a.Id).Union(sqlite.From(Account.Table).Select(a => a.Id)).Intersect(sqlite.From(Account.Table).Select(a => a.Id));",
        "CS1061"
    )]
    [InlineData("var q = sqlite.From(Account.Table).Select(a => a.Id).Union(sqlite.From(Account.Table).Select(a => a.Email));", "CS1503")]
    [InlineData(
        "var q = sqlite.From(Account.Table).Select(a => a.LoginCount).Union(sqlite.From(Account.Table).Select(a => Max(a.LoginCount)));",
        "CS1503"
    )]
    [InlineData("var q = sqlite.From(Account.Table).Select(a => a.Id).Union(pg.From(Account.Table).Select(a => a.Id));", "CS1503")]
    [InlineData(
        "var q = sqlite.From(Account.Table).Select(a => a.Id).Union(sqlite.From(Account.Table).Select(a => a.Id).OrderBy(a => [Asc(a.Id)]));",
        "CS0619"
    )]
    [InlineData("var q = sqlite.From(Account.Table).Select(a => a.Id).Union(sqlite.From(Account.Table).Select(a => a.Id).Limit(1));", "CS0619")]
    [InlineData(
        "var q = sqlite.From(Account.Table).Select(a => a.Id).Union(sqlite.From(Account.Table).Select(a => a.Id).Union(sqlite.From(Account.Table).Select(a => a.Id)));",
        "CS0619"
    )]
    [InlineData(
        "var q = sqlite.From(Account.Table).Select(a => a.Id).Union(sqlite.From(Account.Table).Select(a => a.Id)).Union(sqlite.From(Account.Table).Select(a => a.Id).Limit(1));",
        "CS0619"
    )]
    [InlineData("var q = mssql.From(Account.Table).Select(a => a.Id).Union(mssql.From(Account.Table).Select(a => a.Id)).Limit(1);", "CS0311")]
    [InlineData("var c = In(id, mysql.From(Account.Table).Select(a => a.Id).Union(mysql.From(Account.Table).Select(a => a.Id)).Limit(1));", "CS0311")]
    [InlineData(
        "var c = Exists(sqlite.From(Account.Table).Select(a => a.Id).Union(sqlite.From(Account.Table).Select(a => a.Id)).OrderBy(s => [Asc(s)]));",
        "CS0619"
    )]
    public void SetOperation_InvalidForms_DoNotCompile(string body, params string[] ids)
    {
        CompileAssert.Fails(body, ids);
    }

    /// <summary>
    /// <c>OrderBy</c> after a set operation exists for every number of items, gives each item its type, and names each item by
    /// its position. The stage generator writes one method per number of items, so each number is checked (design §5.10).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - <see cref="Account"/>, whose columns give items of different types.
    ///
    /// Steps:
    /// - Union two statements that select <paramref name="items"/> columns, order by the last item descending and the first
    ///   ascending, assign the result to its exact type, run the snippet, and render it on SQLite.
    ///
    /// Expected:
    /// - No error and no warning; <c>… ORDER BY {items} DESC, 1 ASC</c>.
    /// </remarks>
    /// <param name="items">The number of items, 1 to 8.</param>
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    public void OrderBy_EveryItemCount_NamesThePositions(int items)
    {
        (string Column, string Type, string Sql)[] columns =
        [
            ("Id", "long", "id"),
            ("Email", "string", "email"),
            ("LoginCount", "int", "login_count"),
            ("Status", "AccountStatus", "status"),
            ("DisplayName", "string?", "display_name"),
            ("CreatedAt", "DateTime", "created_at"),
            ("Id", "long", "id"),
            ("Email", "string", "email"),
        ];
        string selected = items == 1 ? "a.Id" : "(" + string.Join(", ", columns.Take(items).Select(c => "a." + c.Column)) + ")";
        string result = items == 1 ? "long" : "(" + string.Join(", ", columns.Take(items).Select(c => c.Type)) + ")";
        string last = items == 1 ? "s" : "s.Item" + items;
        string first = items == 1 ? "s" : "s.Item1";
        string list = string.Join(", ", columns.Take(items).Select(c => "\"accounts\".\"" + c.Sql + "\""));

        string sql = CompileAssert.Evaluate(
            $"""
            var db = new SqlBuilder<SqliteDialect>();
            SelectOrdered<SqliteDialect, SqlOnly, {result}> q = db.From(Account.Table).Select(a => {selected}).Union(db.From(Account.Table).Select(a => {selected})).OrderBy(s => [Desc({last}), Asc({first})]);
            return q.ToSql().Text;
            """
        );

        Assert.Equal($"SELECT {list} FROM \"accounts\" UNION SELECT {list} FROM \"accounts\" ORDER BY {items} DESC, 1 ASC", sql);
    }
}
