namespace AuraORM;

/// <summary>
/// A type whose rows a raw query can read by column name: every <c>[Table]</c> and <c>[QueryResult]</c> type. The source
/// generator implements it (<c>docs/design.ja.md</c> §11.3).
/// </summary>
/// <typeparam name="TSelf">The type.</typeparam>
public interface IQueryResult<TSelf>
    where TSelf : IQueryResult<TSelf>
{
    /// <summary>
    /// Gets the column name of each member, in the order that <see cref="ReadNamed"/> reads them.
    /// </summary>
    static abstract IReadOnlyList<string> ColumnNames { get; }

    /// <summary>
    /// Reads one row, with the columns found by <see cref="ColumnNames"/>.
    /// </summary>
    /// <param name="reader">The reader of the current row.</param>
    /// <returns>The row.</returns>
    static abstract TSelf ReadNamed(ref RowReader reader);
}
