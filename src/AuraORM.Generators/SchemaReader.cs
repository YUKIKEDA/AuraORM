using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AuraORM.Generators;

/// <summary>
/// Reads <c>[Table]</c> and <c>[QueryResult]</c> types into models, and reports the schema errors of
/// <c>docs/design.ja.md</c> §3.7.
/// </summary>
internal static class SchemaReader
{
    public const string TableAttribute = "AuraORM.TableAttribute";
    public const string QueryResultAttribute = "AuraORM.QueryResultAttribute";

    private const string ColumnAttribute = "AuraORM.ColumnAttribute";
    private const string PrimaryKeyAttribute = "AuraORM.PrimaryKeyAttribute";
    private const string AutoIncrementAttribute = "AuraORM.AutoIncrementAttribute";
    private const string UniqueAttribute = "AuraORM.UniqueAttribute";
    private const string MaxLengthAttribute = "AuraORM.MaxLengthAttribute";
    private const string PrecisionAttribute = "AuraORM.PrecisionAttribute";
    private const string DefaultAttribute = "AuraORM.DefaultAttribute";
    private const string DefaultSqlAttribute = "AuraORM.DefaultSqlAttribute";
    private const string ReferencesAttribute = "AuraORM.ReferencesAttribute";
    private const string IndexAttribute = "AuraORM.IndexAttribute";
    private const string NotColumnAttribute = "AuraORM.NotColumnAttribute";

    private static readonly string[] TableGeneratedNames = ["Table", "Columns", "NullableColumns", "Insert"];
    private static readonly string[] TableReservedMemberNames =
    [
        "Table",
        "Columns",
        "NullableColumns",
        "Insert",
        "Create",
        "GetColumns",
        "ReadRow",
        "WriteValues",
    ];

    private static readonly string[] ResultGeneratedNames = ["RowProjection", "GroupProjection"];
    private static readonly string[] ResultReservedMemberNames = ["RowProjection", "GroupProjection", "AddItems", "ReadResult"];

    private static readonly SymbolDisplayFormat TypeFormat = SymbolDisplayFormat.FullyQualifiedFormat.AddMiscellaneousOptions(
        SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier
    );

