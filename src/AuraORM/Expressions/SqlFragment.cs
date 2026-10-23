namespace AuraORM;

/// <summary>
/// A piece of SQL for a hole of a raw fragment: text that is not checked (<see cref="Sql.Unsafe(string)"/>), or a quoted
/// identifier (<see cref="Sql.Identifier(string)"/>).
/// </summary>
public sealed class SqlFragment
{
    internal SqlFragment(SqlNode node)
    {
        Node = node;
    }

    internal SqlNode Node { get; }
}
