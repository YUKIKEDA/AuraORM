namespace AuraORM.Tests;

/// <summary>
/// A statement that a test builds the same way on every dialect. A lambda cannot be generic, so each case is a small class.
/// </summary>
internal interface IQueryCase
{
    /// <summary>
    /// Builds the statement and renders it.
    /// </summary>
    /// <typeparam name="TDialect">The dialect.</typeparam>
    /// <param name="db">A database on a connection that runs nothing.</param>
    /// <returns>The statement.</returns>
    SqlStatement Build<TDialect>(Database<TDialect> db)
        where TDialect : IDialect;
}
