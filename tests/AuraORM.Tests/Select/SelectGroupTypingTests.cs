namespace AuraORM.Tests;

public sealed class SelectGroupTypingTests
{
    // The key columns, in order, with their types: key i of a statement is column i of table ((i - 1) % tables) + 1.
    private static readonly (string Column, string Type)[] KeyColumns =
    [
        ("Email", "string"),
        ("Id", "long"),
        ("LoginCount", "int"),
        ("Status", "AccountStatus"),
        ("DisplayName", "string?"),
        ("CreatedAt", "DateTime"),
        ("Balance", "decimal?"),
        ("Email", "string"),
    ];

    /// <summary>
    /// Valid grouped chains compile with no warning, and each stage has the type the design names (design §5.1, §5.5).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - The consumer snippet of <see cref="CompileAssert"/>, with a database of each dialect.
    ///
    /// Steps:
    /// - Compile a single key, a tuple of keys with <c>Where</c>, <c>Having</c>, <c>OrderBy</c>, <c>Limit</c>, and
    ///   <c>Offset</c>, a key with a value used in <c>Having</c>, a result type's group projection of a key, a
    ///   <c>SelectDistinct</c> ordered by its item, an optional <c>Having</c>, a run method, and a <c>SqlBuilder</c> statement.
    ///
    /// Expected:
    /// - No error and no warning.
    /// </remarks>
    [Fact]
    public void GroupBy_ValidChains_Compile()
    {
        CompileAssert.Compiles(
            """
            SelectGrouped<SqliteDialect, Executable, IExpr<AccountStatus, PerGroup>, Account.Columns> grouped = sqlite.From(Account.Table).GroupBy(a => a.Status);
            SelectGroupedProjected<SqliteDialect, Executable, (AccountStatus, long), IExpr<AccountStatus, PerGroup>, Account.Columns> counts = grouped.Select((k, a) => (k, Count()));
            SelectQuery<PostgreSqlDialect, Executable, (AccountStatus, string?, long?)> page = pg.From(Account.Table)
                .Where(a => Gt(a.LoginCount, 0))
                .GroupBy(a => (a.Status, a.DisplayName))
                .Having((k, a) => Gt(Count(), 2L))
                .Select((k, a) => (k.Item1, k.Item2, Sum(a.LoginCount)))
                .OrderBy((k, a) => [Desc(Count()), Asc(k.Item1)])
                .Limit(3)
                .Offset(1);
            var keyed = mysql.From(Account.Table).GroupBy(a => Add(a.LoginCount, 1)).Having((k, a) => Gt(k, 3)).Select((k, a) => k);
            var named = mssql.From(Account.Table).GroupBy(a => a.Email).Select((k, a) => new NameTotal.GroupProjection { Name = k, Total = Sum(a.Balance) });
            var distinct = sqlite.From(Account.Table).GroupBy(a => a.Status).SelectDistinct((k, a) => (k, Count())).OrderBy(s => [Desc(s.Item2)]);
            var optional = sqlite.From(Account.Table).GroupBy(a => a.Status).Having((k, a) => And(new List<Condition<PerGroup>?>())).Select((k, a) => Max(a.CreatedAt));
            System.Threading.Tasks.Task<List<long>> run = sqlite.From(Account.Table).GroupBy(a => a.Status).Select((k, a) => Count()).ToListAsync();
            SqlStatement built = new SqlBuilder<SqliteDialect>().From(Account.Table).GroupBy(a => a.Status).Select((k, a) => k).ToSql();
            """
        );
    }

