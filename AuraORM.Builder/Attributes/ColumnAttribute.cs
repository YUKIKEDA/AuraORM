namespace AuraORM.Builder.Attributes;

/// <summary>
/// カラム名を指定する属性
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
public sealed class ColumnAttribute : Attribute
{
    /// <summary>
    /// カラム名
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// カラム名を指定する属性
    /// </summary>
    /// <param name="name">カラム名</param>

    public ColumnAttribute(string name)
    {
        Name = name;
    }
}