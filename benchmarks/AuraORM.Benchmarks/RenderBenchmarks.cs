using BenchmarkDotNet.Attributes;
using LinqToDB;
using LinqToDB.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using static AuraORM.Sql;

namespace AuraORM.Benchmarks;

/// <summary>
/// Building a statement and rendering its SQL, without a connection (design §16.1). Dapper takes the SQL text as it is,
/// so it has no cost here. EF Core renders queries only (<c>ToQueryString</c>), and linq2db's queries are rendered with
/// <c>ToSqlQuery</c>; neither renders an INSERT or UPDATE without running it.
/// </summary>
[MemoryDiagnoser]
public class RenderBenchmarks : IDisposable
{
    private static readonly Person.Insert Row = new()
    {
        Id = 1,
        Name = "Alice",
        Age = 30,
        Balance = 1.5m,
        CreatedAt = new DateTime(2024, 1, 1),
        Active = true,
    };

    private readonly SqlBuilder<SqliteDialect> _sql = new();
    private SqliteConnection _connection = null!;
    private BenchContext _ef = null!;
    private DataConnection _linq2Db = null!;

    [GlobalSetup]
    public void Setup()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _ef = BenchContext.Create(_connection);
        _linq2Db = Linq2Db.Create(_connection);
    }

    [GlobalCleanup]
    public void Dispose()
    {
        _ef?.Dispose();
        _linq2Db?.Dispose();
        _connection?.Dispose();
        GC.SuppressFinalize(this);
    }

    [Benchmark]
    [BenchmarkCategory("Select")]
    public string AuraOrmSelect() => _sql.From(Person.Table).Where(p => Gt(p.Age, 30)).Select(p => p).ToSql().Text;

    [Benchmark]
    [BenchmarkCategory("Select")]
    public string Linq2DbSelect() => _linq2Db.GetTable<PersonRow>().Where(p => p.Age > 30).ToSqlQuery().Sql;

    [Benchmark]
    [BenchmarkCategory("Select")]
    public string EfCoreSelect() => _ef.People.Where(p => p.Age > 30).ToQueryString();

    [Benchmark]
    [BenchmarkCategory("JoinGroup")]
    public string AuraOrmJoinGroup() =>
        _sql.From(Person.Table)
            .InnerJoin(Pet.Table, (p, pet) => Eq(pet.OwnerId, p.Id))
            .InnerJoin(Toy.Table, (p, pet, toy) => Eq(toy.PetId, pet.Id))
            .GroupBy((p, pet, toy) => p.Name)
            .Select((k, p, pet, toy) => (k, Count()))
            .ToSql()
            .Text;

    [Benchmark]
    [BenchmarkCategory("JoinGroup")]
    public string Linq2DbJoinGroup() =>
        (
            from p in _linq2Db.GetTable<PersonRow>()
            join pet in _linq2Db.GetTable<PetRow>() on p.Id equals pet.OwnerId
            join toy in _linq2Db.GetTable<ToyRow>() on pet.Id equals toy.PetId
            group p by p.Name into g
            select new { g.Key, Count = g.Count() }
        )
            .ToSqlQuery()
            .Sql;

    [Benchmark]
    [BenchmarkCategory("JoinGroup")]
    public string EfCoreJoinGroup() =>
        (
            from p in _ef.People
            join pet in _ef.Pets on p.Id equals pet.OwnerId
            join toy in _ef.Toys on pet.Id equals toy.PetId
            group p by p.Name into g
            select new { g.Key, Count = g.Count() }
        ).ToQueryString();

    [Benchmark]
    [BenchmarkCategory("Insert")]
    public string AuraOrmInsert() => _sql.Insert(Person.Table).Values(Row).ToSql().Text;

    [Benchmark]
    [BenchmarkCategory("Update")]
    public string AuraOrmUpdate() => _sql.Update(Person.Table).Set(p => [Set(p.Age, 31)]).Where(p => Eq(p.Id, 1L)).ToSql().Text;
}
