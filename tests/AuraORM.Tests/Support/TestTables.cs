namespace AuraORM.Tests;

/// <summary>
/// Columns for tests of expressions, made through the internal constructor of <see cref="TableAlias"/>. The source generator
/// makes them for real tables (row G1).
/// </summary>
internal static class TestTables
{
    public static readonly TableAlias Users = new(schema: null, table: "users", alias: null);

    public static Column<long> Id { get; } = Users.Column<long>("Id");

    public static Column<string> Name { get; } = Users.Column<string>("Name");

    public static Column<string?> Nickname { get; } = Users.Column<string?>("Nickname");

    public static Column<int> Score { get; } = Users.Column<int>("Score");

    public static Column<int?> Age { get; } = Users.Column<int?>("Age");

    public static Column<decimal> Balance { get; } = Users.Column<decimal>("Balance");

    public static Column<double> Rating { get; } = Users.Column<double>("Rating");

    public static Column<bool> IsActive { get; } = Users.Column<bool>("IsActive");

    public static Column<UserStatus> Status { get; } = Users.Column<UserStatus>("Status");
}

/// <summary>
/// An enum column type, stored as its underlying integer.
/// </summary>
internal enum UserStatus : short
{
    Inactive = 0,
    Active = 1,
}