    public static ParseResult<TableModel> ReadTable(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
    {
        var diagnostics = new List<DiagnosticInfo>();
        var type = (INamedTypeSymbol)context.TargetSymbol;
        TypeTarget? target = ReadTarget(type, diagnostics);
        AttributeData table = context.Attributes[0];
        string tableName = table.ConstructorArguments[0].Value as string ?? type.Name;
        string? schema = NamedString(table, "Schema");
        bool snakeCase = NamedInt(table, "Naming") == 1;

        List<Member> members = ReadMembers(type, diagnostics, cancellationToken);
        CheckReservedNames(type, members, TableGeneratedNames, TableReservedMemberNames, diagnostics);
        bool usesConstructor = CheckConstructor(type, members, diagnostics);

        var columns = new List<ColumnModel>();
        foreach (Member member in members)
        {
            ColumnModel? column = ReadColumn(member, snakeCase, diagnostics);
            if (column is not null)
            {
                columns.Add(column);
            }
        }

        CheckDuplicateNames(columns, type, diagnostics);
        CheckAutoIncrement(columns, members, diagnostics);
        List<IndexModel> indexes = ReadIndexes(type, columns, diagnostics);
        CheckMaxLength(columns, members, indexes, diagnostics);

        TableModel? model =
            target is null || diagnostics.Count > 0
                ? null
                : new TableModel(target, tableName, schema, Equatable(columns), Equatable(indexes), usesConstructor);
        return new ParseResult<TableModel>(model, Equatable(diagnostics));
    }

    public static ParseResult<ResultModel> ReadResult(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
    {
        var diagnostics = new List<DiagnosticInfo>();
        var type = (INamedTypeSymbol)context.TargetSymbol;
        TypeTarget? target = ReadTarget(type, diagnostics);
        List<Member> members = ReadMembers(type, diagnostics, cancellationToken);
        CheckReservedNames(type, members, ResultGeneratedNames, ResultReservedMemberNames, diagnostics);
        bool usesConstructor = CheckConstructor(type, members, diagnostics);

        var models = new List<MemberModel>();
        foreach (Member member in members)
        {
            if (ColumnTypeOf(member.Type) is null)
            {
                diagnostics.Add(DiagnosticInfo.Create(Diagnostics.UnsupportedType, member.Location, member.Name, member.Type.ToDisplayString()));
                continue;
            }

            models.Add(member.Model);
        }

        ResultModel? model = target is null || diagnostics.Count > 0 ? null : new ResultModel(target, Equatable(models), usesConstructor);
        return new ParseResult<ResultModel>(model, Equatable(diagnostics));
    }

    /// <summary>
    /// Returns the column type of a CLR type (<c>docs/design.ja.md</c> §3.3), or <see langword="null"/> when it cannot be a
    /// column. An enum has the type of its underlying integer.
    /// </summary>
    /// <param name="type">The type, nullable or not.</param>
    /// <returns>The name of the <c>AuraORM.ColumnType</c> member.</returns>
    public static string? ColumnTypeOf(ITypeSymbol type)
    {
        ITypeSymbol underlying = Unwrap(type);
        if (underlying is INamedTypeSymbol { TypeKind: TypeKind.Enum, EnumUnderlyingType: { } enumBase })
        {
            return enumBase.SpecialType switch
            {
                SpecialType.System_Byte => "Byte",
                SpecialType.System_Int16 => "Int16",
                SpecialType.System_Int32 => "Int32",
                SpecialType.System_Int64 => "Int64",
                _ => null,
            };
        }

        switch (underlying.SpecialType)
        {
            case SpecialType.System_Boolean:
                return "Boolean";
            case SpecialType.System_Byte:
                return "Byte";
            case SpecialType.System_Int16:
                return "Int16";
            case SpecialType.System_Int32:
                return "Int32";
            case SpecialType.System_Int64:
                return "Int64";
            case SpecialType.System_Single:
                return "Single";
            case SpecialType.System_Double:
                return "Double";
            case SpecialType.System_Decimal:
                return "Decimal";
            case SpecialType.System_String:
                return "String";
            case SpecialType.System_DateTime:
                return "DateTime";
        }

        if (underlying is IArrayTypeSymbol { Rank: 1, ElementType.SpecialType: SpecialType.System_Byte })
        {
            return "Binary";
        }

        return underlying.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) switch
        {
            "global::System.Guid" => "Guid",
            "global::System.DateTimeOffset" => "DateTimeOffset",
            "global::System.DateOnly" => "DateOnly",
            "global::System.TimeOnly" => "TimeOnly",
            _ => null,
        };
    }

    private static ColumnModel? ReadColumn(Member member, bool snakeCase, List<DiagnosticInfo> diagnostics)
    {
        string? columnType = ColumnTypeOf(member.Type);
        if (columnType is null)
        {
            diagnostics.Add(DiagnosticInfo.Create(Diagnostics.UnsupportedType, member.Location, member.Name, member.Type.ToDisplayString()));
            return null;
        }

        string name =
            member.Attribute(ColumnAttribute)?.ConstructorArguments[0].Value as string ?? (snakeCase ? Naming.ToSnakeCase(member.Name) : member.Name);
        int maxLength = member.Attribute(MaxLengthAttribute)?.ConstructorArguments[0].Value as int? ?? -1;
        AttributeData? precision = member.Attribute(PrecisionAttribute);
        return new ColumnModel(
            member.Model,
            name,
            columnType,
            IsPrimaryKey: member.Attribute(PrimaryKeyAttribute) is not null,
            IsAutoIncrement: member.Attribute(AutoIncrementAttribute) is not null,
            IsUnique: member.Attribute(UniqueAttribute) is not null,
            maxLength,
            Precision: precision?.ConstructorArguments[0].Value as int? ?? -1,
            Scale: precision?.ConstructorArguments[1].Value as int? ?? -1,
            DefaultCode: ReadDefault(member),
            ReferenceCode: ReadReference(member, diagnostics)
        );
    }

    private static string? ReadDefault(Member member)
    {
        if (member.Attribute(DefaultSqlAttribute) is { } sql)
        {
            return "global::AuraORM.ColumnDefault.FromSql(" + Literal(sql.ConstructorArguments[0].Value) + ")";
        }

        if (member.Attribute(DefaultAttribute) is not { } attribute)
        {
            return null;
        }

        TypedConstant value = attribute.ConstructorArguments[0];
        if (value.Kind == TypedConstantKind.Enum && value.Type?.ToDisplayString() == "AuraORM.SqlDefault")
        {
            return "global::AuraORM.ColumnDefault.CurrentTimestamp";
        }

        string type = value.Type?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ?? "object";
        return "global::AuraORM.ColumnDefault.FromConstant((" + type + ")" + Literal(value.Value) + ")";
    }

    private static string? ReadReference(Member member, List<DiagnosticInfo> diagnostics)
    {
        if (member.Attribute(ReferencesAttribute) is not { } attribute)
        {
            return null;
        }

        if (attribute.ConstructorArguments[0].Value is not INamedTypeSymbol target)
        {
            diagnostics.Add(DiagnosticInfo.Create(Diagnostics.InvalidReference, member.Location, member.Name, "the target type is not known"));
            return null;
        }

        AttributeData? targetTable = target.GetAttributes().FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == TableAttribute);
        if (targetTable is null)
        {
            diagnostics.Add(
                DiagnosticInfo.Create(Diagnostics.InvalidReference, member.Location, member.Name, $"'{target.ToDisplayString()}' has no [Table]")
            );
            return null;
        }

        string memberName = attribute.ConstructorArguments[1].Value as string ?? string.Empty;
        Member? targetMember = ReadMembers(target, [], CancellationToken.None).FirstOrDefault(m => m.Name == memberName);
        if (targetMember is null)
        {
            diagnostics.Add(
                DiagnosticInfo.Create(
                    Diagnostics.InvalidReference,
                    member.Location,
                    member.Name,
                    $"'{memberName}' is not a column of '{target.ToDisplayString()}'"
                )
            );
            return null;
        }

        if (!SymbolEqualityComparer.Default.Equals(Unwrap(member.Type), Unwrap(targetMember.Type)))
        {
            diagnostics.Add(
                DiagnosticInfo.Create(
                    Diagnostics.InvalidReference,
                    member.Location,
                    member.Name,
                    $"its type '{member.Type.ToDisplayString()}' differs from the type '{targetMember.Type.ToDisplayString()}' of '{target.Name}.{memberName}'"
                )
            );
            return null;
        }

        bool targetSnakeCase = NamedInt(targetTable, "Naming") == 1;
        string columnName =
            targetMember.Attribute(ColumnAttribute)?.ConstructorArguments[0].Value as string
            ?? (targetSnakeCase ? Naming.ToSnakeCase(targetMember.Name) : targetMember.Name);
        string tableName = targetTable.ConstructorArguments[0].Value as string ?? target.Name;
        string? schema = NamedString(targetTable, "Schema");
        return "new global::AuraORM.ForeignKeyInfo(" + Literal(tableName) + ", " + Literal(schema) + ", " + Literal(columnName) + ")";
    }

