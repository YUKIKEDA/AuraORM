namespace AuraORM.Generator.Tables;
internal record TableInfo(
    string? Namespace,
    string TypeName,
    string TableName,
    List<ColumnInfo> Columns,
    bool IsRecord);
