namespace AuraORM;

/// <summary>
/// The MariaDB dialect (MariaDB 10.11 or later).
/// </summary>
public sealed class MariaDbDialect
    : IInsertReturningDialect,
        IDeleteReturningDialect,
        IUnorderedLimitDialect,
        IOnDuplicateKeyDialect,
        ICreateTableIfNotExistsDialect,
        ICreateIndexIfNotExistsDialect,
        IDropIndexIfExistsDialect
{
    private MariaDbDialect() { }
}
