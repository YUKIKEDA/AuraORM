using System.Collections.Immutable;
using System.Runtime.InteropServices;

namespace AuraORM;

/// <summary>
/// The SQL text and the parameters of a statement, exactly as AuraORM sends them to the database.
/// </summary>
public sealed class SqlStatement
{
    internal SqlStatement(string text, SqlStatementParameter[] parameters)
    {
        Text = text;
        Parameters = ImmutableCollectionsMarshal.AsImmutableArray(parameters);
    }

    /// <summary>
    /// Gets the SQL text.
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// Gets the parameters in the order their placeholders appear in <see cref="Text"/>. The array cannot be changed, so the
    /// statement that runs is the statement that was rendered.
    /// </summary>
    public ImmutableArray<SqlStatementParameter> Parameters { get; }

    /// <summary>
    /// Returns the SQL text.
    /// </summary>
    /// <returns>The SQL text.</returns>
    public override string ToString() => Text;
}
