namespace AuraORM;

/// <summary>
/// Marks the integer primary key whose values the database assigns. A table has at most one; it is not in the
/// <c>Insert</c> model, and it cannot be assigned in <c>UPDATE</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
public sealed class AutoIncrementAttribute : Attribute { }
