namespace AuraORM;

/// <summary>
/// The SQL Server dialect (SQL Server 2019 or later).
/// </summary>
public sealed class SqlServerDialect
    : IFullJoinDialect,
        ILimitInSubqueryDialect,
        IInsertReturningDialect,
        IUpdateReturningDialect,
        IDeleteReturningDialect,
        IOffsetWithoutLimitDialect,
        IDropIndexIfExistsDialect,
        ITransactionalDdlDialect
{
    private SqlServerDialect() { }
}
