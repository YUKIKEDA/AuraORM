namespace AuraORM;

/// <summary>
/// A <c>GROUP BY</c> key as the later clauses write it (§5.5). It writes the key's own node, so the key keeps its parameters
/// wherever it is written (§5.12). Inside <c>HAVING</c>, on a dialect that does not match an expression there with the
/// <c>GROUP BY</c> key (MySQL and MariaDB), a key that is not a column is written as <c>MIN(key)</c>, which has the same
/// value because the key has one value in each group.
/// </summary>
internal sealed class GroupKeyNode(SqlNode key) : SqlNode
{
    public override SqlNode Unwrapped => key.Unwrapped;

    /// <summary>
    /// Gets the key's own node.
    /// </summary>
    public SqlNode Key => key;

    public override void Render(SqlRenderContext context)
    {
        if (context.InHaving && context.Dialect.AggregatesGroupKeysInHaving && key is not ColumnNode)
        {
            context.Write("MIN(");
            context.WriteGroupKey(key);
            context.Write(')');
            return;
        }

        context.WriteGroupKey(key);
    }
}
