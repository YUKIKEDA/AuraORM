namespace AuraORM;

/// <summary>
/// The PostgreSQL dialect (PostgreSQL 15 or later).
/// </summary>
public sealed class PostgreSqlDialect
    : IFullJoinDialect,
        IInsertReturningDialect,
        IUpdateReturningDialect,
        IDeleteReturningDialect,
        IUnorderedLimitDialect,
        IOffsetWithoutLimitDialect,
        IUnorderedOffsetDialect,
        IOnConflictDialect,
        ICreateTableIfNotExistsDialect,
        ICreateIndexIfNotExistsDialect
{
    private PostgreSqlDialect() { }
}
