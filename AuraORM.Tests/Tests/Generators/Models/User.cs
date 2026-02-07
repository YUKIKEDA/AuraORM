using AuraORM.Builder.Attributes;

namespace AuraORM.Tests.Tests.Generators.Models;

/// <summary>
/// テスト用ユーザーエンティティ
/// </summary>
[Table("users")]
public partial record User(
    [Pk, AutoIncrement, Column("id")] int Id,
    [Column("name")] string Name,
    [Column("age")] int? Age,
    [Column("createdat")] DateTime CreatedAt
);


[QueryResult]
public partial record UserNameResult(
    [Column("name")] string Name
);

[QueryResult]
public partial record UserNameAndAgeResult(
    [Column("name")] string Name,
    [Column("age")] int? Age
);