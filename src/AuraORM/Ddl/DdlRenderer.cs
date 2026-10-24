using System.Collections.Immutable;
using System.Globalization;
using System.Text;

namespace AuraORM;

/// <summary>
/// Writes the DDL of design §13 for one dialect, and checks the limits of §13.7 before anything is sent.
/// </summary>
internal abstract class DdlRenderer
{
    private protected DdlRenderer(DialectRenderer sql)
    {
        Sql = sql;
    }

    protected DialectRenderer Sql { get; }

    protected string Dialect => Sql.Name;

    public SqlStatement CreateTable(TableInfo table, bool ifNotExists)
    {
        CheckTableNames(table);
        foreach (ColumnInfo column in table.Columns)
        {
            CheckColumnType(table, column);
            CheckUnique(table, column);
            CheckDefault(table, column);
        }

        ColumnInfo[] key = table.Columns.Where(c => c.IsPrimaryKey).ToArray();
        CheckKeySize(table, "the primary key", key, KeyLimit.PrimaryKey);
        foreach (ColumnInfo column in table.Columns.Where(c => c.IsUnique))
        {
            CheckKeySize(table, "the unique constraint on " + column.Name, [column], KeyLimit.Unique);
        }

        CheckForeignKeys(table);

        using var writer = new SqlWriter();
        writer.Append(ifNotExists ? "CREATE TABLE IF NOT EXISTS " : "CREATE TABLE ");
        WriteTableName(writer, table.Schema, table.Name);
        writer.Append(" (");
        bool first = true;
        foreach (ColumnInfo column in table.Columns)
        {
            Separate(writer, ref first);
            WriteColumn(writer, column);
        }

        if (key.Length > 0 && !WritesKeyOnColumn(key))
        {
            Separate(writer, ref first);
            writer.Append("PRIMARY KEY (");
            WriteColumnList(writer, key.Select(c => c.Name));
            writer.Append(')');
        }

        foreach (ColumnInfo column in table.Columns.Where(c => c.IsUnique))
        {
            Separate(writer, ref first);
            writer.Append("UNIQUE (");
            WriteIdentifier(writer, column.Name);
            writer.Append(')');
        }

        foreach (ColumnInfo column in table.Columns.Where(c => c.References is not null && NeedsForeignKeyIndex(key, c)))
        {
            Separate(writer, ref first);
            writer.Append("KEY (");
            WriteIdentifier(writer, column.Name);
            writer.Append(')');
        }

        foreach (ColumnInfo column in table.Columns.Where(c => c.References is not null))
        {
            ForeignKeyInfo reference = column.References!;
            Separate(writer, ref first);
            writer.Append("FOREIGN KEY (");
            WriteIdentifier(writer, column.Name);
            writer.Append(") REFERENCES ");
            WriteReferencedTable(writer, table, reference);
            writer.Append(" (");
            WriteIdentifier(writer, reference.Column);
            writer.Append(')');
        }

        writer.Append(')');
        return new SqlStatement(writer.ToString(), []);
    }

    public SqlStatement DropTable(TableInfo table, bool ifExists)
    {
        CheckName(table.Schema);
        CheckName(table.Name);
        using var writer = new SqlWriter();
        writer.Append(ifExists ? "DROP TABLE IF EXISTS " : "DROP TABLE ");
        WriteTableName(writer, table.Schema, table.Name);
        return new SqlStatement(writer.ToString(), []);
    }

    public ImmutableArray<SqlStatement> CreateIndexes(TableInfo table, bool ifNotExists)
    {
        ImmutableArray<SqlStatement>.Builder statements = ImmutableArray.CreateBuilder<SqlStatement>(table.Indexes.Count);
        foreach (IndexInfo index in table.Indexes)
        {
            CheckIndexNames(table, index);
            ColumnInfo[] columns = index.Columns.Select(name => Column(table, name)).ToArray();
            CheckKeySize(table, "the index " + index.Name, columns, index.IsUnique ? KeyLimit.Unique : KeyLimit.Index);
            using var writer = new SqlWriter();
            writer.Append(index.IsUnique ? "CREATE UNIQUE INDEX " : "CREATE INDEX ");
            if (ifNotExists)
            {
                writer.Append("IF NOT EXISTS ");
            }

            WriteIndexTarget(writer, table, index.Name);
            writer.Append(" (");
            WriteColumnList(writer, index.Columns);
            writer.Append(')');
            if (index.IsUnique)
            {
                WriteUniqueIndexFilter(writer, columns);
            }

            statements.Add(new SqlStatement(writer.ToString(), []));
        }

        return statements.MoveToImmutable();
    }

