using System.Globalization;

namespace AuraORM;

/// <summary>
/// The DDL of MySQL and MariaDB (design §13).
/// </summary>
internal sealed class MySqlDdlRenderer(DialectRenderer sql, bool isMariaDb) : DdlRenderer(sql)
{
    // Bytes of the leftover 0 to 8 decimal digits of a DECIMAL ("Data Type Storage Requirements"): nine digits take four.
    private static readonly int[] _leftoverDigitBytes = [0, 1, 1, 2, 2, 3, 3, 4, 4];

    protected override string AutoIncrement => "AUTO_INCREMENT";

    protected override bool WritesForeignKeyIndex => true;

    protected override string TypeName(ColumnInfo column) =>
        column.Type switch
        {
            ColumnType.Boolean => "BOOLEAN",
            ColumnType.Byte => "TINYINT UNSIGNED",
            ColumnType.Int16 => "SMALLINT",
            ColumnType.Int32 => "INT",
            ColumnType.Int64 => "BIGINT",
            ColumnType.Single => "FLOAT",
            ColumnType.Double => "DOUBLE",
            ColumnType.Decimal => string.Format(CultureInfo.InvariantCulture, "DECIMAL({0},{1})", column.Precision, column.Scale),
            ColumnType.String => column.MaxLength is { } n ? "VARCHAR(" + n.ToString(CultureInfo.InvariantCulture) + ")" : "LONGTEXT",
            ColumnType.Binary => column.MaxLength is { } m ? "VARBINARY(" + m.ToString(CultureInfo.InvariantCulture) + ")" : "LONGBLOB",
            ColumnType.Guid => "CHAR(36)",
            ColumnType.DateTime or ColumnType.DateTimeOffset => "DATETIME(6)",
            ColumnType.DateOnly => "DATE",
            _ => "TIME(6)",
        };

    // §13.3: DEFAULT CURRENT_TIMESTAMP on DATETIME(6) is error 1067, and it is the session's local time; an expression
    // default needs MySQL 8.0.13 or MariaDB 10.2.1.
    protected override string CurrentTimestamp(ColumnType type) => "(UTC_TIMESTAMP(6))";

    // §13.3: MySQL takes a default of a BLOB or TEXT column only as an expression (error 1101 otherwise).
    protected override bool WrapsConstant(ColumnInfo column) => column.Type is ColumnType.String or ColumnType.Binary && column.MaxLength is null;

    // §13.3: the meaning of a backslash in a string literal depends on NO_BACKSLASH_ESCAPES in sql_mode.
    protected override void CheckStringConstant(ColumnInfo column, string value)
    {
        if (value.Contains('\\', StringComparison.Ordinal))
        {
            throw Error(
                "The default of "
                    + column.Name
                    + " contains a backslash, which "
                    + Dialect
                    + " reads as an escape or as itself depending on NO_BACKSLASH_ESCAPES in sql_mode; use [DefaultSql] for it."
            );
        }
    }

    // §13.6: an unqualified referenced table is looked up in the database of the table being created.
    protected override void CheckReferencedSchema(TableInfo table, ForeignKeyInfo reference)
    {
        if (reference.Schema is null && table.Schema is not null)
        {
            throw Error(
                "The table "
                    + table.Schema
                    + "."
                    + table.Name
                    + " references "
                    + reference.Table
                    + ", which has no schema. "
                    + Dialect
                    + " looks up an unqualified referenced table in the schema of the table being created, not in the connection's database; give "
                    + reference.Table
                    + " a schema."
            );
        }
    }

    // §13.7: VARCHAR is 16383 characters of utf8mb4 (error 1074); VARBINARY is limited by the row size on MySQL, and to
    // 65532 bytes on MariaDB (error 1074).
    protected override int? MaxStringLength => 16383;

    protected override int? MaxBinaryLength => isMariaDb ? 65532 : null;

    protected override int? MaxPrecision => 65;

    protected override int? MaxScale => isMariaDb ? 38 : 30;

    // §13.7: InnoDB's 3072 bytes with the DYNAMIC row format, for every key.
    protected override int? KeyBytes(KeyLimit limit) => 3072;

    protected override int ColumnBytes(ColumnInfo column) =>
        column.Type switch
        {
            ColumnType.Boolean or ColumnType.Byte => 1,
            ColumnType.Int16 => 2,
            ColumnType.Int32 or ColumnType.Single => 4,
            ColumnType.Int64 or ColumnType.Double or ColumnType.DateTime or ColumnType.DateTimeOffset => 8,
            ColumnType.Decimal => DecimalBytes((column.Precision ?? 0) - (column.Scale ?? 0)) + DecimalBytes(column.Scale ?? 0),
            ColumnType.String => 4 * (column.MaxLength ?? 0),
            ColumnType.Binary => column.MaxLength ?? 0,
            ColumnType.Guid => 4 * 36,
            ColumnType.DateOnly => 3,
            _ => 6,
        };

    // §13.7: 64 characters, and no U+0000 or character outside the Basic Multilingual Plane ("Schema Object Names").
    protected override string? CheckNameLength(string name)
    {
        foreach (char c in name)
        {
            if (c == '\0' || char.IsSurrogate(c))
            {
                return "has U+0000 or a character outside the Basic Multilingual Plane, which " + Dialect + " does not accept in a name.";
            }
        }

        return CodePoints(name) > 64 ? "is longer than the 64 characters that " + Dialect + " accepts in a name." : null;
    }

    // §13.7: the name of the Nth foreign key is <table>_ibfk_N; MariaDB 10.11 accepted 63 characters of it, MySQL 64.
    protected override void CheckForeignKeyNames(TableInfo table, int foreignKeys)
    {
        int limit = isMariaDb ? 63 : 64;
        int length = CodePoints(table.Name) + "_ibfk_".Length + foreignKeys.ToString(CultureInfo.InvariantCulture).Length;
        if (length > limit)
        {
            throw Error(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "{0} names the foreign keys of {1} {1}_ibfk_N, and the name of the last one has {2} characters, which is more than the {3} that it accepts; shorten the table name.",
                    Dialect,
                    table.Name,
                    length,
                    limit
                )
            );
        }
    }

    private static int DecimalBytes(int digits) => (digits / 9 * 4) + _leftoverDigitBytes[digits % 9];
}
