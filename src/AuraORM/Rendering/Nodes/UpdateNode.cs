using System.Collections.Immutable;

namespace AuraORM;

/// <summary>
/// An <c>UPDATE</c> statement: <c>UPDATE "t" SET "a" = …, "b" = … WHERE …</c> (<c>docs/design.ja.md</c> §7). The
/// assignment targets are not qualified, because PostgreSQL rejects a qualified one; every other column is.
/// </summary>
/// <param name="target">The table.</param>
/// <param name="assignments">The assignments: at least one, each to a different column.</param>
/// <param name="where">The condition, or <see langword="null"/> for <c>AllRows()</c>.</param>
/// <param name="returning">The items of <c>Returning</c>, or an empty array (§9).</param>
internal sealed class UpdateNode(TableAlias target, ImmutableArray<Assignment> assignments, SqlNode? where, ImmutableArray<SqlNode> returning)
    : SqlNode
{
    public override void Render(SqlRenderContext context)
    {
        context.EnterMutation(target);
        context.Write("UPDATE ");
        target.RenderTarget(context);
        context.Write(" SET ");
        bool leftToRight = context.Dialect.AssignsLeftToRight;
        var assigned = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < assignments.Length; i++)
        {
            if (i > 0)
            {
                context.Write(", ");
            }

            ColumnNode column = assignments[i].Column;
            context.CheckInScope(column.Table, column.Name);
            context.WriteIdentifier(column.Name);
            context.Write(" = ");

            // §7: MySQL and MariaDB evaluate the assignments from left to right, so a value that reads a column assigned
            // before it would read the new value.
            context.WriteAssignedValue(assignments[i].Value, leftToRight ? assigned : null);
            assigned.Add(column.Name);
        }

        // §9: SQL Server's OUTPUT goes after SET and before WHERE; RETURNING goes at the end.
        bool output = context.Dialect.ReturnsWithOutput;
        if (output)
        {
            context.WriteReturning(target, returning, "INSERTED");
        }

        if (where is not null)
        {
            context.Write(" WHERE ");
            where.Render(context);
        }

        if (!output)
        {
            context.WriteReturning(target, returning, null);
        }

        context.ExitMutation();
    }
}
