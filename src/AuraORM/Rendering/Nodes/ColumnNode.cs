namespace AuraORM;

/// <summary>
/// A column of a table in the query: <c>"table"."column"</c>, or <c>"column"</c> where columns are written unqualified.
/// </summary>
internal sealed class ColumnNode(TableAlias table, string name) : SqlNode
{
    public TableAlias Table { get; } = table;

    public string Name { get; } = name;

    public override void Render(SqlRenderContext context)
    {
        context.CheckInScope(Table, Name);
        if (Table.IsExcluded)
        {
            context.Dialect.RenderExcludedColumn(context, Name);
            return;
        }

        if (!context.UnqualifiedColumns && !context.WriteReturningQualifier(Table))
        {
            context.WriteIdentifier(Table.Reference);
            context.Write('.');
        }

        context.WriteIdentifier(Name);
    }
}
