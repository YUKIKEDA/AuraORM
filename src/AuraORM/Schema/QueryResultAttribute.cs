namespace AuraORM;

/// <summary>
/// Declares a result type: a partial record or class that a projection or a raw query reads rows into. The source
/// generator adds <c>RowProjection</c> and <c>GroupProjection</c> to it (<c>docs/design.ja.md</c> §3.5).
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class QueryResultAttribute : Attribute { }