    public ImmutableArray<SqlStatement> DropIndexes(TableInfo table, bool ifExists)
    {
        ImmutableArray<SqlStatement>.Builder statements = ImmutableArray.CreateBuilder<SqlStatement>(table.Indexes.Count);
        foreach (IndexInfo index in table.Indexes)
        {
            CheckName(table.Schema);
            CheckName(table.Name);
            CheckName(index.Name);
            using var writer = new SqlWriter();
            writer.Append(ifExists ? "DROP INDEX IF EXISTS " : "DROP INDEX ");
            WriteDropIndexTarget(writer, table, index.Name);
            statements.Add(new SqlStatement(writer.ToString(), []));
        }

        return statements.MoveToImmutable();
    }

    // §3.3: the SQL type of a column, after its value converter (the generator writes the provider's column type).
    protected abstract string TypeName(ColumnInfo column);

    // §13.2: the clause after the type of an [AutoIncrement] column (SQLite writes the whole column itself).
    protected abstract string AutoIncrement { get; }

    // §13.2: SQLite puts the key of an [AutoIncrement] column on the column, not in a table constraint.
    protected virtual bool WritesKeyOnColumn(ColumnInfo[] key) => false;

    // §13.2: SQL Server writes NULL, because a column without it follows ANSI_NULL_DFLT_ON / ANSI_NULL_DFLT_OFF.
    protected virtual bool WritesNull => false;

    // §13.5: MySQL and MariaDB need an index of their own on a foreign key column, or DropIndexes fails with error 1553.
    protected virtual bool WritesForeignKeyIndex => false;

    protected abstract string CurrentTimestamp(ColumnType type);

    protected virtual string BooleanLiteral(bool value) => value ? "TRUE" : "FALSE";

    protected virtual string DecimalLiteral(decimal value) => value.ToString(CultureInfo.InvariantCulture);

    protected virtual void WriteStringLiteral(SqlWriter writer, string value)
    {
        writer.Append('\'');
        writer.Append(value.Replace("'", "''", StringComparison.Ordinal));
        writer.Append('\'');
    }

    // §13.3: MySQL and MariaDB take a default of a LONGTEXT or LONGBLOB column only as an expression in parentheses.
    protected virtual bool WrapsConstant(ColumnInfo column) => false;

    // §13.6: how the referenced table of a foreign key is written.
    protected virtual void WriteReferencedTable(SqlWriter writer, TableInfo table, ForeignKeyInfo reference) =>
        WriteTableName(writer, reference.Schema, reference.Table);

    protected virtual void CheckReferencedSchema(TableInfo table, ForeignKeyInfo reference) { }

    // §13.4: the index name and its table in CREATE INDEX.
    protected virtual void WriteIndexTarget(SqlWriter writer, TableInfo table, string index)
    {
        WriteIdentifier(writer, index);
        writer.Append(" ON ");
        WriteTableName(writer, table.Schema, table.Name);
    }

    // §13.4: the index in DROP INDEX: MySQL, MariaDB, and SQL Server name its table.
    protected virtual void WriteDropIndexTarget(SqlWriter writer, TableInfo table, string index)
    {
        WriteIdentifier(writer, index);
        writer.Append(" ON ");
        WriteTableName(writer, table.Schema, table.Name);
    }

    protected virtual void WriteUniqueIndexFilter(SqlWriter writer, ColumnInfo[] columns) { }

