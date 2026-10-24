namespace AuraORM;

/// <summary>
/// The <c>IF NOT EXISTS</c> and <c>IF EXISTS</c> of DDL, on the dialects that have them (design §2.4, §13). Each can be
/// written once: the result has no second one.
/// </summary>
public static class DdlClauses
{
    /// <summary>
    /// Writes <c>CREATE TABLE IF NOT EXISTS</c>, which does nothing when the table exists. SQL Server has no such syntax.
    /// </summary>
    /// <typeparam name="TDialect">A dialect with <see cref="ICreateTableIfNotExistsDialect"/>.</typeparam>
    /// <typeparam name="TMode">Whether the statement can run.</typeparam>
    /// <param name="statement">The <c>CREATE TABLE</c> statement.</param>
    /// <returns>The statement with <c>IF NOT EXISTS</c>.</returns>
    public static TableDdlStatement<TDialect, TMode> IfNotExists<TDialect, TMode>(this CreateTableStatement<TDialect, TMode> statement)
        where TDialect : ICreateTableIfNotExistsDialect
        where TMode : ExecutionMode
    {
        ArgumentNullException.ThrowIfNull(statement);
        return statement.WithIfNotExists();
    }

    /// <summary>
    /// Writes <c>CREATE INDEX IF NOT EXISTS</c>, which does nothing for an index that exists. SQLite, PostgreSQL, and
    /// MariaDB have it.
    /// </summary>
    /// <typeparam name="TDialect">A dialect with <see cref="ICreateIndexIfNotExistsDialect"/>.</typeparam>
    /// <typeparam name="TMode">Whether the statements can run.</typeparam>
    /// <param name="statement">The <c>CREATE INDEX</c> statements.</param>
    /// <returns>The statements with <c>IF NOT EXISTS</c>.</returns>
    public static IndexDdlStatement<TDialect, TMode> IfNotExists<TDialect, TMode>(this CreateIndexesStatement<TDialect, TMode> statement)
        where TDialect : ICreateIndexIfNotExistsDialect
        where TMode : ExecutionMode
    {
        ArgumentNullException.ThrowIfNull(statement);
        return statement.WithIfNotExists();
    }

    /// <summary>
    /// Writes <c>DROP INDEX IF EXISTS</c>, which does nothing for an index that does not exist. SQLite, PostgreSQL,
    /// MariaDB, and SQL Server have it; MySQL does not.
    /// </summary>
    /// <typeparam name="TDialect">A dialect with <see cref="IDropIndexIfExistsDialect"/>.</typeparam>
    /// <typeparam name="TMode">Whether the statements can run.</typeparam>
    /// <param name="statement">The <c>DROP INDEX</c> statements.</param>
    /// <returns>The statements with <c>IF EXISTS</c>.</returns>
    public static IndexDdlStatement<TDialect, TMode> IfExists<TDialect, TMode>(this DropIndexesStatement<TDialect, TMode> statement)
        where TDialect : IDropIndexIfExistsDialect
        where TMode : ExecutionMode
    {
        ArgumentNullException.ThrowIfNull(statement);
        return statement.WithIfExists();
    }

    /// <summary>
    /// Writes <c>DROP TABLE IF EXISTS</c>, which does nothing when the table does not exist. Every dialect has it.
    /// </summary>
    /// <typeparam name="TDialect">The dialect.</typeparam>
    /// <typeparam name="TMode">Whether the statement can run.</typeparam>
    /// <param name="statement">The <c>DROP TABLE</c> statement.</param>
    /// <returns>The statement with <c>IF EXISTS</c>.</returns>
    public static TableDdlStatement<TDialect, TMode> IfExists<TDialect, TMode>(this DropTableStatement<TDialect, TMode> statement)
        where TDialect : IDialect
        where TMode : ExecutionMode
    {
        ArgumentNullException.ThrowIfNull(statement);
        return statement.WithIfExists();
    }
}
