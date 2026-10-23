using System.Data.Common;
using Microsoft.Data.SqlClient;
using MySqlConnector;
using Npgsql;

namespace AuraORM.Integration.Tests;

/// <summary>
/// PostgreSQL through Npgsql.
/// </summary>
public sealed class PostgreSqlTarget : ServerTarget<PostgreSqlDialect>
{
    protected override string Name => "postgresql";

    protected override IReadOnlyList<string> ResetTables { get; } =
    [
        "DROP TABLE IF EXISTS people",
        """
            CREATE TABLE people (
              id bigint PRIMARY KEY,
              name varchar(100) NOT NULL,
              nickname text,
              age integer,
              status smallint NOT NULL,
              balance numeric(18,2) NOT NULL,
              birthday date NOT NULL
            )
            """,
        "DROP TABLE IF EXISTS typed_values",
        """
            CREATE TABLE typed_values (
              id bigint PRIMARY KEY,
              flag_value boolean,
              tiny_value smallint,
              small_value smallint,
              number_value integer,
              big_value bigint,
              single_value real,
              double_value double precision,
              money_value numeric(18,4),
              text_value text,
              blob_value bytea,
              key_value uuid,
              at_value timestamp,
              at_offset_value timestamp with time zone,
              day_value date,
              time_value time,
              state_value smallint
            )
            """,
        "DROP TABLE IF EXISTS pets",
        "CREATE TABLE pets (id bigint PRIMARY KEY, owner_id bigint, name varchar(50) NOT NULL)",
    ];

    protected override async Task<DbDataSource> CreateDataSourceAsync(string connectionString)
    {
        await using (var server = new NpgsqlConnection(connectionString))
        {
            await server.OpenAsync();
            await using NpgsqlCommand exists = server.CreateCommand();
            exists.CommandText = "SELECT 1 FROM pg_database WHERE datname = $1";
            exists.Parameters.Add(new NpgsqlParameter { Value = DatabaseName });
            if (await exists.ExecuteScalarAsync() is null)
            {
                await using NpgsqlCommand create = server.CreateCommand();
                create.CommandText = "CREATE DATABASE " + DatabaseName;
                await create.ExecuteNonQueryAsync();
            }
        }

        var builder = new NpgsqlConnectionStringBuilder(connectionString) { Database = DatabaseName };
        return NpgsqlDataSource.Create(builder.ConnectionString);
    }
}

/// <summary>
/// MySQL through MySqlConnector.
/// </summary>
public sealed class MySqlTarget : ServerTarget<MySqlDialect>
{
    protected override string Name => "mysql";

    protected override IReadOnlyList<string> ResetTables => MySqlFamily.ResetTables;

    protected override Task<DbDataSource> CreateDataSourceAsync(string connectionString) =>
        MySqlFamily.CreateDataSourceAsync(connectionString, DatabaseName);
}

/// <summary>
/// MariaDB through MySqlConnector.
/// </summary>
public sealed class MariaDbTarget : ServerTarget<MariaDbDialect>
{
    protected override string Name => "mariadb";

    protected override IReadOnlyList<string> ResetTables => MySqlFamily.ResetTables;

    protected override Task<DbDataSource> CreateDataSourceAsync(string connectionString) =>
        MySqlFamily.CreateDataSourceAsync(connectionString, DatabaseName);
}

/// <summary>
/// SQL Server through Microsoft.Data.SqlClient.
/// </summary>
public sealed class SqlServerTarget : ServerTarget<SqlServerDialect>
{
    protected override string Name => "sqlserver";

    protected override IReadOnlyList<string> ResetTables { get; } =
    [
        "DROP TABLE IF EXISTS people",
        """
            CREATE TABLE people (
              id bigint PRIMARY KEY,
              name nvarchar(100) NOT NULL,
              nickname nvarchar(max),
              age int,
              status smallint NOT NULL,
              balance decimal(18,2) NOT NULL,
              birthday date NOT NULL
            )
            """,
        "DROP TABLE IF EXISTS typed_values",
        """
            CREATE TABLE typed_values (
              id bigint PRIMARY KEY,
              flag_value bit,
              tiny_value tinyint,
              small_value smallint,
              number_value int,
              big_value bigint,
              single_value real,
              double_value float,
              money_value decimal(18,4),
              text_value nvarchar(max),
              blob_value varbinary(max),
              key_value uniqueidentifier,
              at_value datetime2,
              at_offset_value datetimeoffset,
              day_value date,
              time_value time,
              state_value smallint
            )
            """,
        "DROP TABLE IF EXISTS pets",
        "CREATE TABLE pets (id bigint PRIMARY KEY, owner_id bigint, name nvarchar(50) NOT NULL)",
    ];

    protected override async Task<DbDataSource> CreateDataSourceAsync(string connectionString)
    {
        await using (var master = new SqlConnection(connectionString))
        {
            await master.OpenAsync();
            await using SqlCommand create = master.CreateCommand();
            create.CommandText = "IF DB_ID('" + DatabaseName + "') IS NULL CREATE DATABASE " + DatabaseName;
            await create.ExecuteNonQueryAsync();
        }

        var builder = new SqlConnectionStringBuilder(connectionString) { InitialCatalog = DatabaseName };
        return SqlClientFactory.Instance.CreateDataSource(builder.ConnectionString);
    }
}

/// <summary>
/// The DDL that MySQL and MariaDB share.
/// </summary>
internal static class MySqlFamily
{
    public static async Task<DbDataSource> CreateDataSourceAsync(string connectionString, string databaseName)
    {
        await using (var server = new MySqlConnection(connectionString))
        {
            await server.OpenAsync();
            await using MySqlCommand create = server.CreateCommand();
            create.CommandText = "CREATE DATABASE IF NOT EXISTS " + databaseName;
            await create.ExecuteNonQueryAsync();
        }

        var builder = new MySqlConnectionStringBuilder(connectionString) { Database = databaseName };
        return new MySqlDataSource(builder.ConnectionString);
    }

    public static readonly IReadOnlyList<string> ResetTables =
    [
        "DROP TABLE IF EXISTS people",
        """
            CREATE TABLE people (
              id BIGINT PRIMARY KEY,
              name VARCHAR(100) NOT NULL,
              nickname LONGTEXT,
              age INT,
              status SMALLINT NOT NULL,
              balance DECIMAL(18,2) NOT NULL,
              birthday DATE NOT NULL
            )
            """,
        "DROP TABLE IF EXISTS typed_values",
        """
            CREATE TABLE typed_values (
              id bigint PRIMARY KEY,
              flag_value BOOLEAN,
              tiny_value TINYINT UNSIGNED,
              small_value SMALLINT,
              number_value INT,
              big_value BIGINT,
              single_value FLOAT,
              double_value DOUBLE,
              money_value DECIMAL(18,4),
              text_value LONGTEXT,
              blob_value LONGBLOB,
              key_value CHAR(36),
              at_value DATETIME(6),
              at_offset_value DATETIME(6),
              day_value DATE,
              time_value TIME(6),
              state_value SMALLINT
            )
            """,
        "DROP TABLE IF EXISTS pets",
        "CREATE TABLE pets (id BIGINT PRIMARY KEY, owner_id BIGINT, name VARCHAR(50) NOT NULL)",
    ];
}
