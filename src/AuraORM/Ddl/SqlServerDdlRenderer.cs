using System.Globalization;

namespace AuraORM;

/// <summary>
/// The DDL of SQL Server (design §13).
/// </summary>
internal sealed class SqlServerDdlRenderer(DialectRenderer sql) : DdlRenderer(sql)
{
    protected override string AutoIncrement => "IDENTITY(1,1)";

    protected override bool WritesNull => true;

    protected override string TypeName(ColumnInfo column) =>
        column.Type switch
        {
            ColumnType.Boolean => "bit",
            ColumnType.Byte => "tinyint",
            ColumnType.Int16 => "smallint",
            ColumnType.Int32 => "int",
            ColumnType.Int64 => "bigint",
            ColumnType.Single => "real",
            ColumnType.Double => "float",
            ColumnType.Decimal => string.Format(CultureInfo.InvariantCulture, "decimal({0},{1})", column.Precision, column.Scale),
            ColumnType.String => column.MaxLength is { } n ? "nvarchar(" + n.ToString(CultureInfo.InvariantCulture) + ")" : "nvarchar(max)",
            ColumnType.Binary => column.MaxLength is { } m ? "varbinary(" + m.ToString(CultureInfo.InvariantCulture) + ")" : "varbinary(max)",
            ColumnType.Guid => "uniqueidentifier",
            ColumnType.DateTime => "datetime2",
            ColumnType.DateTimeOffset => "datetimeoffset",
            ColumnType.DateOnly => "date",
            _ => "time",
        };

    // §13.3: CURRENT_TIMESTAMP is GETDATE(), the time of the server's operating system.
    protected override string CurrentTimestamp(ColumnType type) => type == ColumnType.DateTimeOffset ? "SYSDATETIMEOFFSET()" : "SYSUTCDATETIME()";

    protected override string BooleanLiteral(bool value) => value ? "1" : "0";

    protected override void WriteStringLiteral(SqlWriter writer, string value)
    {
        writer.Append('N');
        base.WriteStringLiteral(writer, value);
    }

    // §13.4: a unique constraint allows one NULL (error 2627 on the second), and a constraint takes no filter.
    protected override void CheckUnique(TableInfo table, ColumnInfo column)
    {
        if (column.IsUnique && column.IsNullable)
        {
            throw Error(
                "The column "
                    + table.Name
                    + "."
                    + column.Name
                    + " allows NULL and has [Unique]; a unique constraint of SQL Server allows only one NULL. Declare it with [Index(..., Unique = true)], which SQL Server filters to the rows that are not NULL."
            );
        }
    }

    // §13.4: the rows where any column is NULL are left out of the check, as the other dialects do.
    protected override void WriteUniqueIndexFilter(SqlWriter writer, ColumnInfo[] columns)
    {
        string separator = " WHERE ";
        foreach (ColumnInfo column in columns.Where(c => c.IsNullable))
        {
            writer.Append(separator);
            WriteIdentifier(writer, column.Name);
            writer.Append(" IS NOT NULL");
            separator = " AND ";
        }
    }

    // §13.7: nvarchar(n) to 4000 (message 2717), varbinary(n) to 8000 (message 131), decimal to 38 (message 2750).
    protected override int? MaxStringLength => 4000;

    protected override int? MaxBinaryLength => 8000;

    protected override int? MaxPrecision => 38;

    // §13.7: "Maximum capacity specifications": 900 bytes for the clustered primary key, 1700 for a nonclustered index.
    protected override int? KeyBytes(KeyLimit limit) => limit == KeyLimit.PrimaryKey ? 900 : 1700;

    protected override int ColumnBytes(ColumnInfo column) =>
        column.Type switch
        {
            ColumnType.Boolean or ColumnType.Byte => 1,
            ColumnType.Int16 => 2,
            ColumnType.Int32 or ColumnType.Single => 4,
            ColumnType.Int64 or ColumnType.Double or ColumnType.DateTime => 8,
            ColumnType.Decimal => column.Precision switch
            {
                <= 9 => 5,
                <= 19 => 9,
                <= 28 => 13,
                _ => 17,
            },
            ColumnType.String => 2 * (column.MaxLength ?? 0),
            ColumnType.Binary => column.MaxLength ?? 0,
            ColumnType.Guid => 16,
            ColumnType.DateTimeOffset => 10,
            ColumnType.DateOnly => 3,
            _ => 5,
        };

    // §13.7: 128 characters, counted in UTF-16 units (message 103).
    protected override string? CheckNameLength(string name) =>
        name.Length > 128 ? "is longer than the 128 characters (UTF-16 units) that SQL Server accepts in a name." : null;
}
