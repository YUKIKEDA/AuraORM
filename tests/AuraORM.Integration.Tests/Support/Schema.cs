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
    decimal Balance,
    DateOnly Birthday
);

/// <summary>
/// A result of a raw query.
/// </summary>
[QueryResult]
public sealed partial record NameAndAge([Column("person_name")] string Name, int? Age);
