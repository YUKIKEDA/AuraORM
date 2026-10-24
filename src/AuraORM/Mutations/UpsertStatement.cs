using System.Collections.Immutable;

namespace AuraORM;

/// <summary>
/// An <c>INSERT</c> with the clause that handles a conflict: <c>ON CONFLICT …</c> or <c>ON DUPLICATE KEY UPDATE …</c>
/// (<c>docs/design.ja.md</c> §10). Run it with <see cref="MutationExecution.ExecuteAsync"/>, or add <c>Returning</c>.
/// </summary>
/// <typeparam name="TDialect">The SQL dialect.</typeparam>
/// <typeparam name="TMode">Whether the statement can be run: <see cref="Executable"/> or <see cref="SqlOnly"/>.</typeparam>
/// <typeparam name="TColumns">The column set of the table.</typeparam>
/// <remarks>
/// <para>
/// The row count differs by dialect (§10.3): SQLite and PostgreSQL count each inserted and each updated row once and skip
/// the rows that <c>DO NOTHING</c> skipped; MySQL and MariaDB count an inserted row as 1 and an updated row as 2, and, with
/// MySqlConnector's default (<c>UseAffectedRows=false</c>), a row whose values did not change as 1.
/// </para>
/// <para>
/// When several rows of one statement conflict on the same unique value, PostgreSQL fails, and SQLite, MySQL, and MariaDB
/// apply them one after another, so a later row updates the row an earlier one inserted (§10.3, §14.2).
/// </para>
/// </remarks>
public class UpsertStatement<TDialect, TMode, TColumns> : MutationStatement<TDialect, TMode>
    where TDialect : IDialect
    where TMode : ExecutionMode
    where TColumns : ITableColumns<TColumns>
{
    internal UpsertStatement(InsertStatement<TDialect, TMode, TColumns> insert, UpsertClause clause)
        : base(insert.Session)
    {
        Insert = insert;
        Clause = clause;
    }

    /// <summary>
    /// Gets the columns of the table, which the lambda of <c>Returning</c> gets.
    /// </summary>
    internal TColumns Table => Insert.Table;

    private protected InsertStatement<TDialect, TMode, TColumns> Insert { get; }

    private protected UpsertClause Clause { get; }

    internal override SqlNode CreateNode(ImmutableArray<SqlNode> returning) => Insert.CreateNode(returning, Clause);
}
