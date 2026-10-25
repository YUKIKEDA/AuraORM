namespace AuraORM.Tests;

public sealed class DdlCheckTests
{
    private static readonly SqlBuilder<SqliteDialect> Sqlite = new();
    private static readonly SqlBuilder<PostgreSqlDialect> PostgreSql = new();
    private static readonly SqlBuilder<MySqlDialect> MySql = new();
    private static readonly SqlBuilder<MariaDbDialect> MariaDb = new();
    private static readonly SqlBuilder<SqlServerDialect> SqlServer = new();

    /// <summary>
    /// SQL Server rejects <c>[Unique]</c> on a column that allows NULL when the <c>CREATE TABLE</c> is built, and
    /// accepts it on a column that does not; the other dialects accept both (design §13.4, §14.3).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - A table with a unique nullable column, and one with a unique column that does not allow NULL.
    ///
    /// Steps:
    /// - Render <c>CreateTable</c> of each on SQL Server, and of the nullable one on PostgreSQL.
    ///
    /// Expected:
    /// - <see cref="InvalidOperationException"/> that names the column and <c>[Index(..., Unique = true)]</c>, for the
    ///   nullable one on SQL Server only.
    /// </remarks>
    [Fact]
    public void CreateTable_UniqueNullableColumn_ThrowsOnSqlServerOnly()
    {
        var nullable = Table("t", new ColumnInfo("c", ColumnType.Int32) { IsUnique = true, IsNullable = true });
        var required = Table("t", new ColumnInfo("c", ColumnType.Int32) { IsUnique = true });
        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => SqlServer.CreateTable(nullable).ToSql());
        Assert.Contains("t.c", error.Message, StringComparison.Ordinal);
        Assert.Contains("[Index(..., Unique = true)]", error.Message, StringComparison.Ordinal);
        Assert.Equal("CREATE TABLE [t] ([c] int NOT NULL, UNIQUE ([c]))", SqlServer.CreateTable(required).ToSql().Text);
        Assert.Equal("CREATE TABLE \"t\" (\"c\" integer, UNIQUE (\"c\"))", PostgreSql.CreateTable(nullable).ToSql().Text);
    }

    /// <summary>
    /// The length of a string or a byte array, the precision, and the scale pass at each dialect's limit and throw one past
    /// it; SQLite has no limit (design §13.7, §14.3).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - The limits of the table of §13.7.
    ///
    /// Steps:
    /// - Render <c>CreateTable</c> of a table with one column at the limit, and at the limit plus one.
    ///
    /// Expected:
    /// - The first renders; the second throws <see cref="InvalidOperationException"/> that names the limit and the dialect.
    /// </remarks>
    /// <param name="dialect">The dialect.</param>
    /// <param name="type">The column type.</param>
    /// <param name="what">What is at the limit: the length, the precision, or the scale.</param>
    /// <param name="limit">The limit.</param>
    [Theory]
    [InlineData("PostgreSQL", ColumnType.String, "length", 10485760)]
    [InlineData("PostgreSQL", ColumnType.Decimal, "precision", 1000)]
    [InlineData("MySQL", ColumnType.String, "length", 16383)]
    [InlineData("MySQL", ColumnType.Decimal, "precision", 65)]
    [InlineData("MySQL", ColumnType.Decimal, "scale", 30)]
    [InlineData("MariaDB", ColumnType.String, "length", 16383)]
    [InlineData("MariaDB", ColumnType.Binary, "length", 65532)]
    [InlineData("MariaDB", ColumnType.Decimal, "precision", 65)]
    [InlineData("MariaDB", ColumnType.Decimal, "scale", 38)]
    [InlineData("SQL Server", ColumnType.String, "length", 4000)]
    [InlineData("SQL Server", ColumnType.Binary, "length", 8000)]
    [InlineData("SQL Server", ColumnType.Decimal, "precision", 38)]
    public void CreateTable_TypeBeyondTheLimit_Throws(string dialect, ColumnType type, string what, int limit)
    {
        ColumnInfo Column(int value) =>
            what switch
            {
                "length" => new ColumnInfo("c", type) { MaxLength = value },
                "precision" => new ColumnInfo("c", type) { Precision = value, Scale = 0 },
                _ => new ColumnInfo("c", type) { Precision = 65, Scale = value },
            };

        _ = CreateTable(dialect, Table("t", Column(limit)));
        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => CreateTable(dialect, Table("t", Column(limit + 1))));
        Assert.Contains(limit.ToString(System.Globalization.CultureInfo.InvariantCulture), error.Message, StringComparison.Ordinal);
        Assert.Contains(dialect, error.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// The dialects without a limit of a type take any length, precision, and scale (design §13.7).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - SQLite; PostgreSQL's byte array and scale; MySQL's byte array.
    ///
    /// Steps:
    /// - Render <c>CreateTable</c> of a column of length 100000000, or of precision 2000 and scale 2000.
    ///
    /// Expected:
    /// - No exception.
    /// </remarks>
    [Fact]
    public void CreateTable_NoLimit_RendersAnySize()
    {
        _ = Sqlite.CreateTable(Table("t", new ColumnInfo("s", ColumnType.String) { MaxLength = 100_000_000 })).ToSql();
        _ = Sqlite.CreateTable(Table("t", new ColumnInfo("d", ColumnType.Decimal) { Precision = 2000, Scale = 2000 })).ToSql();
        _ = PostgreSql.CreateTable(Table("t", new ColumnInfo("b", ColumnType.Binary) { MaxLength = 100_000_000 })).ToSql();
        _ = PostgreSql.CreateTable(Table("t", new ColumnInfo("d", ColumnType.Decimal) { Precision = 1000, Scale = 1000 })).ToSql();
        _ = MySql.CreateTable(Table("t", new ColumnInfo("b", ColumnType.Binary) { MaxLength = 100_000_000 })).ToSql();
    }

    /// <summary>
    /// The bytes of a key on MySQL and MariaDB: 3072 for every key, with the size of each column type; the key at the
    /// limit renders and one byte more throws (design §13.7).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - A string column and a second column whose size makes the sum 3072 or just below, then just above:
    ///   <c>BIGINT</c> (8), <c>DATETIME(6)</c> (8), <c>CHAR(36)</c> (144), <c>DECIMAL(27,0)</c> (12) and
    ///   <c>DECIMAL(28,0)</c> (13), a byte array (n), <c>TIME(6)</c> (6), <c>DATE</c> (3), and <c>DECIMAL(18,2)</c> (9).
    ///
    /// Steps:
    /// - Render the two columns as a primary key, as an index, and the string alone as a unique constraint.
    ///
    /// Expected:
    /// - At 3072 bytes no exception; above, <see cref="InvalidOperationException"/> with the bytes.
    /// </remarks>
    /// <param name="mariaDb">Whether the dialect is MariaDB.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Key_MySqlBytes_ThrowAbove3072(bool mariaDb)
    {
        void Check(ColumnInfo first, ColumnInfo second, bool fits)
        {
            ColumnInfo a = Key(first);
            ColumnInfo b = Key(second);
            var table = new Table<ShopUser, ShopUser.Columns, ShopUser.NullableColumns, ShopUser.Insert>(
                new TableInfo("t", null, [a, b], [new IndexInfo("ix", [a.Name, b.Name], false)])
            );
            var indexTable = new Table<ShopUser, ShopUser.Columns, ShopUser.NullableColumns, ShopUser.Insert>(
                new TableInfo("t", null, [first, second], [new IndexInfo("ix", [first.Name, second.Name], false)])
            );
            Action create = mariaDb ? () => MariaDb.CreateTable(table).ToSql() : () => MySql.CreateTable(table).ToSql();
            Action indexes = mariaDb ? () => MariaDb.CreateIndexes(indexTable).ToSql() : () => MySql.CreateIndexes(indexTable).ToSql();
            if (fits)
            {
                create();
                indexes();
            }
            else
            {
                Assert.Contains("3072", Assert.Throws<InvalidOperationException>(create).Message, StringComparison.Ordinal);
                Assert.Contains("3072", Assert.Throws<InvalidOperationException>(indexes).Message, StringComparison.Ordinal);
            }
        }

        ColumnInfo Text(int n) => new("a", ColumnType.String) { MaxLength = n };
        Check(Text(766), new("b", ColumnType.Int64), fits: true);
        Check(Text(767), new("b", ColumnType.Int64), fits: false);
        Check(Text(766), new("b", ColumnType.DateTime), fits: true);
        Check(Text(767), new("b", ColumnType.DateTimeOffset), fits: false);
        Check(Text(732), new("b", ColumnType.Guid), fits: true);
        Check(Text(733), new("b", ColumnType.Guid), fits: false);
        Check(Text(765), new("b", ColumnType.Decimal) { Precision = 27, Scale = 0 }, fits: true);
        Check(Text(765), new("b", ColumnType.Decimal) { Precision = 28, Scale = 0 }, fits: false);
        Check(Text(765), new("b", ColumnType.Decimal) { Precision = 18, Scale = 2 }, fits: true);
        Check(Text(766), new("b", ColumnType.Decimal) { Precision = 18, Scale = 2 }, fits: false);
        Check(Text(766), new("b", ColumnType.TimeOnly), fits: true);
        Check(Text(767), new("b", ColumnType.DateOnly), fits: true);
        Check(Text(768), new("b", ColumnType.DateOnly), fits: false);
        Check(new("a", ColumnType.Binary) { MaxLength = 3064 }, new("b", ColumnType.Int64), fits: true);
        Check(new("a", ColumnType.Binary) { MaxLength = 3065 }, new("b", ColumnType.Int64), fits: false);

        var unique = Table("t", new ColumnInfo("a", ColumnType.String) { MaxLength = 768, IsUnique = true });
        var tooLong = Table("t", new ColumnInfo("a", ColumnType.String) { MaxLength = 769, IsUnique = true });
        if (mariaDb)
        {
            _ = MariaDb.CreateTable(unique).ToSql();
            _ = Assert.Throws<InvalidOperationException>(() => MariaDb.CreateTable(tooLong).ToSql());
        }
        else
        {
            _ = MySql.CreateTable(unique).ToSql();
            _ = Assert.Throws<InvalidOperationException>(() => MySql.CreateTable(tooLong).ToSql());
        }
    }

    /// <summary>
    /// The bytes of a key on SQL Server: 900 for the primary key and 1700 for a unique constraint and an index (design
    /// §13.7).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - <c>nvarchar(446)</c> and <c>bigint</c> (900), <c>nvarchar(445)</c> and <c>datetimeoffset</c> (900),
    ///   <c>nvarchar(842)</c> and <c>uniqueidentifier</c> (1700), <c>varbinary(1700)</c>, <c>nvarchar(850)</c>, and
    ///   <c>nvarchar(441)</c> and <c>decimal(19,0)</c> (891), each and one step more.
    ///
    /// Steps:
    /// - Render the primary keys, the index, and the unique constraint.
    ///
    /// Expected:
    /// - At the limit no exception; above, <see cref="InvalidOperationException"/> with the limit.
    /// </remarks>
    [Fact]
    public void Key_SqlServerBytes_ThrowAboveTheLimit()
    {
        void Key2(ColumnInfo a, ColumnInfo b, bool fits)
        {
            var table = Table("t", a, b);
            if (fits)
            {
                _ = SqlServer.CreateTable(table).ToSql();
            }
            else
            {
                Assert.Contains(
                    "900",
                    Assert.Throws<InvalidOperationException>(() => SqlServer.CreateTable(table).ToSql()).Message,
                    StringComparison.Ordinal
                );
            }
        }

        Key2(Key(new("a", ColumnType.String) { MaxLength = 446 }), Key(new("b", ColumnType.Int64)), fits: true);
        Key2(Key(new("a", ColumnType.String) { MaxLength = 447 }), Key(new("b", ColumnType.Int64)), fits: false);
        Key2(Key(new("a", ColumnType.String) { MaxLength = 445 }), Key(new("b", ColumnType.DateTimeOffset)), fits: true);
        Key2(Key(new("a", ColumnType.String) { MaxLength = 446 }), Key(new("b", ColumnType.DateTimeOffset)), fits: false);
        Key2(Key(new("a", ColumnType.String) { MaxLength = 445 }), Key(new("b", ColumnType.Decimal) { Precision = 19, Scale = 0 }), fits: true);
        Key2(Key(new("a", ColumnType.String) { MaxLength = 446 }), Key(new("b", ColumnType.Decimal) { Precision = 19, Scale = 0 }), fits: false);

        void Index(ColumnInfo[] columns, bool fits)
        {
            var table = new Table<ShopUser, ShopUser.Columns, ShopUser.NullableColumns, ShopUser.Insert>(
                new TableInfo("t", null, columns, [new IndexInfo("ix", columns.Select(c => c.Name).ToArray(), false)])
            );
            if (fits)
            {
                _ = SqlServer.CreateIndexes(table).ToSql();
            }
            else
            {
                Assert.Contains(
                    "1700",
                    Assert.Throws<InvalidOperationException>(() => SqlServer.CreateIndexes(table).ToSql()).Message,
                    StringComparison.Ordinal
                );
            }
        }

        Index([new("a", ColumnType.String) { MaxLength = 842 }, new("b", ColumnType.Guid)], fits: true);
        Index([new("a", ColumnType.String) { MaxLength = 843 }, new("b", ColumnType.Guid)], fits: false);
        Index([new("a", ColumnType.Binary) { MaxLength = 1700 }], fits: true);
        Index([new("a", ColumnType.Binary) { MaxLength = 1701 }], fits: false);

        _ = SqlServer.CreateTable(Table("t", new ColumnInfo("a", ColumnType.String) { MaxLength = 850, IsUnique = true })).ToSql();
        var tooLong = Table("t", new ColumnInfo("a", ColumnType.String) { MaxLength = 851, IsUnique = true });
        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => SqlServer.CreateTable(tooLong).ToSql());
        Assert.Contains("1700", error.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// SQL Server's size of a decimal in a key follows its precision: 5, 9, 13, and 17 bytes for precisions 1–9, 10–19,
    /// 20–28, and 29–38, at both ends of each range (design §13.7).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - A primary key of a string and two decimals of the precision, whose sizes add up to exactly 900 bytes.
    ///
    /// Steps:
    /// - Render <c>CreateTable</c>, then with the string one character longer.
    ///
    /// Expected:
    /// - The first renders; the second throws <see cref="InvalidOperationException"/>.
    /// </remarks>
    /// <param name="precision">The precision of the decimals.</param>
    /// <param name="length">The length of the string that makes the key 900 bytes.</param>
    [Theory]
    [InlineData(1, 445)]
    [InlineData(9, 445)]
    [InlineData(10, 441)]
    [InlineData(19, 441)]
    [InlineData(20, 437)]
    [InlineData(28, 437)]
    [InlineData(29, 433)]
    [InlineData(38, 433)]
    public void Key_SqlServerDecimal_HasTheSizeOfItsPrecision(int precision, int length)
    {
        Table<ShopUser, ShopUser.Columns, ShopUser.NullableColumns, ShopUser.Insert> Key3(int n) =>
            Table(
                "t",
                Key(new("a", ColumnType.String) { MaxLength = n }),
                Key(new("b", ColumnType.Decimal) { Precision = precision, Scale = 0 }),
                Key(new("c", ColumnType.Decimal) { Precision = precision, Scale = 0 })
            );

        _ = SqlServer.CreateTable(Key3(length)).ToSql();
        _ = Assert.Throws<InvalidOperationException>(() => SqlServer.CreateTable(Key3(length + 1)).ToSql());
    }

    /// <summary>
    /// MySQL's size of a <c>DECIMAL(p,s)</c> in a key: four bytes per nine digits of each part and 0 to 4 bytes for the
    /// rest, for every number of digits (design §13.7).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - An index of a byte array and a decimal, for precisions 1 to 18 with scale 0, and with scales 1 to 9.
    ///
    /// Steps:
    /// - Compute the decimal's size by hand: 4 per nine digits, and 0, 1, 1, 2, 2, 3, 3, 4, 4 bytes for 0 to 8 leftover
    ///   digits, for the integer part and the fraction each; render <c>CreateIndexes</c> with the byte array of 3072 minus
    ///   that size, then one byte longer.
    ///
    /// Expected:
    /// - The first renders; the second throws <see cref="InvalidOperationException"/>.
    /// </remarks>
    [Fact]
    public void Key_MySqlDecimal_HasTheSizeOfItsDigits()
    {
        int[] leftover = [0, 1, 1, 2, 2, 3, 3, 4, 4];
        int Bytes(int digits) => (digits / 9 * 4) + leftover[digits % 9];
        var cases = Enumerable.Range(1, 18).Select(p => (p, 0)).Concat(Enumerable.Range(1, 9).Select(scale => (18, scale)));
        foreach ((int precision, int scale) in cases)
        {
            int size = Bytes(precision - scale) + Bytes(scale);
            Table<ShopUser, ShopUser.Columns, ShopUser.NullableColumns, ShopUser.Insert> Index(int n) =>
                Wrap(
                    new TableInfo(
                        "t",
                        null,
                        [new("a", ColumnType.Binary) { MaxLength = n }, new("b", ColumnType.Decimal) { Precision = precision, Scale = scale }],
                        [new IndexInfo("ix", ["a", "b"], false)]
                    )
                );
            _ = MySql.CreateIndexes(Index(3072 - size)).ToSql();
            _ = Assert.Throws<InvalidOperationException>(() => MySql.CreateIndexes(Index(3073 - size)).ToSql());
        }
    }

    /// <summary>
    /// PostgreSQL and SQLite have no declared limit of a key, and take any key (design §13.7, §14.2).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - A primary key of a string of 100000 and an index of it.
    ///
    /// Steps:
    /// - Render <c>CreateTable</c> and <c>CreateIndexes</c> on PostgreSQL and SQLite.
    ///
    /// Expected:
    /// - No exception.
    /// </remarks>
    [Fact]
    public void Key_PostgreSqlAndSqlite_HaveNoLimit()
    {
        ColumnInfo a = Key(new("a", ColumnType.String) { MaxLength = 100_000 });
        var table = new Table<ShopUser, ShopUser.Columns, ShopUser.NullableColumns, ShopUser.Insert>(
            new TableInfo("t", null, [a], [new IndexInfo("ix", ["a"], true)])
        );
        _ = PostgreSql.CreateTable(table).ToSql();
        _ = PostgreSql.CreateIndexes(table).ToSql();
        _ = Sqlite.CreateTable(table).ToSql();
        _ = Sqlite.CreateIndexes(table).ToSql();
    }

    /// <summary>
    /// A name at each dialect's limit renders, and one past it throws, in each place a DDL statement writes a name: the
    /// schema, the table, a column, an index, and a referenced table and column (design §13.7, §14.3).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - PostgreSQL: 63 bytes of UTF-8 (63 ASCII letters, 21 × U+3042) and 64 (64 letters, 22 × U+3042).
    /// - MySQL and MariaDB: 64 characters (64 × U+3042) and 65.
    /// - SQL Server: 128 UTF-16 units (128 letters, 64 × U+1F600) and more (129 letters, 65 × U+1F600).
    ///
    /// Steps:
    /// - Put the name in each place, and render <c>CreateTable</c>, <c>CreateIndexes</c>, <c>DropIndexes</c>, and
    ///   <c>DropTable</c>.
    ///
    /// Expected:
    /// - The name at the limit renders in every place; the longer name throws <see cref="InvalidOperationException"/> that
    ///   names it, in every place.
    /// </remarks>
    /// <param name="dialect">The dialect.</param>
    /// <param name="fitChar">The character of the name at the limit.</param>
    /// <param name="fitCount">The number of those characters.</param>
    /// <param name="longChar">The character of the name past the limit.</param>
    /// <param name="longCount">The number of those characters.</param>
    [Theory]
    [InlineData("PostgreSQL", 'a', 63, 'a', 64)]
    [InlineData("PostgreSQL", 'あ', 21, 'あ', 22)]
    [InlineData("MySQL", 'あ', 64, 'a', 65)]
    [InlineData("MariaDB", 'あ', 64, 'あ', 65)]
    [InlineData("SQL Server", 'a', 128, 'a', 129)]
    public void Names_BeyondTheLimit_Throw(string dialect, char fitChar, int fitCount, char longChar, int longCount)
    {
        string fits = new(fitChar, fitCount);
        string tooLong = new(longChar, longCount);
        foreach ((Func<string, TableInfo> place, Writes writes) in Places())
        {
            RenderAll(dialect, place(fits), writes);
            Assert.All(Renders(dialect, place(tooLong), writes), render => Assert.Throws<InvalidOperationException>(render));
        }
    }

    /// <summary>
    /// SQL Server counts a name in UTF-16 units, so 64 characters outside the Basic Multilingual Plane fit and 65 do not;
    /// SQLite has no limit (design §13.7).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - 64 and 65 × U+1F600 (128 and 130 UTF-16 units); a name of 1000 letters.
    ///
    /// Steps:
    /// - Render <c>CreateTable</c> of a table with that name on SQL Server, and of the long name on SQLite.
    ///
    /// Expected:
    /// - 64 renders and 65 throws on SQL Server; SQLite renders the long name.
    /// </remarks>
    [Fact]
    public void Names_SqlServerUtf16AndSqlite_CountAsTheDesignSays()
    {
        string emoji = char.ConvertFromUtf32(0x1F600);
        _ = SqlServer.CreateTable(Table(string.Concat(Enumerable.Repeat(emoji, 64)), new ColumnInfo("c", ColumnType.Int32))).ToSql();
        _ = Assert.Throws<InvalidOperationException>(() =>
            SqlServer.CreateTable(Table(string.Concat(Enumerable.Repeat(emoji, 65)), new ColumnInfo("c", ColumnType.Int32))).ToSql()
        );
        foreach ((Func<string, TableInfo> place, Writes writes) in Places())
        {
            RenderAll("SQLite", place(new string('a', 1000)), writes);
        }
    }

    /// <summary>
    /// MySQL and MariaDB reject a name with U+0000 or a character outside the Basic Multilingual Plane; the other dialects
    /// accept the character outside it (design §13.7).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - A column named U+1F600, and one with U+0000.
    ///
    /// Steps:
    /// - Render <c>CreateTable</c> on MySQL, MariaDB, PostgreSQL, and SQL Server.
    ///
    /// Expected:
    /// - <see cref="InvalidOperationException"/> on MySQL and MariaDB for both; the others render U+1F600.
    /// </remarks>
    [Fact]
    public void Names_MySqlCharacters_RejectNulAndSupplementary()
    {
        string emoji = char.ConvertFromUtf32(0x1F600);
        foreach (string name in new[] { emoji, "a\0b" })
        {
            var table = Table("t", new ColumnInfo(name, ColumnType.Int32));
            _ = Assert.Throws<InvalidOperationException>(() => MySql.CreateTable(table).ToSql());
            _ = Assert.Throws<InvalidOperationException>(() => MariaDb.CreateTable(table).ToSql());
        }

        _ = PostgreSql.CreateTable(Table("t", new ColumnInfo(emoji, ColumnType.Int32))).ToSql();
        _ = SqlServer.CreateTable(Table("t", new ColumnInfo(emoji, ColumnType.Int32))).ToSql();
    }

    /// <summary>
    /// The name MySQL and MariaDB give a foreign key, <c>table_ibfk_N</c>, must fit in 64 characters on MySQL and 63 on
    /// MariaDB, counted with the number of the last foreign key (design §13.7).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Tables with one foreign key and with ten.
    ///
    /// Steps:
    /// - Render <c>CreateTable</c> of a table name at the longest length that fits, and one character longer.
    ///
    /// Expected:
    /// - MySQL: 57 characters with one foreign key and 56 with ten fit, 58 and 57 throw.
    /// - MariaDB: 56 with one and 55 with ten fit, 57 and 56 throw.
    /// - PostgreSQL takes the longer names.
    /// </remarks>
    [Fact]
    public void ForeignKeyNames_MySqlAndMariaDb_FitTheLimit()
    {
        TableInfo WithForeignKeys(int nameLength, int count) =>
            new(
                new string('a', nameLength),
                null,
                Enumerable
                    .Range(1, count)
                    .Select(i => new ColumnInfo("c" + i, ColumnType.Int64) { References = new ForeignKeyInfo("p", null, "id") })
                    .ToArray(),
                []
            );

        void Check(Func<TableInfo, SqlStatement> render, int nameLength, int count)
        {
            _ = render(WithForeignKeys(nameLength, count));
            InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => render(WithForeignKeys(nameLength + 1, count)));
            Assert.Contains("_ibfk_N", error.Message, StringComparison.Ordinal);
        }

        Check(t => MySql.CreateTable(Wrap(t)).ToSql(), 57, 1);
        Check(t => MySql.CreateTable(Wrap(t)).ToSql(), 56, 10);
        Check(t => MariaDb.CreateTable(Wrap(t)).ToSql(), 56, 1);
        Check(t => MariaDb.CreateTable(Wrap(t)).ToSql(), 55, 10);
        _ = PostgreSql.CreateTable(Wrap(WithForeignKeys(58, 1))).ToSql();
    }

    /// <summary>
    /// A table in a schema that references a table without one: MySQL and MariaDB throw, PostgreSQL and SQL Server write
    /// the referenced table unqualified; SQLite throws for any reference into another schema and writes REFERENCES
    /// without a schema (design §13.6, §14.3).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - <see cref="Membership"/> in <c>app</c>, which references <see cref="Account"/> without a schema;
    ///   <see cref="Audit"/> in <c>app</c>, which references <see cref="ArchivedAccount"/> in <c>archive</c>; a table in
    ///   <c>main</c> that references a table in <c>main</c>.
    ///
    /// Steps:
    /// - Render <c>CreateTable</c> of each.
    ///
    /// Expected:
    /// - MySQL and MariaDB throw for <see cref="Membership"/> and render <see cref="Audit"/>.
    /// - PostgreSQL and SQL Server render <c>REFERENCES "accounts" ("id")</c> for <see cref="Membership"/>.
    /// - SQLite throws for both, and renders the table in <c>main</c> with <c>REFERENCES "p" ("id")</c>.
    /// </remarks>
    [Fact]
    public void CreateTable_ReferenceAcrossSchemas_FollowsEachDialect()
    {
        _ = Assert.Throws<InvalidOperationException>(() => MySql.CreateTable(Membership.Table).ToSql());
        _ = Assert.Throws<InvalidOperationException>(() => MariaDb.CreateTable(Membership.Table).ToSql());
        _ = MySql.CreateTable(Audit.Table).ToSql();
        Assert.EndsWith(
            "FOREIGN KEY (\"AccountId\") REFERENCES \"accounts\" (\"id\"))",
            PostgreSql.CreateTable(Membership.Table).ToSql().Text,
            StringComparison.Ordinal
        );
        Assert.EndsWith(
            "FOREIGN KEY ([AccountId]) REFERENCES [accounts] ([id]))",
            SqlServer.CreateTable(Membership.Table).ToSql().Text,
            StringComparison.Ordinal
        );
        _ = Assert.Throws<InvalidOperationException>(() => Sqlite.CreateTable(Membership.Table).ToSql());
        _ = Assert.Throws<InvalidOperationException>(() => Sqlite.CreateTable(Audit.Table).ToSql());
        var same = Wrap(
            new TableInfo("c", "main", [new ColumnInfo("p", ColumnType.Int64) { References = new ForeignKeyInfo("p", "main", "id") }], [])
        );
        Assert.Equal(
            "CREATE TABLE \"main\".\"c\" (\"p\" INTEGER NOT NULL, FOREIGN KEY (\"p\") REFERENCES \"p\" (\"id\"))",
            Sqlite.CreateTable(same).ToSql().Text
        );
    }

    /// <summary>
    /// MySQL and MariaDB reject a string default with a backslash; the other dialects write it as it is (design §13.3).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - A column with the default <c>a\b</c>.
    ///
    /// Steps:
    /// - Render <c>CreateTable</c> on every dialect.
    ///
    /// Expected:
    /// - <see cref="InvalidOperationException"/> naming <c>NO_BACKSLASH_ESCAPES</c> on MySQL and MariaDB;
    ///   <c>DEFAULT 'a\b'</c> on SQLite and PostgreSQL, <c>DEFAULT N'a\b'</c> on SQL Server.
    /// </remarks>
    [Fact]
    public void CreateTable_BackslashDefault_ThrowsOnMySqlAndMariaDb()
    {
        var table = Table("t", new ColumnInfo("s", ColumnType.String) { MaxLength = 10, Default = ColumnDefault.FromConstant("a\\b") });
        Assert.Contains(
            "NO_BACKSLASH_ESCAPES",
            Assert.Throws<InvalidOperationException>(() => MySql.CreateTable(table).ToSql()).Message,
            StringComparison.Ordinal
        );
        _ = Assert.Throws<InvalidOperationException>(() => MariaDb.CreateTable(table).ToSql());
        Assert.Equal("CREATE TABLE \"t\" (\"s\" TEXT NOT NULL DEFAULT 'a\\b')", Sqlite.CreateTable(table).ToSql().Text);
        Assert.Equal("CREATE TABLE \"t\" (\"s\" varchar(10) NOT NULL DEFAULT 'a\\b')", PostgreSql.CreateTable(table).ToSql().Text);
        Assert.Equal("CREATE TABLE [t] ([s] nvarchar(10) NOT NULL DEFAULT N'a\\b')", SqlServer.CreateTable(table).ToSql().Text);
    }

    /// <summary>
    /// DDL takes the table itself: a table with an alias throws <see cref="ArgumentException"/> from every entry point
    /// (design §14.3).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - <c>ShopUser.Table.As("u")</c>.
    ///
    /// Steps:
    /// - Start each of the four statements from <see cref="SqlBuilder{TDialect}"/>, <see cref="Database{TDialect}"/>, and
    ///   a transaction of SQLite.
    ///
    /// Expected:
    /// - <see cref="ArgumentException"/> for the parameter <c>table</c> each time.
    /// </remarks>
    [Fact]
    public async Task Statements_AliasedTable_ThrowArgumentException()
    {
        var aliased = ShopUser.Table.As("u");
        var db = new Database<SqliteDialect>(new FakeConnection());
        await using Transaction<SqliteDialect> tx = await db.BeginTransactionAsync(cancellationToken: TestContext.Current.CancellationToken);
        Action[] starts =
        [
            () => Sqlite.CreateTable(aliased),
            () => Sqlite.CreateIndexes(aliased),
            () => Sqlite.DropIndexes(aliased),
            () => Sqlite.DropTable(aliased),
            () => db.CreateTable(aliased),
            () => db.CreateIndexes(aliased),
            () => db.DropIndexes(aliased),
            () => db.DropTable(aliased),
            () => tx.CreateTable(aliased),
            () => tx.CreateIndexes(aliased),
            () => tx.DropIndexes(aliased),
            () => tx.DropTable(aliased),
        ];
        Assert.All(starts, start => Assert.Equal("table", Assert.Throws<ArgumentException>(start).ParamName));
    }

    /// <summary>
    /// A <see cref="TableInfo"/> made by hand that the generator would reject throws when the DDL is built: the current
    /// time on a column that is not a date and time, and an index of a column that the table does not have (design
    /// §14.3).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - A table with <c>SqlDefault.CurrentTimestamp</c> on an <see cref="int"/> column; a table whose index names a
    ///   column it does not have.
    ///
    /// Steps:
    /// - Render <c>CreateTable</c> of the first and <c>CreateIndexes</c> of the second.
    ///
    /// Expected:
    /// - <see cref="InvalidOperationException"/> for each.
    /// </remarks>
    [Fact]
    public void Statements_HandMadeTableInfo_ThrowWhatTheGeneratorRejects()
    {
        var timestamp = Table("t", new ColumnInfo("c", ColumnType.Int32) { Default = ColumnDefault.CurrentTimestamp });
        _ = Assert.Throws<InvalidOperationException>(() => PostgreSql.CreateTable(timestamp).ToSql());
        var index = Wrap(new TableInfo("t", null, [new ColumnInfo("c", ColumnType.Int32)], [new IndexInfo("ix", ["missing"], false)]));
        _ = Assert.Throws<InvalidOperationException>(() => PostgreSql.CreateIndexes(index).ToSql());
    }

    /// <summary>
    /// <c>ExecuteAsync</c> builds every statement before it runs one, so a check that fails runs nothing; then it runs
    /// the statements in order, one command each (design §13.1).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - A connection that records its commands.
    ///
    /// Steps:
    /// - Run <c>CreateTable</c> of a nullable <c>[Unique]</c> column on SQL Server; run <c>CreateIndexes</c> of
    ///   <see cref="ShopOrder"/> on PostgreSQL.
    ///
    /// Expected:
    /// - The first throws <see cref="InvalidOperationException"/> and no command ran.
    /// - The second ran the two statements of <c>ToSql()</c>, in that order.
    /// </remarks>
    [Fact]
    public async Task ExecuteAsync_ChecksFirstAndRunsInOrder()
    {
        var failing = new FakeConnection();
        var sqlServer = new Database<SqlServerDialect>(failing);
        var nullable = Table("t", new ColumnInfo("c", ColumnType.Int32) { IsUnique = true, IsNullable = true });
        _ = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sqlServer.CreateTable(nullable).ExecuteAsync(TestContext.Current.CancellationToken)
        );
        Assert.Empty(failing.Commands);

        var connection = new FakeConnection();
        var db = new Database<PostgreSqlDialect>(connection);
        await db.CreateIndexes(ShopOrder.Table).ExecuteAsync(TestContext.Current.CancellationToken);
        Assert.Equal(PostgreSql.CreateIndexes(ShopOrder.Table).ToSql().Select(s => s.Text), connection.Commands.Select(c => c.CommandText));
    }

    /// <summary>
    /// <c>ExecuteAsync</c> stops at the first statement that fails: the database's error reaches the caller, and the
    /// statements after it do not run (design §13.1).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - A connection whose command of the first index of <see cref="ShopOrder"/> fails with a <see cref="DbException"/>.
    ///
    /// Steps:
    /// - Run <c>CreateIndexes(ShopOrder.Table)</c> on PostgreSQL.
    ///
    /// Expected:
    /// - The <see cref="DbException"/>, unwrapped; one command was created, the second index's never.
    /// </remarks>
    [Fact]
    public async Task ExecuteAsync_FirstStatementFails_StopsThere()
    {
        IReadOnlyList<SqlStatement> statements = PostgreSql.CreateIndexes(ShopOrder.Table).ToSql();
        var connection = new FakeConnection { FailingCommand = statements[0].Text };
        var db = new Database<PostgreSqlDialect>(connection);
        _ = await Assert.ThrowsAsync<FakeDbException>(() => db.CreateIndexes(ShopOrder.Table).ExecuteAsync(TestContext.Current.CancellationToken));
        Assert.Equal([statements[0].Text], connection.Commands.Select(c => c.CommandText));
    }

    private static ColumnInfo Key(ColumnInfo column) =>
        new(column.Name, column.Type)
        {
            IsPrimaryKey = true,
            MaxLength = column.MaxLength,
            Precision = column.Precision,
            Scale = column.Scale,
        };

    private static Table<ShopUser, ShopUser.Columns, ShopUser.NullableColumns, ShopUser.Insert> Table(string name, params ColumnInfo[] columns) =>
        Wrap(new TableInfo(name, null, columns, []));

    private static Table<ShopUser, ShopUser.Columns, ShopUser.NullableColumns, ShopUser.Insert> Wrap(TableInfo info) => new(info);

    // The places a DDL statement writes a name, with the statements that write it there.
    private static IEnumerable<(Func<string, TableInfo> Place, Writes Writes)> Places()
    {
        yield return (name => new TableInfo("t", name, [new ColumnInfo("c", ColumnType.Int32)], [new IndexInfo("ix", ["c"], false)]), Writes.All);
        yield return (name => new TableInfo(name, null, [new ColumnInfo("c", ColumnType.Int32)], [new IndexInfo("ix", ["c"], false)]), Writes.All);
        yield return (
            name => new TableInfo("t", null, [new ColumnInfo(name, ColumnType.Int32)], [new IndexInfo("ix", [name], false)]),
            Writes.CreateTable | Writes.CreateIndexes
        );
        yield return (
            name => new TableInfo("t", null, [new ColumnInfo("c", ColumnType.Int32)], [new IndexInfo(name, ["c"], false)]),
            Writes.CreateIndexes | Writes.DropIndexes
        );
        yield return (
            name => new TableInfo("t", null, [new ColumnInfo("c", ColumnType.Int32) { References = new ForeignKeyInfo(name, null, "id") }], []),
            Writes.CreateTable
        );
        yield return (
            name => new TableInfo("t", null, [new ColumnInfo("c", ColumnType.Int32) { References = new ForeignKeyInfo("p", null, name) }], []),
            Writes.CreateTable
        );
    }

    private static Action[] Renders(string dialect, TableInfo info, Writes writes)
    {
        var table = Wrap(info);
        var renders = new List<Action>();
        if (writes.HasFlag(Writes.CreateTable))
        {
            renders.Add(() => CreateTable(dialect, table));
        }

        if (writes.HasFlag(Writes.CreateIndexes))
        {
            renders.Add(
                dialect switch
                {
                    "PostgreSQL" => () => PostgreSql.CreateIndexes(table).ToSql(),
                    "MySQL" => () => MySql.CreateIndexes(table).ToSql(),
                    "MariaDB" => () => MariaDb.CreateIndexes(table).ToSql(),
                    "SQL Server" => () => SqlServer.CreateIndexes(table).ToSql(),
                    _ => () => Sqlite.CreateIndexes(table).ToSql(),
                }
            );
        }

        if (writes.HasFlag(Writes.DropIndexes))
        {
            renders.Add(
                dialect switch
                {
                    "PostgreSQL" => () => PostgreSql.DropIndexes(table).ToSql(),
                    "MySQL" => () => MySql.DropIndexes(table).ToSql(),
                    "MariaDB" => () => MariaDb.DropIndexes(table).ToSql(),
                    "SQL Server" => () => SqlServer.DropIndexes(table).ToSql(),
                    _ => () => Sqlite.DropIndexes(table).ToSql(),
                }
            );
        }

        if (writes.HasFlag(Writes.DropTable))
        {
            renders.Add(
                dialect switch
                {
                    "PostgreSQL" => () => PostgreSql.DropTable(table).ToSql(),
                    "MySQL" => () => MySql.DropTable(table).ToSql(),
                    "MariaDB" => () => MariaDb.DropTable(table).ToSql(),
                    "SQL Server" => () => SqlServer.DropTable(table).ToSql(),
                    _ => () => Sqlite.DropTable(table).ToSql(),
                }
            );
        }

        return [.. renders];
    }

    private static void RenderAll(string dialect, TableInfo info, Writes writes)
    {
        foreach (Action render in Renders(dialect, info, writes))
        {
            render();
        }
    }

    private static SqlStatement CreateTable(string dialect, Table<ShopUser, ShopUser.Columns, ShopUser.NullableColumns, ShopUser.Insert> table) =>
        dialect switch
        {
            "PostgreSQL" => PostgreSql.CreateTable(table).ToSql(),
            "MySQL" => MySql.CreateTable(table).ToSql(),
            "MariaDB" => MariaDb.CreateTable(table).ToSql(),
            "SQL Server" => SqlServer.CreateTable(table).ToSql(),
            _ => Sqlite.CreateTable(table).ToSql(),
        };

    [Flags]
    private enum Writes
    {
        CreateTable = 1,
        CreateIndexes = 2,
        DropIndexes = 4,
        DropTable = 8,
        All = CreateTable | CreateIndexes | DropIndexes | DropTable,
    }
}
