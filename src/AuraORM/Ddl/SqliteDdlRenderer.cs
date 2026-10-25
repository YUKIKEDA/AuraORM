namespace AuraORM;

/// <summary>
/// The DDL of SQLite (design §13).
/// </summary>
internal sealed class SqliteDdlRenderer(DialectRenderer sql) : DdlRenderer(sql)
{
    // §13.2: the column is the rowid alias only when its type is exactly INTEGER and it is the primary key.
    protected override string AutoIncrement => "INTEGER PRIMARY KEY AUTOINCREMENT";

    protected override bool WritesKeyOnColumn(ColumnInfo[] key) => key.Length == 1 && key[0].IsAutoIncrement;

    protected override string TypeName(ColumnInfo column) =>
        column.Type switch
        {
            ColumnType.Boolean or ColumnType.Byte or ColumnType.Int16 or ColumnType.Int32 or ColumnType.Int64 => "INTEGER",
            ColumnType.Single or ColumnType.Double => "REAL",
            ColumnType.Binary => "BLOB",
            _ => "TEXT",
        };

    // §13.3: the text Microsoft.Data.Sqlite writes for DateTime and DateTimeOffset, with milliseconds (%f is SS.SSS).
    protected override string CurrentTimestamp(ColumnType type) =>
        type == ColumnType.DateTimeOffset ? "(strftime('%Y-%m-%d %H:%M:%f+00:00', 'now'))" : "(strftime('%Y-%m-%d %H:%M:%f', 'now'))";

    protected override string BooleanLiteral(bool value) => value ? "1" : "0";

    // §13.3: the text Microsoft.Data.Sqlite writes for a decimal parameter, so the default equals it as a string.
    protected override string DecimalLiteral(decimal value) =>
        "'" + value.ToString("0.0###########################", System.Globalization.CultureInfo.InvariantCulture) + "'";

    // §13.6: REFERENCES takes no schema, and the parent table must be in the database of the child table.
    protected override void WriteReferencedTable(SqlWriter writer, TableInfo table, ForeignKeyInfo reference) =>
        WriteIdentifier(writer, reference.Table);

    protected override void CheckReferencedSchema(TableInfo table, ForeignKeyInfo reference)
    {
        if (table.Schema != reference.Schema)
        {
            throw Error(
                "The table "
                    + table.Name
                    + " references "
                    + reference.Table
                    + " in another schema. SQLite's REFERENCES takes no schema, and the referenced table must be in the database of the table that references it."
            );
        }
    }

    // §13.6: SQLite qualifies the index name, not the table name.
    protected override void WriteIndexTarget(SqlWriter writer, TableInfo table, string index)
    {
        WriteTableName(writer, table.Schema, index);
        writer.Append(" ON ");
        WriteIdentifier(writer, table.Name);
    }

    protected override void WriteDropIndexTarget(SqlWriter writer, TableInfo table, string index) => WriteTableName(writer, table.Schema, index);
}
