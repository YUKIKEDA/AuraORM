namespace AuraORM;

/// <summary>
/// The MySQL dialect (MySQL 8.4 or later). For MariaDB, use <see cref="MariaDbDialect"/>.
/// </summary>
public sealed class MySqlDialect : IUnorderedLimitDialect, IOnDuplicateKeyDialect, ICreateTableIfNotExistsDialect
{
    private MySqlDialect() { }
}
