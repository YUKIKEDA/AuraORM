namespace AuraORM.Tests;

public sealed class MutationTypingTests
{
    /// <summary>
    /// Valid <c>INSERT</c>, <c>UPDATE</c>, and <c>DELETE</c> statements compile with no warning, and <c>ExecuteAsync</c>
    /// returns the row count (design §6–§8).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - The consumer snippet of <see cref="CompileAssert"/>, with a database of each dialect.
    ///
    /// Steps:
    /// - Compile an insert of one row and of a list, an update that assigns a value, an expression, null to a nullable column,
    ///   and a non-nullable expression to a nullable column, with <c>Where</c> and with <c>AllRows()</c>, and a delete with
    ///   each, all run with <c>ExecuteAsync</c>, and the same statements from a <see cref="SqlBuilder{TDialect}"/> with
    ///   <c>ToSql()</c>.
    ///
    /// Expected:
    /// - No error and no warning.
    /// </remarks>
    [Fact]
    public void Mutation_ValidForms_Compile()
    {
        CompileAssert.Compiles(
            """
            System.Threading.Tasks.Task<int> one = sqlite.Insert(Account.Table).Values(new Account.Insert { Email = "a@example.com", Status = AccountStatus.Open }).ExecuteAsync();
            System.Threading.Tasks.Task<int> many = pg.Insert(Account.Table).Values([new Account.Insert { Email = "a@example.com", Status = AccountStatus.Open }, new Account.Insert { Email = "b@example.com", Status = AccountStatus.Open }]).ExecuteAsync();
            System.Threading.Tasks.Task<int> update = mysql.Update(Account.Table)
                .Set(a => [Set(a.Email, "b@example.com"), Set(a.LoginCount, Add(a.LoginCount, 1)), Set(a.DisplayName, null), Set(a.DisplayName, a.Email), Set(a.Balance, Coalesce(a.Balance, 0m))])
                .Where(a => Eq(a.Id, 1L))
                .ExecuteAsync();
            System.Threading.Tasks.Task<int> updateAll = mssql.Update(Account.Table).Set(a => [Set(a.LoginCount, 0)]).AllRows().ExecuteAsync();
            System.Threading.Tasks.Task<int> delete = sqlite.Delete(Account.Table).Where(a => Eq(a.Id, 1L)).ExecuteAsync();
            System.Threading.Tasks.Task<int> deleteAll = pg.Delete(Account.Table).AllRows().ExecuteAsync();
            var sql = new SqlBuilder<SqliteDialect>();
            SqlStatement insertSql = sql.Insert(Account.Table).Values(new Account.Insert { Email = "a@example.com", Status = AccountStatus.Open }).ToSql();
            SqlStatement updateSql = sql.Update(Account.Table).Set(a => [Set(a.LoginCount, 0)]).AllRows().ToSql();
            SqlStatement deleteSql = sql.Delete(Account.Table).Where(a => Eq(a.Id, 1L)).ToSql();
            """
        );
    }

    /// <summary>
    /// An <c>INSERT</c> that leaves out a required column does not compile, and neither does one that sets the
    /// <c>[AutoIncrement]</c> column (design §6.1, §14.1).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - <see cref="Account"/>: <c>Email</c> and <c>Status</c> are required, <c>Id</c> is <c>[AutoIncrement]</c>.
    ///
    /// Steps:
    /// - Compile an insert without <c>Email</c>, and an insert that sets <c>Id</c>.
    ///
    /// Expected:
    /// - CS9035 and CS0117.
    /// </remarks>
    [Fact]
    public void Insert_MissingRequiredOrAutoIncrementColumn_DoesNotCompile()
    {
        CompileAssert.Fails("""sqlite.Insert(Account.Table).Values(new Account.Insert { Status = AccountStatus.Open });""", "CS9035");
        CompileAssert.Fails(
            """sqlite.Insert(Account.Table).Values(new Account.Insert { Id = 1, Email = "a@example.com", Status = AccountStatus.Open });""",
            "CS0117"
        );
    }

