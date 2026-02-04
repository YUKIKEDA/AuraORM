namespace AuraORM.Builder.Attributes;

/// <summary>
/// SQLの結果をマッピングするための型であることを示す属性
/// Source Generatorがこの属性を持つrecordからマッパーメソッドを生成する
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
public sealed class QueryResultAttribute : Attribute
{
}
