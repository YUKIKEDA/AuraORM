namespace AuraORM.Generators.Tests;

public sealed class SchemaGeneratorTests
{
    private const string Usings = """
        using System;
        using System.Collections.Generic;
        using AuraORM;

        """;

    /// <summary>
    /// A table with every column type of design §3.3, a nullable form of each kind, enums, attributes, and a member that is
    /// not a column compiles with its generated code in a C# 12 consumer, with no warning.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - A positional record in a namespace, nested in a partial class, with an extra settable property.
    ///
    /// Steps:
    /// - Run the generator and compile.
    ///
    /// Expected:
    /// - No diagnostic; one generated source for the table.
    /// </remarks>
    [Fact]
    public void Generate_AllColumnTypes_CompilesWithoutWarnings()
    {
        IReadOnlyDictionary<string, string> sources = GeneratorHarness.Compiles(
            Usings
                + """
                namespace App.Data;

                public enum Status : short { Off, On }

                public static partial class Schema
                {
                    [Table("everything", Schema = "app", Naming = ColumnNaming.SnakeCase)]
                    [Index("ix_name", nameof(Name), Unique = true)]
                    public sealed partial record Everything(
                        [PrimaryKey, AutoIncrement] long Id,
                        bool Flag, byte Small, short Short, int Number, float Single, double Double,
                        [Precision(10, 3)] decimal Money,
                        [MaxLength(50)] string Name, string? Note, byte[] Blob, byte[]? MaybeBlob,
                        Guid Key, DateTime At, DateTimeOffset AtOffset, DateOnly Day, TimeOnly Time,
                        Status State, Status? MaybeState,
                        int? MaybeNumber, Guid? MaybeKey,
                        [Default(SqlDefault.CurrentTimestamp)] DateTime CreatedAt,
                        [Default(5)] int Five,
                        [DefaultSql("'x'")] string Code,
                        [Column("custom")] int Renamed)
                    {
                        public string? Extra { get; init; }

                        [NotColumn]
                        public string Computed => Name + Code;
                    }
                }
                """
        );

        Assert.Single(sources);
        Assert.Contains("App.Data.Schema.Everything.Table.g.cs", sources.Keys);
    }

    /// <summary>
    /// Member names that are lowercase, that equal the names of generated parameters and locals, or that are keywords, and
    /// negative default constants, compile in the generated code.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - A table and a result type whose members are named <c>id</c>, <c>reader</c>, <c>Reader</c>, <c>table</c>,
    ///   <c>items</c>, and <c>@class</c>, and a table with the defaults -1, -1.5, a negative enum value, and the int 0 on a
    ///   long column, 1 on a byte column, -2 on a short column, and 0 on an enum column (the constant conversions of C#).
    ///
    /// Steps:
    /// - Run the generator and compile.
    ///
    /// Expected:
    /// - No diagnostic (the generated locals, fields, and parameters cannot collide with members: CS0841, CS0136, CS0075).
    /// </remarks>
    [Fact]
    public void Generate_AwkwardNamesAndNegativeDefaults_Compile()
    {
        GeneratorHarness.Compiles(
            Usings
                + """
                public enum Sign { Minus = -1, Plus = 1 }

                [Table("awkward")]
                public sealed partial record Awkward(
                    [PrimaryKey] long id,
                    int reader,
                    [Column("reader_2")] int Reader,
                    string table,
                    string? items,
                    int @class,
                    [Default(-1)] int minus,
                    [Default(-1.5)] double half,
                    [Default(Sign.Minus)] Sign sign,
                    [Default(0)] long zero,
                    [Default(1)] byte tiny,
                    [Default(-2)] short small,
                    [Default(0)] Sign unset);

                [QueryResult]
                public sealed partial record AwkwardResult(int reader, string? items, int @class, int Reader);
                """
        );
    }

