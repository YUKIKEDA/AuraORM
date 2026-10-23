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
