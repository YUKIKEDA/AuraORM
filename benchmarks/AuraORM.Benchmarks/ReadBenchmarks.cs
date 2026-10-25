using System.Data.Common;
using AuraORM.Tests;
using BenchmarkDotNet.Attributes;
using Dapper;

namespace AuraORM.Benchmarks;

/// <summary>
/// Reading rows from an in-memory reader whose getters do not allocate (design §16.1): the cost of the reading code
/// itself, against a loop written by hand that calls the typed getters by ordinal.
/// </summary>
[MemoryDiagnoser]
public class ReadBenchmarks
{
    private FakeConnection _people = null!;
    private FakeConnection _maybe = null!;
    private Database<SqliteDialect> _peopleDb = null!;
    private Database<SqliteDialect> _maybeDb = null!;

    [Params(1, 100, 10000)]
    public int Count { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _people = Rows.People(Count);
        _maybe = Rows.Maybe(Count);
        _peopleDb = new Database<SqliteDialect>(_people);
        _maybeDb = new Database<SqliteDialect>(_maybe);
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("People")]
    public async Task<List<Person>> HandWritten()
    {
        await using DbCommand command = _people.CreateCommand();
        command.CommandText = Rows.SelectPeople;
        await using DbDataReader reader = await command.ExecuteReaderAsync();
        var rows = new List<Person>();
        while (await reader.ReadAsync())
        {
            rows.Add(
                new Person(
                    reader.GetInt64(0),
                    reader.GetString(1),
                    reader.GetInt32(2),
                    reader.GetDecimal(3),
                    reader.GetDateTime(4),
                    reader.GetBoolean(5)
                )
            );
        }

        return rows;
    }

    [Benchmark]
    [BenchmarkCategory("People")]
    public Task<List<Person>> AuraOrm() => _peopleDb.From(Person.Table).Select(p => p).ToListAsync();

    [Benchmark]
    [BenchmarkCategory("People")]
    public async Task<List<Person>> Dapper() => (await _people.QueryAsync<Person>(Rows.SelectPeople)).AsList();

    [Benchmark]
    [BenchmarkCategory("People")]
    public Task<List<Person>> DapperAot() => AotQueries.PeopleAsync(_people);

    [Benchmark]
    [BenchmarkCategory("Maybe")]
    public async Task<List<Maybe>> HandWrittenNullable()
    {
        await using DbCommand command = _maybe.CreateCommand();
        command.CommandText = Rows.SelectMaybe;
        await using DbDataReader reader = await command.ExecuteReaderAsync();
        var rows = new List<Maybe>();
        while (await reader.ReadAsync())
        {
            rows.Add(
                new Maybe(
                    reader.GetInt64(0),
                    reader.IsDBNull(1) ? null : reader.GetString(1),
                    reader.IsDBNull(2) ? null : reader.GetInt32(2),
                    reader.IsDBNull(3) ? null : reader.GetDecimal(3),
                    reader.IsDBNull(4) ? null : reader.GetDateTime(4),
                    reader.IsDBNull(5) ? null : reader.GetBoolean(5)
                )
            );
        }

        return rows;
    }

    [Benchmark]
    [BenchmarkCategory("Maybe")]
    public Task<List<Maybe>> AuraOrmNullable() => _maybeDb.From(Maybe.Table).Select(p => p).ToListAsync();

    [Benchmark]
    [BenchmarkCategory("Maybe")]
    public async Task<List<Maybe>> DapperNullable() => (await _maybe.QueryAsync<Maybe>(Rows.SelectMaybe)).AsList();

    [Benchmark]
    [BenchmarkCategory("Maybe")]
    public Task<List<Maybe>> DapperAotNullable() => AotQueries.MaybeAsync(_maybe);
}
