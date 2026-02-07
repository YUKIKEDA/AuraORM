using AuraORM.Builder.Attributes;

namespace AuraORM.Tests.Tests.Generators.Models;

public enum OrderStatus
{
    Pending = 0,
    Processing = 1,
    Shipped = 2,
    Delivered = 3,
    Cancelled = 4
}

[Table("orders")]
public partial record Order(
    [Pk, AutoIncrement, Column("id")] int Id,
    [Column("userid")] int UserId,
    [Column("productname")] string ProductName,
    [Column("amount")] decimal Amount,
    [Column("status")] OrderStatus Status,
    [Column("orderdate")] DateTime OrderDate,
    [Column("shippeddate")] DateTime? ShippedDate
);

[QueryResult]
public partial record UserOrderSummary(
    [Column("users_id")] int UserId,
    [Column("users_name")] string UserName,
    [Column("users_age")] int? Age,
    [Column("users_createdat")] DateTime CreatedAt,
    [Column("orders_id")] int OrderId,
    [Column("orders_productname")] string ProductName,
    [Column("orders_amount")] decimal Amount,
    [Column("orders_status")] OrderStatus Status,
    [Column("orders_orderdate")] DateTime OrderDate,
    [Column("orders_shippeddate")] DateTime? ShippedDate
);

/// <summary>LEFT JOIN 用（注文のないユーザーは order 側が null のため nullable）</summary>
[QueryResult]
public partial record UserOrderSummaryLeftJoin(
    [Column("users_id")] int UserId,
    [Column("users_name")] string UserName,
    [Column("users_age")] int? Age,
    [Column("users_createdat")] DateTime CreatedAt,
    [Column("orders_id")] int? OrderId,
    [Column("orders_productname")] string? ProductName,
    [Column("orders_amount")] decimal? Amount,
    [Column("orders_status")] OrderStatus? Status,
    [Column("orders_orderdate")] DateTime? OrderDate,
    [Column("orders_shippeddate")] DateTime? ShippedDate
);

[QueryResult]
public partial record ProductNameResult(
    [Column("productname")] string ProductName
);

[QueryResult]
public partial record NameAndProductNameResult(
    [Column("name")] string UserName,
    [Column("productname")] string ProductName
);
