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

    protected override IReadOnlyList<string> ResetPeople { get; } =
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
    ];

    protected override Task<DbDataSource> CreateDataSourceAsync(string connectionString) =>
        Task.FromResult<DbDataSource>(NpgsqlDataSource.Create(connectionString));
}

/// <summary>
/// MySQL through MySqlConnector.
/// </summary>
public sealed class MySqlTarget : ServerTarget<MySqlDialect>
{
    protected override string Name => "mysql";

    protected override IReadOnlyList<string> ResetPeople => MySqlFamily.ResetPeople;

    protected override Task<DbDataSource> CreateDataSourceAsync(string connectionString) =>
        Task.FromResult<DbDataSource>(new MySqlDataSource(connectionString));
}

/// <summary>
/// MariaDB through MySqlConnector.
/// </summary>
public sealed class MariaDbTarget : ServerTarget<MariaDbDialect>
{
    protected override string Name => "mariadb";

    protected override IReadOnlyList<string> ResetPeople => MySqlFamily.ResetPeople;

    protected override Task<DbDataSource> CreateDataSourceAsync(string connectionString) =>
        Task.FromResult<DbDataSource>(new MySqlDataSource(connectionString));
}

/// <summary>
/// SQL Server through Microsoft.Data.SqlClient. The tests use a database of their own, created on first use.
/// </summary>
public sealed class SqlServerTarget : ServerTarget<SqlServerDialect>
{
    protected override string Name => "sqlserver";

    protected override IReadOnlyList<string> ResetPeople { get; } =
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
    ];

    protected override async Task<DbDataSource> CreateDataSourceAsync(string connectionString)
    {
        await using (var master = new SqlConnection(connectionString))
        {
            await master.OpenAsync();
            await using SqlCommand create = master.CreateCommand();
            create.CommandText = "IF DB_ID('aura') IS NULL CREATE DATABASE aura";
            await create.ExecuteNonQueryAsync();
        }

        var builder = new SqlConnectionStringBuilder(connectionString) { InitialCatalog = "aura" };
        return SqlClientFactory.Instance.CreateDataSource(builder.ConnectionString);
    }
}

/// <summary>
/// The DDL that MySQL and MariaDB share.
/// </summary>
internal static class MySqlFamily
{
    public static readonly IReadOnlyList<string> ResetPeople =
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
    ];
}
