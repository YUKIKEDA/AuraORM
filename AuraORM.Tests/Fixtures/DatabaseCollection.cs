namespace AuraORM.Tests.Fixtures;

/// <summary>
/// xUnit用のコレクション定義
/// </summary>
[CollectionDefinition("Database")]
public class DatabaseCollection : ICollectionFixture<DatabaseFixture>
{
}
