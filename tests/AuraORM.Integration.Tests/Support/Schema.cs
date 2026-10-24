[assembly: AuraORM.ValueConverter(typeof(AuraORM.Integration.Tests.UriConverter))]

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

/// <summary>
/// A note, whose key the database assigns and whose other columns may be left out of an insert: the table of the insert
/// tests (design §6).
/// </summary>
[Table("notes", Naming = ColumnNaming.SnakeCase)]
public sealed partial record Note([PrimaryKey, AutoIncrement] long Id, string? Body, [Default(0)] int Priority);

/// <summary>
/// The number of pets of an owner, for the derived tables and CTEs of the tests (design §3.5, §5.11).
/// </summary>
[QueryResult]
public sealed partial record OwnerPets(long? Owner, long Count);

/// <summary>
/// A name and a number, for the set operations as named queries of the tests (design §5.11).
/// </summary>
[QueryResult]
public sealed partial record NamedValue(string Name, long Value);

/// <summary>
/// An order id, stored as its <see cref="long"/> (design §3.6).
/// </summary>
/// <param name="Value">The stored value.</param>
[ValueConverter(typeof(OrderIdConverter))]
public readonly record struct OrderId(long Value);

/// <summary>
/// Converts <see cref="OrderId"/> to <see cref="long"/>.
/// </summary>
public sealed class OrderIdConverter : IValueConverter<OrderId, long>
{
    public static long ToProvider(OrderId value) => value.Value;

    public static OrderId FromProvider(long value) => new(value);
}

/// <summary>
/// A price, stored as its <see cref="decimal"/> (design §3.6).
/// </summary>
/// <param name="Amount">The stored value.</param>
[ValueConverter(typeof(PriceConverter))]
public readonly record struct Price(decimal Amount);

/// <summary>
/// Converts <see cref="Price"/> to <see cref="decimal"/>.
/// </summary>
public sealed class PriceConverter : IValueConverter<Price, decimal>
{
    public static decimal ToProvider(Price value) => value.Amount;

    public static Price FromProvider(decimal value) => new(value);
}

/// <summary>
/// Converts <see cref="Uri"/>, a type of another library, to <see cref="string"/>; declared on the assembly (design §3.6).
/// </summary>
public sealed class UriConverter : IValueConverter<Uri, string>
{
    public static string ToProvider(Uri value) => value.ToString();

    public static Uri FromProvider(string value) => new(value);
}

/// <summary>
/// An order, whose columns are converted: the table of the value converter tests (design §3.6).
/// </summary>
[Table("orders", Naming = ColumnNaming.SnakeCase)]
public sealed partial record Order([PrimaryKey] OrderId Id, OrderId? ParentId, Uri? Link, [Precision(18, 2)] Price? Total);

/// <summary>
/// The id and the link of an order, read by a raw query (design §3.6, §11.3).
/// </summary>
[QueryResult]
public sealed partial record OrderLink(OrderId Id, Uri? Link);
