using AuraORM.Builder.Attributes;

namespace AuraORM.Tests.Tests.Generators.Models;

/// <summary>
/// テスト用ユーザーエンティティ
/// </summary>
[Table("users")]
public partial record User(
    [Pk, AutoIncrement] int Id,
    string Name,
    int? Age,
    DateTime CreatedAt
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