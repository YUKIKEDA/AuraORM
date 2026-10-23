using System.Data;
using static AuraORM.Sql;

namespace AuraORM.Tests;

public sealed class GeneratedTableTests
{
    private static readonly TableAlias Accounts = new(schema: null, table: "accounts", alias: null);

    /// <summary>
    /// The generated table information carries the names and attributes of the declaration (design §3.1, §3.2).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - <see cref="Account"/> with snake case naming, and <see cref="Membership"/> with a schema, a renamed column, a
    ///   composite key, and a foreign key.
    ///
    /// Steps:
    /// - Read <c>Table.Info</c> of both.
    ///
    /// Expected:
    /// - Column names follow the naming, in declaration order, with the extra property last.
    /// - Keys, auto-increment, nullability, lengths, defaults, the index, and the foreign key are as declared.
    /// </remarks>
    [Fact]
    public void TableInfo_GeneratedTables_MatchTheDeclarations()
    {
        TableInfo account = Account.Table.Info;
        Assert.Equal("accounts", account.Name);
        Assert.Null(account.Schema);
        Assert.Equal(["id", "email", "display_name", "status", "created_at", "login_count", "balance"], account.Columns.Select(c => c.Name));
        Assert.Equal(
            [ColumnType.Int64, ColumnType.String, ColumnType.String, ColumnType.Int16, ColumnType.DateTime, ColumnType.Int32, ColumnType.Decimal],
            account.Columns.Select(c => c.Type)
        );
        Assert.True(account.Columns[0].IsPrimaryKey && account.Columns[0].IsAutoIncrement);
        Assert.Equal(200, account.Columns[1].MaxLength);
        Assert.Equal([false, false, true, false, false, false, true], account.Columns.Select(c => c.IsNullable));
        Assert.Same(ColumnDefault.CurrentTimestamp, account.Columns[4].Default);
        Assert.Equal(0, account.Columns[5].Default!.Constant);
        IndexInfo index = Assert.Single(account.Indexes);
        Assert.Equal(new IndexInfo("ix_accounts_email", ["email"], true).Name, index.Name);
        Assert.Equal(["email"], index.Columns);
        Assert.True(index.IsUnique);

        TableInfo membership = Membership.Table.Info;
        Assert.Equal("app", membership.Schema);
        Assert.Equal(["AccountId", "Role", "joined_on"], membership.Columns.Select(c => c.Name));
        Assert.Equal([true, true, false], membership.Columns.Select(c => c.IsPrimaryKey));
        Assert.Equal(new ForeignKeyInfo("accounts", null, "id"), membership.Columns[0].References);
    }

    /// <summary>
    /// The whole-row projection selects every column in declaration order, qualified by the table (design §5.6).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - The columns of <see cref="Account"/> for one occurrence of <c>accounts</c>.
    ///
    /// Steps:
    /// - Collect the select items of the column set and render them on SQLite and SQL Server.
    ///
    /// Expected:
    /// - Seven qualified columns in declaration order.
    /// </remarks>
    [Fact]
    public void SelectItems_WholeRow_EveryColumnInOrder()
    {
        IProjection<Account, PerRow> row = Account.Columns.Create(Accounts);
        var items = new List<SqlNode>();
        row.CollectSelectItems(items);

        string[] names = ["id", "email", "display_name", "status", "created_at", "login_count", "balance"];
        Assert.Equal(
            names.Select(n => "\"accounts\".\"" + n + "\""),
            items.Select(i => SqlRenderContext.Render(DialectRenderer.For<SqliteDialect>(), i).Text)
        );
        Assert.Equal(
            names.Select(n => "[accounts].[" + n + "]"),
            items.Select(i => SqlRenderContext.Render(DialectRenderer.For<SqlServerDialect>(), i).Text)
        );
    }

    /// <summary>
    /// Generated columns are typed expressions that the functions accept, with the CLR type of the member.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - The columns of <see cref="Account"/>.
    ///
    /// Steps:
    /// - Render a condition on the enum column and the nullable display name.
    ///
    /// Expected:
    /// - The SQL uses the column names, and the enum value is bound as its underlying integer.
    /// </remarks>
    [Fact]
    public void Columns_InCondition_RenderAsQualifiedColumns()
    {
        Account.Columns a = Account.Columns.Create(Accounts);

        SqlAssert.Renders(
            And(Eq(a.Status, AccountStatus.Open), IsNotNull(a.DisplayName)),
            new(
                "(\"accounts\".\"status\" = @p0 AND \"accounts\".\"display_name\" IS NOT NULL)",
                "(\"accounts\".\"status\" = $1 AND \"accounts\".\"display_name\" IS NOT NULL)",
                "(`accounts`.`status` = @p0 AND `accounts`.`display_name` IS NOT NULL)",
                "([accounts].[status] = @p0 AND [accounts].[display_name] IS NOT NULL)"
            ),
            (short)1
        );
    }

    /// <summary>
    /// A row is read into the row type, positional members and the extra property included (design §3.4).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - A reader with one row of every column of <see cref="Account"/>; the display name is NULL.
    ///
    /// Steps:
    /// - Read it with the whole-row projection.
    ///
    /// Expected:
    /// - The record equals the row, with a null display name and the balance set.
    /// </remarks>
    [Fact]
    public void ReadRow_FullRow_ReadsEveryMember()
    {
        DateTime created = new(2026, 10, 23, 1, 2, 3, DateTimeKind.Utc);
        using DataTableReader reader = AccountRows((7L, "a@example.com", DBNull.Value, (short)1, created, 3, 12.5m));
        Assert.True(reader.Read());

        Account account = Read<Account>(Account.Columns.Create(Accounts), reader);

        Assert.Equal(new Account(7, "a@example.com", null, AccountStatus.Open, created, 3) { Balance = 12.5m }, account);
    }

