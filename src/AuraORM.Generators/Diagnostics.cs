using Microsoft.CodeAnalysis;

namespace AuraORM.Generators;

/// <summary>
/// The diagnostics of the generator (<c>docs/design.ja.md</c> §15). Each message says what is wrong and what to write instead.
/// </summary>
internal static class Diagnostics
{
    private const string Category = "AuraORM";

    public static readonly DiagnosticDescriptor NotPartial = Error(
        "AURA0101",
        "A table or result type must be a non-generic partial class or record class",
        "'{0}' must be a non-generic partial class or record class, and every type that contains it must be partial; add the partial modifier"
    );

    public static readonly DiagnosticDescriptor NullableDisabled = Error(
        "AURA0102",
        "Nullable reference types must be enabled for a table or result type",
        "The nullability of '{0}' is unknown because nullable reference types are disabled here; enable them with <Nullable>enable</Nullable> or #nullable enable"
    );

    public static readonly DiagnosticDescriptor UnsupportedType = Error(
        "AURA0103",
        "The type of a column is not supported",
        "The type '{1}' of '{0}' is not a column type; use one of the types in docs/design.ja.md section 3.3, declare a value converter with [ValueConverter] (section 3.6), or add [NotColumn]"
    );

    public static readonly DiagnosticDescriptor ReservedName = Error(
        "AURA0104",
        "A member has the name of a generated member",
        "'{0}' has the name of a member that AuraORM generates ({1}); rename the member ([Column] changes only the column name)"
    );

    public static readonly DiagnosticDescriptor InvalidConverter = Error(
        "AURA0105",
        "A value converter is not valid",
        "The value converter '{0}' is not valid: {1}; see docs/design.ja.md section 3.6"
    );

    public static readonly DiagnosticDescriptor DuplicateConverter = Error(
        "AURA0115",
        "A type has two value converters",
        "The type '{0}' has more than one value converter ({1}); a type has one converter, so remove all but one [ValueConverter]"
    );

    public static readonly DiagnosticDescriptor RegistrationCollision = Error(
        "AURA0116",
        "A type has the name of the generated converter registration",
        "The type 'AuraORM.Generated.__AuraValueConverters' has the name of the class that AuraORM generates to register value converters; rename it"
    );

    public static readonly DiagnosticDescriptor CurrentTimestampType = Error(
        "AURA0117",
        "SqlDefault.CurrentTimestamp is on a column that is not DateTime or DateTimeOffset",
        "The default SqlDefault.CurrentTimestamp of '{0}' needs a DateTime or DateTimeOffset column, and '{0}' is of type '{1}'; write the dialect's expression with [DefaultSql]"
    );

    public static readonly DiagnosticDescriptor DuplicateIndexName = Error(
        "AURA0118",
        "Two indexes in one schema have the same name",
        "The index name '{0}' of '{1}' is also the name of an index of '{2}' in the same schema; PostgreSQL and SQLite need an index name that is unique in its schema, so rename one of them"
    );

    public static readonly DiagnosticDescriptor InvalidSize = Error(
        "AURA0119",
        "[MaxLength] or [Precision] is out of range",
        "'{0}' has {1}; a length and a precision are 1 or more, and a scale is from 0 to the precision"
    );

    public static readonly DiagnosticDescriptor DuplicateColumn = Error(
        "AURA0106",
        "Two columns have the same name",
        "The column name '{0}' is used by both '{1}' and '{2}'; give one of them another name with [Column]"
    );

    public static readonly DiagnosticDescriptor InvalidAutoIncrement = Error(
        "AURA0107",
        "[AutoIncrement] is not on the one integer primary key",
        "[AutoIncrement] on '{0}' is not allowed: {1}"
    );

    public static readonly DiagnosticDescriptor InvalidReference = Error(
        "AURA0108",
        "[References] does not name a column of the same type",
        "[References] on '{0}' is not valid: {1}"
    );

    public static readonly DiagnosticDescriptor UnknownIndexMember = Error(
        "AURA0109",
        "[Index] names a member that is not a column",
        "The index '{0}' names '{1}', which is not a column of '{2}'; name the member with nameof"
    );

    public static readonly DiagnosticDescriptor MissingMaxLength = Error(
        "AURA0110",
        "A key or indexed text or binary column needs [MaxLength]",
        "'{0}' is used in a key, a unique constraint, an index, or a foreign key, so it needs [MaxLength]; MySQL and SQL Server cannot index a column without a length"
    );

    public static readonly DiagnosticDescriptor NoConstructor = Error(
        "AURA0111",
        "The row type has no constructor that AuraORM can call",
        "'{0}' needs a positional record constructor or an accessible parameterless constructor to be read from rows"
    );

    public static readonly DiagnosticDescriptor MissingPrecision = Error(
        "AURA0112",
        "A decimal column needs [Precision]",
        "'{0}' is a decimal column without [Precision]; add [Precision(precision, scale)], because a default would round values on some databases and not on others"
    );

    public static readonly DiagnosticDescriptor DefaultTypeMismatch = Error(
        "AURA0113",
        "The [Default] value cannot be the default of the column",
        "The [Default] value of '{0}' is not valid: {1}"
    );

    public static readonly DiagnosticDescriptor NamedQueriesCollision = Error(
        "AURA0114",
        "The generated class of As has the name of another type",
        "The class '{0}' that the generator writes for the As of '{1}' has the name of another type in the namespace; rename one of the types"
    );

    public static readonly DiagnosticDescriptor NullLiteralComparison = Error(
        "AURA0201",
        "A comparison with a null constant matches no row",
        "'{0}' compares with null, which is never true in SQL; use IsNull or IsNotNull instead"
    );

    public static readonly DiagnosticDescriptor NullableComparison = Error(
        "AURA0202",
        "A comparison with a value that may be null matches no row when it is null",
        "The value of '{0}' is of type '{1}' and may be null, and a comparison with null is never true in SQL; use IsNotDistinctFrom, or check the value for null and pass it with .Value or !"
    );

    public static readonly DiagnosticDescriptor NullableNotInSubquery = Error(
        "AURA0203",
        "NOT IN with a subquery that may return null matches no row",
        "The subquery of NotIn returns '{0}', which may be null, and a NOT IN whose subquery returns a null matches no row in SQL; write Not(Exists(...)), or select a value that is not null with Coalesce"
    );

    public static readonly DiagnosticDescriptor UnsupportedHole = Error(
        "AURA0301",
        "A raw SQL hole holds a value of a type that is not a column type",
        "A hole of type '{0}' would become a parameter of a type that is not a column type; pass a column type of docs/design.ja.md section 3.3, a type with a value converter (section 3.6), an expression, a condition, or a fragment such as Identifier(...)"
    );

    private static readonly DiagnosticDescriptor[] All =
    [
        NotPartial,
        NullableDisabled,
        UnsupportedType,
        ReservedName,
        InvalidConverter,
        DuplicateColumn,
        InvalidAutoIncrement,
        InvalidReference,
        UnknownIndexMember,
        MissingMaxLength,
        NoConstructor,
        MissingPrecision,
        DefaultTypeMismatch,
        NamedQueriesCollision,
        DuplicateConverter,
        RegistrationCollision,
        CurrentTimestampType,
        DuplicateIndexName,
        InvalidSize,
    ];

    public static DiagnosticDescriptor ById(string id) => All.First(d => d.Id == id);

    private static DiagnosticDescriptor Error(string id, string title, string message) =>
        new(
            id,
            title,
            message,
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            helpLinkUri: "https://github.com/YUKIKEDA/AuraORM/blob/main/docs/design.ja.md"
        );
}