    /// <summary>
    /// Only a column can be assigned: an <c>[AutoIncrement]</c> column gives CS0619 with the reason, a column of the nullable
    /// side of a join and an expression give CS1503, and a value of another type gives CS0311 or CS1503 (design §7, §14.1).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - <see cref="Account"/>: <c>Id</c> is <c>[AutoIncrement]</c>, a <see cref="ReadOnlyColumn{T}"/>.
    ///
    /// Steps:
    /// - Compile <c>Set</c> of a value and of an expression on <c>a.Id</c>, <c>Set</c> on a column of
    ///   <c>NullableColumns</c> and on <c>Add(a.LoginCount, 1)</c>, and <c>Set(a.LoginCount, …)</c> with a string, a string
    ///   column, and <c>null</c>.
    ///
    /// Expected:
    /// - CS0619 naming <c>[AutoIncrement]</c> for <c>a.Id</c>; CS1503 for the nullable column, the expression, and
    ///   <c>null</c>; CS0311 for the string and the string column.
    /// </remarks>
    [Fact]
    public void Set_NotAColumnOrWrongType_DoesNotCompile()
    {
        CompileAssert.FailsWith("""sqlite.Update(Account.Table).Set(a => [Set(a.Id, 2L)]);""", "CS0619", "[AutoIncrement]");
        CompileAssert.FailsWith("""sqlite.Update(Account.Table).Set(a => [Set(a.Id, Add(a.Id, 1L))]);""", "CS0619", "[AutoIncrement]");
        CompileAssert.Fails("""sqlite.Update(Account.Table).Set(a => [Set(Account.NullableColumns.Create(null!).LoginCount, 2)]);""", "CS1503");
        CompileAssert.Fails("""sqlite.Update(Account.Table).Set(a => [Set(Add(a.LoginCount, 1), 2)]);""", "CS1503");
        CompileAssert.Fails("""sqlite.Update(Account.Table).Set(a => [Set(a.LoginCount, "x")]);""", "CS0311");
        CompileAssert.Fails("""sqlite.Update(Account.Table).Set(a => [Set(a.LoginCount, a.Email)]);""", "CS0311");
        CompileAssert.Fails("""sqlite.Update(Account.Table).Set(a => [Set(a.LoginCount, null)]);""", "CS1503");
    }

    /// <summary>
    /// <c>null</c> or a nullable expression assigned to a non-nullable reference column compiles with a nullability warning,
    /// as for every reference type (design §7, §14.2).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - <see cref="Account"/>: <c>Email</c> is a non-nullable string, <c>DisplayName</c> a nullable one.
    ///
    /// Steps:
    /// - Compile <c>Set(a.Email, null)</c> and <c>Set(a.Email, a.DisplayName)</c>.
    ///
    /// Expected:
    /// - No error; CS8620 and CS8631.
    /// </remarks>
    [Fact]
    public void Set_NullIntoNonNullableReference_Warns()
    {
        CompileAssert.Warns("""sqlite.Update(Account.Table).Set(a => [Set(a.Email, null)]);""", "CS8620");
        CompileAssert.Warns("""sqlite.Update(Account.Table).Set(a => [Set(a.Email, a.DisplayName)]);""", "CS8631");
    }

    /// <summary>
    /// An <c>UPDATE</c> or <c>DELETE</c> without <c>Where</c> or <c>AllRows()</c> has no <c>ExecuteAsync</c> and no
    /// <c>ToSql</c>, and a statement from a <see cref="SqlBuilder{TDialect}"/> has no <c>ExecuteAsync</c> (design §7, §8,
    /// §12.3, §14.1).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - The consumer snippet of <see cref="CompileAssert"/>.
    ///
    /// Steps:
    /// - Compile <c>ExecuteAsync</c> and <c>ToSql</c> right after <c>Set</c>, and after <c>Delete</c>; and
    ///   <c>ExecuteAsync</c> on complete statements of a <see cref="SqlBuilder{TDialect}"/>.
    ///
    /// Expected:
    /// - CS1061 without a condition; CS1929 on the <see cref="SqlOnly"/> statements.
    /// </remarks>
    [Fact]
    public void Mutation_WithoutConditionOrSqlOnly_CannotRun()
    {
        CompileAssert.Fails("""sqlite.Update(Account.Table).Set(a => [Set(a.LoginCount, 0)]).ExecuteAsync();""", "CS1061");
        CompileAssert.Fails("""sqlite.Update(Account.Table).Set(a => [Set(a.LoginCount, 0)]).ToSql();""", "CS1061");
        CompileAssert.Fails("""sqlite.Delete(Account.Table).ExecuteAsync();""", "CS1061");
        CompileAssert.Fails("""sqlite.Delete(Account.Table).ToSql();""", "CS1061");
        CompileAssert.Fails(
            """new SqlBuilder<SqliteDialect>().Insert(Account.Table).Values(new Account.Insert { Email = "a", Status = AccountStatus.Open }).ExecuteAsync();""",
            "CS1929"
        );
        CompileAssert.Fails("""new SqlBuilder<SqliteDialect>().Delete(Account.Table).AllRows().ExecuteAsync();""", "CS1929");
    }

    /// <summary>
    /// The <c>Where</c> of <c>UPDATE</c> and <c>DELETE</c> does not take a condition that may be empty, the <c>And</c> of a
    /// list (design §4.2, §7, §8, §14.1).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - A list of conditions.
    ///
    /// Steps:
    /// - Compile <c>Where(a =&gt; And(list))</c> on an update and on a delete.
    ///
    /// Expected:
    /// - CS0619, whose message says to write <c>AllRows()</c>, for each.
    /// </remarks>
    [Fact]
    public void Where_OptionalCondition_DoesNotCompile()
    {
        CompileAssert.FailsWith(
            """sqlite.Update(Account.Table).Set(a => [Set(a.LoginCount, 0)]).Where(a => And(new List<Condition<PerRow>> { Eq(a.Id, 1L) }));""",
            "CS0619",
            "AllRows()"
        );
        CompileAssert.FailsWith(
            """sqlite.Delete(Account.Table).Where(a => And(new List<Condition<PerRow>> { Eq(a.Id, 1L) }));""",
            "CS0619",
            "AllRows()"
        );
    }
}
