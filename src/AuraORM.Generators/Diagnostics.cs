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
        "The type '{1}' of '{0}' is not a column type; use one of the types in docs/design.ja.md section 3.3, or add [NotColumn]"
    );

    public static readonly DiagnosticDescriptor ReservedName = Error(
        "AURA0104",
        "A member has the name of a generated member",
        "'{0}' has the name of a member that AuraORM generates ({1}); rename it, or map it with [Column] under another member name"
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

    private static readonly DiagnosticDescriptor[] All =
    [
        NotPartial,
        NullableDisabled,
        UnsupportedType,
        ReservedName,
        DuplicateColumn,
        InvalidAutoIncrement,
        InvalidReference,
        UnknownIndexMember,
        MissingMaxLength,
        NoConstructor,
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
