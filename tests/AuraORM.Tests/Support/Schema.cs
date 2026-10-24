namespace AuraORM.Tests;

/// <summary>
/// The status of an account, stored as its underlying <see cref="short"/>.
/// </summary>
public enum AccountStatus : short
{
    Closed = 0,
    Open = 1,
}

/// <summary>
/// A table with an auto-increment key, a required and a nullable column, defaults, an enum, and a member outside the
/// positional parameters.
/// </summary>
[Table("accounts", Naming = ColumnNaming.SnakeCase)]
[Index("ix_accounts_email", nameof(Email), Unique = true)]
public sealed partial record Account(
    [PrimaryKey, AutoIncrement] long Id,
    [MaxLength(200)] string Email,
    string? DisplayName,
    AccountStatus Status,
    [Default(SqlDefault.CurrentTimestamp)] DateTime CreatedAt,
    [Default(0)] int LoginCount
)
{
    [Precision(18, 2)]
    public decimal? Balance { get; init; }
}

/// <summary>
/// A table with a foreign key to <see cref="Account"/> and a composite primary key.
/// </summary>
[Table("memberships", Schema = "app")]
public sealed partial record Membership(
    [PrimaryKey, References(typeof(Account), nameof(Account.Id))] long AccountId,
    [PrimaryKey, MaxLength(30)] string Role,
    [Column("joined_on")] DateOnly JoinedOn
);

/// <summary>
/// A result type of a name and a total.
/// </summary>
[QueryResult]
public sealed partial record NameTotal(string Name, decimal? Total);

/// <summary>
/// A result type of an email and a count, for the derived tables and CTEs of the tests (design §3.5, §5.11).
/// </summary>
[QueryResult]
public sealed partial record EmailCount(string Email, long Count);

/// <summary>
/// A table whose columns all have a value without being set: an auto-increment key, a nullable column, and a default, for
/// an <c>INSERT</c> that sets no column (design §6.2).
/// </summary>
[Table("notes")]
public sealed partial record Note([PrimaryKey, AutoIncrement] long Id, string? Body, [Default(0)] int Priority);

/// <summary>
/// A table with the name of <see cref="Account"/>'s table in another schema, which a check on the table being changed must
/// tell apart from it (design §7).
/// </summary>
[Table("accounts", Schema = "archive", Naming = ColumnNaming.SnakeCase)]
public sealed partial record ArchivedAccount([PrimaryKey] long Id, [MaxLength(200)] string Email);

/// <summary>
/// A table named <c>Excluded</c>, the name of the row proposed for insertion in an upsert, in another case (design §10).
/// </summary>
[Table("Excluded")]
public sealed partial record ExcludedRow([PrimaryKey] long Id, int N);
