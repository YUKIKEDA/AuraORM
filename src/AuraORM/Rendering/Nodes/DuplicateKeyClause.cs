using System.Collections.Immutable;

namespace AuraORM;

/// <summary>
/// <c>ON DUPLICATE KEY UPDATE "a" = …</c>, after MySQL's row alias <c>AS `excluded`</c> (MySQL and MariaDB, §10.2).
/// </summary>
/// <param name="excluded">The occurrence of the row proposed for insertion.</param>
/// <param name="assignments">The assignments.</param>
internal sealed class DuplicateKeyClause(TableAlias excluded, ImmutableArray<Assignment> assignments) : UpsertClause(excluded)
{
    public override void Render(SqlRenderContext context, TableAlias target, ImmutableArray<string> columns)
    {
        CheckExcludedName(context, target);
        if (context.Dialect.NamesProposedRowExcluded)
        {
            context.Write(" AS ");
            context.WriteIdentifier(TableAlias.ExcludedName);
        }

        context.Write(" ON DUPLICATE KEY UPDATE ");
        context.EnterUpsert(target, Excluded, columns);
        WriteAssignments(context, assignments);
        context.ExitUpsert(target);
    }
}
