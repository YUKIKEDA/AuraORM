using AuraORM.Generator.Tables;

namespace AuraORM.Generator.Query;

internal record QueryResult(
    string? Namespace,
    string TypeName,
    List<ColumnInfo> Columns,
    bool IsRecord);