    /// <summary>
    /// Nested and internal table and result types get their column sets and an <c>As</c> class at the namespace level, which
    /// is internal when a type or a type around it is not public, so a named query of each compiles and is listed in
    /// <c>With</c> (design §3.5, §5.11).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - An internal static class <c>Outer</c> in a namespace, holding an internal table and a public result type.
    ///
    /// Steps:
    /// - Run the generator and compile code that names a whole row of the table, groups it into the result type, names that,
    ///   lists both in <c>With</c>, and reads the result type's whole row.
    ///
    /// Expected:
    /// - No diagnostic, and the generated classes <c>OuterItemNamedQueries</c> and <c>OuterTotalNamedQueries</c> are
    ///   internal.
    /// </remarks>
    [Fact]
    public void Generate_NestedInternalTypes_NamedQueriesCompile()
    {
        IReadOnlyDictionary<string, string> sources = GeneratorHarness.Compiles(
            Usings
                + """
                namespace App.Data
                {
                    using static AuraORM.Sql;

                    internal static partial class Outer
                    {
                        [Table("items")]
                        internal sealed partial record Item([PrimaryKey] long Id, string Name);

                        [QueryResult]
                        public sealed partial record Total(string Name, long Count);
                    }

                    internal static class Use
                    {
                        internal static SqlStatement Run()
                        {
                            var db = new SqlBuilder<SqliteDialect>();
                            var items = db.From(Outer.Item.Table).Select(x => x).As("i");
                            var totals = db.From(items).GroupBy(x => x.Name).Select((k, x) => new Outer.Total.GroupProjection { Name = k, Count = Count() }).As("t");
                            return db.With(items, totals).From(totals).Select(x => x).ToSql();
                        }
                    }
                }
                """
        );

        Assert.Contains(sources.Values, s => s.Contains("internal static class OuterItemNamedQueries", StringComparison.Ordinal));
        Assert.Contains(sources.Values, s => s.Contains("internal static class OuterTotalNamedQueries", StringComparison.Ordinal));
    }

    /// <summary>
    /// A class with settable properties is a table too, read with its parameterless constructor.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - A partial class in the global namespace with public get/set and get/init properties.
    ///
    /// Steps:
    /// - Run the generator and compile.
    ///
    /// Expected:
    /// - No diagnostic.
    /// </remarks>
    [Fact]
    public void Generate_ClassTable_CompilesWithoutWarnings()
    {
        GeneratorHarness.Compiles(
            Usings
                + """
                [Table("settings")]
                public sealed partial class Setting
                {
                    [PrimaryKey, MaxLength(64)]
                    public string Key { get; set; } = "";

                    public string? Value { get; init; }

                    public int Version { get; set; }
                }
                """
        );
    }

    /// <summary>
    /// The <c>Insert</c> model requires every non-nullable column without a default, and has no <c>[AutoIncrement]</c> column
    /// (design §6.1).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - A table with an auto-increment key, a required name, a nullable age, and a defaulted timestamp.
    ///
    /// Steps:
    /// - Compile an initializer that sets everything that may be set, one that leaves out the name, and one that sets the id.
    ///
    /// Expected:
    /// - The first compiles; the second fails with CS9035; the third fails with CS0117.
    /// </remarks>
    [Theory]
    [InlineData("""new User.Insert { Name = "a", Age = 3, CreatedAt = DateTime.UnixEpoch }""", null)]
    [InlineData("""new User.Insert { Age = 3 }""", "CS9035")]
    [InlineData("""new User.Insert { Id = 1, Name = "a" }""", "CS0117")]
    public void Generate_InsertModel_RequiresColumnsWithoutDefault(string initializer, string? error)
    {
        string source =
            Usings
            + """
                [Table("users")]
                public sealed partial record User(
                    [PrimaryKey, AutoIncrement] long Id,
                    string Name,
                    int? Age,
                    [Default(SqlDefault.CurrentTimestamp)] DateTime CreatedAt);

                public static class Use
                {
                    public static object Make() =>
                """
            + initializer
            + """
                ;
                }
                """;
        if (error is null)
        {
            GeneratorHarness.Compiles(source);
        }
        else
        {
            GeneratorHarness.Fails(source, error);
        }
    }

    /// <summary>
    /// Column sets cannot be made by user code: their constructors are private, and the table occurrence they need has an
    /// internal constructor (design §3.4).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - A table <c>User</c>.
    ///
    /// Steps:
    /// - Compile <c>new User.Columns(…)</c>, and <c>new TableAlias(…)</c>.
    ///
    /// Expected:
    /// - Each fails with CS0122 (inaccessible) or CS1729 (no such constructor).
    /// </remarks>
    [Theory]
    [InlineData("new User.Columns(null!)", "CS0122")]
    [InlineData("new User.NullableColumns(null!)", "CS0122")]
    [InlineData("""new TableAlias(null, "users", null)""", "CS1729")]
    public void Generate_ColumnSets_CannotBeCreatedByUsers(string expression, string error)
    {
        GeneratorHarness.Fails(
            Usings
                + """
                [Table("users")]
                public sealed partial record User([PrimaryKey] long Id);

                public static class Use
                {
                    public static object Make() =>
                """
                + expression
                + """
                ;
                }
                """,
            error
        );
    }

