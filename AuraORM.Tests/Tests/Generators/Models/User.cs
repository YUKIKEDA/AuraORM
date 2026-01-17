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
