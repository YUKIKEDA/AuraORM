namespace AuraORM;

/// <summary>
/// Excludes a property from the columns of a table or the members of a result type.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public sealed class NotColumnAttribute : Attribute { }
