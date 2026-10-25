using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;
using Microsoft.EntityFrameworkCore;

namespace AuraORM.Benchmarks;

/// <summary>
/// The EF Core model of the benchmark tables.
/// </summary>
public sealed class BenchContext(DbContextOptions<BenchContext> options) : DbContext(options)
{
    public DbSet<PersonRow> People => Set<PersonRow>();

    public DbSet<PetRow> Pets => Set<PetRow>();

    public DbSet<ToyRow> Toys => Set<ToyRow>();

    public static BenchContext Create(System.Data.Common.DbConnection connection) =>
        new(new DbContextOptionsBuilder<BenchContext>().UseSqlite(connection).Options);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        _ = modelBuilder.Entity<PersonRow>().ToTable("people").HasKey(p => p.Id);
        _ = modelBuilder.Entity<PetRow>().ToTable("pets").HasKey(p => p.Id);
        _ = modelBuilder.Entity<ToyRow>().ToTable("toys").HasKey(p => p.Id);
    }
}

/// <summary>
/// The linq2db mapping of the benchmark tables.
/// </summary>
internal static class Linq2Db
{
    private static readonly MappingSchema Mapping = CreateMapping();

    public static DataConnection Create(System.Data.Common.DbConnection connection) =>
        new(
            new DataOptions()
                .UseConnection(connection)
                .UseSQLite(LinqToDB.DataProvider.SQLite.SQLiteProvider.Microsoft, o => o)
                .UseMappingSchema(Mapping)
        );

    private static MappingSchema CreateMapping()
    {
        var schema = new MappingSchema();
        var builder = new FluentMappingBuilder(schema);
        _ = builder.Entity<PersonRow>().HasTableName("people").HasPrimaryKey(p => p.Id);
        _ = builder.Entity<PetRow>().HasTableName("pets").HasPrimaryKey(p => p.Id);
        _ = builder.Entity<ToyRow>().HasTableName("toys").HasPrimaryKey(p => p.Id);
        builder.Build();
        return schema;
    }
}