    private static List<IndexModel> ReadIndexes(INamedTypeSymbol type, List<ColumnModel> columns, List<DiagnosticInfo> diagnostics)
    {
        var indexes = new List<IndexModel>();
        foreach (AttributeData attribute in type.GetAttributes().Where(a => a.AttributeClass?.ToDisplayString() == IndexAttribute))
        {
            string name = attribute.ConstructorArguments[0].Value as string ?? string.Empty;
            var names = new List<string>();
            foreach (TypedConstant memberName in attribute.ConstructorArguments[1].Values)
            {
                string text = memberName.Value as string ?? string.Empty;
                ColumnModel? column = columns.FirstOrDefault(c => c.Member.MemberName == text);
                if (column is null)
                {
                    Location? location = attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation();
                    diagnostics.Add(DiagnosticInfo.Create(Diagnostics.UnknownIndexMember, location, name, text, type.Name));
                    continue;
                }

                names.Add(column.ColumnName);
            }

            bool unique = attribute.NamedArguments.Any(a => a.Key == "Unique" && a.Value.Value is true);
            indexes.Add(new IndexModel(name, Equatable(names), unique));
        }

        return indexes;
    }

    private static void CheckMaxLength(List<ColumnModel> columns, List<Member> members, List<IndexModel> indexes, List<DiagnosticInfo> diagnostics)
    {
        foreach (ColumnModel column in columns)
        {
            if (column.ColumnType is not ("String" or "Binary") || column.MaxLength >= 0)
            {
                continue;
            }

            bool keyed =
                column.IsPrimaryKey || column.IsUnique || column.ReferenceCode is not null || indexes.Any(i => i.Columns.Contains(column.ColumnName));
            if (keyed)
            {
                Member member = members.First(m => m.Name == column.Member.MemberName);
                diagnostics.Add(DiagnosticInfo.Create(Diagnostics.MissingMaxLength, member.Location, member.Name));
            }
        }
    }

