using System.Collections.Immutable;

namespace AuraORM;

/// <summary>
/// <c>ON CONFLICT ("a") DO NOTHING</c>, <c>ON CONFLICT DO NOTHING</c>, or <c>ON CONFLICT ("a") DO UPDATE SET … WHERE …</c>
/// (SQLite and PostgreSQL, §10.1).
/// </summary>
/// <param name="excluded">The occurrence of the row proposed for insertion.</param>
/// <param name="conflictTarget">The names of the columns of the conflict target, or an empty array for none.</param>
/// <param name="assignments">The assignments of <c>DO UPDATE</c>, or an empty array for <c>DO NOTHING</c>.</param>
/// <param name="where">The condition of <c>DO UPDATE</c>, or <see langword="null"/>.</param>
internal sealed class ConflictClause(
    TableAlias excluded,
    ImmutableArray<string> conflictTarget,
    ImmutableArray<Assignment> assignments,
    SqlNode? where
) : UpsertClause(excluded)
{
    public override void Render(SqlRenderContext context, TableAlias target, ImmutableArray<string> columns)
    {
        CheckExcludedName(context, target);
        context.Write(" ON CONFLICT");
        if (!conflictTarget.IsEmpty)
        {
            context.Write(" (");
            for (int i = 0; i < conflictTarget.Length; i++)
            {
                if (i > 0)
                {
                    context.Write(", ");
                }

                context.WriteIdentifier(conflictTarget[i]);
            }

            context.Write(')');
        }

        if (assignments.IsEmpty)
        {
            context.Write(" DO NOTHING");
            return;
        }

        context.Write(" DO UPDATE SET ");
        context.EnterUpsert(target, Excluded, columns);
        WriteAssignments(context, assignments);
        if (where is not null)
        {
            context.Write(" WHERE ");
            where.Render(context);
        }

        context.ExitUpsert(target);
    }
}