    /// <summary>
    /// A result type gets a row projection and a group projection whose members are all required (design §3.5).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - A positional result record with a non-nullable and a nullable member.
    ///
    /// Steps:
    /// - Compile a group projection that sets both members from group-level expressions, and one that sets only one.
    ///
    /// Expected:
    /// - The first compiles; the second fails with CS9035.
    /// </remarks>
    [Theory]
    [InlineData("""new Total.GroupProjection { Name = Sql.Max(name)!, Sum = Sql.Sum(amount) }""", null)]
    [InlineData("""new Total.GroupProjection { Sum = Sql.Sum(amount) }""", "CS9035")]
    public void Generate_ResultProjection_RequiresEveryMember(string initializer, string? error)
    {
        string source =
            Usings
            + """
                [QueryResult]
                public sealed partial record Total(string? Name, decimal? Sum);

                public static class Use
                {
                    public static object Make(IExpr<string, PerRow> name, IExpr<decimal, PerRow> amount) =>
                """
            + initializer
            + """
                ;
                }
                """;
        if (error is null)
        {
            GeneratorHarness.Compiles(source);
        }
        else
        {
            GeneratorHarness.Fails(source, error);
        }
    }

    /// <summary>
    /// Each schema error of design §3.7 is reported with its diagnostic id (design §15).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - One table declaration per case, each with exactly one error.
    ///
    /// Steps:
    /// - Run the generator and compile.
    ///
    /// Expected:
    /// - The case's id is the only diagnostic.
    /// </remarks>
    [Theory]
    [InlineData("AURA0101", """[Table("t")] public sealed record T([PrimaryKey] long Id);""")]
    [InlineData("AURA0101", """public static class Outer { [Table("t")] public sealed partial record T([PrimaryKey] long Id); }""")]
    [InlineData("AURA0101", """[Table("t")] public sealed partial record T<TKey>([PrimaryKey] long Id);""")]
    [InlineData("AURA0103", """[Table("t")] public sealed partial record T([PrimaryKey] long Id, Uri Link);""")]
    [InlineData("AURA0103", """public enum Big : uint { A } [Table("t")] public sealed partial record T([PrimaryKey] long Id, Big Size);""")]
    [InlineData("AURA0104", """[Table("t")] public sealed partial record T([PrimaryKey] long Id, int Create);""")]
    [InlineData("AURA0104", """[Table("t")] public sealed partial record T([PrimaryKey] long Id) { public static int Insert => 0; }""")]
    [InlineData("AURA0104", """[Table("t")] public sealed partial record T([PrimaryKey] long Id, int __auraValue0);""")]
    [InlineData("AURA0104", """[QueryResult] public sealed partial record T(int __auraReader);""")]
    [InlineData("AURA0104", """[QueryResult] public sealed partial record T(int Columns);""")]
    [InlineData("AURA0104", """[QueryResult] public sealed partial record T(int NullableColumns);""")]
    [InlineData("AURA0104", """[QueryResult] public sealed partial record T(int ReadRow);""")]
    [InlineData("AURA0106", """[Table("t")] public sealed partial record T([PrimaryKey] long Id, [Column("id")] long Other);""")]
    [InlineData("AURA0107", """[Table("t")] public sealed partial record T([PrimaryKey, AutoIncrement, MaxLength(10)] string Id);""")]
    [InlineData("AURA0107", """[Table("t")] public sealed partial record T([AutoIncrement] long Id);""")]
    [InlineData("AURA0107", """[Table("t")] public sealed partial record T([PrimaryKey, AutoIncrement] long Id, [AutoIncrement] long Other);""")]
    [InlineData(
        "AURA0108",
        """[Table("t")] public sealed partial record T([PrimaryKey] long Id, [References(typeof(string), "Length")] int Other);"""
    )]
    [InlineData(
        "AURA0108",
        """[Table("u")] public sealed partial record U([PrimaryKey] long Id); [Table("t")] public sealed partial record T([PrimaryKey] long Id, [References(typeof(U), "Missing")] long UId);"""
    )]
    [InlineData(
        "AURA0108",
        """[Table("u")] public sealed partial record U([PrimaryKey] long Id); [Table("t")] public sealed partial record T([PrimaryKey] long Id, [References(typeof(U), nameof(U.Id))] int UId);"""
    )]
    [InlineData("AURA0109", """[Table("t")] [Index("ix", "Missing")] public sealed partial record T([PrimaryKey] long Id);""")]
    [InlineData("AURA0110", """[Table("t")] public sealed partial record T([PrimaryKey] string Id);""")]
    [InlineData("AURA0110", """[Table("t")] public sealed partial record T([PrimaryKey] long Id, [Unique] string Email);""")]
    [InlineData("AURA0111", """[Table("t")] public sealed partial class T { private T(int x) { } public long Id { get; set; } }""")]
    [InlineData("AURA0112", """[Table("t")] public sealed partial record T([PrimaryKey] long Id, decimal Price);""")]
    [InlineData("AURA0112", """[Table("t")] public sealed partial record T([PrimaryKey] long Id, decimal? Price);""")]
    [InlineData("AURA0113", """[Table("t")] public sealed partial record T([PrimaryKey] long Id, [Default("x")] int Count);""")]
    [InlineData("AURA0113", """[Table("t")] public sealed partial record T([PrimaryKey] long Id, [Default(1.5)] int Count);""")]
    [InlineData("AURA0113", """[Table("t")] public sealed partial record T([PrimaryKey] long Id, [Default(null!)] int Count);""")]
    [InlineData("AURA0113", """[Table("t")] public sealed partial record T([PrimaryKey] long Id, [Default(300)] byte Count);""")]
    [InlineData("AURA0113", """[Table("t")] public sealed partial record T([PrimaryKey] long Id, [Default(40000)] short Count);""")]
    [InlineData("AURA0113", """public enum E { A, B } [Table("t")] public sealed partial record T([PrimaryKey] long Id, [Default(1)] E State);""")]
    public void Generate_SchemaError_ReportsDiagnostic(string id, string declaration)
    {
        GeneratorHarness.Fails(Usings + declaration, id);
    }

