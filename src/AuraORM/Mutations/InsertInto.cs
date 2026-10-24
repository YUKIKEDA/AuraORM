using System.Collections.Immutable;
using System.Globalization;

namespace AuraORM;

/// <summary>
/// The start of an <c>INSERT</c>: <c>db.Insert(User.Table)</c>. Give it the rows with <c>Values</c>
/// (<c>docs/design.ja.md</c> §6).
/// </summary>
/// <typeparam name="TDialect">The SQL dialect.</typeparam>
/// <typeparam name="TMode">Whether the statement can be run: <see cref="Executable"/> or <see cref="SqlOnly"/>.</typeparam>
/// <typeparam name="TColumns">The column set of the table.</typeparam>
/// <typeparam name="TInsert">The <c>INSERT</c> model of the table.</typeparam>
public sealed class InsertInto<TDialect, TMode, TColumns, TInsert>
    where TDialect : IDialect
    where TMode : ExecutionMode
    where TColumns : ITableColumns<TColumns>
    where TInsert : InsertModel
{
    private readonly IStatementRunner? _session;
    private readonly TableAlias _target;
    private readonly TableInfo _table;

    internal InsertInto(IStatementRunner? session, TableAlias target, TableInfo table)
    {
        _session = session;
        _target = target;
        _table = table;
    }

    /// <summary>
    /// Inserts one row: <c>INSERT INTO "t" ("a", "b") VALUES (@p0, @p1)</c>. The columns are the ones the row sets, in
    /// declaration order; a row that sets none writes <c>DEFAULT VALUES</c> (MySQL and MariaDB: <c>() VALUES ()</c>).
    /// </summary>
    /// <param name="row">The row.</param>
    /// <returns>The statement.</returns>
    public InsertStatement<TDialect, TMode, TColumns> Values(TInsert row)
    {
        ArgumentNullException.ThrowIfNull(row);
        return Values([row]);
    }

    /// <summary>
    /// Inserts several rows: <c>INSERT INTO "t" ("a", "b") VALUES (@p0, @p1), (@p2, @p3)</c>. The statement is not split.
    /// </summary>
    /// <param name="rows">The rows. Every row sets the same columns.</param>
    /// <returns>The statement.</returns>
    /// <exception cref="ArgumentException">The list is empty; two rows set different columns; several rows set no column on
    /// SQLite, PostgreSQL, or SQL Server, where <c>DEFAULT VALUES</c> is one row; or there are more rows than the dialect
    /// accepts in one <c>INSERT … VALUES</c> (SQL Server: 1000) (<c>docs/design.ja.md</c> §6.2, §14.3).</exception>
    public InsertStatement<TDialect, TMode, TColumns> Values(IReadOnlyList<TInsert> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);
        if (rows.Count == 0)
        {
            throw new ArgumentException("INSERT needs at least one row.", nameof(rows));
        }

        DialectRenderer dialect = DialectRenderer.For<TDialect>();
        if (rows.Count > dialect.MaxInsertRows)
        {
            throw new ArgumentException(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "The INSERT has {0} rows, which is more than the {1} that {2} accepts in one INSERT … VALUES; split it into several statements.",
                    rows.Count,
                    dialect.MaxInsertRows,
                    dialect.Name
                ),
                nameof(rows)
            );
        }

        var values = new List<(int Column, ParameterNode Value)>();
        int[]? columns = null;
        var builder = ImmutableArray.CreateBuilder<ImmutableArray<ParameterNode>>(rows.Count);
        for (int r = 0; r < rows.Count; r++)
        {
            TInsert row = rows[r];
            ArgumentNullException.ThrowIfNull(row, nameof(rows));
            values.Clear();
            var receiver = new InsertValues(values);
            row.Write(ref receiver);
            if (columns is null)
            {
                columns = new int[values.Count];
                for (int i = 0; i < values.Count; i++)
                {
                    columns[i] = values[i].Column;
                }
            }
            else if (!SameColumns(columns, values))
            {
                throw new ArgumentException(
                    "Row "
                        + r.ToString(CultureInfo.InvariantCulture)
                        + " sets the columns "
                        + Names(values.ConvertAll(v => v.Column))
                        + ", but the first row sets "
                        + Names(columns)
                        + ". Every row of one INSERT sets the same columns.",
                    nameof(rows)
                );
            }

            var rowValues = ImmutableArray.CreateBuilder<ParameterNode>(values.Count);
            foreach ((_, ParameterNode value) in values)
            {
                rowValues.Add(value);
            }

            builder.Add(rowValues.MoveToImmutable());
        }

        if (columns!.Length == 0 && rows.Count > 1 && !dialect.WritesEmptyValueRows)
        {
            throw new ArgumentException(
                "Several rows set no column. "
                    + dialect.Name
                    + " writes such a row as DEFAULT VALUES, which inserts one row; insert the rows one at a time.",
                nameof(rows)
            );
        }

        var names = ImmutableArray.CreateBuilder<string>(columns.Length);
        foreach (int column in columns)
        {
            names.Add(_table.Columns[column].Name);
        }

        return new InsertStatement<TDialect, TMode, TColumns>(_session, _target, names.MoveToImmutable(), builder.MoveToImmutable());
    }

    private static bool SameColumns(int[] columns, List<(int Column, ParameterNode Value)> values)
    {
        if (columns.Length != values.Count)
        {
            return false;
        }

        for (int i = 0; i < columns.Length; i++)
        {
            if (columns[i] != values[i].Column)
            {
                return false;
            }
        }

        return true;
    }

    private string Names(IEnumerable<int> columns)
    {
        string names = string.Join(", ", columns.Select(c => _table.Columns[c].Name));
        return names.Length == 0 ? "(none)" : names;
    }
}
