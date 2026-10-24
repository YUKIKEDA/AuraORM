using System.Collections.Immutable;

namespace AuraORM;

/// <summary>
/// A named query itself (<c>docs/design.ja.md</c> §5.11): its name, its statement, the dialect it was built for, and the
/// names of its items. Every reference to it (<c>t</c>, <c>t.As("t2")</c>) shares this object, which is how a statement
/// knows that a reference names one of its CTEs.
/// </summary>
/// <param name="Name">The name.</param>
/// <param name="State">The statement.</param>
/// <param name="Dialect">The dialect the statement was built for.</param>
/// <param name="ColumnNames">The names of the selected items, in order.</param>
internal sealed record NamedQueryDefinition(string Name, SelectState State, DialectRenderer Dialect, ImmutableArray<string> ColumnNames);
