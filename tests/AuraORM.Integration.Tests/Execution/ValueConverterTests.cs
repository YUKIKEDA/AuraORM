using static AuraORM.Sql;

namespace AuraORM.Integration.Tests;

/// <summary>
/// Value converters (design §3.6) on one database: the converted values are stored as their provider type and read back. A
/// derived class per database gives the target.
/// </summary>
/// <typeparam name="TDialect">The dialect.</typeparam>
public abstract class ValueConverterTests<TDialect>
    where TDialect : IDialect
{
    private protected abstract ITestTarget<TDialect> Target { get; }

    private protected static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>
    /// Converted values are inserted, compared, ordered, updated, and read back through their converters, null included
    /// (design §3.6).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - An empty <c>orders</c> table: the id, the parent id, and the link are converted, the total is a converted decimal.
    ///
    /// Steps:
    /// - Insert order 1 with a link and a total of 1.50, and order 2 with parent 1 and no link; select the orders whose id is
    ///   in [1, 2], ordered by the id descending; update order 2's total to 3.25; select order 2.
    ///
    /// Expected:
    /// - Order 2 (parent 1, no link, no total) then order 1 (no parent, the link, 1.50).
    /// - After the update, order 2 has the total 3.25.
    /// </remarks>
    [Fact]
    public async Task ConvertedColumns_RoundTrip()
    {
        Database<TDialect> db = await Target.CreateDatabaseAsync();
        var link = new Uri("https://example.com/a");

        await db.Insert(Order.Table)
            .Values([
                new Order.Insert
                {
                    Id = new OrderId(1),
                    ParentId = null,
                    Link = link,
                    Total = new Price(1.50m),
                },
                new Order.Insert
                {
                    Id = new OrderId(2),
                    ParentId = new OrderId(1),
                    Link = null,
                    Total = null,
                },
            ])
            .ExecuteAsync(Ct);
        List<Order> orders = await db.From(Order.Table)
            .Where(o => In(o.Id, [new OrderId(1), new OrderId(2)]))
            .Select(o => o)
            .OrderBy(o => [Desc(o.Id)])
            .ToListAsync(Ct);
        await db.Update(Order.Table).Set(o => [Set(o.Total, new Price(3.25m))]).Where(o => Eq(o.Id, new OrderId(2))).ExecuteAsync(Ct);
        Order updated = await db.From(Order.Table).Where(o => Eq(o.Id, new OrderId(2))).Select(o => o).SingleAsync(Ct);

        Assert.Equal([new Order(new OrderId(2), new OrderId(1), null, null), new Order(new OrderId(1), null, link, new Price(1.50m))], orders);
        Assert.Equal(new Price(3.25m), updated.Total);
    }

    /// <summary>
    /// A converted value in a raw SQL hole is a parameter of its stored value, and a raw query reads converted members of a
    /// result type (design §3.6, §11.1, §11.3).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Order 7 with a link.
    ///
    /// Steps:
    /// - Run a raw query for the id and the link of the order whose id is <c>OrderId(7)</c>, into <see cref="OrderLink"/>.
    ///
    /// Expected:
    /// - One row: <c>OrderId(7)</c> and the link.
    /// </remarks>
    [Fact]
    public async Task RawQuery_ConvertsHolesAndMembers()
    {
        Database<TDialect> db = await Target.CreateDatabaseAsync();
        var link = new Uri("https://example.com/7");
        await db.Insert(Order.Table).Values(new Order.Insert { Id = new OrderId(7), Link = link }).ExecuteAsync(Ct);

        List<OrderLink> rows = await db.QueryAsync<OrderLink>($"SELECT id, link FROM orders WHERE id = {new OrderId(7)}", Ct);

        Assert.Equal([new OrderLink(new OrderId(7), link)], rows);
    }
}

/// <summary>
/// <see cref="ValueConverterTests{TDialect}"/> on SQLite in memory.
/// </summary>
public sealed class SqliteValueConverterTests : ValueConverterTests<SqliteDialect>, IAsyncLifetime
{
    private readonly SqliteTarget _target = new();

    private protected override ITestTarget<SqliteDialect> Target => _target;

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public ValueTask DisposeAsync() => _target.DisposeAsync();
}
