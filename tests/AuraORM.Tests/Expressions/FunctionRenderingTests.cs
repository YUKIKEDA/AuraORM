using static AuraORM.Sql;
using static AuraORM.Tests.TestTables;

namespace AuraORM.Tests;

public sealed class FunctionRenderingTests
{
    private const string U = "\"users\"";
    private const string B = "`users`";
    private const string S = "[users]";

    public static TheoryData<string, Func<object>, DialectSql, object?[]> Cases =>
        new()
        {
            { "Eq value", () => Eq(Score, 3), new($"{U}.\"Score\" = @p0", $"{U}.\"Score\" = $1", $"{B}.`Score` = @p0", $"{S}.[Score] = @p0"), [3] },
            {
                "Eq nullable and non-nullable columns",
                () => Eq(Age, Score),
                new($"{U}.\"Age\" = {U}.\"Score\"", $"{U}.\"Age\" = {U}.\"Score\"", $"{B}.`Age` = {B}.`Score`", $"{S}.[Age] = {S}.[Score]"),
                []
            },
            { "Ne", () => Ne(Name, "x"), new($"{U}.\"Name\" <> @p0", $"{U}.\"Name\" <> $1", $"{B}.`Name` <> @p0", $"{S}.[Name] <> @p0"), ["x"] },
            {
                "Gt / Ge / Lt / Le in And",
                () => And(Gt(Score, 1), Ge(Score, 2), Lt(Score, 3), Le(Score, 4)),
                new(
                    $"({U}.\"Score\" > @p0 AND {U}.\"Score\" >= @p1 AND {U}.\"Score\" < @p2 AND {U}.\"Score\" <= @p3)",
                    $"({U}.\"Score\" > $1 AND {U}.\"Score\" >= $2 AND {U}.\"Score\" < $3 AND {U}.\"Score\" <= $4)",
                    $"({B}.`Score` > @p0 AND {B}.`Score` >= @p1 AND {B}.`Score` < @p2 AND {B}.`Score` <= @p3)",
                    $"({S}.[Score] > @p0 AND {S}.[Score] >= @p1 AND {S}.[Score] < @p2 AND {S}.[Score] <= @p3)"
                ),
                [1, 2, 3, 4]
            },
            {
                "Between",
                () => Between(Score, 1, 9),
                new(
                    $"{U}.\"Score\" BETWEEN @p0 AND @p1",
                    $"{U}.\"Score\" BETWEEN $1 AND $2",
                    $"{B}.`Score` BETWEEN @p0 AND @p1",
                    $"{S}.[Score] BETWEEN @p0 AND @p1"
                ),
                [1, 9]
            },
            {
                "In",
                () => In(Age, [1, 2]),
                new($"{U}.\"Age\" IN (@p0, @p1)", $"{U}.\"Age\" IN ($1, $2)", $"{B}.`Age` IN (@p0, @p1)", $"{S}.[Age] IN (@p0, @p1)"),
                [1, 2]
            },
            { "In with an empty list", () => In(Score, []), new("1 = 0", "1 = 0", "1 = 0", "1 = 0"), [] },
            {
                "NotIn",
                () => NotIn(Name, ["a"]),
                new($"{U}.\"Name\" NOT IN (@p0)", $"{U}.\"Name\" NOT IN ($1)", $"{B}.`Name` NOT IN (@p0)", $"{S}.[Name] NOT IN (@p0)"),
                ["a"]
            },
            { "NotIn with an empty list", () => NotIn(Name, []), new("1 = 1", "1 = 1", "1 = 1", "1 = 1"), [] },
            {
                "Like",
                () => Like(Nickname, "A%"),
                new($"{U}.\"Nickname\" LIKE @p0", $"{U}.\"Nickname\" LIKE $1", $"{B}.`Nickname` LIKE @p0", $"{S}.[Nickname] LIKE @p0"),
                ["A%"]
            },
            {
                "NotLike",
                () => NotLike(Name, "A%"),
                new($"{U}.\"Name\" NOT LIKE @p0", $"{U}.\"Name\" NOT LIKE $1", $"{B}.`Name` NOT LIKE @p0", $"{S}.[Name] NOT LIKE @p0"),
                ["A%"]
            },
            { "IsNull", () => IsNull(Age), new($"{U}.\"Age\" IS NULL", $"{U}.\"Age\" IS NULL", $"{B}.`Age` IS NULL", $"{S}.[Age] IS NULL"), [] },
            {
                "IsNotNull",
                () => IsNotNull(Nickname),
                new($"{U}.\"Nickname\" IS NOT NULL", $"{U}.\"Nickname\" IS NOT NULL", $"{B}.`Nickname` IS NOT NULL", $"{S}.[Nickname] IS NOT NULL"),
                []
            },
            {
                "IsNotDistinctFrom with a value",
                () => IsNotDistinctFrom(Age, 5),
                new(
                    $"{U}.\"Age\" IS @p0",
                    $"{U}.\"Age\" IS NOT DISTINCT FROM $1",
                    $"{B}.`Age` <=> @p0",
                    $"({S}.[Age] = @p0 OR ({S}.[Age] IS NULL AND @p1 IS NULL))"
                )
                {
                    SqlServerParameters = [5, 5],
                },
                [5]
            },
            {
                "IsDistinctFrom with a column",
                () => IsDistinctFrom(Age, Score),
                new(
                    $"{U}.\"Age\" IS NOT {U}.\"Score\"",
                    $"{U}.\"Age\" IS DISTINCT FROM {U}.\"Score\"",
                    $"NOT ({B}.`Age` <=> {B}.`Score`)",
                    $"NOT ({S}.[Age] = {S}.[Score] OR ({S}.[Age] IS NULL AND {S}.[Score] IS NULL))"
                ),
                []
            },
            {
                "Coalesce",
                () => Coalesce(Age, 0),
                new($"COALESCE({U}.\"Age\", @p0)", $"COALESCE({U}.\"Age\", $1)", $"COALESCE({B}.`Age`, @p0)", $"COALESCE({S}.[Age], @p0)"),
                [0]
            },
            {
                "NullIf",
                () => NullIf(Name, string.Empty),
                new($"NULLIF({U}.\"Name\", @p0)", $"NULLIF({U}.\"Name\", $1)", $"NULLIF({B}.`Name`, @p0)", $"NULLIF({S}.[Name], @p0)"),
                [string.Empty]
            },
            {
                "Or and Not",
                () => Or(IsNull(Age), Not(Eq(IsActive, true))),
                new(
                    $"({U}.\"Age\" IS NULL OR NOT ({U}.\"IsActive\" = @p0))",
                    $"({U}.\"Age\" IS NULL OR NOT ({U}.\"IsActive\" = $1))",
                    $"({B}.`Age` IS NULL OR NOT ({B}.`IsActive` = @p0))",
                    $"({S}.[Age] IS NULL OR NOT ({S}.[IsActive] = @p0))"
                ),
                [true]
            },
            {
                "Add and Multiply",
                () => Multiply(Add(Score, 1), Score),
                new(
                    $"(({U}.\"Score\" + @p0) * {U}.\"Score\")",
                    $"(({U}.\"Score\" + $1) * {U}.\"Score\")",
                    $"(({B}.`Score` + @p0) * {B}.`Score`)",
                    $"(({S}.[Score] + @p0) * {S}.[Score])"
                ),
                [1]
            },
            {
                "Subtract and Modulo with a nullable operand",
                () => Modulo(Subtract(Age, 1), 2),
                new($"(({U}.\"Age\" - @p0) % @p1)", $"(({U}.\"Age\" - $1) % $2)", $"(({B}.`Age` - @p0) % @p1)", $"(({S}.[Age] - @p0) % @p1)"),
                [1, 2]
            },
            {
                "Divide integers",
                () => Divide(Score, 2),
                new($"({U}.\"Score\" / @p0)", $"({U}.\"Score\" / $1)", $"({B}.`Score` DIV @p0)", $"({S}.[Score] / @p0)"),
                [2]
            },
            {
                "Divide decimals",
                () => Divide(Balance, 2m),
                new($"({U}.\"Balance\" / @p0)", $"({U}.\"Balance\" / $1)", $"({B}.`Balance` / @p0)", $"({S}.[Balance] / @p0)"),
                [2m]
            },
            { "Negate", () => Negate(Rating), new($"(-{U}.\"Rating\")", $"(-{U}.\"Rating\")", $"(-{B}.`Rating`)", $"(-{S}.[Rating])"), [] },
            {
                "Concat",
                () => Concat(Name, Lower(Name), Upper(Name)),
                new(
                    $"({U}.\"Name\" || LOWER({U}.\"Name\") || UPPER({U}.\"Name\"))",
                    $"({U}.\"Name\" || LOWER({U}.\"Name\") || UPPER({U}.\"Name\"))",
                    $"CONCAT({B}.`Name`, LOWER({B}.`Name`), UPPER({B}.`Name`))",
                    $"({S}.[Name] + LOWER({S}.[Name]) + UPPER({S}.[Name]))"
                ),
                []
            },
            {
                "Concat with a value",
                () => Concat(Nickname, "!"),
                new($"({U}.\"Nickname\" || @p0)", $"({U}.\"Nickname\" || $1)", $"CONCAT({B}.`Nickname`, @p0)", $"({S}.[Nickname] + @p0)"),
                ["!"]
            },
            {
                "Length",
                () => Length(Name),
                new($"LENGTH({U}.\"Name\")", $"CHAR_LENGTH({U}.\"Name\")", $"CHAR_LENGTH({B}.`Name`)", $"LEN({S}.[Name])"),
                []
            },
            {
                "Trim and Substring",
                () => Substring(Trim(Name), 2, 3),
                new(
                    $"SUBSTRING(TRIM({U}.\"Name\"), @p0, @p1)",
                    $"SUBSTRING(TRIM({U}.\"Name\"), $1, $2)",
                    $"SUBSTRING(TRIM({B}.`Name`), @p0, @p1)",
                    $"SUBSTRING(TRIM({S}.[Name]), @p0, @p1)"
                ),
                [2, 3]
            },
            {
                "Cast to Int64",
                () => Cast(Name, SqlType.Int64),
                new($"CAST({U}.\"Name\" AS INTEGER)", $"CAST({U}.\"Name\" AS bigint)", $"CAST({B}.`Name` AS SIGNED)", $"CAST({S}.[Name] AS bigint)"),
                []
            },
            {
                "Cast to Double",
                () => Cast(Score, SqlType.Double),
                new(
                    $"CAST({U}.\"Score\" AS REAL)",
                    $"CAST({U}.\"Score\" AS double precision)",
                    $"CAST({B}.`Score` AS DOUBLE)",
                    $"CAST({S}.[Score] AS float)"
                ),
                []
            },
            {
                "Cast to Decimal",
                () => Cast(Rating, SqlType.Decimal),
                new(
                    $"CAST({U}.\"Rating\" AS TEXT)",
                    $"CAST({U}.\"Rating\" AS numeric)",
                    $"CAST({B}.`Rating` AS DECIMAL(65,30))",
                    $"CAST({S}.[Rating] AS decimal(38,18))"
                ),
                []
            },
            {
                "Cast to String",
                () => Cast(Score, SqlType.String),
                new(
                    $"CAST({U}.\"Score\" AS TEXT)",
                    $"CAST({U}.\"Score\" AS text)",
                    $"CAST({B}.`Score` AS CHAR)",
                    $"CAST({S}.[Score] AS nvarchar(max))"
                ),
                []
            },
            {
                "Cast to DateTime",
                () => Cast(Name, SqlType.DateTime),
                new(
                    $"CAST({U}.\"Name\" AS TEXT)",
                    $"CAST({U}.\"Name\" AS timestamp)",
                    $"CAST({B}.`Name` AS DATETIME(6))",
                    $"CAST({S}.[Name] AS datetime2)"
                ),
                []
            },
            {
                "Cast to DateOnly",
                () => Cast(Name, SqlType.DateOnly),
                new($"CAST({U}.\"Name\" AS TEXT)", $"CAST({U}.\"Name\" AS date)", $"CAST({B}.`Name` AS DATE)", $"CAST({S}.[Name] AS date)"),
                []
            },
            {
                "Case",
                () => Case(Gt(Score, 90), "high").When(Gt(Score, 50), "mid").Else("low"),
                new(
                    $"CASE WHEN {U}.\"Score\" > @p0 THEN @p1 WHEN {U}.\"Score\" > @p2 THEN @p3 ELSE @p4 END",
                    $"CASE WHEN {U}.\"Score\" > $1 THEN $2 WHEN {U}.\"Score\" > $3 THEN $4 ELSE $5 END",
                    $"CASE WHEN {B}.`Score` > @p0 THEN @p1 WHEN {B}.`Score` > @p2 THEN @p3 ELSE @p4 END",
                    $"CASE WHEN {S}.[Score] > @p0 THEN @p1 WHEN {S}.[Score] > @p2 THEN @p3 ELSE @p4 END"
                ),
                [90, "high", 50, "mid", "low"]
            },
            { "Value", () => Value(7L), new("@p0", "$1", "@p0", "@p0"), [7L] },
            {
                "CurrentTimestamp",
                () => CurrentTimestamp(),
                new("CURRENT_TIMESTAMP", "CURRENT_TIMESTAMP", "CURRENT_TIMESTAMP", "CURRENT_TIMESTAMP"),
                []
            },
            { "Count rows", () => Count(), new("COUNT(*)", "COUNT(*)", "COUNT(*)", "COUNT_BIG(*)"), [] },
            {
                "Count values",
                () => Count(Nickname),
                new($"COUNT({U}.\"Nickname\")", $"COUNT({U}.\"Nickname\")", $"COUNT({B}.`Nickname`)", $"COUNT_BIG({S}.[Nickname])"),
                []
            },
            {
                "CountDistinct",
                () => CountDistinct(Name),
                new(
                    $"COUNT(DISTINCT {U}.\"Name\")",
                    $"COUNT(DISTINCT {U}.\"Name\")",
                    $"COUNT(DISTINCT {B}.`Name`)",
                    $"COUNT_BIG(DISTINCT {S}.[Name])"
                ),
                []
            },
            {
                "Sum of integers",
                () => Sum(Score),
                new(
                    $"SUM({U}.\"Score\")",
                    $"CAST(SUM({U}.\"Score\") AS bigint)",
                    $"CAST(SUM({B}.`Score`) AS SIGNED)",
                    $"SUM(CAST({S}.[Score] AS bigint))"
                ),
                []
            },
            {
                "Sum of decimals",
                () => Sum(Balance),
                new($"SUM({U}.\"Balance\")", $"SUM({U}.\"Balance\")", $"SUM({B}.`Balance`)", $"SUM({S}.[Balance])"),
                []
            },
            {
                "Avg of integers",
                () => Avg(Age),
                new(
                    $"AVG({U}.\"Age\")",
                    $"AVG(CAST({U}.\"Age\" AS double precision))",
                    $"CAST(AVG({B}.`Age`) AS DOUBLE)",
                    $"AVG(CAST({S}.[Age] AS float))"
                ),
                []
            },
            {
                "Avg of decimals",
                () => Avg(Balance),
                new($"AVG({U}.\"Balance\")", $"AVG({U}.\"Balance\")", $"AVG({B}.`Balance`)", $"AVG({S}.[Balance])"),
                []
            },
            {
                "Min and Max",
                () => And(Gt(Max(Score), 1), Lt(Min(Name), Max(Nickname))),
                new(
                    $"(MAX({U}.\"Score\") > @p0 AND MIN({U}.\"Name\") < MAX({U}.\"Nickname\"))",
                    $"(MAX({U}.\"Score\") > $1 AND MIN({U}.\"Name\") < MAX({U}.\"Nickname\"))",
                    $"(MAX({B}.`Score`) > @p0 AND MIN({B}.`Name`) < MAX({B}.`Nickname`))",
                    $"(MAX({S}.[Score]) > @p0 AND MIN({S}.[Name]) < MAX({S}.[Nickname]))"
                ),
                [1]
            },
        };

    /// <summary>
    /// Every function of design §4.2 and §4.3 renders the SQL the design gives for each dialect.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Columns of a table "users" built through the internal API (the generator makes them from row G1).
    /// - One case per function or overload family: comparisons, NULL functions, logic, arithmetic, strings, casts, CASE,
    ///   values, the current time, and aggregates.
    ///
    /// Steps:
    /// - Render the expression of the case with each of the five dialects.
    ///
    /// Expected:
    /// - The SQL text equals the text written by hand from the design for that dialect.
    /// - The parameter values equal the case's values, in order.
    /// </remarks>
    [Theory]
    [MemberData(nameof(Cases))]
    public void Render_Function_MatchesDesign(string name, Func<object> build, DialectSql expected, object?[] parameters)
    {
        _ = name;
        SqlAssert.Renders(build(), expected, parameters);
    }
}