    /// <summary>
    /// After <c>GroupBy</c>, the select list, <c>Having</c>, and <c>OrderBy</c> accept only keys and aggregates, and the key
    /// is not a row value (design §4.5, §14.1).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - The consumer snippet of <see cref="GroupBy_ValidChains_Compile"/>; statements grouped by <c>a.Status</c>.
    ///
    /// Steps:
    /// - Compile a column that is not a key in the select list (alone, beside <c>Count()</c>, and as the whole row), a row
    ///   value in <c>Having</c> and in <c>OrderBy</c>, an aggregate as a key, an aggregate of the key, and <c>k.Item1</c> on a
    ///   single key.
    ///
    /// Expected:
    /// - Each fails with the diagnostics named in the test case, and nothing else.
    /// </remarks>
    [Theory]
    [InlineData("var q = sqlite.From(Account.Table).GroupBy(a => a.Status).Select((k, a) => a.LoginCount);", "CS0029", "CS1662")]
    [InlineData("var q = sqlite.From(Account.Table).GroupBy(a => a.Status).Select((k, a) => (a.LoginCount, Count()));", "CS0029", "CS1662")]
    [InlineData("var q = sqlite.From(Account.Table).GroupBy(a => a.Status).Select((k, a) => a);", "CS0029", "CS1662")]
    [InlineData("var q = sqlite.From(Account.Table).GroupBy(a => a.Status).Having((k, a) => Gt(a.LoginCount, 1));", "CS0029", "CS1662")]
    [InlineData("var q = sqlite.From(Account.Table).GroupBy(a => a.Status).Select((k, a) => k).OrderBy((k, a) => [Asc(a.Id)]);", "CS0029", "CS1662")]
    [InlineData("var q = sqlite.From(Account.Table).GroupBy(a => Count());", "CS0266", "CS1662")]
    [InlineData("var q = sqlite.From(Account.Table).GroupBy(a => a.Status).Select((k, a) => Count(k));", "CS1503")]
    [InlineData("var q = sqlite.From(Account.Table).GroupBy(a => a.Status).Select((k, a) => k.Item1);", "CS1061")]
    public void GroupBy_RowValuesAfterGrouping_DoNotCompile(string body, params string[] ids)
    {
        CompileAssert.Fails(body, ids);
    }

    /// <summary>
    /// <c>GroupBy</c> and <c>Having</c> are written once each, in their place, because the stage after a clause has no
    /// method for it (design §5.1).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - The consumer snippet of <see cref="GroupBy_ValidChains_Compile"/>.
    ///
    /// Steps:
    /// - Compile a second <c>GroupBy</c>, a second <c>Having</c>, <c>Having</c> without <c>GroupBy</c>, <c>Where</c> and a join
    ///   after <c>GroupBy</c>, <c>GroupBy</c> after <c>Having</c>, and <c>GroupBy</c> after <c>Select</c>.
    ///
    /// Expected:
    /// - Each fails with CS1061.
    /// </remarks>
    [Theory]
    [InlineData("var q = sqlite.From(Account.Table).GroupBy(a => a.Status).GroupBy(a => a.Email);")]
    [InlineData("var q = sqlite.From(Account.Table).GroupBy(a => a.Status).Having((k, a) => Gt(Count(), 1L)).Having((k, a) => Gt(Count(), 2L));")]
    [InlineData("var q = sqlite.From(Account.Table).Having(a => Gt(Count(), 1L));")]
    [InlineData("var q = sqlite.From(Account.Table).GroupBy(a => a.Status).Where(a => Gt(a.Id, 1L));")]
    [InlineData("var q = sqlite.From(Account.Table).GroupBy(a => a.Status).CrossJoin(Membership.Table);")]
    [InlineData("var q = sqlite.From(Account.Table).GroupBy(a => a.Status).Having((k, a) => Gt(Count(), 1L)).GroupBy(a => a.Email);")]
    [InlineData("var q = sqlite.From(Account.Table).Select(a => a.Status).GroupBy(a => a.Status);")]
    public void GroupBy_ClauseOrder_IsEnforced(string body)
    {
        CompileAssert.Fails(body, "CS1061");
    }

