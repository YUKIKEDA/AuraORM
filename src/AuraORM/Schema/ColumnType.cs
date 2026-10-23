namespace AuraORM;

/// <summary>
/// The CLR types a column can have (<c>docs/design.ja.md</c> §3.3). An enum column has the type of its underlying integer.
/// </summary>
public enum ColumnType
{
    /// <summary><see cref="bool"/>.</summary>
    Boolean,

    /// <summary><see cref="byte"/>.</summary>
    Byte,

    /// <summary><see cref="short"/>.</summary>
    Int16,

    /// <summary><see cref="int"/>.</summary>
    Int32,

    /// <summary><see cref="long"/>.</summary>
    Int64,

    /// <summary><see cref="float"/>.</summary>
    Single,

    /// <summary><see cref="double"/>.</summary>
    Double,

    /// <summary><see cref="decimal"/>.</summary>
    Decimal,

    /// <summary><see cref="string"/>.</summary>
    String,

    /// <summary>A byte array.</summary>
    Binary,

    /// <summary><see cref="System.Guid"/>.</summary>
    Guid,

    /// <summary><see cref="System.DateTime"/>.</summary>
    DateTime,

    /// <summary><see cref="System.DateTimeOffset"/>.</summary>
    DateTimeOffset,

    /// <summary><see cref="System.DateOnly"/>.</summary>
    DateOnly,

    /// <summary><see cref="System.TimeOnly"/>.</summary>
    TimeOnly,
}
