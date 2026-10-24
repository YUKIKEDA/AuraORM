namespace AuraORM.Generators.Tests;

public sealed class AnalyzerTests
{
    /// <summary>
    /// A comparison function whose value is a typed null constant (<c>(int?)null</c>, <c>default(int?)</c>) is AURA0201: in SQL,
    /// a comparison with null is never true. A bare <c>null</c> is already ambiguous between the value and the expression
    /// overloads (CS0121) (design §4.4).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - A table with a nullable int and string column, in a statement of a <c>SqlBuilder</c>.
    ///
    /// Steps:
    /// - Compile <c>Where</c> with each comparison of the test case.
    ///
    /// Expected:
    /// - AURA0201, and no other diagnostic.
    /// </remarks>
    /// <param name="condition">The condition of <c>Where</c>.</param>
    [Theory]
    [InlineData("Eq(u.Age, (int?)null)")]
    [InlineData("Ne(u.Nickname, (string?)null)")]
    [InlineData("Gt(u.Age, default(int?))")]
    [InlineData("Between(u.Age, 1, (int?)null)")]
    [InlineData("Like(u.Name, (string)null!)")]
    [InlineData("NotLike(u.Nickname, default(string)!)")]
    public void Comparison_NullConstant_ReportsAura0201(string condition)
    {
        GeneratorHarness.Fails(Snippet($"Where(u => {condition})"), "AURA0201");
    }

    /// <summary>
    /// A comparison function whose value may be null at run time is AURA0202: a nullable value type always, a reference type
    /// when the compiler's flow analysis says it may be null; so is a list of nullable values for <c>In</c> (design §4.4).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - The snippet's parameters <c>int? x</c>, <c>string? s</c>, and <c>List&lt;int?&gt; ages</c>, and a method that
    ///   returns <c>string?</c>.
    ///
    /// Steps:
    /// - Compile <c>Where</c> with each comparison of the test case.
    ///
    /// Expected:
    /// - AURA0202, and no other diagnostic.
    /// </remarks>
    /// <param name="condition">The condition of <c>Where</c>.</param>
    [Theory]
    [InlineData("Eq(u.Age, x)")]
    [InlineData("Gt(u.Age, x)")]
    [InlineData("Eq(u.Nickname, s)")]
    [InlineData("Eq(u.Name, Get())")]
    [InlineData("In(u.Age, ages)")]
    [InlineData("NotIn(u.Age, new int?[] { 1, 2 })")]
    public void Comparison_NullableValue_ReportsAura0202(string condition)
    {
        GeneratorHarness.Fails(Snippet($"Where(u => {condition})"), "AURA0202");
    }

    /// <summary>
    /// Comparisons with values that cannot be null compile: a checked or unwrapped value, a non-null default, a constant, a
    /// value that is not null cast to a nullable type, a nullable value cast to its value type (which throws for null, like
    /// <c>.Value</c>), <c>IsNotDistinctFrom</c> with a nullable value, and <c>In</c> with non-null values (design §4.4).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - The snippet of <see cref="Comparison_NullableValue_ReportsAura0202"/>.
    ///
    /// Steps:
    /// - Compile <c>Where</c> with each comparison, one of them after a null check of <c>s</c>.
    ///
    /// Expected:
    /// - No diagnostic.
    /// </remarks>
    [Fact]
    public void Comparison_NonNullValues_Compile()
    {
        GeneratorHarness.Compiles(
            Snippet(
                """
                Where(u => And(
                    Eq(u.Age, x!.Value),
                    Eq(u.Nickname, s ?? "none"),
                    Eq(u.Nickname, s!),
                    Eq(u.Name, (string?)name),
                    Eq(u.Age, (int?)count),
                    Eq(u.Age, (int)x!),
                    Gt(u.Age, 3),
                    IsNotDistinctFrom(u.Age, x),
                    IsNull(u.Age),
                    In(u.Name, names),
                    Like(u.Name, "a%")))
                """
            )
                + """

                public static class Checked
                {
                    public static object Run(string? s) =>
                        s is null ? "" : new SqlBuilder<SqliteDialect>().From(User.Table).Where(u => Eq(u.Nickname, s)).Select(u => u.Id);
                }
                """
        );
    }

    /// <summary>
    /// A raw SQL hole whose value would be a parameter of a type that is not a column type is AURA0301 (design §11.1).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - The snippet's parameters <c>object o</c> and <c>char c</c>.
    ///
    /// Steps:
    /// - Compile <c>Where</c> with a raw condition whose hole is the value of the test case.
    ///
    /// Expected:
    /// - AURA0301, and no other diagnostic.
    /// </remarks>
    /// <param name="hole">The value in the hole.</param>
    [Theory]
    [InlineData("o")]
    [InlineData("c")]
    [InlineData("u")]
    [InlineData("And(new List<Condition<PerRow>?>())")]
    [InlineData("1u")]
    public void RawHole_UnsupportedType_ReportsAura0301(string hole)
    {
        GeneratorHarness.Fails(Snippet($"Where(u => Raw($\"{{u.Name}} = {{{hole}}}\"))"), "AURA0301");
    }