    private static void CheckAutoIncrement(List<ColumnModel> columns, List<Member> members, List<DiagnosticInfo> diagnostics)
    {
        List<ColumnModel> auto = columns.Where(c => c.IsAutoIncrement).ToList();
        foreach (ColumnModel column in auto)
        {
            Location? location = members.First(m => m.Name == column.Member.MemberName).Location;
            if (auto.Count > 1)
            {
                diagnostics.Add(
                    DiagnosticInfo.Create(Diagnostics.InvalidAutoIncrement, location, column.Member.MemberName, "a table has at most one")
                );
            }
            else if (column.ColumnType is not ("Int16" or "Int32" or "Int64") || column.Member.IsNullable)
            {
                diagnostics.Add(
                    DiagnosticInfo.Create(
                        Diagnostics.InvalidAutoIncrement,
                        location,
                        column.Member.MemberName,
                        "the column must be a non-nullable short, int, or long"
                    )
                );
            }
            else if (!column.IsPrimaryKey || columns.Count(c => c.IsPrimaryKey) != 1)
            {
                diagnostics.Add(
                    DiagnosticInfo.Create(
                        Diagnostics.InvalidAutoIncrement,
                        location,
                        column.Member.MemberName,
                        "the column must be the only [PrimaryKey] column"
                    )
                );
            }
        }
    }

    private static void CheckDuplicateNames(List<ColumnModel> columns, INamedTypeSymbol type, List<DiagnosticInfo> diagnostics)
    {
        var seen = new Dictionary<string, ColumnModel>(StringComparer.OrdinalIgnoreCase);
        foreach (ColumnModel column in columns)
        {
            if (seen.TryGetValue(column.ColumnName, out ColumnModel? first))
            {
                Location? location = type.Locations.FirstOrDefault();
                diagnostics.Add(
                    DiagnosticInfo.Create(Diagnostics.DuplicateColumn, location, column.ColumnName, first.Member.MemberName, column.Member.MemberName)
                );
            }
            else
            {
                seen.Add(column.ColumnName, column);
            }
        }
    }

    private static void CheckReservedNames(
        INamedTypeSymbol type,
        List<Member> members,
        string[] generatedNames,
        string[] reservedMemberNames,
        List<DiagnosticInfo> diagnostics
    )
    {
        foreach (string name in generatedNames)
        {
            foreach (ISymbol existing in type.GetMembers(name))
            {
                diagnostics.Add(DiagnosticInfo.Create(Diagnostics.ReservedName, existing.Locations.FirstOrDefault(), name, type.Name + "." + name));
            }
        }

        foreach (Member member in members.Where(m => reservedMemberNames.Contains(m.Name)))
        {
            diagnostics.Add(DiagnosticInfo.Create(Diagnostics.ReservedName, member.Location, member.Name, "a member of the generated classes"));
        }
    }

    private static bool CheckConstructor(INamedTypeSymbol type, List<Member> members, List<DiagnosticInfo> diagnostics)
    {
        if (members.Any(m => m.IsPositional))
        {
            return true;
        }

        bool parameterless = type.InstanceConstructors.Any(c =>
            c.Parameters.Length == 0 && c.DeclaredAccessibility is Accessibility.Public or Accessibility.Internal
        );
        if (!parameterless)
        {
            diagnostics.Add(DiagnosticInfo.Create(Diagnostics.NoConstructor, type.Locations.FirstOrDefault(), type.Name));
        }

        return false;
    }

    private static TypeTarget? ReadTarget(INamedTypeSymbol type, List<DiagnosticInfo> diagnostics)
    {
        string? declaration = Declaration(type);
        var containers = new List<string>();
        bool valid = declaration is not null && type.TypeKind == TypeKind.Class && !type.IsGenericType && IsPartial(type);
        for (INamedTypeSymbol? outer = type.ContainingType; outer is not null; outer = outer.ContainingType)
        {
            string? outerDeclaration = Declaration(outer);
            valid &= outerDeclaration is not null && !outer.IsGenericType && IsPartial(outer);
            containers.Insert(0, outerDeclaration ?? string.Empty);
        }

        if (!valid)
        {
            diagnostics.Add(DiagnosticInfo.Create(Diagnostics.NotPartial, type.Locations.FirstOrDefault(), type.Name));
            return null;
        }

        string? ns = type.ContainingNamespace.IsGlobalNamespace ? null : type.ContainingNamespace.ToDisplayString();
        return new TypeTarget(ns, Equatable(containers), declaration!, type.Name);
    }

    private static string? Declaration(INamedTypeSymbol type)
    {
        string? keyword = type switch
        {
            { IsRecord: true, TypeKind: TypeKind.Class } => "record",
            { IsRecord: true, TypeKind: TypeKind.Struct } => "record struct",
            { TypeKind: TypeKind.Class } => "class",
            { TypeKind: TypeKind.Struct } => "struct",
            { TypeKind: TypeKind.Interface } => "interface",
            _ => null,
        };
        return keyword is null ? null : "partial " + keyword + " " + type.Name;
    }

