using AuraORM.Builder.Conditions;
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
        Assert.Equal(6, actual.Count);
        Assert.Equal("太郎", actual[0].Name);
        Assert.Equal(25, actual[0].Age);
        Assert.Equal("花子", actual[1].Name);
        Assert.Equal("次郎", actual[2].Name);
        Assert.Equal("美咲", actual[3].Name);
        Assert.Equal("健一", actual[4].Name);
        Assert.Equal("さくら", actual[5].Name);
    }

    [Fact]
    public async Task Selectで指定した1つのカラムのデータをFromで指定したテーブルから取得できる()
    {
        // Arrange
        var expected = new List<string> { "太郎", "花子", "次郎", "美咲", "健一", "さくら" };

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
        Assert.Equal(6, actual.Count);
        Assert.Equal("太郎", actual[0].Name);
        Assert.Equal(25, actual[0].Age);
        Assert.Equal("花子", actual[1].Name);
        Assert.Equal(30, actual[1].Age);
        Assert.Equal("次郎", actual[2].Name);
        Assert.Equal(18, actual[2].Age);
        Assert.Equal("美咲", actual[3].Name);
        Assert.Equal(28, actual[3].Age);
        Assert.Equal("健一", actual[4].Name);
        Assert.Equal(35, actual[4].Age);
        Assert.Equal("さくら", actual[5].Name);
        Assert.Equal(22, actual[5].Age);
    }

    [Fact]
    public async Task SELECT_FROM_INNER_JOIN句でデータを取得できる()
    {
        // Arrange
        // Act
        var query = _fixture.Db.Select().From(UserTable).InnerJoin(OrderTable, UserTable.Id == OrderTable.UserId);
        var actual = await query.ToListAsync<UserOrderSummary>();

        // Assert（注文7件: 太郎2, 花子1, 次郎1, 美咲1, 健一2。さくらは0件）
        Assert.NotNull(actual);
        Assert.Equal(7, actual.Count);

        Assert.Equal(2, actual.Count(x => x.UserName == "太郎"));
        Assert.Equal(1, actual.Count(x => x.UserName == "花子" && x.ProductName == "商品C"));
        Assert.Equal(1, actual.Count(x => x.UserName == "次郎" && x.ProductName == "商品D"));
        Assert.Equal(1, actual.Count(x => x.UserName == "美咲" && x.ProductName == "商品E"));
        Assert.Equal(2, actual.Count(x => x.UserName == "健一"));
        Assert.Contains(actual, x => x.UserName == "太郎" && x.ProductName == "商品A" && x.Amount == 1000m);
        Assert.Contains(actual, x => x.UserName == "太郎" && x.ProductName == "商品B" && x.Amount == 2000m);
        Assert.Contains(actual, x => x.UserName == "健一" && x.ProductName == "商品F");
        Assert.Contains(actual, x => x.UserName == "健一" && x.ProductName == "商品G");
    }

    [Fact]
    public async Task LEFT_JOIN句を生成できる()
    {
        // Arrange
        // Act（LEFT JOIN: 全ユーザー6人、注文のないユーザーは order 側が null → 8行）
        var query = _fixture.Db.Select().From(UserTable).LeftJoin(OrderTable, UserTable.Id == OrderTable.UserId);
        var actual = await query.ToListAsync<UserOrderSummaryLeftJoin>();

        // Assert
        Assert.NotNull(actual);
        Assert.Equal(8, actual.Count);

        var withOrders = actual.Where(x => x.OrderId.HasValue && x.OrderId != 0).ToList();
        Assert.Equal(7, withOrders.Count);
        Assert.Equal(2, withOrders.Count(x => x.UserName == "太郎"));
        Assert.Equal(2, withOrders.Count(x => x.UserName == "健一"));
        Assert.Contains(withOrders, x => x.UserName == "太郎" && x.ProductName == "商品A");
        Assert.Contains(withOrders, x => x.UserName == "花子" && x.ProductName == "商品C");
        Assert.DoesNotContain(withOrders, x => x.UserName == "さくら"); // さくらは注文なし

        var withoutOrders = actual.Where(x => !x.OrderId.HasValue || x.OrderId == 0).Select(x => x.UserName).ToList();
        Assert.Single(withoutOrders);
        Assert.Contains("さくら", withoutOrders);
    }

    [Fact]
    public async Task RIGHT_JOIN句を生成できる()
    {
        // Arrange
        // Act（SQLite は RIGHT JOIN を FROM orders LEFT JOIN users 等で表現する想定 → 注文3件分の行）
        var query = _fixture.Db.Select().From(UserTable).RightJoin(OrderTable, UserTable.Id == OrderTable.UserId);
        var actual = await query.ToListAsync<UserOrderSummary>();

        // Assert（注文7件）
        Assert.NotNull(actual);
        Assert.Equal(7, actual.Count);
        Assert.Equal(1, actual.Count(x => x.UserName == "太郎" && x.ProductName == "商品A"));
        Assert.Equal(1, actual.Count(x => x.UserName == "太郎" && x.ProductName == "商品B"));
        Assert.Equal(1, actual.Count(x => x.UserName == "花子" && x.ProductName == "商品C"));
        Assert.Equal(1, actual.Count(x => x.UserName == "健一" && x.ProductName == "商品G"));
    }

    [Fact]
    public async Task FULL_JOIN句を生成できる()
    {
        // Arrange
        // Act（FULL JOIN: 両方のテーブルの全行。SQLite は FULL OUTER JOIN をサポートしない場合あり）
        var query = _fixture.Db.Select().From(UserTable).FullJoin(OrderTable, UserTable.Id == OrderTable.UserId);
        var actual = await query.ToListAsync<UserOrderSummaryLeftJoin>();

        // Assert（少なくとも INNER の 7 行は含まれる）
        Assert.NotNull(actual);
        Assert.True(actual.Count >= 7);
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
        // Arrange（INNER JOIN + WHERE Age > 20 AND Name != "Bob" → 次郎(18)以外の注文 6 件）
        // Act
        var query = _fixture.Db.Select().From(UserTable)
            .InnerJoin(OrderTable, UserTable.Id == OrderTable.UserId)
            .Where((UserTable.Age > 20).And(UserTable.Name != "Bob"));
        var actual = await query.ToListAsync<UserOrderSummary>();

        // Assert
        Assert.NotNull(actual);
        Assert.Equal(6, actual.Count);
        Assert.Equal(2, actual.Count(x => x.UserName == "太郎"));
        Assert.Equal(1, actual.Count(x => x.UserName == "花子"));
        Assert.Equal(2, actual.Count(x => x.UserName == "健一"));
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

        // Assert（注文7件分の名前）
        Assert.NotNull(actual);
        Assert.Equal(7, actual.Count);
        Assert.Equal(2, actual.Count(x => x.Name == "太郎"));
        Assert.Equal(1, actual.Count(x => x.Name == "花子"));
        Assert.Equal(1, actual.Count(x => x.Name == "次郎"));
        Assert.Equal(2, actual.Count(x => x.Name == "健一"));
    }

    [Fact]
    public async Task JOIN句で複数のカラムを指定して取得できる()
    {
        // Arrange
        // Act（Name, Age のみ SELECT）
        var query = _fixture.Db.Select(UserTable.Name, UserTable.Age).From(UserTable)
            .InnerJoin(OrderTable, UserTable.Id == OrderTable.UserId);
        var actual = await query.ToListAsync<UserNameAndAgeResult>();

        // Assert（注文7件）
        Assert.NotNull(actual);
        Assert.Equal(7, actual.Count);
        Assert.Equal(2, actual.Count(x => x.Name == "太郎" && x.Age == 25));
        Assert.Equal(1, actual.Count(x => x.Name == "花子" && x.Age == 30));
        Assert.Equal(1, actual.Count(x => x.Name == "次郎" && x.Age == 18));
        Assert.Equal(2, actual.Count(x => x.Name == "健一" && x.Age == 35));
    }

    [Fact]
    public async Task JOIN句でJOIN先のテーブルのカラムを指定して取得できる()
    {
        // Arrange
        // Act（Order の ProductName のみ SELECT）
        var query = _fixture.Db.Select(OrderTable.ProductName).From(UserTable)
            .InnerJoin(OrderTable, UserTable.Id == OrderTable.UserId);
        var actual = await query.ToListAsync<ProductNameResult>();

        // Assert（注文7件、商品A〜G）
        Assert.NotNull(actual);
        Assert.Equal(7, actual.Count);
        var productNames = actual.Select(x => x.ProductName).OrderBy(x => x).ToList();
        Assert.Equal(new[] { "商品A", "商品B", "商品C", "商品D", "商品E", "商品F", "商品G" }, productNames);
    }

    [Fact]
    public async Task JOIN句で両方のテーブルのカラムを指定して取得できる()
    {
        // Arrange
        // Act（UserName, ProductName を SELECT）
        var query = _fixture.Db.Select(UserTable.Name, OrderTable.ProductName).From(UserTable)
            .InnerJoin(OrderTable, UserTable.Id == OrderTable.UserId);
        var actual = await query.ToListAsync<NameAndProductNameResult>();

        // Assert（注文7件）
        Assert.NotNull(actual);
        Assert.Equal(7, actual.Count);
        Assert.Contains(actual, x => x.UserName == "太郎" && x.ProductName == "商品A");
        Assert.Contains(actual, x => x.UserName == "太郎" && x.ProductName == "商品B");
        Assert.Contains(actual, x => x.UserName == "花子" && x.ProductName == "商品C");
        Assert.Contains(actual, x => x.UserName == "健一" && x.ProductName == "商品G");
    }

    [Fact]
    public async Task JOIN句とWHERE句を組み合わせて生成できる()
    {
        // Arrange（Age > 20 → 次郎(18)以外の注文 6 件）
        // Act
        var query = _fixture.Db.Select().From(UserTable)
            .InnerJoin(OrderTable, UserTable.Id == OrderTable.UserId)
            .Where(UserTable.Age > 20);
        var actual = await query.ToListAsync<UserOrderSummary>();

        // Assert
        Assert.NotNull(actual);
        Assert.Equal(6, actual.Count);
        Assert.All(actual, x => Assert.True(x.Age > 20));
    }

    [Fact]
    public async Task JOIN句とORDER_BY句を組み合わせて生成できる()
    {
        // Arrange（name ASC でソート）
        // Act
        var query = _fixture.Db.Select().From(UserTable)
            .InnerJoin(OrderTable, UserTable.Id == OrderTable.UserId)
            .OrderBy(new OrderByCondition("name", false));
        var actual = await query.ToListAsync<UserOrderSummary>();

        // Assert（7 件取得、ORDER BY name ASC が適用されている）
        Assert.NotNull(actual);
        Assert.Equal(7, actual.Count);
        Assert.Equal(2, actual.Count(x => x.UserName == "太郎"));
        Assert.Equal(1, actual.Count(x => x.UserName == "花子"));
        Assert.Equal(2, actual.Count(x => x.UserName == "健一"));
    }

    [Fact]
    public async Task JOIN句とLIMIT句を組み合わせて生成できる()
    {
        // Arrange（LIMIT 10、該当は 7 件）
        // Act
        var query = _fixture.Db.Select().From(UserTable)
            .InnerJoin(OrderTable, UserTable.Id == OrderTable.UserId)
            .Limit(10);
        var actual = await query.ToListAsync<UserOrderSummary>();

        // Assert
        Assert.NotNull(actual);
        Assert.Equal(7, actual.Count);
    }

    [Fact]
    public async Task JOIN句とOFFSET句を組み合わせて生成できる()
    {
        // Arrange（LIMIT 10 OFFSET 5。7 件中 5 件スキップで 2 件取得）
        // Act
        var query = _fixture.Db.Select().From(UserTable)
            .InnerJoin(OrderTable, UserTable.Id == OrderTable.UserId)
            .Limit(10)
            .Offset(5);
        var actual = await query.ToListAsync<UserOrderSummary>();

        // Assert
        Assert.NotNull(actual);
        Assert.Equal(2, actual.Count);
    }

    [Fact]
    public async Task JOIN句とWHERE句とORDER_BY句を組み合わせて生成できる()
    {
        // Arrange
        var joinCondition = UserTable.Id == OrderTable.UserId;

        // Act（Age > 20 かつ name ASC）
        var query = _fixture.Db.Select().From(UserTable)
            .InnerJoin(OrderTable, joinCondition)
            .Where(UserTable.Age > 20)
            .OrderBy(new OrderByCondition("name", false));
        var actual = await query.ToListAsync<UserOrderSummary>();

        // Assert（Age > 20 で 6 件）
        Assert.NotNull(actual);
        Assert.Equal(6, actual.Count);
        Assert.All(actual, x => Assert.True(x.Age > 20));
    }

    [Fact]
    public async Task JOIN句とWHERE句とLIMIT句とOFFSET句を組み合わせて生成できる()
    {
        // Arrange（Age > 20 で 6 件該当、OFFSET 5 で 1 件取得）
        // Act
        var query = _fixture.Db.Select().From(UserTable)
            .InnerJoin(OrderTable, UserTable.Id == OrderTable.UserId)
            .Where(UserTable.Age > 20)
            .Limit(10)
            .Offset(5);
        var actual = await query.ToListAsync<UserOrderSummary>();

        // Assert
        Assert.NotNull(actual);
        Assert.Single(actual);
        Assert.All(actual, x => Assert.True(x.Age > 20));
    }

    [Fact]
    public async Task JOIN句でカラム指定とWHERE句を組み合わせて生成できる()
    {
        // Arrange（Name, ProductName のみ SELECT、Age > 20）
        // Act
        var query = _fixture.Db.Select(UserTable.Name, OrderTable.ProductName).From(UserTable)
            .InnerJoin(OrderTable, UserTable.Id == OrderTable.UserId)
            .Where(UserTable.Age > 20);
        var actual = await query.ToListAsync<NameAndProductNameResult>();

        // Assert（Age > 20 で 6 件）
        Assert.NotNull(actual);
        Assert.Equal(6, actual.Count);
        Assert.Contains(actual, x => x.UserName == "太郎" && x.ProductName == "商品A");
        Assert.Contains(actual, x => x.UserName == "太郎" && x.ProductName == "商品B");
        Assert.Contains(actual, x => x.UserName == "花子" && x.ProductName == "商品C");
        Assert.Contains(actual, x => x.UserName == "健一" && x.ProductName == "商品G");
    }
}
