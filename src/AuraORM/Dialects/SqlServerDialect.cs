namespace AuraORM;

/// <summary>
/// The SQL Server dialect (SQL Server 2019 or later).
/// </summary>
public sealed class SqlServerDialect
    : IFullJoinDialect,
        IInsertReturningDialect,
        IUpdateReturningDialect,
        IDeleteReturningDialect,
        IOffsetWithoutLimitDialect
{
    private SqlServerDialect() { }
}
