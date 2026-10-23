namespace AuraORM;

/// <summary>
/// How a source of a SELECT statement joins the sources before it (<c>docs/design.ja.md</c> §5.3).
/// </summary>
internal enum JoinKind
{
    /// <summary>The first table: <c>FROM t</c>.</summary>
    From,

    /// <summary><c>INNER JOIN t ON …</c>.</summary>
    Inner,

    /// <summary><c>LEFT JOIN t ON …</c>.</summary>
    Left,

    /// <summary><c>RIGHT JOIN t ON …</c>.</summary>
    Right,

    /// <summary><c>FULL JOIN t ON …</c>.</summary>
    Full,

    /// <summary><c>CROSS JOIN t</c>.</summary>
    Cross,
}
