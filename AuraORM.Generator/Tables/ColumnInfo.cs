namespace AuraORM.Generator.Tables;

internal record ColumnInfo(
    string PropertyName,
    string ColumnName,
    string TypeName,
    bool IsNullable,
    bool IsPrimaryKey,
    bool IsAutoIncrement,
    int Ordinal,
    bool IsEnum = false,
    string? UnderlyingEnumType = null);