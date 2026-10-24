using System.ComponentModel;

namespace AuraORM;

/// <summary>
/// One occurrence of a table in a statement. Columns are created from it, so that a column is always bound to a table that
/// the statement contains (<c>docs/design.ja.md</c> §3.4).
/// </summary>
/// <remarks>
/// Only AuraORM creates instances. The members are used by the code that the source generator emits.
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class TableAlias
{
    internal TableAlias(string? schema, string table, string? alias)
    {
        Schema = schema;
        Table = table;
        Alias = alias;
    }

    internal TableAlias(NamedQueryDefinition query, string? alias)
    {
        Table = query.Name;
        Alias = alias;
        Query = query;
    }

    /// <summary>
    /// Gets the named query that this occurrence names, or <see langword="null"/> for a table (§5.11).
    /// </summary>
    internal NamedQueryDefinition? Query { get; }

    internal string? Schema { get; }

    internal string Table { get; }

    internal string? Alias { get; }

    /// <summary>
    /// Gets the name that qualifies the columns: the alias if there is one, otherwise the table name.
    /// </summary>
    internal string Reference => Alias ?? Table;

    /// <summary>
    /// Writes the table as a source of <c>FROM</c> or <c>JOIN</c>: <c>"schema"."table"</c>, then <c>AS "alias"</c> when it has one.
    /// </summary>
    /// <param name="context">The rendering context.</param>
    internal void RenderSource(SqlRenderContext context)
    {
        if (Query is not null)
        {
            context.WriteNamedQuerySource(this, Query);
            return;
        }

        context.CheckCteShadow(this);
        context.CheckTargetRead(this);
        RenderName(context);
        if (Alias is not null)
        {
            context.Write(" AS ");
            context.WriteIdentifier(Alias);
        }
    }

    /// <summary>
    /// Writes the table that an <c>INSERT</c>, <c>UPDATE</c>, or <c>DELETE</c> changes: <c>"schema"."table"</c>. The target has
    /// no alias (§6.2).
    /// </summary>
    /// <param name="context">The rendering context.</param>
    internal void RenderTarget(SqlRenderContext context) => RenderName(context);

    private void RenderName(SqlRenderContext context)
    {
        if (Schema is not null)
        {
            context.WriteIdentifier(Schema);
            context.Write('.');
        }

        context.WriteIdentifier(Table);
    }

    /// <summary>
    /// Creates a column that can be assigned in <c>UPDATE</c>.
    /// </summary>
    /// <typeparam name="T">The CLR type of the column, including whether it can be null.</typeparam>
    /// <param name="name">The column name.</param>
    /// <returns>The column.</returns>
    public Column<T> Column<T>(string name) => new(this, name);

    /// <summary>
    /// Creates a column that cannot be assigned (<c>[AutoIncrement]</c>).
    /// </summary>
    /// <typeparam name="T">The CLR type of the column.</typeparam>
    /// <param name="name">The column name.</param>
    /// <returns>The column.</returns>
    public ReadOnlyColumn<T> ReadOnlyColumn<T>(string name) => new(this, name);

    /// <summary>
    /// Creates a column of the nullable side of an outer join. It is an expression, not a column.
    /// </summary>
    /// <typeparam name="T">The nullable CLR type of the column.</typeparam>
    /// <param name="name">The column name.</param>
    /// <returns>The expression.</returns>
    public NullableColumn<T> NullableColumn<T>(string name) => new(this, name);
}
