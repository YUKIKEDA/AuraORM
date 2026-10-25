using AuraORM;

namespace AuraORM.Benchmarks;

/// <summary>
/// A row of six columns that do not allow null: numbers, a string, a decimal, a time, and a Boolean.
/// </summary>
[Table("people")]
public sealed partial record Person(
    [PrimaryKey] long Id,
    [MaxLength(50)] string Name,
    int Age,
    [Precision(18, 2)] decimal Balance,
    DateTime CreatedAt,
    bool Active
);

/// <summary>
/// A row of six columns, five of which allow null.
/// </summary>
[Table("maybe")]
public sealed partial record Maybe([PrimaryKey] long Id, string? Nickname, int? Age, [Precision(18, 2)] decimal? Balance, DateTime? Seen, bool? Flag);

/// <summary>
/// A pet of a person, for the statement with two joins.
/// </summary>
[Table("pets")]
public sealed partial record Pet([PrimaryKey] long Id, long OwnerId, [MaxLength(50)] string Name);

/// <summary>
/// A toy of a pet, for the statement with two joins.
/// </summary>
[Table("toys")]
public sealed partial record Toy([PrimaryKey] long Id, long PetId, [Precision(18, 2)] decimal Price);

/// <summary>
/// The row of <see cref="Person"/> as the mutable class that EF Core and linq2db map.
/// </summary>
public sealed class PersonRow
{
    public long Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int Age { get; set; }

    public decimal Balance { get; set; }

    public DateTime CreatedAt { get; set; }

    public bool Active { get; set; }
}

/// <summary>
/// The row of <see cref="Pet"/> as a mutable class.
/// </summary>
public sealed class PetRow
{
    public long Id { get; set; }

    public long OwnerId { get; set; }

    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// The row of <see cref="Toy"/> as a mutable class.
/// </summary>
public sealed class ToyRow
{
    public long Id { get; set; }

    public long PetId { get; set; }

    public decimal Price { get; set; }
}
