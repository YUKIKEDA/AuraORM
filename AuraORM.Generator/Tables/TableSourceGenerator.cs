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
            .Select((info, _) => info!); // null許容を外す

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
        sb.AppendLine("using AuraORM.Builder.Conditions;");
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
        
        sb.AppendLine();
        
        // クエリビルダー拡張メソッドを生成
        GenerateQueryBuilderExtensions(sb, tableInfo);

        sb.AppendLine();

        // マッパー拡張メソッドを生成
        GenerateMapperExtensions(sb, tableInfo);


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
        sb.AppendLine($"{indent}    public static readonly {tableInfo.TypeName}Table Instance = new {tableInfo.TypeName}Table();");
        sb.AppendLine();

        // テーブル名
        sb.AppendLine($"{indent}    public string TableName => \"{tableInfo.TableName}\";");
        sb.AppendLine();

        // カラム名配列
        var columnNames = string.Join(", ", tableInfo.Columns.Select(c => $"\"{c.ColumnName}\""));
        sb.AppendLine($"{indent}    public string[] ColumnNames => new[] {{ {columnNames} }};");
        sb.AppendLine();

        // プライマリキー情報
        var pkColumn = tableInfo.Columns.FirstOrDefault(c => c.IsPrimaryKey);
        var pkColumnName = pkColumn != null ? $"\"{pkColumn.ColumnName}\"" : "null";
        sb.AppendLine($"{indent}    public string? PrimaryKeyColumnName => {pkColumnName};");
        sb.AppendLine();

        // AutoIncrement情報
        var isAutoIncrement = tableInfo.Columns.Any(c => c.IsAutoIncrement);
        sb.AppendLine($"{indent}    public bool IsAutoIncrement => {(isAutoIncrement ? "true" : "false")};");
        sb.AppendLine();

        // INSERT時に使用するカラム名（AutoIncrement列を除く）
        var insertColumnNames = string.Join(", ", tableInfo.Columns
            .Where(c => !c.IsAutoIncrement)
            .Select(c => $"\"{c.ColumnName}\""));
        sb.AppendLine($"{indent}    public string[] InsertColumnNames => new[] {{ {insertColumnNames} }};");
        sb.AppendLine();

        // 各カラム
        foreach (var column in tableInfo.Columns)
        {
            sb.AppendLine($"{indent}    /// <summary>{column.ColumnName}カラム</summary>");
            sb.AppendLine($"{indent}    public static readonly Column<{column.TypeName}> {column.PropertyName} = new Column<{column.TypeName}>(\"{column.ColumnName}\", {column.Ordinal});");
        }
        
        sb.AppendLine($"{indent}}}");
    }

    /// <summary>
    /// クエリビルダー拡張メソッドを生成する
    /// </summary>
    /// <param name="sb">StringBuilder</param>
    /// <param name="tableInfo">テーブル情報</param>
    private static void GenerateQueryBuilderExtensions(StringBuilder sb, TableInfo tableInfo)
    {
        var indent = tableInfo.Namespace != null ? "    " : "";

        sb.AppendLine($"{indent}/// <summary>");
        sb.AppendLine($"{indent}/// {tableInfo.TypeName}用のクエリビルダー拡張メソッド");
        sb.AppendLine($"{indent}/// </summary>");
        sb.AppendLine($"{indent}public static class {tableInfo.TypeName}QueryBuilderExtensions");
        sb.AppendLine($"{indent}{{");

        // Select句拡張メソッド (必要に応じて追加)
        GenerateSelectQueryExtensions(sb, tableInfo);
        
        sb.AppendLine();

        // Select Columns拡張メソッド (必要に応じて追加)
        GenerateSelectColumnsQueryExtensions(sb, tableInfo);

        // Insert句拡張メソッド (必要に応じて追加)

        // Update句拡張メソッド (必要に応じて追加)

        // Delete句拡張メソッド (必要に応じて追加)

        sb.AppendLine($"{indent}}}");
    }

    private static void GenerateSelectQueryExtensions(StringBuilder sb, TableInfo tableInfo)
    {
        var indent = tableInfo.Namespace != null ? "    " : "";

        var typeName = tableInfo.TypeName;
        var tableTypeName = $"{typeName}Table";

        // Fromメソッド
        sb.AppendLine($"{indent}    /// <summary>");
        sb.AppendLine($"{indent}    /// {typeName}テーブルを指定してクエリビルダを生成（型推論付き）");
        sb.AppendLine($"{indent}    /// </summary>");
        sb.AppendLine($"{indent}    public static AuraORM.Builder.Query.SelectQuery<{typeName}, {tableTypeName}> From(this AuraORM.Builder.Query.SelectQuery builder, {tableTypeName} table)");
        sb.AppendLine($"{indent}    {{");
        sb.AppendLine($"{indent}        return builder.From<{typeName}, {tableTypeName}>(table);");
        sb.AppendLine($"{indent}    }}");

        sb.AppendLine();

        // ToListAsync()メソッド
        sb.AppendLine($"{indent}    /// <summary>");
        sb.AppendLine($"{indent}    /// SELECTクエリを非同期で実行し、結果をリストで取得");
        sb.AppendLine($"{indent}    /// </summary>");
        sb.AppendLine($"{indent}    public static async System.Threading.Tasks.Task<System.Collections.Generic.List<{typeName}>> ToListAsync(");
        sb.AppendLine($"{indent}        this AuraORM.Builder.Query.SelectQuery<{typeName}, {tableTypeName}> query,");
        sb.AppendLine($"{indent}        System.Threading.CancellationToken token = default)");
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
        sb.AppendLine($"{indent}            results.Add(reader.Map{typeName}());");
        sb.AppendLine($"{indent}        }}");
        sb.AppendLine();
        sb.AppendLine($"{indent}        return results;");
        sb.AppendLine($"{indent}    }}");
    }

    private static void GenerateSelectColumnsQueryExtensions(StringBuilder sb, TableInfo tableInfo)
    {
        var indent = tableInfo.Namespace != null ? "    " : "";

        var typeName = tableInfo.TypeName;
        var tableTypeName = $"{typeName}Table";

        // Fromメソッド
        sb.AppendLine($"{indent}    /// <summary>");
        sb.AppendLine($"{indent}    /// {typeName}テーブルを指定してクエリビルダを生成（型推論付き）");
        sb.AppendLine($"{indent}    /// </summary>");
        sb.AppendLine($"{indent}    public static AuraORM.Builder.Query.SelectColumnsQuery<{typeName}, {tableTypeName}> From(this AuraORM.Builder.Query.SelectColumnsQuery builder, {tableTypeName} table)");
        sb.AppendLine($"{indent}    {{");
        sb.AppendLine($"{indent}        return builder.From<{typeName}, {tableTypeName}>(table);");
        sb.AppendLine($"{indent}    }}");

        sb.AppendLine();

    }

    // TODO ヘルパークラスにまとめるべきか検討
    private static void GenerateMapperExtensions(StringBuilder sb, TableInfo tableInfo)
    {
        var indent = tableInfo.Namespace != null ? "    " : "";

        sb.AppendLine($"{indent}/// <summary>");
        sb.AppendLine($"{indent}/// {tableInfo.TypeName}のマッパー拡張メソッド");
        sb.AppendLine($"{indent}/// </summary>");
        sb.AppendLine($"{indent}public static class {tableInfo.TypeName}Extensions");
        sb.AppendLine($"{indent}{{");
        // Map メソッド (IDataReaderから読み込み)
        sb.AppendLine($"{indent}    /// <summary>");
        sb.AppendLine($"{indent}    /// IDataReaderから{tableInfo.TypeName}を生成（リフレクション不使用）");
        sb.AppendLine($"{indent}    /// </summary>");
        sb.AppendLine($"{indent}    public static {tableInfo.TypeName} Map{tableInfo.TypeName}(this IDataReader reader)");
        sb.AppendLine($"{indent}    {{");

        if (tableInfo.IsRecord)
        {
            // recordの場合はコンストラクタを使用
            var args = string.Join(",\n" + indent + "            ", tableInfo.Columns.Select(c => GenerateReaderCall(c)));
            sb.AppendLine($"{indent}        return new {tableInfo.TypeName}(");
            sb.AppendLine($"{indent}            {args});");
        }
        else
        {
            // クラスの場合はオブジェクト初期化子を使用
            sb.AppendLine($"{indent}        return new {tableInfo.TypeName}");
            sb.AppendLine($"{indent}        {{");
            for (int i = 0; i < tableInfo.Columns.Count; i++)
            {
                var column = tableInfo.Columns[i];
                var comma = i < tableInfo.Columns.Count - 1 ? "," : "";
                sb.AppendLine($"{indent}            {column.PropertyName} = {GenerateReaderCall(column)}{comma}");
            }
            sb.AppendLine($"{indent}        }};");
        }

        sb.AppendLine($"{indent}    }}");
        sb.AppendLine($"{indent}}}");
    }

    // TODO ヘルパークラスにまとめるべきか検討
    private static string GenerateReaderCall(ColumnInfo column)
    {
        var baseType = column.TypeName.TrimEnd('?');
        var isNullable = column.TypeName.EndsWith("?") || column.IsNullable;

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

            if (isNullable)
            {
                // Nullable<Enum>の場合、明示的なキャストが必要
                return $"reader.IsDBNull({column.Ordinal}) ? ({baseType}?)null : ({baseType})reader.{underlyingGetter}({column.Ordinal})";
            }
            return $"({baseType})reader.{underlyingGetter}({column.Ordinal})";
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
            return isNullable
                ? $"reader.IsDBNull({column.Ordinal}) ? default : ({column.TypeName})reader.GetValue({column.Ordinal})"
                : $"({baseType})reader.GetValue({column.Ordinal})";
        }

        return isNullable
            ? $"reader.IsDBNull({column.Ordinal}) ? null : reader.{getterMethod}({column.Ordinal})"
            : $"reader.{getterMethod}({column.Ordinal})";
    }
}
