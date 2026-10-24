namespace AuraORM.Tests;

public sealed class NamedQueryTypingTests
{
    /// <summary>
    /// Valid named queries compile with no warning, and each has the type the design names: the column set of the result type
    /// or the table, a nullable whole row on the nullable side of an outer join, and the reading of the whole row (design
    /// §3.5, §5.3, §5.11).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - The consumer snippet of <see cref="CompileAssert"/>; <see cref="EmailCount"/> and <see cref="Membership"/>.
    ///
    /// Steps:
    /// - Compile <c>As</c> after a grouped projection, a limit, an offset, a whole row, and a set operation; <c>From</c> and
    ///   <c>With</c> of them; a left join whose whole row is nullable; and a whole row of a named query read as its type.
    ///
    /// Expected:
    /// - No error and no warning.
    /// </remarks>
    [Fact]
    public void NamedQuery_ValidForms_Compile()
    {
        CompileAssert.Compiles(
            """
            NamedQuery<EmailCount.Columns, EmailCount.NullableColumns> totals = sqlite.From(Account.Table)
                .GroupBy(a => a.Email)
                .Select((k, a) => new EmailCount.GroupProjection { Email = k, Count = Count() })
                .As("t");
            NamedQuery<Membership.Columns, Membership.NullableColumns> rows = sqlite.From(Membership.Table).Select(m => m).As("m");
            var limited = sqlite.From(Membership.Table).Select(m => m).OrderBy(m => [Asc(m.AccountId)]).Limit(3).As("l");
            var offset = pg.From(Membership.Table).Select(m => m).Offset(3).As("o");
            var union = sqlite.From(Account.Table).Select(a => new EmailCount.RowProjection { Email = a.Email, Count = a.Id })
                .Union(sqlite.From(Account.Table).Select(a => new EmailCount.RowProjection { Email = a.Email, Count = a.Id }))
                .As("u");
            SelectQuery<SqliteDialect, Executable, EmailCount> whole = sqlite.From(totals).Select(t => t);
            SelectQuery<SqliteDialect, Executable, (string, EmailCount?)> left = sqlite.From(Account.Table)
                .LeftJoin(totals, (a, t) => Eq(t.Email, a.Email))
                .Select((a, t) => (a.Email, t));
            var cte = sqlite.With(totals, rows).From(rows).InnerJoin(totals.As("t2"), (m, t) => Eq(t.Count, m.AccountId)).Select((m, t) => (m.Role, t.Count));
            var builder = new SqlBuilder<SqliteDialect>().With(union).From(union).Select(x => x.Email).ToSql();
            """
        );
    }

    /// <summary>
    /// <c>As</c> on a query whose rows are an expression or a tuple, and on a query with <c>OrderBy</c> but no limit, fails with
    /// CS0619 and the fix (design §5.11, §14.1).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - The consumer snippet of <see cref="NamedQuery_ValidForms_Compile"/>.
    ///
    /// Steps:
    /// - Compile <c>As</c> on a single column, a tuple, a limited tuple, an offset tuple, and an ordered result projection.
    ///
    /// Expected:
    /// - CS0619 for each.
    /// </remarks>
    /// <param name="body">The statement.</param>
    [Theory]
    [InlineData("var q = sqlite.From(Account.Table).Select(a => a.Email).As(\"x\");")]
    [InlineData("var q = sqlite.From(Account.Table).Select(a => (a.Id, a.Email)).As(\"x\");")]
    [InlineData("var q = sqlite.From(Account.Table).Select(a => (a.Id, a.Email)).Limit(1).As(\"x\");")]
    [InlineData("var q = pg.From(Account.Table).Select(a => (a.Id, a.Email)).Offset(1).As(\"x\");")]
    [InlineData(
        "var q = sqlite.From(Account.Table).Select(a => new EmailCount.RowProjection { Email = a.Email, Count = a.Id }).OrderBy(a => [Asc(a.Id)]).As(\"x\");"
    )]
    public void NamedQuery_UnnamedOrOrdered_IsCs0619(string body)
    {
        CompileAssert.Fails(body, "CS0619");
    }
}
