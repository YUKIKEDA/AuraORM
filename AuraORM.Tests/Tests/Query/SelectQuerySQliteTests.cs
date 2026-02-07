using System.Diagnostics;

using AuraORM.Tests.Fixtures;
using AuraORM.Tests.Tests.Generators.Models;

namespace AuraORM.Tests.Tests.Query;

[Collection("Database")]
public class SelectQuerySQliteTests : IDisposable
{
    private static readonly UserTable UserTable = UserTable.Instance;
    private static readonly OrderTable OrderTable = OrderTable.Instance;

    private readonly DatabaseFixture _fixture;
    public SelectQuerySQliteTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _fixture.ResetDatabase();
        _fixture.SeedUsers();
        _fixture.SeedOrders();
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task Selectで何もしていない場合はFrom句で指定したテーブルのデータを全件取得できる()
    {
        // Arrange & Act
        var query = _fixture.Db.Select().From(UserTable);
        var actual = await query.ToListAsync();

        // Assert
        Assert.Equal(4, actual.Count);
        Assert.Equal("太郎", actual[0].Name);
        Assert.Equal(25, actual[0].Age);
        Assert.Equal("花子", actual[1].Name);
        Assert.Equal("次郎", actual[2].Name);
        Assert.Equal("美咲", actual[3].Name);
    }

    [Fact]
    public async Task Selectで指定した1つのカラムのデータをFromで指定したテーブルから取得できる()
    {
        // Arrange
        var expected = new List<string> { "太郎", "花子", "次郎", "美咲" };

        // Act
        var query = _fixture.Db.Select(UserTable.Name).From(UserTable);
        var actual = await query.ToListAsync<UserNameResult>();

        // Assert
        Assert.Equal(expected, actual.Select(x => x.Name).ToList());
    }

    [Fact]
    public async Task Selectで指定した複数のカラムのデータをFromで指定したテーブルから取得できる()
    {
        // Arrange & Act
        var query = _fixture.Db.Select(UserTable.Name, UserTable.Age).From(UserTable);
        var actual = await query.ToListAsync<UserNameAndAgeResult>();

        // Assert
        Assert.Equal(4, actual.Count);
        Assert.Equal("太郎", actual[0].Name);
        Assert.Equal(25, actual[0].Age);
        Assert.Equal("花子", actual[1].Name);
        Assert.Equal(30, actual[1].Age);
        Assert.Equal("次郎", actual[2].Name);
        Assert.Equal(18, actual[2].Age);
        Assert.Equal("美咲", actual[3].Name);
        Assert.Equal(28, actual[3].Age);
    }

    [Fact]
    public async Task SELECT_FROM_INNER_JOIN句でデータを取得できる()
    {
        // Arrange
        // Act
        var query = _fixture.Db.Select().From(UserTable).InnerJoin(OrderTable, UserTable.Id == OrderTable.UserId);
        var actual = await query.ToListAsync<UserOrderSummary>();

        // Assert
        Assert.NotNull(actual);
        Assert.Equal(3, actual.Count);

        // ユーザー1（太郎）の注文2件
        var taroOrders = actual.Where(x => x.UserName == "太郎").OrderBy(x => x.OrderId).ToList();
        Assert.Equal(2, taroOrders.Count);
        Assert.Equal(1, taroOrders[0].UserId);
        Assert.Equal("太郎", taroOrders[0].UserName);
        Assert.Equal(25, taroOrders[0].Age);
        Assert.Equal("商品A", taroOrders[0].ProductName);
        Assert.Equal(1000m, taroOrders[0].Amount);
        Assert.Equal(OrderStatus.Pending, taroOrders[0].Status);

        Assert.Equal("商品B", taroOrders[1].ProductName);
        Assert.Equal(2000m, taroOrders[1].Amount);
        Assert.Equal(OrderStatus.Shipped, taroOrders[1].Status);

        // ユーザー2（花子）の注文1件
        var hanakoOrder = actual.Single(x => x.UserName == "花子");
        Assert.Equal(2, hanakoOrder.UserId);
        Assert.Equal("花子", hanakoOrder.UserName);
        Assert.Equal(30, hanakoOrder.Age);
        Assert.Equal("商品C", hanakoOrder.ProductName);
        Assert.Equal(1500m, hanakoOrder.Amount);
        Assert.Equal(OrderStatus.Processing, hanakoOrder.Status);
    }

