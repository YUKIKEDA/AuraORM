namespace AuraORM;

/// <summary>
/// Marks a column of the primary key. On several columns, it makes a composite key in declaration order.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
public sealed class PrimaryKeyAttribute : Attribute { }