    /// <summary>
    /// A raw SQL hole may hold a value of a type with one value converter, which becomes a parameter of the converted value;
    /// a type with no converter is AURA0301 (design §3.6, §11.1).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - <c>UserId</c> with a converter, and <c>Plain</c> without one.
    ///
    /// Steps:
    /// - Compile <c>Where</c> with a raw condition whose hole is a <c>UserId</c>, then one whose hole is a <c>Plain</c>.
    ///
    /// Expected:
    /// - No diagnostic for <c>UserId</c>; AURA0301 for <c>Plain</c>.
    /// </remarks>
    [Fact]
    public void RawHole_ConvertedType_IsAllowed()
    {
        const string types = """

            [ValueConverter(typeof(UserIdConverter))]
            public readonly record struct UserId(long Value);

            public readonly record struct Plain(long Value);

            public sealed class UserIdConverter : IValueConverter<UserId, long>
            {
                public static long ToProvider(UserId value) => value.Value;
                public static UserId FromProvider(long value) => new(value);
            }
            """;
        GeneratorHarness.Compiles(Snippet("Where(u => Raw($\"{u.Id} = {new UserId(1)}\"))") + types);
        GeneratorHarness.Fails(Snippet("Where(u => Raw($\"{u.Id} = {new Plain(1)}\"))") + types, "AURA0301");
    }

    /// <summary>
    /// A raw SQL hole whose value has a generic type is AURA0301: the type is not known to be a column type (design §11.1).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - A generic method that writes its parameter of type <c>T</c> into a hole.
    ///
    /// Steps:
    /// - Compile it.
    ///
    /// Expected:
    /// - AURA0301, and no other diagnostic.
    /// </remarks>
    [Fact]
    public void RawHole_GenericValue_ReportsAura0301()
    {
        GeneratorHarness.Fails(
            Snippet("Where(u => Eq(u.Id, 1L))")
                + """

                public static class Generic
                {
                    public static Condition<PerRow> Run<T>(T value) => Raw($"x = {value}");
                }
                """,
            "AURA0301"
        );
    }

    /// <summary>
    /// Raw SQL holes with column values, nullable column values, enums, expressions, conditions, fragments, and a table
    /// reference compile (design §11.1).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - The snippet's parameters.
    ///
    /// Steps:
    /// - Compile one raw condition with a hole of each kind.
    ///
    /// Expected:
    /// - No diagnostic.
    /// </remarks>
    [Fact]
    public void RawHole_SupportedTypes_Compile()
    {
        GeneratorHarness.Compiles(
            Snippet(
                """
                Where(u => Raw($"{u.Name} {1} {x} {s} {true} {1.5m} {Guid.Empty} {DateTime.UnixEpoch} {DateOnly.MinValue} {new byte[0]} {DayOfWeek.Monday} {Identifier("a")} {Unsafe("1 = 1")} {User.Table} {Gt(u.Age, 1)} {Count()}"))
                """
            )
        );
    }

    /// <summary>
    /// A <c>NotIn</c> whose subquery returns a type that allows null is AURA0203, for a nullable value type and a nullable
    /// reference type, with or without a row limit: one null makes <c>NOT IN</c> match no row (design §4.4, §5.11).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - <c>User</c> with the nullable columns <c>Age</c> (<c>int?</c>) and <c>Nickname</c> (<c>string?</c>).
    ///
    /// Steps:
    /// - Compile <c>NotIn</c> with a subquery of each nullable column, and one of <c>Age</c> with <c>Limit</c>.
    ///
    /// Expected:
    /// - AURA0203, and no other diagnostic.
    /// </remarks>
    /// <param name="condition">The condition.</param>
    [Theory]
    [InlineData("NotIn(u.Age, new SqlBuilder<SqliteDialect>().From(User.Table).Select(v => v.Age))")]
    [InlineData("NotIn(u.Nickname, new SqlBuilder<SqliteDialect>().From(User.Table).Select(v => v.Nickname))")]
    [InlineData("NotIn(u.Age, new SqlBuilder<SqliteDialect>().From(User.Table).Select(v => v.Age).Limit(3))")]
    public void NotIn_NullableSubquery_ReportsAura0203(string condition)
    {
        GeneratorHarness.Fails(Snippet($"Where(u => {condition})"), "AURA0203");
    }

    /// <summary>
    /// <c>NotIn</c> with a subquery that returns no null, and <c>In</c> with a subquery that may, have no diagnostic: a null in
    /// <c>IN</c> only fails to match itself (design §5.11).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - The <c>User</c> table of <see cref="NotIn_NullableSubquery_ReportsAura0203"/>.
    ///
    /// Steps:
    /// - Compile <c>NotIn</c> of the id and of <c>Coalesce(v.Age, 0)</c> for the nullable <c>Age</c>, and <c>In</c> of the nullable
    ///   <c>Age</c>.
    ///
    /// Expected:
    /// - No diagnostic.
    /// </remarks>
    [Fact]
    public void NotIn_NonNullSubquery_Compiles()
    {
        GeneratorHarness.Compiles(
            Snippet(
                """
                Where(u => And(
                    NotIn(u.Id, new SqlBuilder<SqliteDialect>().From(User.Table).Select(v => v.Id)),
                    NotIn(u.Age, new SqlBuilder<SqliteDialect>().From(User.Table).Select(v => Coalesce(v.Age, 0))),
                    In(u.Age, new SqlBuilder<SqliteDialect>().From(User.Table).Select(v => v.Age))))
                """
            )
        );
    }

    private static string Snippet(string clause) =>
        $$"""
            using System;
            using System.Collections.Generic;
            using AuraORM;
            using static AuraORM.Sql;

            [Table("users")]
            public sealed partial record User([PrimaryKey] long Id, string Name, string? Nickname, int? Age);

            public static class Snippet
            {
                private static string? Get() => null;

                public static object Run(int? x, string? s, List<int?> ages, List<string> names, object o, char c, string name = "n", int count = 0) =>
                    new SqlBuilder<SqliteDialect>().From(User.Table).{{clause}}.Select(u => u.Id);
            }
            """;
}
