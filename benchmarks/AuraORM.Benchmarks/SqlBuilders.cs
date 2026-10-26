using SqExpress;
using Sqlify.Core;

namespace AuraORM.Benchmarks;

/// <summary>
/// The tables of the benchmarks for SqExpress, without a schema. Each has its own name as its alias: without an alias,
/// SqExpress writes the columns of a join without their table (<c>"OwnerId"="Id"</c>).
/// </summary>
public sealed class SqPeople : TableBase
{
    public SqPeople()
        : base(null, "people", "people")
    {
        Id = CreateInt64Column("Id");
        Name = CreateStringColumn("Name", 50);
        Age = CreateInt32Column("Age");
        Balance = CreateDecimalColumn("Balance");
        CreatedAt = CreateDateTimeColumn("CreatedAt");
        Active = CreateBooleanColumn("Active");
    }

    public Int64TableColumn Id { get; }

    public StringTableColumn Name { get; }

    public Int32TableColumn Age { get; }

    public DecimalTableColumn Balance { get; }

    public DateTimeTableColumn CreatedAt { get; }

    public BooleanTableColumn Active { get; }
}

/// <summary>
/// The pets table for SqExpress.
/// </summary>
public sealed class SqPets : TableBase
{
    public SqPets()
        : base(null, "pets", "pets")
    {
        Id = CreateInt64Column("Id");
        OwnerId = CreateInt64Column("OwnerId");
        Name = CreateStringColumn("Name", 50);
    }

    public Int64TableColumn Id { get; }

    public Int64TableColumn OwnerId { get; }

    public StringTableColumn Name { get; }
}

/// <summary>
/// The toys table for SqExpress.
/// </summary>
public sealed class SqToys : TableBase
{
    public SqToys()
        : base(null, "toys", "toys")
    {
        Id = CreateInt64Column("Id");
        PetId = CreateInt64Column("PetId");
        Price = CreateDecimalColumn("Price");
    }

    public Int64TableColumn Id { get; }

    public Int64TableColumn PetId { get; }

    public DecimalTableColumn Price { get; }
}

[Sqlify.Table("people")]
public interface ISqlifyPeople : ITable
{
    Sqlify.Core.Expressions.Column<long> Id { get; }

    Sqlify.Core.Expressions.Column<string> Name { get; }

    Sqlify.Core.Expressions.Column<int> Age { get; }

    Sqlify.Core.Expressions.Column<decimal> Balance { get; }

    Sqlify.Core.Expressions.Column<DateTime> CreatedAt { get; }

    Sqlify.Core.Expressions.Column<bool> Active { get; }
}

[Sqlify.Table("pets")]
public interface ISqlifyPets : ITable
{
    Sqlify.Core.Expressions.Column<long> Id { get; }

    Sqlify.Core.Expressions.Column<long> OwnerId { get; }

    Sqlify.Core.Expressions.Column<string> Name { get; }
}

[Sqlify.Table("toys")]
public interface ISqlifyToys : ITable
{
    Sqlify.Core.Expressions.Column<long> Id { get; }

    Sqlify.Core.Expressions.Column<long> PetId { get; }

    Sqlify.Core.Expressions.Column<decimal> Price { get; }
}
