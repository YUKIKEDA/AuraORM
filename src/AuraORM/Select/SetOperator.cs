namespace AuraORM;

/// <summary>
/// The operator of a set operation (<c>docs/design.ja.md</c> §5.10).
/// </summary>
internal enum SetOperator
{
    Union,
    UnionAll,
    Intersect,
    Except,
}
