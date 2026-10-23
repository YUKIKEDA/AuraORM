namespace AuraORM;

/// <summary>
/// Adds a unique constraint on one column.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
public sealed class UniqueAttribute : Attribute { }
