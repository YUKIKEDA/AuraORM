namespace AuraORM.Builder.Attributes;

/// <summary>
/// 自動インクリメントを指定する属性
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
public sealed class AutoIncrementAttribute : Attribute
{
}