    /// <summary>
    /// Every member that a generated class inherits from its base type is a reserved name, so a base type that gains a member
    /// cannot silently break a table or result type that has a member of the same name (AURA0104).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - The public and protected members of the base types of the generated classes, read by reflection.
    ///
    /// Steps:
    /// - For each name, compile a table (or, for <see cref="ResultProjection{TResult, TLevel}"/>, a result type) with a member
    ///   of that name.
    ///
    /// Expected:
    /// - AURA0104 for every name.
    /// </remarks>
    [Fact]
    public void Generate_MemberNamedLikeBaseMember_ReportsAura0104()
    {
        const System.Reflection.BindingFlags Flags =
            System.Reflection.BindingFlags.Public
            | System.Reflection.BindingFlags.NonPublic
            | System.Reflection.BindingFlags.Instance
            | System.Reflection.BindingFlags.Static
            | System.Reflection.BindingFlags.DeclaredOnly;
        static IEnumerable<string> Names(Type type) =>
            type.GetMembers(Flags)
                .Where(m => m is not System.Reflection.ConstructorInfo && !m.Name.Contains('.', StringComparison.Ordinal))
                .Where(m =>
                    m
                        is System.Reflection.MethodInfo { IsFamily: true }
                            or System.Reflection.MethodInfo { IsPublic: true }
                            or System.Reflection.PropertyInfo
                )
                .Select(m => m.Name)
                .Where(n => !n.StartsWith("get_", StringComparison.Ordinal))
                .Distinct();

        string[] tableNames =
        [
            .. new[] { typeof(TableColumns<>), typeof(NullableTableColumns<>), typeof(InsertModel), typeof(ITableColumns<>) }.SelectMany(Names),
        ];
        string[] resultNames = [.. Names(typeof(ResultProjection<,>))];
        Assert.NotEmpty(tableNames);
        Assert.NotEmpty(resultNames);

        foreach (string name in tableNames)
        {
            GeneratorHarness.Fails(Usings + "[Table(\"t\")] public sealed partial record T([PrimaryKey] long Id, int " + name + ");", "AURA0104");
        }

        foreach (string name in resultNames)
        {
            GeneratorHarness.Fails(Usings + "[QueryResult] public sealed partial record T(int " + name + ");", "AURA0104");
        }
    }

    /// <summary>
    /// A table whose reference-type columns have unknown nullability is reported as AURA0102.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Nullable reference types disabled for the compilation.
    ///
    /// Steps:
    /// - Run the generator on a table with a <c>string</c> column.
    ///
    /// Expected:
    /// - AURA0102 is reported.
    /// </remarks>
    [Fact]
    public void Generate_NullableDisabled_ReportsAura0102()
    {
        (_, System.Collections.Immutable.ImmutableArray<Microsoft.CodeAnalysis.Diagnostic> diagnostics) = GeneratorHarness.Run(
            Usings + """[Table("t")] public sealed partial record T([PrimaryKey] long Id, string Name);""",
            Microsoft.CodeAnalysis.NullableContextOptions.Disable
        );

        Assert.Contains(diagnostics, d => d.Id == "AURA0102");
    }

    /// <summary>
    /// Snake case follows the three rules of design §3.2.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Member names with a lower-to-upper step, a run of capitals, a digit, and a single word.
    ///
    /// Steps:
    /// - Convert each name.
    ///
    /// Expected:
    /// - The column names of the cases.
    /// </remarks>
    [Theory]
    [InlineData("CreatedAt", "created_at")]
    [InlineData("HTTPCode", "http_code")]
    [InlineData("UserID2", "user_id2")]
    [InlineData("Version2Name", "version2_name")]
    [InlineData("Id", "id")]
    [InlineData("ID", "id")]
    [InlineData("already_snake", "already_snake")]
    public void ToSnakeCase_MemberName_FollowsTheRules(string member, string column)
    {
        Assert.Equal(column, Naming.ToSnakeCase(member));
    }
}