    [Fact]
    public async Task LEFT_JOIN句を生成できる()
    {
        // Arrange
        // Act（LEFT JOIN: 全ユーザー4人、注文のないユーザーは order 側が null → 5行）
        var query = _fixture.Db.Select().From(UserTable).LeftJoin(OrderTable, UserTable.Id == OrderTable.UserId);
        var actual = await query.ToListAsync<UserOrderSummaryLeftJoin>();

        // Assert
        Assert.NotNull(actual);
        Assert.Equal(5, actual.Count);

        var withOrders = actual.Where(x => x.OrderId.HasValue && x.OrderId != 0).ToList();
        Assert.Equal(3, withOrders.Count);
        Assert.Equal(2, withOrders.Count(x => x.UserName == "太郎"));
        Assert.Equal(1, withOrders.Count(x => x.UserName == "花子" && x.ProductName == "商品C"));
        Assert.Contains(withOrders, x => x.UserName == "太郎" && x.ProductName == "商品A");
        Assert.Contains(withOrders, x => x.UserName == "太郎" && x.ProductName == "商品B");

        var withoutOrders = actual.Where(x => !x.OrderId.HasValue || x.OrderId == 0).Select(x => x.UserName).OrderBy(x => x).ToList();
        Assert.Equal(2, withoutOrders.Count);
        Assert.Contains("次郎", withoutOrders);
        Assert.Contains("美咲", withoutOrders);
    }

    [Fact]
    public async Task RIGHT_JOIN句を生成できる()
    {
        // Arrange
        // Act（SQLite は RIGHT JOIN を FROM orders LEFT JOIN users 等で表現する想定 → 注文3件分の行）
        var query = _fixture.Db.Select().From(UserTable).RightJoin(OrderTable, UserTable.Id == OrderTable.UserId);
        var actual = await query.ToListAsync<UserOrderSummary>();

        // Assert
        Assert.NotNull(actual);
        Assert.Equal(3, actual.Count);
        Assert.Equal(1, actual.Count(x => x.UserName == "太郎" && x.ProductName == "商品A"));
        Assert.Equal(1, actual.Count(x => x.UserName == "太郎" && x.ProductName == "商品B"));
        Assert.Equal(1, actual.Count(x => x.UserName == "花子" && x.ProductName == "商品C"));
    }

    [Fact]
    public async Task FULL_JOIN句を生成できる()
    {
        // Arrange
        // Act（FULL JOIN: 両方のテーブルの全行。SQLite は FULL OUTER JOIN をサポートしない場合あり）
        var query = _fixture.Db.Select().From(UserTable).FullJoin(OrderTable, UserTable.Id == OrderTable.UserId);
        var actual = await query.ToListAsync<UserOrderSummaryLeftJoin>();

        // Assert（少なくとも INNER の 3 行は含まれる）
        Assert.NotNull(actual);
        Assert.True(actual.Count >= 3);
        var withOrders = actual.Where(x => x.OrderId.HasValue && x.OrderId != 0).ToList();
        Assert.Contains(withOrders, x => x.UserName == "太郎" && x.ProductName == "商品A");
        Assert.Contains(withOrders, x => x.UserName == "花子" && x.ProductName == "商品C");
    }


    [Fact]
    public async Task JOIN句で異なるカラムを結合条件として使用できる()
    {
        // Arrange（users.name = orders.productname → 名前と商品名が一致する行のみ。シードデータでは 0 件）
        // Act
        var query = _fixture.Db.Select().From(UserTable)
            .InnerJoin(OrderTable, UserTable.Name == OrderTable.ProductName);
        var actual = await query.ToListAsync<UserOrderSummary>();

        // Assert
        Assert.NotNull(actual);
        Assert.Empty(actual);
    }

