using System;

using AuraORM.Tests.Fixtures;
using AuraORM.Tests.Tests.Generators.Models;

using Microsoft.Data.Sqlite;

namespace AuraORM.Tests.Tests.Query;

[Collection("Database")]
public class TransactionTests : IDisposable
{
    private static readonly UserTable UserTable = UserTable.Instance;
    private static readonly OrderTable OrderTable = OrderTable.Instance;
    private readonly DatabaseFixture _fixture;

    public TransactionTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _fixture.ResetDatabase();
    }

    public void Dispose() => GC.SuppressFinalize(this);

    [Fact]
    public async Task トランザクションが正常終了するとコミットされる()
    {
        // Arrange & Act
        await _fixture.Db.TransactionAsync(async tx =>
        {
            await tx.Insert(UserTable.Instance).Values(new()
            {
                { (UserTable.Name, "トランザクションユーザー"), (UserTable.Age, 25), (UserTable.CreatedAt, DateTime.Now) }
            }).ExecuteAsync();
        });

        // Assert
        Assert.NotNull(GetUserByName("トランザクションユーザー"));
    }

    [Fact]
    public async Task トランザクションで複数操作を実行すると全てコミットされる()
    {
        // Arrange & Act
        await _fixture.Db.TransactionAsync(async tx =>
        {
            var now = DateTime.Now;
            await tx.Insert(UserTable.Instance).Values(new()
            {
                { (UserTable.Name, "ユーザー1"), (UserTable.Age, 20), (UserTable.CreatedAt, now) }
            }).ExecuteAsync();

            await tx.Insert(UserTable.Instance).Values(new()
            {
                { (UserTable.Name, "ユーザー2"), (UserTable.Age, 25), (UserTable.CreatedAt, now) }
            }).ExecuteAsync();

            await tx.Insert(UserTable.Instance).Values(new()
            {
                { (UserTable.Name, "ユーザー3"), (UserTable.Age, 30), (UserTable.CreatedAt, now) }
            }).ExecuteAsync();
        });

        // Assert
        Assert.NotNull(GetUserByName("ユーザー1"));
        Assert.NotNull(GetUserByName("ユーザー2"));
        Assert.NotNull(GetUserByName("ユーザー3"));
    }

    [Fact]
    public async Task トランザクションは戻り値を正しく返す()
    {
        // Arrange & Act
        var result = await _fixture.Db.TransactionAsync(async tx =>
        {
            await tx.Insert(UserTable.Instance).Values(new()
            {
                { (UserTable.Name, "テストユーザー"), (UserTable.Age, 25), (UserTable.CreatedAt, DateTime.Now) }
            }).ExecuteAsync();
            // 戻り値の検証用。42は『銀河ヒッチハイク・ガイド』由来の慣習的なダミー値
            return 42;
        });

        // Assert
        Assert.Equal(42, result);
        Assert.NotNull(GetUserByName("テストユーザー"));
    }

    [Fact]
    public async Task トランザクションで例外が発生するとロールバックされる()
    {
        // Arrange

        // Act
        try
        {
            await _fixture.Db.TransactionAsync(async tx =>
            {
                await tx.Insert(UserTable.Instance).Values(new()
                {
                    { (UserTable.Name, "ロールバックユーザー"), (UserTable.Age, 25), (UserTable.CreatedAt, DateTime.Now) }
                }).ExecuteAsync();

                throw new InvalidOperationException("意図的な例外");
            });
        }
        catch (InvalidOperationException)
        {
            // 例外を握りつぶす
        }

        // Assert - 挿入がロールバックされ存在しない
        Assert.Null(GetUserByName("ロールバックユーザー"));
    }

    [Fact]
    public async Task トランザクションの途中で例外が発生するとそれ以前の操作もロールバックされる()
    {
        // Arrange

        // Act
        try
        {
            await _fixture.Db.TransactionAsync(async tx =>
            {
                // 最初の挿入
                await tx.Insert(UserTable.Instance).Values(new()
                {
                    { (UserTable.Name, "ユーザー1"), (UserTable.Age, 20), (UserTable.CreatedAt, DateTime.Now) }
                }).ExecuteAsync();

                // 2番目の挿入
                await tx.Insert(UserTable.Instance).Values(new()
                {
                    { (UserTable.Name, "ユーザー2"), (UserTable.Age, 25), (UserTable.CreatedAt, DateTime.Now) }
                }).ExecuteAsync();

                // 例外を投げる
                throw new InvalidOperationException("途中で例外");
            });
        }
        catch (InvalidOperationException)
        {
            // 例外を握りつぶす
        }

        // Assert - 最初の挿入もロールバックされる
        Assert.Null(GetUserByName("ユーザー1"));
        Assert.Null(GetUserByName("ユーザー2"));
    }

    [Fact]
    public async Task トランザクションで複数テーブルを操作すると全てコミットされる()
    {
        // Arrange & Act（先にユーザーを挿入すると id=1 になる）
        await _fixture.Db.TransactionAsync(async tx =>
        {
            await tx.Insert(UserTable.Instance).Values(new()
            {
                { (UserTable.Name, "購入者"), (UserTable.Age, 30), (UserTable.CreatedAt, DateTime.Now) }
            }).ExecuteAsync();

            var now = DateTime.Now;
            await tx.Insert(OrderTable.Instance).Values(new()
            {
                { (OrderTable.UserId, 1), (OrderTable.ProductName, "購入商品"), (OrderTable.Amount, 1000m), (OrderTable.Status, OrderStatus.Pending), (OrderTable.OrderDate, now) }
            }).ExecuteAsync();
        });

        // Assert
        Assert.NotNull(GetUserByName("購入者"));
        Assert.True(CountOrdersByProductName("購入商品") >= 1);
    }

    [Fact]
    public async Task トランザクションで複数テーブル操作中に例外が発生すると全てロールバックされる()
    {
        // Act
        try
        {
            await _fixture.Db.TransactionAsync(async tx =>
            {
                await tx.Insert(UserTable.Instance).Values(new()
                {
                    { (UserTable.Name, "購入者"), (UserTable.Age, 30), (UserTable.CreatedAt, DateTime.Now) }
                }).ExecuteAsync();

                var now = DateTime.Now;
                await tx.Insert(OrderTable.Instance).Values(new()
                {
                    { (OrderTable.UserId, 1), (OrderTable.ProductName, "購入商品"), (OrderTable.Amount, 1000m), (OrderTable.Status, OrderStatus.Pending), (OrderTable.OrderDate, now) }
                }).ExecuteAsync();

                throw new InvalidOperationException("購入処理で例外");
            });
        }
        catch (InvalidOperationException)
        {
            // 例外を握りつぶす
        }

        // Assert - ユーザーも注文もロールバックされている
        Assert.Null(GetUserByName("購入者"));
        Assert.Equal(0, CountOrdersByProductName("購入商品"));
    }

    [Fact]
    public async Task トランザクションでInsertUpdateDeleteを実行すると全てコミットされる()
    {
        // Arrange - 初期データ
        _fixture.SeedUsers();

        // Act
        await _fixture.Db.TransactionAsync(async tx =>
        {
            // INSERT
            await tx.Insert(UserTable.Instance).Values(new()
            {
                { (UserTable.Name, "新規ユーザー"), (UserTable.Age, 22), (UserTable.CreatedAt, DateTime.Now) }
            }).ExecuteAsync();

            // UPDATE
            await tx.Update(UserTable.Instance).Set(new()
            {
                { (UserTable.Age, 99) }
            }).Where(UserTable.Name == "太郎").ExecuteAsync();

            // DELETE
            await tx.Delete(UserTable.Instance).Where(UserTable.Name == "次郎").ExecuteAsync();
        });

        // Assert
        Assert.NotNull(GetUserByName("新規ユーザー"));
        var taro = GetUserByName("太郎");
        Assert.NotNull(taro);
        Assert.Equal(99, taro.Value.Age);
        Assert.Null(GetUserByName("次郎"));
    }

    private (string Name, int? Age)? GetUserByName(string name)
    {
        using var cmd = new SqliteCommand(
            "SELECT name, age FROM users WHERE name = @name",
            _fixture.Connection);
        cmd.Parameters.AddWithValue("@name", name);
        using var reader = cmd.ExecuteReader();
        if (!reader.Read())
            return null;
        var n = reader.GetString(reader.GetOrdinal("name"));
        var age = reader.IsDBNull(reader.GetOrdinal("age")) ? (int?)null : reader.GetInt32(reader.GetOrdinal("age"));
        return (Name: n, Age: age);
    }

    private int CountOrdersByProductName(string productName)
    {
        using var cmd = new SqliteCommand(
            "SELECT COUNT(*) FROM orders WHERE productname = @productName",
            _fixture.Connection);
        cmd.Parameters.AddWithValue("@productName", productName);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }
}
