using System.Data.Common;
using BenchmarkDotNet.Attributes;
using Dapper;
using LinqToDB;
using LinqToDB.Async;
using LinqToDB.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AuraORM.Benchmarks;

/// <summary>
/// A query of every row of a table on SQLite in memory: the whole round trip of each library, on one open connection
/// (design §16.1).
/// </summary>
[MemoryDiagnoser]
public class RoundTripBenchmarks : IDisposable
{
    private static readonly Func<BenchContext, IAsyncEnumerable<PersonRow>> Compiled = EF.CompileAsyncQuery(
        (BenchContext c) => c.People.AsNoTracking()
    );

    private SqliteConnection _connection = null!;
    private Database<SqliteDialect> _db = null!;
    private BenchContext _ef = null!;
    private DataConnection _linq2Db = null!;

    [Params(1, 100)]
    public int Count { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        _db = new Database<SqliteDialect>(_connection);
        _db.CreateTable(Person.Table).ExecuteAsync().GetAwaiter().GetResult();
        Person.Insert[] rows = Enumerable
            .Range(1, Count)
            .Select(i => new Person.Insert
            {
                Id = i,
                Name = "name" + i,
                Age = 20 + (i % 50),
                Balance = i * 1.5m,
                CreatedAt = new DateTime(2024, 1, 1).AddMinutes(i),
                Active = i % 2 == 0,
            })
            .ToArray();
        _ = _db.Insert(Person.Table).Values(rows).ExecuteAsync().GetAwaiter().GetResult();
        _ef = BenchContext.Create(_connection);
        _linq2Db = Benchmarks.Linq2Db.Create(_connection);
    }

    [GlobalCleanup]
    public void Dispose()
    {
        _ef?.Dispose();
        _linq2Db?.Dispose();
        _connection?.Dispose();
        GC.SuppressFinalize(this);
    }

    [Benchmark(Baseline = true)]
    public async Task<List<Person>> HandWritten()
    {
        await using DbCommand command = _connection.CreateCommand();
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
    public Task<List<Person>> AuraOrm() => _db.From(Person.Table).Select(p => p).ToListAsync();

    [Benchmark]
    public async Task<List<PersonRow>> Dapper() => (await _connection.QueryAsync<PersonRow>(Rows.SelectPeople)).AsList();

    [Benchmark]
    public Task<List<PersonRow>> DapperAot() => AotQueries.PeopleRowsAsync(_connection);

    [Benchmark]
    public Task<List<PersonRow>> Linq2Db() => AsyncExtensions.ToListAsync(_linq2Db.GetTable<PersonRow>());

    [Benchmark]
    public Task<List<PersonRow>> EfCore() => EntityFrameworkQueryableExtensions.ToListAsync(_ef.People.AsNoTracking());

    [Benchmark]
    public async Task<List<PersonRow>> EfCoreCompiled()
    {
        var rows = new List<PersonRow>();
        await foreach (PersonRow row in Compiled(_ef))
        {
            rows.Add(row);
        }

        return rows;
    }
}