    // §13.4: SQL Server's unique constraint allows one NULL, and a constraint takes no filter.
    protected virtual void CheckUnique(TableInfo table, ColumnInfo column) { }

    protected virtual void CheckStringConstant(ColumnInfo column, string value) { }

    // §13.7: the limits of a type; null is no limit.
    protected virtual int? MaxStringLength => null;

    protected virtual int? MaxBinaryLength => null;

    protected virtual int? MaxPrecision => null;

    protected virtual int? MaxScale => null;

    // §13.7: the limits of a key in bytes, and the size of a column in it; null is no limit.
    protected virtual int? KeyBytes(KeyLimit limit) => null;

    protected virtual int ColumnBytes(ColumnInfo column) => 0;

    // §13.7: the length of a name, or null when it is within the dialect's limit.
    protected virtual string? CheckNameLength(string name) => null;

    protected virtual void CheckForeignKeyNames(TableInfo table, int foreignKeys) { }

    protected void WriteIdentifier(SqlWriter writer, string name) => Sql.WriteIdentifier(writer, name);

    protected void WriteTableName(SqlWriter writer, string? schema, string name)
    {
        if (schema is not null)
        {
            WriteIdentifier(writer, schema);
            writer.Append('.');
        }

        WriteIdentifier(writer, name);
    }

    protected static InvalidOperationException Error(string message) => new(message);

    protected static int CodePoints(string name)
    {
        int count = 0;
        foreach (Rune _ in name.EnumerateRunes())
        {
            count++;
        }

        return count;
    }

    private static ColumnInfo Column(TableInfo table, string name) =>
        table.Columns.FirstOrDefault(c => c.Name == name)
        ?? throw new InvalidOperationException("The index of " + table.Name + " names " + name + ", which is not a column of the table.");

    private static void Separate(SqlWriter writer, ref bool first)
    {
        if (!first)
        {
            writer.Append(", ");
        }

        first = false;
    }

    private bool NeedsForeignKeyIndex(ColumnInfo[] key, ColumnInfo column) =>
        WritesForeignKeyIndex && !column.IsUnique && (key.Length == 0 || key[0].Name != column.Name);

    private void WriteColumnList(SqlWriter writer, IEnumerable<string> names)
    {
        bool first = true;
        foreach (string name in names)
        {
            Separate(writer, ref first);
            WriteIdentifier(writer, name);
        }
    }

    private void WriteColumn(SqlWriter writer, ColumnInfo column)
    {
        WriteIdentifier(writer, column.Name);
        if (column.IsAutoIncrement && WritesKeyOnColumn([column]))
        {
            writer.Append(' ');
            writer.Append(AutoIncrement);
            return;
        }

        writer.Append(' ');
        writer.Append(TypeName(column));
        if (column.IsAutoIncrement)
        {
            writer.Append(' ');
            writer.Append(AutoIncrement);
        }

        if (!column.IsNullable)
        {
            writer.Append(" NOT NULL");
        }
        else if (WritesNull)
        {
            writer.Append(" NULL");
        }

        if (column.Default is { } value)
        {
            writer.Append(" DEFAULT ");
            WriteDefault(writer, column, value);
        }
    }

    private void WriteDefault(SqlWriter writer, ColumnInfo column, ColumnDefault value)
    {
        if (value.Sql is { } sql)
        {
            writer.Append(sql);
            return;
        }

        if (value.Named is SqlDefault.CurrentTimestamp)
        {
            writer.Append(CurrentTimestamp(column.Type));
            return;
        }

        bool wrap = WrapsConstant(column);
        if (wrap)
        {
            writer.Append('(');
        }

        switch (value.Constant)
        {
            case null:
                writer.Append("NULL");
                break;
            case bool b:
                writer.Append(BooleanLiteral(b));
                break;
            case string s:
                WriteStringLiteral(writer, s);
                break;
            case decimal m:
                writer.Append(DecimalLiteral(m));
                break;
            case double d:
                writer.Append(d.ToString("R", CultureInfo.InvariantCulture));
                break;
            case float f:
                writer.Append(f.ToString("R", CultureInfo.InvariantCulture));
                break;
            case Enum e:
                writer.Append(Convert.ToInt64(e, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture));
                break;
            case IFormattable number:
                writer.Append(number.ToString(null, CultureInfo.InvariantCulture));
                break;
            default:
                throw Error("The default of " + column.Name + " is a constant of a type that DDL cannot write; use [DefaultSql] for it.");
        }

        if (wrap)
        {
            writer.Append(')');
        }
    }

