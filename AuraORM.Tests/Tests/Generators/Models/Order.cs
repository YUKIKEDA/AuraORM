using AuraORM.Builder.Attributes;

namespace AuraORM.Tests.Tests.Generators.Models;

/// <summary>
/// テスト用注文エンティティ
/// </summary>
[Table("orders")]
public partial record Order(
    [Pk, AutoIncrement] int Id,
    int UserId,
    string ProductName,
    decimal Amount,
    DateTime CreatedAt
);
