namespace AuraORM;

/// <summary>
/// A column set that AuraORM creates for each occurrence of a table in a statement. The source generator implements it.
/// </summary>
/// <typeparam name="TSelf">The column set type.</typeparam>
public interface ITableColumns<TSelf>
    where TSelf : ITableColumns<TSelf>
{
    /// <summary>
    /// Creates the column set of one occurrence of the table.
    /// </summary>
    /// <param name="table">The occurrence. Only AuraORM can create it, so only AuraORM can create column sets.</param>
    /// <returns>The column set.</returns>
    static abstract TSelf Create(TableAlias table);
}