    private void CheckDefault(TableInfo table, ColumnInfo column)
    {
        if (column.Default?.Constant is string s)
        {
            CheckStringConstant(column, s);
        }

        if (column.Default?.Named is SqlDefault.CurrentTimestamp && column.Type is not (ColumnType.DateTime or ColumnType.DateTimeOffset))
        {
            throw Error(
                "The column "
                    + table.Name
                    + "."
                    + column.Name
                    + " has the default SqlDefault.CurrentTimestamp, which only a DateTime or DateTimeOffset column can have; use [DefaultSql] for it."
            );
        }
    }

    private void CheckColumnType(TableInfo table, ColumnInfo column)
    {
        int? length = column.MaxLength;
        int? max = column.Type switch
        {
            ColumnType.String => MaxStringLength,
            ColumnType.Binary => MaxBinaryLength,
            _ => null,
        };
        if (length is { } n && max is { } limit && n > limit)
        {
            throw Error(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "The column {0}.{1} has [MaxLength({2})], which is more than the {3} that {4} accepts for its type.",
                    table.Name,
                    column.Name,
                    n,
                    limit,
                    Dialect
                )
            );
        }

        if (column.Precision is { } precision && MaxPrecision is { } maxPrecision && precision > maxPrecision)
        {
            throw Error(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "The column {0}.{1} has the precision {2}, which is more than the {3} that {4} accepts.",
                    table.Name,
                    column.Name,
                    precision,
                    maxPrecision,
                    Dialect
                )
            );
        }

        if (column.Scale is { } scale && MaxScale is { } maxScale && scale > maxScale)
        {
            throw Error(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "The column {0}.{1} has the scale {2}, which is more than the {3} that {4} accepts.",
                    table.Name,
                    column.Name,
                    scale,
                    maxScale,
                    Dialect
                )
            );
        }
    }

    private void CheckKeySize(TableInfo table, string key, ColumnInfo[] columns, KeyLimit kind)
    {
        if (columns.Length == 0 || KeyBytes(kind) is not { } limit)
        {
            return;
        }

        int bytes = columns.Sum(ColumnBytes);
        if (bytes > limit)
        {
            throw Error(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "The columns of {0} of {1} take up to {2} bytes, which is more than the {3} bytes that {4} accepts in a key; shorten their [MaxLength].",
                    key,
                    table.Name,
                    bytes,
                    limit,
                    Dialect
                )
            );
        }
    }

    private void CheckForeignKeys(TableInfo table)
    {
        int count = 0;
        foreach (ColumnInfo column in table.Columns)
        {
            if (column.References is not { } reference)
            {
                continue;
            }

            count++;
            CheckName(reference.Schema);
            CheckName(reference.Table);
            CheckName(reference.Column);
            CheckReferencedSchema(table, reference);
        }

        if (count > 0)
        {
            CheckForeignKeyNames(table, count);
        }
    }

    private void CheckTableNames(TableInfo table)
    {
        CheckName(table.Schema);
        CheckName(table.Name);
        foreach (ColumnInfo column in table.Columns)
        {
            CheckName(column.Name);
        }
    }

    private void CheckIndexNames(TableInfo table, IndexInfo index)
    {
        CheckName(table.Schema);
        CheckName(table.Name);
        CheckName(index.Name);
        foreach (string column in index.Columns)
        {
            CheckName(column);
        }
    }

    private void CheckName(string? name)
    {
        if (name is not null && CheckNameLength(name) is { } problem)
        {
            throw Error("The name " + name + " " + problem);
        }
    }

    internal enum KeyLimit
    {
        PrimaryKey,
        Unique,
        Index,
    }
}