    /// <summary>
    /// The grouped stages exist for every number of tables and every number of keys, and pass the key and every table's
    /// column set to each lambda. The stage generator writes them per number of tables and per number of keys and items, so
    /// each pair is checked (design §5.1, §5.5).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - A statement of <paramref name="tables"/> tables (<see cref="Account"/> under the aliases t1, t2, …), joined with
    ///   <c>CrossJoin</c>.
    /// - Key i is a column of table ((i - 1) % tables) + 1, with a different type for each i up to 7.
    ///
    /// Steps:
    /// - Group by <paramref name="keys"/> keys, add <c>Having</c> on a count of the last table, select every key, and order
    ///   by the first key; select the keys with <c>SelectDistinct</c> and order by the first item; select a result type's
    ///   group projection (and a count for each other key) with <c>SelectDistinct</c>. Assign each stage to its exact type.
    ///
    /// Expected:
    /// - No error and no warning; a key of the wrong type or a stage of the wrong type would be CS0029 or CS0266.
    /// </remarks>
    /// <param name="tables">The number of tables, 1 to 8.</param>
    /// <param name="keys">The number of keys, 1 to 8.</param>
    [Theory]
    [MemberData(nameof(ArityCases))]
    public void GroupBy_EveryTableAndKeyCount_HasItsStages(int tables, int keys)
    {
        string chain =
            "sqlite.From(Account.Table.As(\"t1\"))"
            + string.Concat(Enumerable.Range(2, tables - 1).Select(i => $".CrossJoin(Account.Table.As(\"t{i}\"))"));
        string views = string.Join(", ", Enumerable.Repeat("Account.Columns", tables));
        string lambda = "(k, " + string.Join(", ", Enumerable.Range(1, tables).Select(i => $"t{i}")) + ")";
        string rowLambda = tables == 1 ? "t1" : "(" + string.Join(", ", Enumerable.Range(1, tables).Select(i => $"t{i}")) + ")";
        string[] keyItems = [.. Enumerable.Range(0, keys).Select(i => $"t{(i % tables) + 1}.{KeyColumns[i].Column}")];
        string[] keyTypes = [.. Enumerable.Range(0, keys).Select(i => KeyColumns[i].Type)];
        string keyType = keys == 1 ? $"IExpr<{keyTypes[0]}, PerGroup>" : "(" + string.Join(", ", keyTypes.Select(t => $"IExpr<{t}, PerGroup>")) + ")";
        string[] keyRefs = keys == 1 ? ["k"] : [.. Enumerable.Range(1, keys).Select(i => $"k.Item{i}")];
        string result = keys == 1 ? keyTypes[0] : "(" + string.Join(", ", keyTypes) + ")";
        string selected = keys == 1 ? "k" : "(" + string.Join(", ", keyRefs) + ")";
        string groupBy = keys == 1 ? keyItems[0] : "(" + string.Join(", ", keyItems) + ")";
        string distinctFirst = keys == 1 ? "s" : "s.Item1";
        string rowsResult = keys == 1 ? "NameTotal" : "(NameTotal, " + string.Join(", ", Enumerable.Repeat("long", keys - 1)) + ")";
        string projection = $"new NameTotal.GroupProjection {{ Name = {keyRefs[0]}, Total = Sum(t1.Balance) }}";
        string rows = keys == 1 ? projection : "(" + projection + ", " + string.Join(", ", Enumerable.Repeat("Count()", keys - 1)) + ")";

        CompileAssert.Compiles(
            $"""
            SelectGrouped<SqliteDialect, Executable, {keyType}, {views}> g = {chain}.GroupBy({rowLambda} => {groupBy});
            SelectHaving<SqliteDialect, Executable, {keyType}, {views}> h = g.Having({lambda} => Gt(Count(t{tables}.Id), 0L));
            SelectGroupedProjected<SqliteDialect, Executable, {result}, {keyType}, {views}> p = h.Select({lambda} => {selected});
            SelectOrdered<SqliteDialect, Executable, {result}> o = p.OrderBy({lambda} => [Asc({keyRefs[0]})]);
            SelectOrdered<SqliteDialect, Executable, {result}> d = g.SelectDistinct({lambda} => {selected}).OrderBy(s => [Asc({distinctFirst})]);
            SelectDistinctRows<SqliteDialect, Executable, {rowsResult}, PerGroup> r = g.SelectDistinct({lambda} => {rows});
            """
        );
    }

    public static TheoryData<int, int> ArityCases()
    {
        var data = new TheoryData<int, int>();
        for (int tables = 1; tables <= 8; tables++)
        {
            for (int keys = 1; keys <= 8; keys++)
            {
                data.Add(tables, keys);
            }
        }

        return data;
    }
}
