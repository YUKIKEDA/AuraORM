using System.Text;

using AuraORM.Generator.Query;
using AuraORM.Generator.Tables;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace AuraORM.Generator.QueryResults;

/// <summary>
/// [QueryResult]属性が付与されたrecordへのクエリ結果マッピング用ソースジェネレータ
/// </summary>
[Generator]
public class QueryResultSourceGenerator : IIncrementalGenerator
{
    private const string QueryResultAttributeFullName = "AuraORM.Builder.Attributes.QueryResultAttribute";
    private const string ColumnAttributeFullName = "AuraORM.Builder.Attributes.ColumnAttribute";

    /// <summary>
    /// 
    /// </summary>
    /// <param name="context"></param>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // [QueryResult]属性を持つ型を収集
        var queryResultTypes = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                QueryResultAttributeFullName,
                predicate: (node, _) => node is RecordDeclarationSyntax or ClassDeclarationSyntax,
                transform: (ctx, ct) => GetQueryResult(ctx, ct))
            .Where(result => result is not null)
            .Select((result, _) => result!); // null許容を外す

        // コード生成
        context.RegisterSourceOutput(queryResultTypes, (ctx, result) =>
        {
            var source = GenerateSource(result);
            ctx.AddSource($"{result.TypeName}.QueryResult.g.cs", SourceText.From(source, Encoding.UTF8));
        });
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="context"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    private static QueryResult? GetQueryResult(GeneratorAttributeSyntaxContext context, CancellationToken ct)
    {
        if (context.TargetSymbol is not INamedTypeSymbol typeSymbol)
        {
            return null;
        }

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
                var typeName = parameterSymbol.Type.ToDisplayString();

                // Nullable判定
                var isNullable = IsNullableType(parameterSymbol.Type);

                var (isEnum, underlyingEnumType) = GetEnumInfo(parameterSymbol.Type);

                columns.Add(new ColumnInfo(
                    PropertyName: parameterName,
                    ColumnName: columnName,
                    TypeName: typeName,
                    IsNullable: isNullable,
                    IsPrimaryKey: false,
                    IsAutoIncrement: false,
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
            var typeName = property.Type.ToDisplayString();

            // Nullable判定
            var isNullable = IsNullableType(property.Type);

            var (isEnum, underlyingEnumType) = GetEnumInfo(property.Type);

            columns.Add(new ColumnInfo(
                PropertyName: property.Name,
                ColumnName: columnName,
                TypeName: typeName,
                IsNullable: isNullable,
                IsPrimaryKey: false,
                IsAutoIncrement: false,
                Ordinal: ordinal++,
                IsEnum: isEnum,
                UnderlyingEnumType: underlyingEnumType));
        }

        return new QueryResult(
            Namespace: typeSymbol.ContainingNamespace.IsGlobalNamespace
                ? null
                : typeSymbol.ContainingNamespace.ToDisplayString(),
            TypeName: typeSymbol.Name,
            Columns: columns,
            IsRecord: context.TargetNode is RecordDeclarationSyntax);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="symbol"></param>
    /// <param name="fallback"></param>
    /// <returns></returns>
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
    /// 
    /// </summary>
    /// <param name="typeSymbol"></param>
    /// <returns></returns>
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
    /// 
    /// </summary>
    /// <param name="typeSymbol"></param>
    /// <returns></returns>
    private static (bool IsEnum, string? UnderlyingType) GetEnumInfo(ITypeSymbol typeSymbol)
    {
        var actualType = typeSymbol;
        if (typeSymbol is INamedTypeSymbol namedType 
            && namedType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T 
            && namedType.TypeArguments.Length == 1)
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
    /// 
    /// </summary>
    /// <param name="result"></param>
    /// <returns></returns>
    private static string GenerateSource(QueryResult info)
    {
        var sb = new StringBuilder();

        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Data;");
        sb.AppendLine("using AuraORM.Builder.Query;");
        sb.AppendLine("using AuraORM.Builder.Table;");
        sb.AppendLine();

        // 名前空間
        if (info.Namespace != null)
        {
            sb.AppendLine($"namespace {info.Namespace}");
            sb.AppendLine("{");
        }

        var indent = info.Namespace != null ? "    " : "";

        // ----------------メインのソースコードを生成----------------

        // クエリビルダー拡張メソッド生成
        GenerateQueryBuilderExtensions(sb, info, indent);

        sb.AppendLine();

        // マッパー拡張メソッドを生成
        GenerateMapperExtensions(sb, info, indent);

        // -------------------------------------------------------

        if (info.Namespace != null)
        {
            sb.AppendLine("}");
        }

        return sb.ToString();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="sb"></param>
    /// <param name="info"></param>
    /// <param name="indent"></param>
    private static void GenerateQueryBuilderExtensions(StringBuilder sb, QueryResult info, string indent)
    {
        sb.AppendLine($"{indent}/// <summary>");
        sb.AppendLine($"{indent}/// {info.TypeName}用のクエリビルダー拡張メソッド");
        sb.AppendLine($"{indent}/// </summary>");
        sb.AppendLine($"{indent}public static class {info.TypeName}QueryResultExtensions");
        sb.AppendLine($"{indent}{{");
        // SelectColumns拡張メソッド生成
        GenerateSelectColumnsQueryExtensions(sb, info, indent);

        sb.AppendLine($"{indent}}}");
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="sb"></param>
    /// <param name="info"></param>
    /// <param name="indent"></param>
    private static void GenerateSelectColumnsQueryExtensions(StringBuilder sb, QueryResult info, string indent)
    {
        var typeName = info.TypeName;

        sb.AppendLine($"{indent}    /// <summary>");
        sb.AppendLine($"{indent}    /// SELECT COLUMNSクエリを非同期で実行し、結果を{typeName}のリストで取得");
        sb.AppendLine($"{indent}    /// </summary>");
        sb.AppendLine($"{indent}    public static async System.Threading.Tasks.Task<System.Collections.Generic.List<{typeName}>> ToListAsync<TResult>(");
        sb.AppendLine($"{indent}        this ISelectColumnsQuery query,");
        sb.AppendLine($"{indent}        System.Threading.CancellationToken token = default) where TResult : {typeName}");
        sb.AppendLine($"{indent}    {{");
        sb.AppendLine($"{indent}        var results = new System.Collections.Generic.List<{typeName}>();");
        sb.AppendLine();
        sb.AppendLine($"{indent}        var db = query.DbContext;");
        sb.AppendLine();
        sb.AppendLine($"{indent}        var transaction = db.TransactionContext?.Transaction;");
        sb.AppendLine($"{indent}        var connection = db.Connection;");
        sb.AppendLine();
        sb.AppendLine($"{indent}        var (sql, parameters) = query.Build();");
        sb.AppendLine();
        sb.AppendLine($"{indent}        await using var command = connection.CreateCommand();");
        sb.AppendLine($"{indent}        command.CommandText = sql;");
        sb.AppendLine($"{indent}        command.Transaction = transaction;");
        sb.AppendLine();
        sb.AppendLine($"{indent}        foreach (var (name, value) in parameters)");
        sb.AppendLine($"{indent}        {{");
        sb.AppendLine($"{indent}            var param = command.CreateParameter();");
        sb.AppendLine($"{indent}            param.ParameterName = name;");
        sb.AppendLine($"{indent}            param.Value = value ?? System.DBNull.Value;");
        sb.AppendLine($"{indent}            command.Parameters.Add(param);");
        sb.AppendLine($"{indent}        }}");
        sb.AppendLine();
        sb.AppendLine($"{indent}        await using var reader = await command.ExecuteReaderAsync(token).ConfigureAwait(false);");
        sb.AppendLine($"{indent}        while (await reader.ReadAsync(token).ConfigureAwait(false))");
        sb.AppendLine($"{indent}        {{");
        sb.AppendLine($"{indent}            results.Add(reader.Map{typeName}ByColumnName());");
        sb.AppendLine($"{indent}        }}");
        sb.AppendLine();
        sb.AppendLine($"{indent}        return results;");
        sb.AppendLine($"{indent}    }}");
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="sb"></param>
    /// <param name="info"></param>
    /// <param name="indent"></param>
    private static void GenerateMapperExtensions(StringBuilder sb, QueryResult info, string indent)
    {
        sb.AppendLine($"{indent}/// <summary>");
        sb.AppendLine($"{indent}/// {info.TypeName}のマッパー拡張メソッド");
        sb.AppendLine($"{indent}/// </summary>");
        sb.AppendLine($"{indent}public static class {info.TypeName}Extensions");
        sb.AppendLine($"{indent}{{");
        // Map メソッド (IDataReaderから読み込み)
        sb.AppendLine($"{indent}    /// <summary>");
        sb.AppendLine($"{indent}    /// IDataReaderから{info.TypeName}を生成（リフレクション不使用）");
        sb.AppendLine($"{indent}    /// </summary>");
        sb.AppendLine($"{indent}    public static {info.TypeName} Map{info.TypeName}ByColumnName(this IDataReader reader)");
        sb.AppendLine($"{indent}    {{");

        if (info.IsRecord)
        {
            // recordの場合はコンストラクタを使用
            var args = string.Join(",\n" + indent + "            ", info.Columns.Select(c => GenerateReaderCall(c)));
            sb.AppendLine($"{indent}        return new {info.TypeName}(");
            sb.AppendLine($"{indent}            {args});");
        }
        else
        {
            // クラスの場合はオブジェクト初期化子を使用
            sb.AppendLine($"{indent}        return new {info.TypeName}");
            sb.AppendLine($"{indent}        {{");
            for (int i = 0; i < info.Columns.Count; i++)
            {
                var column = info.Columns[i];
                var comma = i < info.Columns.Count - 1 ? "," : "";
                sb.AppendLine($"{indent}            {column.PropertyName} = {GenerateReaderCall(column)}{comma}");
            }
            sb.AppendLine($"{indent}        }};");
        }

        sb.AppendLine($"{indent}    }}");
        sb.AppendLine($"{indent}}}");
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="column"></param>
    /// <returns></returns>
    private static string GenerateReaderCall(ColumnInfo column)
    {
        var baseType = column.TypeName.TrimEnd('?');
        var isNullable = column.TypeName.EndsWith("?") || column.IsNullable;

        var columnName = $"\"{column.ColumnName}\"";
        var nullCheck = $"reader.IsDBNull(reader.GetOrdinal({columnName}))";

        // Enum型の処理
        if (column.IsEnum)
        {
            var underlyingGetter = column.UnderlyingEnumType switch
            {
                "int" or "System.Int32" => "GetInt32",
                "long" or "System.Int64" => "GetInt64",
                "short" or "System.Int16" => "GetInt16",
                "byte" or "System.Byte" => "GetByte",
                "sbyte" or "System.SByte" => "GetByte", // SByteは特殊対応
                "uint" or "System.UInt32" => "GetInt32", // UInt32はInt32で読み取り
                "ulong" or "System.UInt64" => "GetInt64", // UInt64はInt64で読み取り
                "ushort" or "System.UInt16" => "GetInt16", // UInt16はInt16で読み取り
                _ => "GetInt32"
            };

            var getterCall = $"reader.{underlyingGetter}(reader.GetOrdinal({columnName}))";

            if (isNullable)
            {
                // Nullable<Enum>の場合、明示的なキャストが必要
                return $"{nullCheck} ? ({baseType}?)null : ({baseType}){getterCall}";
            }

            return $"({baseType}){getterCall}";
        }

        var getterMethod = baseType switch
        {
            "int" or "System.Int32" => "GetInt32",
            "long" or "System.Int64" => "GetInt64",
            "short" or "System.Int16" => "GetInt16",
            "byte" or "System.Byte" => "GetByte",
            "bool" or "System.Boolean" => "GetBoolean",
            "float" or "System.Single" => "GetFloat",
            "double" or "System.Double" => "GetDouble",
            "decimal" or "System.Decimal" => "GetDecimal",
            "string" or "System.String" => "GetString",
            "System.DateTime" or "DateTime" => "GetDateTime",
            "System.Guid" or "Guid" => "GetGuid",
            "System.DateTimeOffset" or "DateTimeOffset" => null, // 特殊対応
            "System.TimeSpan" or "TimeSpan" => null, // 特殊対応
            _ => null
        };

        // DateTimeOffset/TimeSpanの特殊対応
        if (baseType is "System.DateTimeOffset" or "DateTimeOffset")
        {
            return isNullable
                ? $"reader.IsDBNull({column.Ordinal}) ? null : (DateTimeOffset)reader.GetValue({column.Ordinal})"
                : $"(DateTimeOffset)reader.GetValue({column.Ordinal})";
        }

        if (baseType is "System.TimeSpan" or "TimeSpan")
        {
            return isNullable
                ? $"reader.IsDBNull({column.Ordinal}) ? null : (TimeSpan)reader.GetValue({column.Ordinal})"
                : $"(TimeSpan)reader.GetValue({column.Ordinal})";
        }

        if (getterMethod == null)
        {
            // 未知の型はGetValueで取得
            var getValueCall = $"reader.GetValue(reader.GetOrdinal({columnName}))";

            return isNullable
                ? $"{nullCheck} ? default : ({column.TypeName}){getValueCall}"
                : $"({baseType}){getValueCall}";
        }

        var methodCall = $"reader.{getterMethod}(reader.GetOrdinal({columnName}))";

        return isNullable
            ? $"{nullCheck} ? null : {methodCall}"
            : methodCall;
    }
}
