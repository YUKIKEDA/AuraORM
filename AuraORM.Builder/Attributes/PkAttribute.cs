namespace AuraORM.Builder.Attributes;

/// <summary>
/// 主キーを指定する属性
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
public sealed class PkAttribute : Attribute
{
}
