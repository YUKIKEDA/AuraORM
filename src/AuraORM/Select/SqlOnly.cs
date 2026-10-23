namespace AuraORM;

/// <summary>
/// The mode of a statement that has no connection: it comes from a <see cref="SqlBuilder{TDialect}"/> and gives only its SQL
/// (<c>ToSql()</c>). It has no run methods, so running it does not compile (CS1929).
/// </summary>
public sealed class SqlOnly : ExecutionMode
{
    private SqlOnly() { }
}
