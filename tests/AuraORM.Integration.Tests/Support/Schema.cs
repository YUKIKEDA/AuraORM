namespace AuraORM.Integration.Tests;

/// <summary>
/// The status of a person, stored as its underlying <see cref="short"/>.
/// </summary>
public enum PersonStatus : short
{
    Inactive = 0,
    Active = 1,
}

/// <summary>
/// The table the integration tests use. Every database creates it with the DDL of its <see cref="ITestTarget{TDialect}"/>.
/// </summary>
[Table("people", Naming = ColumnNaming.SnakeCase)]
public sealed partial record Person(
    [PrimaryKey] long Id,
    [MaxLength(100)] string Name,
    string? Nickname,
    int? Age,
    PersonStatus Status,
    [Precision(18, 2)] decimal Balance,
    DateOnly Birthday
);

/// <summary>
/// A result of a raw query.
/// </summary>
[QueryResult]
public sealed partial record NameAndAge([Column("person_name")] string Name, int? Age);

/// <summary>
/// A row with one nullable column of each column type of design §3.3, to check that each type and its null are written and
/// read back on every database.
/// </summary>
[Table("typed_values", Naming = ColumnNaming.SnakeCase)]
public sealed partial record TypedRow(
    [PrimaryKey] long Id,
    bool? FlagValue,
    byte? TinyValue,
    short? SmallValue,
    int? NumberValue,
    long? BigValue,
    float? SingleValue,
    double? DoubleValue,
    [Precision(18, 4)] decimal? MoneyValue,
    string? TextValue,
    byte[]? BlobValue,
    Guid? KeyValue,
    DateTime? AtValue,
    DateTimeOffset? AtOffsetValue,
    DateOnly? DayValue,
    TimeOnly? TimeValue,
    PersonStatus? StateValue
);

/// <summary>
/// A pet, whose owner is a person or nobody: the table the join tests join to <see cref="Person"/>.
/// </summary>
[Table("pets", Naming = ColumnNaming.SnakeCase)]
public sealed partial record Pet([PrimaryKey] long Id, long? OwnerId, [MaxLength(50)] string Name);
