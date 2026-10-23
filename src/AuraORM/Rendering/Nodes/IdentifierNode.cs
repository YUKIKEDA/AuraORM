namespace AuraORM;

/// <summary>
/// An identifier chosen at run time, quoted with the dialect's quote (<see cref="Sql.Identifier(string)"/>), or a table
/// written in a raw fragment (<c>"schema"."table"</c>).
/// </summary>
internal sealed class IdentifierNode(string? schema, string name) : SqlNode
{
    public override void Render(SqlRenderContext context)
    {
        if (schema is not null)
        {
            context.WriteIdentifier(schema);
            context.Write('.');
        }

        context.WriteIdentifier(name);
    }
}
