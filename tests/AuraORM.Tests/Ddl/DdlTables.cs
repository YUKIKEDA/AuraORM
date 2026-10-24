namespace AuraORM.Tests;

/// <summary>
/// The <c>User</c> of design §3.1, the referenced table of <see cref="ShopOrder"/>.
/// </summary>
[Table("users")]
public sealed partial record ShopUser(
    [PrimaryKey, AutoIncrement] long Id,
    [MaxLength(100)] string Name,
    int? Age,
    [Default(SqlDefault.CurrentTimestamp)] DateTime CreatedAt
);

/// <summary>
/// The <c>Order</c> of design §13: a foreign key, a unique column, a nullable unique index, and defaults.
/// </summary>
[Table("orders")]
[Index("ix_orders_user", nameof(UserId), nameof(CreatedAt))]
[Index("ux_orders_coupon", nameof(Coupon), Unique = true)]
public sealed partial record ShopOrder(
    [PrimaryKey, AutoIncrement] long Id,
    [References(typeof(ShopUser), nameof(ShopUser.Id))] long UserId,
    [Unique, MaxLength(20)] string Code,
    [MaxLength(20)] string? Coupon,
    [Precision(18, 2), Default(0)] decimal Total,
    [Default(SqlDefault.CurrentTimestamp)] DateTime CreatedAt
);

/// <summary>
/// A column of every type of design §3.3, with and without a length, nullable, an enum, and a converted column.
/// </summary>
[Table("every_type")]
public sealed partial record EveryType(
    [PrimaryKey] long Id,
    bool Flag,
    byte Tiny,
    short Small,
    int Number,
    float Single,
    double Real,
    [Precision(18, 2)] decimal Money,
    string Text,
    [MaxLength(20)] string Short,
    byte[] Blob,
    [MaxLength(16)] byte[] Bytes,
    Guid Key,
    DateTime At,
    DateTimeOffset AtOffset,
    DateOnly Day,
    TimeOnly Time,
    AccountStatus Status,
    int? MaybeNumber,
    [MaxLength(200)] Uri? Link
);

/// <summary>
/// A default of every kind of design §13.3: each constant type, null, the current time, and raw SQL.
/// </summary>
[Table("defaults")]
public sealed partial record DefaultRow(
    [PrimaryKey, AutoIncrement] long Id,
    [Default(true)] bool Flag,
    [Default(false)] bool Off,
    [Default(255)] byte Tiny,
    [Default(-2)] short Small,
    [Default(-1)] int Number,
    [Default(0)] long Big,
    [Default(1.5f)] float Single,
    [Default(-1.5)] double Real,
    [Precision(18, 2), Default(12)] decimal Money,
    [Default("it's é")] string Text,
    [MaxLength(20), Default("x")] string Short,
    [Default(AccountStatus.Open)] AccountStatus Status,
    [Default(null)] string? Missing,
    [Default(null)] int? MaybeNumber,
    [Default(SqlDefault.CurrentTimestamp)] DateTime At,
    [Default(SqlDefault.CurrentTimestamp)] DateTimeOffset AtOffset,
    [DefaultSql("42")] int Raw
);

/// <summary>
/// A composite primary key whose first column is a foreign key, and a unique foreign key column: MySQL and MariaDB need
/// no index of their own for either (design §13.5).
/// </summary>
[Table("order_lines")]
public sealed partial record OrderLine(
    [PrimaryKey, References(typeof(ShopOrder), nameof(ShopOrder.Id))] long OrderId,
    [PrimaryKey] int Line,
    [Unique, References(typeof(ShopUser), nameof(ShopUser.Id))] long ReviewerId
);

/// <summary>
/// A table in a schema that references a table in a schema, with an index (design §13.6).
/// </summary>
[Table("audit", Schema = "app")]
[Index("ix_audit_account", nameof(AccountId))]
public sealed partial record Audit([PrimaryKey] long Id, [References(typeof(ArchivedAccount), nameof(ArchivedAccount.Id))] long AccountId);

/// <summary>
/// A unique index of two nullable columns: SQL Server filters out the rows where either is NULL (design §13.4).
/// </summary>
[Table("pairs")]
[Index("ux_pairs", nameof(Left), nameof(Right), Unique = true)]
public sealed partial record Pair([PrimaryKey] long Id, int? Left, [MaxLength(10)] string? Right);
