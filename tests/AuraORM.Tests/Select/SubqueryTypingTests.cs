namespace AuraORM.Tests;

public sealed class SubqueryTypingTests
{
    /// <summary>
    /// Valid subqueries compile with no warning, and each function has the type the design names (design §5.11).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - The consumer snippet of <see cref="CompileAssert"/>, with a database of each dialect.
    ///
    /// Steps:
    /// - Compile <c>In</c> and <c>NotIn</c> (with a nullable value, and with a subquery of a nullable value type), <c>Exists</c> on each kind of stage (a projection, a
    ///   grouped projection, <c>SelectDistinct</c>, a limit, an offset on PostgreSQL), <c>Scalar</c> of a value type, a
    ///   nullable value type, and a reference type, <c>GroupScalar</c> and <c>GroupExists</c> with <c>Outer(k)</c>, and an
    ///   <c>In</c> with a limit on SQLite and SQL Server.
    ///
    /// Expected:
    /// - No error and no warning.
    /// </remarks>
    [Fact]
    public void Subquery_ValidForms_Compile()
    {
        CompileAssert.Compiles(
            """
            var ids = sqlite.From(Membership.Table).Select(m => m.AccountId);
            Condition<PerRow> inIds = In(id, ids);
            Condition<PerRow> notInIds = NotIn(id, ids);
            Condition<PerRow> nullableIn = In(age, sqlite.From(Account.Table).Select(a => a.LoginCount));
            Condition<PerRow> nullableSubqueryIn = In(id, sqlite.From(Membership.Table).Select(m => Scalar(sqlite.From(Account.Table).Where(a => Eq(a.Id, m.AccountId)).Select(a => a.Id))));
            Condition<PerRow> exists = Exists(ids);
            Condition<PerRow> existsGrouped = Exists(sqlite.From(Membership.Table).GroupBy(m => m.Role).Select((k, m) => k));
            Condition<PerRow> existsDistinct = Exists(sqlite.From(Membership.Table).SelectDistinct(m => m.Role));
            Condition<PerRow> existsLimited = Exists(mysql.From(Membership.Table).Select(m => m.Role).Limit(1));
            Condition<PerRow> existsOffset = Exists(pg.From(Membership.Table).Select(m => m.Role).Offset(1));
            IExpr<long?, PerRow> scalarValue = Scalar(sqlite.From(Account.Table).Select(a => Count()));
            IExpr<int?, PerRow> scalarNullable = Scalar(sqlite.From(Account.Table).Select(a => Max(a.LoginCount)));
            IExpr<string?, PerRow> scalarReference = Scalar(sqlite.From(Account.Table).Select(a => a.Email));
            IExpr<string?, PerRow> scalarNullableReference = Scalar(sqlite.From(Account.Table).Select(a => a.DisplayName));
            var grouped = sqlite.From(Account.Table)
                .GroupBy(a => a.Id)
                .Having((k, a) => And(
                    Le(Count(), GroupScalar(sqlite.From(Membership.Table).Where(m => Eq(m.AccountId, Outer(k))).Select(m => Count()))),
                    GroupExists(sqlite.From(Membership.Table).Where(m => Eq(m.AccountId, Outer(k))).Select(m => m.Role))))
                .Select((k, a) => (k, GroupScalar(sqlite.From(Membership.Table).Where(m => Eq(m.AccountId, Outer(k))).Select(m => Max(m.JoinedOn)))));
            Condition<PerRow> limitedIn = In(id, sqlite.From(Membership.Table).Select(m => m.AccountId).Limit(3));
            Condition<PerRow> orderedLimitedIn = In(id, mssql.From(Membership.Table).Select(m => m.AccountId).OrderBy(m => [Asc(m.AccountId)]).Limit(3));
            """
        );
    }

    /// <summary>
    /// Invalid subqueries do not compile: a row limit in <c>In</c> on MySQL and MariaDB, a subquery with <c>OrderBy</c> but no
    /// limit, a subquery of another type or of a tuple, and a subquery of the wrong evaluation level (design §5.11, §14.1).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - The consumer snippet of <see cref="Subquery_ValidForms_Compile"/>.
    ///
    /// Steps:
    /// - Compile each case.
    ///
    /// Expected:
    /// - Each fails with the diagnostics named in the test case, and nothing else.
    /// </remarks>
    [Theory]
    [InlineData("var c = In(id, mysql.From(Membership.Table).Select(m => m.AccountId).Limit(1));", "CS0311")]
    [InlineData(
        "var c = NotIn(id, mysql.From(Membership.Table).Select(m => m.AccountId).OrderBy(m => [Asc(m.AccountId)]).Limit(1).Offset(1));",
        "CS0311"
    )]
    [InlineData("var c = In(id, sqlite.From(Membership.Table).Select(m => m.AccountId).OrderBy(m => [Asc(m.AccountId)]));", "CS1503")]
    [InlineData("var c = Exists(sqlite.From(Membership.Table).Select(m => m.AccountId).OrderBy(m => [Asc(m.AccountId)]));", "CS0411")]
    [InlineData("var c = Scalar(sqlite.From(Membership.Table).Select(m => m.AccountId).OrderBy(m => [Asc(m.AccountId)]));", "CS0411")]
    [InlineData("var c = In(id, sqlite.From(Membership.Table).Select(m => m.Role));", "CS0453")]
    [InlineData("var c = In(id, sqlite.From(Membership.Table).Select(m => (m.AccountId, m.Role)));", "CS1503")]
    [InlineData(
        "var q = sqlite.From(Account.Table).GroupBy(a => a.Id).Having((k, a) => Gt(Count(), Scalar(sqlite.From(Membership.Table).Select(m => Count()))));",
        "CS0411"
    )]
    [InlineData("var q = sqlite.From(Account.Table).Where(a => GroupExists(sqlite.From(Membership.Table).Select(m => m.Role)));", "CS0029", "CS1662")]
    [InlineData(
        "var q = sqlite.From(Account.Table).GroupBy(a => a.Id).Having((k, a) => GroupExists(sqlite.From(Membership.Table).Where(m => Eq(m.AccountId, k)).Select(m => m.Role)));",
        "CS0411"
    )]
    public void Subquery_InvalidForms_DoNotCompile(string body, params string[] ids)
    {
        CompileAssert.Fails(body, ids);
    }
}
