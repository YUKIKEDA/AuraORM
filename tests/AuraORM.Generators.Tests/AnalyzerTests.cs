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
    /// value that is not null cast to a nullable type, <c>IsNotDistinctFrom</c> with a nullable value, and <c>In</c> with
    /// non-null values (design §4.4).
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