    [Fact]
    public async Task JOIN句で複数の条件を組み合わせて使用できる()
    {
        // Arrange（INNER JOIN + WHERE Age > 20 AND Name != "Bob" → 太郎・花子の注文 3 件）
        // Act
        var query = _fixture.Db.Select().From(UserTable)
            .InnerJoin(OrderTable, UserTable.Id == OrderTable.UserId)
            .Where((UserTable.Age > 20).And(UserTable.Name != "Bob"));
        var actual = await query.ToListAsync<UserOrderSummary>();

        // Assert
        Assert.NotNull(actual);
        Assert.Equal(3, actual.Count);
        Assert.Equal(2, actual.Count(x => x.UserName == "太郎"));
        Assert.Equal(1, actual.Count(x => x.UserName == "花子"));
        Assert.All(actual, x => Assert.True(x.Age > 20));
        Assert.All(actual, x => Assert.NotEqual("Bob", x.UserName));
    }

    [Fact]
    public async Task JOIN句で1つのカラムを指定して取得できる()
    {
        // Arrange
        // Act（Name のみ SELECT → UserNameResult で受け取る）
        var query = _fixture.Db.Select(UserTable.Name).From(UserTable)
            .InnerJoin(OrderTable, UserTable.Id == OrderTable.UserId);
        var actual = await query.ToListAsync<UserNameResult>();

        // Assert
        Assert.NotNull(actual);
        Assert.Equal(3, actual.Count);
        Assert.Equal(2, actual.Count(x => x.Name == "太郎"));
        Assert.Equal(1, actual.Count(x => x.Name == "花子"));
    }

    [Fact]
    public async Task JOIN句で複数のカラムを指定して取得できる()
    {
        // Arrange
        // Act（Name, Age のみ SELECT）
        var query = _fixture.Db.Select(UserTable.Name, UserTable.Age).From(UserTable)
            .InnerJoin(OrderTable, UserTable.Id == OrderTable.UserId);
        var actual = await query.ToListAsync<UserNameAndAgeResult>();

        // Assert
        Assert.NotNull(actual);
        Assert.Equal(3, actual.Count);
        Assert.Equal(2, actual.Count(x => x.Name == "太郎" && x.Age == 25));
        Assert.Equal(1, actual.Count(x => x.Name == "花子" && x.Age == 30));
    }

    [Fact]
    public async Task JOIN句でJOIN先のテーブルのカラムを指定して取得できる()
    {
        // Arrange
        // Act（Order の ProductName のみ SELECT）
        var query = _fixture.Db.Select(OrderTable.ProductName).From(UserTable)
            .InnerJoin(OrderTable, UserTable.Id == OrderTable.UserId);
        var actual = await query.ToListAsync<ProductNameResult>();

        // Assert
        Assert.NotNull(actual);
        Assert.Equal(3, actual.Count);
        var productNames = actual.Select(x => x.ProductName).OrderBy(x => x).ToList();
        Assert.Equal("商品A", productNames[0]);
        Assert.Equal("商品B", productNames[1]);
        Assert.Equal("商品C", productNames[2]);
    }

    [Fact]
    public async Task JOIN句で両方のテーブルのカラムを指定して取得できる()
    {
        // Arrange
        // Act（UserName, ProductName を SELECT）
        var query = _fixture.Db.Select(UserTable.Name, OrderTable.ProductName).From(UserTable)
            .InnerJoin(OrderTable, UserTable.Id == OrderTable.UserId);
        var actual = await query.ToListAsync<NameAndProductNameResult>();

        // Assert
        Assert.NotNull(actual);
        Assert.Equal(3, actual.Count);
        Assert.Contains(actual, x => x.UserName == "太郎" && x.ProductName == "商品A");
        Assert.Contains(actual, x => x.UserName == "太郎" && x.ProductName == "商品B");
        Assert.Contains(actual, x => x.UserName == "花子" && x.ProductName == "商品C");
    }
}