    private static bool IsPartial(INamedTypeSymbol type) =>
        type.DeclaringSyntaxReferences.Length > 0
        && type.DeclaringSyntaxReferences.All(r =>
            r.GetSyntax() is TypeDeclarationSyntax declaration && declaration.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword))
        );

    private static List<Member> ReadMembers(INamedTypeSymbol type, List<DiagnosticInfo> diagnostics, CancellationToken cancellationToken)
    {
        var members = new List<Member>();
        IMethodSymbol? primary = type.InstanceConstructors.FirstOrDefault(c =>
            c.DeclaringSyntaxReferences.Any(r => r.GetSyntax(cancellationToken) is RecordDeclarationSyntax { ParameterList: not null })
        );
        if (primary is not null)
        {
            foreach (IParameterSymbol parameter in primary.Parameters)
            {
                IPropertySymbol? property = type.GetMembers(parameter.Name).OfType<IPropertySymbol>().FirstOrDefault();
                ImmutableArray<AttributeData> attributes = property is null
                    ? parameter.GetAttributes()
                    : parameter.GetAttributes().AddRange(property.GetAttributes());
                members.Add(CreateMember(parameter.Name, parameter.Type, attributes, parameter.Locations.FirstOrDefault(), true, diagnostics));
            }
        }

        foreach (IPropertySymbol property in type.GetMembers().OfType<IPropertySymbol>())
        {
            bool settable = property.SetMethod is { DeclaredAccessibility: Accessibility.Public or Accessibility.Internal };
            if (
                property.IsStatic
                || property.IsIndexer
                || property.IsImplicitlyDeclared
                || property.DeclaredAccessibility != Accessibility.Public
                || !settable
                || members.Any(m => m.Name == property.Name)
                || property.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == NotColumnAttribute)
            )
            {
                continue;
            }

            members.Add(
                CreateMember(property.Name, property.Type, property.GetAttributes(), property.Locations.FirstOrDefault(), false, diagnostics)
            );
        }

        return members;
    }

    private static Member CreateMember(
        string name,
        ITypeSymbol type,
        ImmutableArray<AttributeData> attributes,
        Location? location,
        bool positional,
        List<DiagnosticInfo> diagnostics
    )
    {
        bool isReference = type.IsReferenceType;
        if (isReference && type.NullableAnnotation == NullableAnnotation.None)
        {
            diagnostics.Add(DiagnosticInfo.Create(Diagnostics.NullableDisabled, location, name));
        }

        bool isNullable = isReference
            ? type.NullableAnnotation == NullableAnnotation.Annotated
            : type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T };
        string typeName = type.ToDisplayString(TypeFormat);
        string nullableTypeName = isNullable ? typeName : typeName + "?";
        var model = new MemberModel(name, typeName, nullableTypeName, isReference, isNullable, positional);
        return new Member(name, type, attributes, location, positional, model);
    }

    private static ITypeSymbol Unwrap(ITypeSymbol type) =>
        type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullable
            ? nullable.TypeArguments[0]
            : type.WithNullableAnnotation(NullableAnnotation.NotAnnotated);

    private static string? NamedString(AttributeData attribute, string name) =>
        attribute.NamedArguments.FirstOrDefault(a => a.Key == name).Value.Value as string;

    private static int NamedInt(AttributeData attribute, string name) =>
        attribute.NamedArguments.FirstOrDefault(a => a.Key == name).Value.Value as int? ?? 0;

    private static string Literal(object? value) =>
        value switch
        {
            null => "null",
            string s => SymbolDisplay.FormatLiteral(s, quote: true),
            bool b => b ? "true" : "false",
            char c => SymbolDisplay.FormatLiteral(c, quote: true),
            _ => SymbolDisplay.FormatPrimitive(value, quoteStrings: true, useHexadecimalNumbers: false),
        };

    private static EquatableArray<T> Equatable<T>(List<T> items)
        where T : IEquatable<T> => new(ImmutableArray.CreateRange(items));

    /// <summary>
    /// A member as read from the symbols, with what diagnostics and models need.
    /// </summary>
    private sealed record Member(
        string Name,
        ITypeSymbol Type,
        ImmutableArray<AttributeData> Attributes,
        Location? Location,
        bool IsPositional,
        MemberModel Model
    )
    {
        public AttributeData? Attribute(string fullName) => Attributes.FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == fullName);
    }
}
