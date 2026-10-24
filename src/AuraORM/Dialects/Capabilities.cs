namespace AuraORM;

/// <summary>
/// A dialect with <c>FULL OUTER JOIN</c>.
/// </summary>
public interface IFullJoinDialect : IDialect;

/// <summary>
/// A dialect that can return the rows an <c>INSERT</c> affected (<c>RETURNING</c> or <c>OUTPUT</c>).
/// </summary>
public interface IInsertReturningDialect : IDialect;

/// <summary>
/// A dialect that can return the rows an <c>UPDATE</c> affected (<c>RETURNING</c> or <c>OUTPUT</c>).
/// </summary>
public interface IUpdateReturningDialect : IDialect;

/// <summary>
/// A dialect that can return the rows a <c>DELETE</c> affected (<c>RETURNING</c> or <c>OUTPUT</c>).
/// </summary>
public interface IDeleteReturningDialect : IDialect;

/// <summary>
/// A dialect that accepts a row limit without <c>ORDER BY</c>.
/// </summary>
public interface IUnorderedLimitDialect : IDialect;

/// <summary>
/// A dialect that accepts <c>OFFSET</c> after <c>ORDER BY</c> without a row limit.
/// </summary>
public interface IOffsetWithoutLimitDialect : IDialect;

/// <summary>
/// A dialect that accepts <c>OFFSET</c> with neither <c>ORDER BY</c> nor a row limit.
/// </summary>
public interface IUnorderedOffsetDialect : IDialect;

/// <summary>
/// A dialect that accepts a row limit in a subquery of <c>IN</c> or <c>NOT IN</c>. MySQL and MariaDB reject it with error
/// 1235 (<c>docs/design.ja.md</c> §5.11).
/// </summary>
public interface ILimitInSubqueryDialect : IDialect;

/// <summary>
/// A dialect with <c>INSERT … ON CONFLICT</c>.
/// </summary>
public interface IOnConflictDialect : IDialect;

/// <summary>
/// A dialect with <c>INSERT … ON DUPLICATE KEY UPDATE</c>.
/// </summary>
public interface IOnDuplicateKeyDialect : IDialect;

/// <summary>
/// A dialect with <c>CREATE TABLE IF NOT EXISTS</c>.
/// </summary>
public interface ICreateTableIfNotExistsDialect : IDialect;

/// <summary>
/// A dialect with <c>CREATE INDEX IF NOT EXISTS</c>.
/// </summary>
public interface ICreateIndexIfNotExistsDialect : IDialect;
