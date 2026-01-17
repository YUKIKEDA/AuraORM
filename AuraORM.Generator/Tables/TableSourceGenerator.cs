using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace AuraORM.Generator.Tables;

[Generator]
public class TableSourceGenerator : IIncrementalGenerator
{
    private const string TableAttributeFullName = "AuraORM.Builder.Attributes.TableAttribute";
    private const string ColumnAttributeFullName = "AuraORM.Builder.Attributes.ColumnAttribute";
    private const string PkAttributeFullName = "AuraORM.Builder.Attributes.PkAttribute";
    private const string AutoIncrementAttributeFullName = "AuraORM.Builder.Attributes.AutoIncrementAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // [Table]属性を持つ型を収集
        var tableTypes = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                TableAttributeFullName,
                predicate: (node, _) => node is RecordDeclarationSyntax or ClassDeclarationSyntax,
                transform: (ctx, ct) => GetTableInfo(ctx, ct))
            .Where(info => info is not null)
            .Select((info, _) => info!);

        // コード生成
        context.RegisterSourceOutput(tableTypes, (ctx, tableInfo) =>
        {
            var source = GenerateSource(tableInfo);
            ctx.AddSource($"{tableInfo.TypeName}.g.cs", SourceText.From(source, Encoding.UTF8));
        });
    }

    /// <summary>
    /// テーブル情報を取得する
    /// </summary>
    /// <param name="context">シンタックスコンテキスト</param>
    /// <param name="ct">キャンセルトークン</param>
    /// <returns>テーブル情報</returns>
    private static TableInfo? GetTableInfo(GeneratorAttributeSyntaxContext context, CancellationToken ct)
    {
        if (context.TargetSymbol is not INamedTypeSymbol typeSymbol)
        {
            return null;
        }

        // テーブル属性からテーブル名を取得
        var tableAttribute = context.Attributes.FirstOrDefault(a =>
            a.AttributeClass?.ToDisplayString() == TableAttributeFullName);

        if (tableAttribute is null)
        {
            return null;
        }

        var tableName = tableAttribute.ConstructorArguments.Length > 0
            ? tableAttribute.ConstructorArguments[0].Value?.ToString() ?? typeSymbol.Name.ToLowerInvariant()
            : typeSymbol.Name.ToLowerInvariant();

        // カラム情報を収集
        var columns = new List<ColumnInfo>();
        var ordinal = 0;

        // record型のプライマリコンストラクタパラメータを取得
        if (context.TargetNode is RecordDeclarationSyntax recordSyntax && recordSyntax.ParameterList != null)
        {
            foreach (var parameter in recordSyntax.ParameterList.Parameters)
            {
                var parameterName = parameter.Identifier.ValueText;
                var semanticModel = context.SemanticModel;

                if (semanticModel.GetDeclaredSymbol(parameter, ct) is not IParameterSymbol parameterSymbol)
                {
                    continue;
                }

                var columnName = GetColumnName(parameterSymbol, parameterName);
                var isPk = HasAttribute(parameterSymbol, PkAttributeFullName);
                var isAutoIncrement = HasAttribute(parameterSymbol, AutoIncrementAttributeFullName);
                var typeName = parameterSymbol.Type.ToDisplayString();

                // Nullable判定: Nullable<T>か、参照型でNullableAnnotation.Annotatedか
                var isNullable = IsNullableType(parameterSymbol.Type);

                // Enum型の検出
                var (isEnum, underlyingEnumType) = GetEnumInfo(parameterSymbol.Type);

                columns.Add(new ColumnInfo(
                    PropertyName: parameterName,
                    ColumnName: columnName,
                    TypeName: typeName,
                    IsNullable: isNullable,
                    IsPrimaryKey: isPk,
                    IsAutoIncrement: isAutoIncrement,
                    Ordinal: ordinal++,
                    IsEnum: isEnum,
                    UnderlyingEnumType: underlyingEnumType));
            }
        }

        // 通常のプロパティも確認
        foreach (var member in typeSymbol.GetMembers())
        {
            if (member is not IPropertySymbol property)
            {
                continue;
            }

            // recordのプライマリコンストラクタで既に処理済みかチェック
            if (columns.Exists(c => c.PropertyName == property.Name))
            {
                continue;
            }

            // 読み取り専用でないプロパティのみ
            if (property.IsReadOnly && property.SetMethod == null)
            {
                continue;
            }

            var columnName = GetColumnName(property, property.Name);
            var isPk = HasAttribute(property, PkAttributeFullName);
            var isAutoIncrement = HasAttribute(property, AutoIncrementAttributeFullName);
            var typeName = property.Type.ToDisplayString();

            // Nullable判定
            var isNullable = IsNullableType(property.Type);

            // Enum型の検出
            var (isEnum, underlyingEnumType) = GetEnumInfo(property.Type);

            columns.Add(new ColumnInfo(
                PropertyName: property.Name,
                ColumnName: columnName,
                TypeName: typeName,
                IsNullable: isNullable,
                IsPrimaryKey: isPk,
                IsAutoIncrement: isAutoIncrement,
                Ordinal: ordinal++,
                IsEnum: isEnum,
                UnderlyingEnumType: underlyingEnumType));
        }

        return new TableInfo(
            Namespace: typeSymbol.ContainingNamespace.IsGlobalNamespace
                ? null
                : typeSymbol.ContainingNamespace.ToDisplayString(),
            TypeName: typeSymbol.Name,
            TableName: tableName,
            Columns: columns,
            IsRecord: context.TargetNode is RecordDeclarationSyntax);
    }

    /// <summary>
    /// カラム名を取得する
    /// </summary>
    /// <param name="symbol">シンボル</param>
    /// <param name="fallback">フォールバック</param>
    /// <returns>カラム名</returns>
    private static string GetColumnName(ISymbol symbol, string fallback)
    {
        foreach (var attr in symbol.GetAttributes())
        {
            if (attr.AttributeClass?.ToDisplayString() == ColumnAttributeFullName)
            {
                if (attr.ConstructorArguments.Length > 0 && attr.ConstructorArguments[0].Value is string name)
                {
                    return name;
                }
            }
        }
        return fallback.ToLowerInvariant();
    }

    /// <summary>
    /// 属性があるかどうかを判定する
    /// </summary>
    /// <param name="symbol">シンボル</param>
    /// <param name="attributeFullName">属性名</param>
    /// <returns>属性があるかどうか</returns>
    private static bool HasAttribute(ISymbol symbol, string attributeFullName)
    {
        return symbol.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == attributeFullName);
    }

    /// <summary>
    /// Nullable型かどうかを判定する
    /// </summary>
    /// <param name="typeSymbol">型シンボル</param>
    /// <returns>Nullable型かどうか</returns>
    private static bool IsNullableType(ITypeSymbol typeSymbol)
    {
        // Nullable<T>かどうかチェック
        if (typeSymbol is INamedTypeSymbol namedType &&
            namedType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
        {
            return true;
        }

        // 参照型でNullableアノテーションがあるかチェック
        return !typeSymbol.IsValueType && typeSymbol.NullableAnnotation == NullableAnnotation.Annotated;
    }

    /// <summary>
    /// Enum型かどうかを判定し、基になる型を取得する
    /// </summary>
    /// <param name="typeSymbol">型シンボル</param>
    /// <returns>Enum型かどうか、基になる型</returns>
    private static (bool IsEnum, string? UnderlyingType) GetEnumInfo(ITypeSymbol typeSymbol)
    {
        // Nullable<T>の場合は内部の型をチェック
        var actualType = typeSymbol;
        if (typeSymbol is INamedTypeSymbol namedType && 
            namedType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T &&
            namedType.TypeArguments.Length == 1)
        {
            actualType = namedType.TypeArguments[0];
        }

        if (actualType.TypeKind == TypeKind.Enum)
        {
            var enumType = (INamedTypeSymbol)actualType;
            var underlyingType = enumType.EnumUnderlyingType?.ToDisplayString() ?? "int";
            return (true, underlyingType);
        }

        return (false, null);
    }

    /// <summary>
    /// ソースコードを生成する
    /// </summary>
    /// <param name="tableInfo">テーブル情報</param>
    /// <returns>ソースコード</returns>
    private static string GenerateSource(TableInfo tableInfo)
    {
        var sb = new StringBuilder();

        // ヘッダー
        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Data;");
        sb.AppendLine("using AuraORM.Builder.Query;");
        sb.AppendLine("using AuraORM.Builder.Table;");
        sb.AppendLine();

        // 名前空間
        if (tableInfo.Namespace != null)
        {
            sb.AppendLine($"namespace {tableInfo.Namespace}");
            sb.AppendLine("{");
        }

        // ----------------メインのソースコードを生成----------------

        // テーブルクラスを生成
        GenerateTableClass(sb, tableInfo);

        // -------------------------------------------------------

        if (tableInfo.Namespace != null)
        {
            sb.AppendLine("}");
        }

        return sb.ToString();
    }

    /// <summary>
    /// テーブルクラスを生成する
    /// </summary>
    /// <param name="sb">StringBuilder</param>
    /// <param name="tableInfo">テーブル情報</param>
    private static void GenerateTableClass(StringBuilder sb, TableInfo tableInfo)
    {
        var indent = tableInfo.Namespace != null ? "    " : "";

        sb.AppendLine($"{indent}/// <summary>");
        sb.AppendLine($"{indent}/// {tableInfo.TypeName}テーブルクラス");
        sb.AppendLine($"{indent}/// </summary>");
        sb.AppendLine($"{indent}public sealed class {tableInfo.TypeName}Table : ITable");
        sb.AppendLine($"{indent}{{");

        // シングルトンインスタンス
        sb.AppendLine($"{indent}{indent}public static readonly {tableInfo.TypeName}Table Instance = new {tableInfo.TypeName}Table();");
        sb.AppendLine();

        // テーブル名
        sb.AppendLine($"{indent}{indent}public string TableName => \"{tableInfo.TableName}\";");
        sb.AppendLine();

        // カラム名配列
        var columnNames = string.Join(", ", tableInfo.Columns.Select(c => $"\"{c.ColumnName}\""));
        sb.AppendLine($"{indent}{indent}public string[] ColumnNames => new[] {{ {columnNames} }};");
        sb.AppendLine();

        // プライマリキー情報
        var pkColumn = tableInfo.Columns.FirstOrDefault(c => c.IsPrimaryKey);
        var pkColumnName = pkColumn != null ? $"\"{pkColumn.ColumnName}\"" : "null";
        sb.AppendLine($"{indent}{indent}public string? PrimaryKeyColumnName => {pkColumnName};");
        sb.AppendLine();

        // AutoIncrement情報
        var isAutoIncrement = tableInfo.Columns.Any(c => c.IsAutoIncrement);
        sb.AppendLine($"{indent}{indent}public bool IsAutoIncrement => {(isAutoIncrement ? "true" : "false")};");
        sb.AppendLine();

        // INSERT時に使用するカラム名（AutoIncrement列を除く）
        var insertColumnNames = string.Join(", ", tableInfo.Columns
            .Where(c => !c.IsAutoIncrement)
            .Select(c => $"\"{c.ColumnName}\""));
        sb.AppendLine($"{indent}{indent}public string[] InsertColumnNames => new[] {{ {insertColumnNames} }};");
        sb.AppendLine();

        // 各カラム
        foreach (var column in tableInfo.Columns)
        {
            sb.AppendLine($"{indent}{indent}/// <summary>{column.ColumnName}カラム</summary>");
            sb.AppendLine($"{indent}{indent}public static readonly Column<{column.TypeName}> {column.PropertyName} = new Column<{column.TypeName}>(\"{column.ColumnName}\", {column.Ordinal});");
        }
        
        sb.AppendLine($"{indent}}}");
    }
}
