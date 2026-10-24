namespace AuraORM.Integration.Tests;

/// <summary>
/// The names of the schema of the DDL tests, one per test process, so the processes of <c>net8.0</c> and
/// <c>net10.0</c> do not share one on MySQL and MariaDB, where a schema is a database.
/// </summary>
internal static class DdlNames
{
#if NET10_0_OR_GREATER
    public const string Schema = "aura_ddl10";
#else
    public const string Schema = "aura_ddl8";
#endif
}

/// <summary>
/// The <c>User</c> of design §3.1, created by DDL.
/// </summary>
[Table("ddl_users")]
public sealed partial record DdlUser(
    [PrimaryKey, AutoIncrement] long Id,
    [MaxLength(100)] string Name,
    int? Age,
    [Default(SqlDefault.CurrentTimestamp)] DateTime CreatedAt
);

/// <summary>
/// The <c>Order</c> of design §13, created by DDL: a foreign key, a unique column, a nullable unique index, and defaults.
/// </summary>
[Table("ddl_orders")]
[Index("ix_ddl_orders_user", nameof(UserId), nameof(CreatedAt))]
[Index("ux_ddl_orders_coupon", nameof(Coupon), Unique = true)]
public sealed partial record DdlOrder(
    [PrimaryKey, AutoIncrement] long Id,
    [References(typeof(DdlUser), nameof(DdlUser.Id))] long UserId,
    [Unique, MaxLength(20)] string Code,
    [MaxLength(20)] string? Coupon,
    [Precision(18, 2), Default(0)] decimal Total,
    [Default(SqlDefault.CurrentTimestamp)] DateTime CreatedAt
);

/// <summary>
/// A column of every type of design §3.3, created by DDL.
/// </summary>
[Table("ddl_every")]
public sealed partial record DdlEvery(
    [PrimaryKey] long Id,
    bool Flag,
    byte Tiny,
    short Small,
    int Number,
    long Big,
    float Single,
    double Real,
    [Precision(18, 4)] decimal Money,
    string Text,
    [MaxLength(20)] string Short,
    byte[] Blob,
    [MaxLength(16)] byte[] Bytes,
    Guid Key,
    DateTime At,
    DateTimeOffset AtOffset,
    DateOnly Day,
    TimeOnly Time,
    PersonStatus Status,
    int? Missing
);

/// <summary>
/// A default of every kind of design §13.3, created by DDL.
/// </summary>
[Table("ddl_defaults")]
public sealed partial record DdlDefault(
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
    [Default(PersonStatus.Active)] PersonStatus Status,
    [Default(null)] string? Missing,
    [Default(null)] int? MaybeNumber,
    [Default(SqlDefault.CurrentTimestamp)] DateTime At,
    [Default(SqlDefault.CurrentTimestamp)] DateTimeOffset AtOffset,
    [DefaultSql("42")] int Raw
);

/// <summary>
/// The referenced table of <see cref="DdlScopedChild"/>, in the schema of the DDL tests (design §13.6).
/// </summary>
[Table("ddl_parents", Schema = DdlNames.Schema)]
public sealed partial record DdlScopedParent([PrimaryKey] long Id);

/// <summary>
/// A table in the schema of the DDL tests that references a table in it, with an index (design §13.6).
/// </summary>
[Table("ddl_children", Schema = DdlNames.Schema)]
[Index("ix_ddl_children_parent", nameof(ParentId))]
public sealed partial record DdlScopedChild([PrimaryKey] long Id, [References(typeof(DdlScopedParent), nameof(DdlScopedParent.Id))] long ParentId);