    /// <summary>
    /// A non-nullable text column that holds NULL throws, because the schema does not match the declaration.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - A row whose email is NULL.
    ///
    /// Steps:
    /// - Read it with the whole-row projection.
    ///
    /// Expected:
    /// - <see cref="InvalidOperationException"/> that names the column.
    /// </remarks>
    [Fact]
    public void ReadRow_NullInNonNullableText_Throws()
    {
        using DataTableReader reader = AccountRows((7L, DBNull.Value, DBNull.Value, (short)1, DateTime.UnixEpoch, 3, DBNull.Value));
        Assert.True(reader.Read());

        var error = Assert.Throws<InvalidOperationException>(() => Read<Account>(Account.Columns.Create(Accounts), reader));
        Assert.Contains("email", error.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// The nullable column set reads null when the key is null, and the row otherwise (design §5.3).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Two rows: one with every column NULL, one with values.
    ///
    /// Steps:
    /// - Read each with the nullable column set.
    ///
    /// Expected:
    /// - The first is null; the second is the account.
    /// </remarks>
    [Fact]
    public void ReadRow_NullableSide_NullWhenKeyIsNull()
    {
        using DataTableReader reader = AccountRows(
            (DBNull.Value, DBNull.Value, DBNull.Value, DBNull.Value, DBNull.Value, DBNull.Value, DBNull.Value),
            (8L, "b@example.com", "B", (short)0, DateTime.UnixEpoch, 0, DBNull.Value)
        );
        IProjection<Account?, PerRow> nullable = Account.NullableColumns.Create(Accounts);

        Assert.True(reader.Read());
        Assert.Null(Read(nullable, reader));
        Assert.True(reader.Read());
        Assert.Equal(new Account(8, "b@example.com", "B", AccountStatus.Closed, DateTime.UnixEpoch, 0), Read(nullable, reader));
    }

    /// <summary>
    /// The insert model writes only the columns that were set, with their position in the table (design §6.1).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - An <see cref="Account.Insert"/> that sets the email, the status, and the balance.
    ///
    /// Steps:
    /// - Collect its values.
    ///
    /// Expected:
    /// - Positions 1, 3, and 6 with their values; the defaulted and unset columns are absent.
    /// </remarks>
    [Fact]
    public void WriteValues_SomeColumnsSet_WritesOnlyThem()
    {
        var row = new Account.Insert
        {
            Email = "c@example.com",
            Status = AccountStatus.Open,
            Balance = 1m,
        };
        var values = new List<(int Column, ParameterNode Value)>();
        var receiver = new InsertValues(values);

        row.Write(ref receiver);

        Assert.Equal([1, 3, 6], values.Select(v => v.Column));
        Assert.Equal(["c@example.com", (short)1, 1m], values.Select(v => v.Value.ProviderValue));
    }

    /// <summary>
    /// A result projection selects its members in order and reads them into the result type (design §3.5).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - <see cref="NameTotal.RowProjection"/> over the email and the balance of <see cref="Account"/>.
    ///
    /// Steps:
    /// - Collect its select items, then read a row.
    ///
    /// Expected:
    /// - Two items, the email then the balance; the result holds the row's values.
    /// </remarks>
    [Fact]
    public void ResultProjection_Members_SelectedAndReadInOrder()
    {
        Account.Columns a = Account.Columns.Create(Accounts);
        IProjection<NameTotal, PerRow> projection = new NameTotal.RowProjection { Name = a.Email, Total = a.Balance };
        var items = new List<SqlNode>();
        projection.CollectSelectItems(items);

        var table = new DataTable();
        table.Columns.Add("email", typeof(string));
        table.Columns.Add("balance", typeof(decimal));
        table.Rows.Add("d@example.com", 2.5m);
        using DataTableReader reader = table.CreateDataReader();
        Assert.True(reader.Read());

        Assert.Equal(
            ["\"accounts\".\"email\"", "\"accounts\".\"balance\""],
            items.Select(i => SqlRenderContext.Render(DialectRenderer.For<SqliteDialect>(), i).Text)
        );
        Assert.Equal(new NameTotal("d@example.com", 2.5m), Read(projection, reader));
    }

    private static T Read<T>(IProjection<T, PerRow> projection, DataTableReader reader)
    {
        var row = new RowReader(reader);
        return row.Read(projection);
    }

    private static DataTableReader AccountRows(
        params (object Id, object Email, object Name, object Status, object Created, object Logins, object Balance)[] rows
    )
    {
        var table = new DataTable();
        table.Columns.Add("id", typeof(long));
        table.Columns.Add("email", typeof(string));
        table.Columns.Add("display_name", typeof(string));
        table.Columns.Add("status", typeof(short));
        table.Columns.Add("created_at", typeof(DateTime));
        table.Columns.Add("login_count", typeof(int));
        table.Columns.Add("balance", typeof(decimal));
        foreach ((object id, object email, object name, object status, object created, object logins, object balance) in rows)
        {
            table.Rows.Add(id, email, name, status, created, logins, balance);
        }

        return table.CreateDataReader();
    }
}
