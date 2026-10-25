namespace AuraORM.Generators;

/// <summary>
/// Where a generated partial type goes: its namespace and the partial types that contain it.
/// </summary>
/// <param name="Namespace">The namespace, or <see langword="null"/> for the global namespace.</param>
/// <param name="Containers">The declarations of the containing types, outermost first (<c>partial class Outer</c>).</param>
/// <param name="Declaration">The declaration of the type itself (<c>partial record User</c>).</param>
/// <param name="Name">The type name.</param>
/// <param name="QualifiedName">The type name with its namespace and containing types (<c>global::App.Outer.User</c>).</param>
/// <param name="FlatName">The names of the containing types and the type, joined (<c>OuterUser</c>), for the generated
/// class at the namespace level.</param>
/// <param name="IsPublic">A value indicating whether the type and every type that contains it are public.</param>
internal sealed record TypeTarget(
    string? Namespace,
    EquatableArray<string> Containers,
    string Declaration,
    string Name,
    string QualifiedName,
    string FlatName,
    bool IsPublic
);

/// <summary>
/// A member of a table or result type that the generated code reads or writes.
/// </summary>
/// <param name="MemberName">The C# member name.</param>
/// <param name="TypeName">The member type, fully qualified, with its nullable annotation.</param>
/// <param name="NullableTypeName">The member type made nullable.</param>
/// <param name="IsReferenceType">A value indicating whether the type is a reference type.</param>
/// <param name="IsNullable">A value indicating whether the member allows null.</param>
/// <param name="IsPositional">A value indicating whether the member is a parameter of the record's constructor.</param>
internal sealed record MemberModel(
    string MemberName,
    string TypeName,
    string NullableTypeName,
    bool IsReferenceType,
    bool IsNullable,
    bool IsPositional
);

/// <summary>
/// A value converter (<c>docs/design.ja.md</c> §3.6), with fully qualified names.
/// </summary>
/// <param name="ModelType">The user's type, not nullable.</param>
/// <param name="ProviderType">The stored type.</param>
/// <param name="ConverterType">The converter.</param>
/// <param name="IsValueType">A value indicating whether the user's type is a value type.</param>
/// <param name="ProviderColumnType">The <c>AuraORM.ColumnType</c> member of the stored type.</param>
/// <param name="IsAccessible">A value indicating whether generated code in this compilation can name the types.</param>
internal sealed record ConverterModel(
    string ModelType,
    string ProviderType,
    string ConverterType,
    bool IsValueType,
    string ProviderColumnType,
    bool IsAccessible
);

/// <summary>
/// A column of a table.
/// </summary>
internal sealed record ColumnModel(
    MemberModel Member,
    string ColumnName,
    string ColumnType,
    bool IsPrimaryKey,
    bool IsAutoIncrement,
    bool IsUnique,
    int MaxLength,
    int Precision,
    int Scale,
    string? DefaultCode,
    string? ReferenceCode,
    ConverterModel? Converter = null,
    string? DirectRead = null
)
{
    /// <summary>
    /// Gets a value indicating whether the column must be set in <c>Insert</c>.
    /// </summary>
    public bool IsRequiredOnInsert => !Member.IsNullable && DefaultCode is null && !IsAutoIncrement;
}

/// <summary>
/// An index of a table.
/// </summary>
internal sealed record IndexModel(string Name, EquatableArray<string> Columns, bool IsUnique);

/// <summary>
/// A <c>[Table]</c> type.
/// </summary>
internal sealed record TableModel(
    TypeTarget Target,
    string TableName,
    string? Schema,
    EquatableArray<ColumnModel> Columns,
    EquatableArray<IndexModel> Indexes,
    bool UsesConstructor
);

/// <summary>
/// A member of a <c>[QueryResult]</c> type and the column name a raw query reads it from.
/// </summary>
internal sealed record ResultMemberModel(MemberModel Member, string ColumnName, ConverterModel? Converter = null);

/// <summary>
/// A <c>[QueryResult]</c> type.
/// </summary>
internal sealed record ResultModel(TypeTarget Target, EquatableArray<ResultMemberModel> Members, bool UsesConstructor);

/// <summary>
/// The model of one type and the diagnostics found while reading it. The model is <see langword="null"/> when an error
/// stops generation.
/// </summary>
internal sealed record ParseResult<TModel>(TModel? Model, EquatableArray<DiagnosticInfo> Diagnostics)
    where TModel : class;
