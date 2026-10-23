namespace AuraORM;

/// <summary>
/// The SQLite dialect (SQLite 3.39 or later).
/// </summary>
public sealed class SqliteDialect
    : IFullJoinDialect,
        IInsertReturningDialect,
        IUpdateReturningDialect,
        IDeleteReturningDialect,
        IUnorderedLimitDialect,
        IOnConflictDialect,
        ICreateTableIfNotExistsDialect,
        ICreateIndexIfNotExistsDialect
{
    private SqliteDialect() { }
}
