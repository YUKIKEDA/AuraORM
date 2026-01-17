namespace AuraORM.Builder.Table;

public interface ITable
{
    /// <summary>
    /// テーブル名
    /// </summary>
    string TableName { get; }

    /// <summary>
    /// カラム名
    /// </summary>
    string[] ColumnNames { get; }

    /// <summary>
    /// 主キーのカラム名
    /// </summary>
    string? PrimaryKeyColumnName { get; }

    /// <summary>
    /// 自動インクリメントかどうか
    /// </summary>
    bool IsAutoIncrement { get; }

    /// <summary>
    /// INSERT時に使用するカラム名
    /// </summary>
    string[] InsertColumnNames { get; }
}
