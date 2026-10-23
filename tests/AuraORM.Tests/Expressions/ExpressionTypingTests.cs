namespace AuraORM.Tests;

public sealed class ExpressionTypingTests
{
    /// <summary>
    /// Valid expressions compile in a consumer project with no warning, including mixed nullability (design §4.1, §4.2).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - A consumer snippet in C# 12 with nullable reference types on, outside the AuraORM namespace.
    ///
    /// Steps:
    /// - Compile comparisons of a nullable and a non-nullable column, string functions on <c>string</c> and <c>string?</c>,
    ///   arithmetic on a nullable integer, aggregates compared with values, and a CASE.
    ///
    /// Expected:
    /// - No error and no warning (CS8620 in particular).
    /// </remarks>
    [Fact]
    public void Functions_ValidUses_CompileWithoutWarnings()
    {
        CompileAssert.Compiles(
            """
            Condition<PerRow> a = And(Eq(nickname, name), Eq(name, nickname), Like(nickname, "a%"), Like(name, "a%"));
            IExpr<string, PerRow> upper = Upper(Concat(name, name));
            IExpr<string?, PerRow> mixed = Concat(name, nickname);
            IExpr<int?, PerRow> next = Add(age, 1);
            Condition<PerRow> b = And(Eq(age, 3), IsNotDistinctFrom(age, 3), In(age, new[] { 1, 2 }));
            Condition<PerGroup> c = And(Gt(Count(), 1L), Gt(Sum(age), 10L), Lt(Max(name), "m"));
            IExpr<string, PerRow> d = Case(Gt(id, 1L), "x").Else("y");
            IExpr<long?, PerRow> e = Cast(name, SqlType.Int64);
            IExpr<int, PerRow> f = Coalesce(age, 0);
            IExpr<int?, PerGroup> g = Max(age);
            IExpr<DateOnly?, PerGroup> h = Min(Value(new DateOnly(2000, 1, 1)));
            IExpr<DayOfWeek?, PerGroup> i = Max(Value(DayOfWeek.Monday));
            Condition<PerRow> j = Like(name, "50!%", '!');
            """
        );
    }

    /// <summary>
    /// An aggregate cannot be combined with a row value, nor used where a row condition is expected (design §4.5).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - The consumer snippet of <see cref="Functions_ValidUses_CompileWithoutWarnings"/>.
    ///
    /// Steps:
    /// - Compile <c>Eq(Count(), id)</c>, and a <c>PerGroup</c> condition assigned to a <c>PerRow</c> condition.
    ///
    /// Expected:
    /// - Each fails with the diagnostic named in the test case, and nothing else.
    /// </remarks>
    [Theory]
    [InlineData("var c = Eq(Count(), id);", "CS0411")]
    [InlineData("Condition<PerRow> c = Gt(Count(), 1L);", "CS0029")]
    [InlineData("var c = Sum(Count());", "CS1503")]
    public void Functions_MixedLevels_DoNotCompile(string body, string id)
    {
        CompileAssert.Fails(body, id);
    }

    /// <summary>
    /// Operands of different types, string functions on non-strings, and <c>MIN</c> / <c>MAX</c> of a type without an order on
    /// every dialect (<see cref="bool"/>, <see cref="Guid"/>) do not compile (design §4.2, §14.1).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - The consumer snippet of <see cref="Functions_ValidUses_CompileWithoutWarnings"/>.
    ///
    /// Steps:
    /// - Compile a comparison of a <c>long</c> column with a string, a comparison of two columns of different types,
    ///   <c>Like</c> on a number, and <c>Min</c> / <c>Max</c> of a <see cref="bool"/> and a <see cref="Guid"/>.
    ///
    /// Expected:
    /// - Each fails with the diagnostic named in the test case, and nothing else.
    /// </remarks>
    [Theory]
    [InlineData("var c = Min(Value(true));", "CS0315")]
    [InlineData("var c = Max(Value(Guid.Empty));", "CS0315")]
    [InlineData("var c = Eq(id, \"x\");", "CS1503")]
    [InlineData("var c = Eq(id, age);", "CS0453")]
    [InlineData("var c = Like(age, \"1%\");", "CS0313")]
    public void Functions_WrongTypes_DoNotCompile(string body, string id)
    {
        CompileAssert.Fails(body, id);
    }
}
