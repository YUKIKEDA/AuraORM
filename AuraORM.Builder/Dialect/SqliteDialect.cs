namespace AuraORM.Builder.Dialect;

/// <summary>
/// SQLite用のSQL方言実装
/// </summary>
public sealed class SqliteDialect : ISqlDialect
{
    /// <summary>
    /// シングルトンインスタンス
    /// </summary>
    public static readonly SqliteDialect Instance = new();

    private SqliteDialect() { }

    /// <inheritdoc />
    public string Name => "SQLite";

    /// <inheritdoc />
    public string GetParameterPlaceholder(int index) => $"@p{index}";

    /// <inheritdoc />
    public string QuoteIdentifier(string identifier) => $"\"{identifier}\"";

    /// <inheritdoc />
    public string BuildLimitOffset(int? limit, int? offset)
    {
        if (limit == null && offset == null)
            return string.Empty;

        var result = string.Empty;
        if (limit.HasValue)
            result = $" LIMIT {limit.Value}";
        if (offset.HasValue)
            result += $" OFFSET {offset.Value}";
        return result;
    }

    /// <inheritdoc />
    public string GetLastInsertIdClause(string tableName, string primaryKeyColumn)
        => "; SELECT last_insert_rowid();";

    /// <inheritdoc />
    public string GetBooleanLiteral(bool value) => value ? "1" : "0";

    /// <inheritdoc />
    public string GetConcatOperator() => "||";

    /// <inheritdoc />
    public string GetCurrentTimestampFunction() => "CURRENT_TIMESTAMP";
}
