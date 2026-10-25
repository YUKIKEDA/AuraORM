using System.Data.Common;
using Dapper;

namespace AuraORM.Benchmarks;

/// <summary>
/// The Dapper calls that Dapper.AOT rewrites at build time; the other Dapper calls of the benchmarks are plain Dapper.
/// </summary>
[DapperAot]
internal static class AotQueries
{
    public static async Task<List<Person>> PeopleAsync(DbConnection connection) => (await connection.QueryAsync<Person>(Rows.SelectPeople)).AsList();

    public static async Task<List<Maybe>> MaybeAsync(DbConnection connection) => (await connection.QueryAsync<Maybe>(Rows.SelectMaybe)).AsList();

    public static async Task<List<PersonRow>> PeopleRowsAsync(DbConnection connection) =>
        (await connection.QueryAsync<PersonRow>(Rows.SelectPeople)).AsList();
}
