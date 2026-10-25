using BenchmarkDotNet.Attributes;
using LinqToDB;
using LinqToDB.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SqExpress;
using SqExpress.SqlExport;
using Sqlify.Core;
using SqlKata;
using SqlKata.Compilers;
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

    private readonly SqPeople _sqPeople = new();
    private readonly SqPets _sqPets = new();
    private readonly SqToys _sqToys = new();
    private readonly ISqlifyPeople _sqlifyPeople = Sqlify.Sql.Table<ISqlifyPeople>();
    private readonly ISqlifyPets _sqlifyPets = Sqlify.Sql.Table<ISqlifyPets>();
    private readonly ISqlifyToys _sqlifyToys = Sqlify.Sql.Table<ISqlifyToys>();

    private readonly SqlBuilder<SqliteDialect> _sql = new();
    private readonly SqliteCompiler _sqlKata = new();
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
    [BenchmarkCategory("Select")]
    public string SqExpressSelect() =>
        SqQueryBuilder
            .Select(_sqPeople.Id, _sqPeople.Name, _sqPeople.Age, _sqPeople.Balance, _sqPeople.CreatedAt, _sqPeople.Active)
            .From(_sqPeople)
            .Where(_sqPeople.Age > 30)
            .Done()
            .ToSql(SqliteExporter.Default);

    [Benchmark]
    [BenchmarkCategory("Select")]
    public string SqlKataSelect() =>
        _sqlKata
            .Compile(
                new Query("people")
                    .Select("people.Id", "people.Name", "people.Age", "people.Balance", "people.CreatedAt", "people.Active")
                    .Where("people.Age", ">", 30)
            )
            .Sql;

    [Benchmark]
    [BenchmarkCategory("Select")]
    public string SqlifySelect()
    {
        var writer = new SqlWriter();
        Sqlify
            .Sql.Select(_sqlifyPeople.Id, _sqlifyPeople.Name, _sqlifyPeople.Age, _sqlifyPeople.Balance, _sqlifyPeople.CreatedAt, _sqlifyPeople.Active)
            .From(_sqlifyPeople)
            .Where(_sqlifyPeople.Age > 30)
            .Format(writer);
        return writer.GetCommand();
    }

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
    [BenchmarkCategory("JoinGroup")]
    public string SqExpressJoinGroup() =>
        SqQueryBuilder
            .Select(_sqPeople.Name, SqQueryBuilder.CountOne())
            .From(_sqPeople)
            .InnerJoin(_sqPets, on: _sqPets.OwnerId == _sqPeople.Id)
            .InnerJoin(_sqToys, on: _sqToys.PetId == _sqPets.Id)
            .GroupBy(_sqPeople.Name)
            .Done()
            .ToSql(SqliteExporter.Default);

    [Benchmark]
    [BenchmarkCategory("JoinGroup")]
    public string SqlKataJoinGroup() =>
        _sqlKata
            .Compile(
                new Query("people")
                    .Join("pets", "pets.OwnerId", "people.Id")
                    .Join("toys", "toys.PetId", "pets.Id")
                    .GroupBy("people.Name")
                    .Select("people.Name")
                    .SelectRaw("COUNT(*)")
            )
            .Sql;

    [Benchmark]
    [BenchmarkCategory("JoinGroup")]
    public string SqlifyJoinGroup()
    {
        var writer = new SqlWriter();
        Sqlify
            .Sql.Select(_sqlifyPeople.Name, Sqlify.Sql.Count())
            .From(_sqlifyPeople)
            .Join(_sqlifyPets, _sqlifyPets.OwnerId == _sqlifyPeople.Id)
            .Join(_sqlifyToys, _sqlifyToys.PetId == _sqlifyPets.Id)
            .GroupBy(_sqlifyPeople.Name)
            .Format(writer);
        return writer.GetCommand();
    }

    [Benchmark]
    [BenchmarkCategory("Insert")]
    public string AuraOrmInsert() => _sql.Insert(Person.Table).Values(Row).ToSql().Text;

    [Benchmark]
    [BenchmarkCategory("Update")]
    public string AuraOrmUpdate() => _sql.Update(Person.Table).Set(p => [Set(p.Age, 31)]).Where(p => Eq(p.Id, 1L)).ToSql().Text;
}
