namespace AuraORM.Builder.Attributes;

/// <summary>
/// テーブル名を指定する属性
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
public sealed class TableAttribute : Attribute
{
    /// <summary>
    /// テーブル名
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// テーブル名を指定する属性
    /// </summary>
    /// <param name="name">テーブル名</param>
    public TableAttribute(string name)
    {
        Name = name;
    }
}
