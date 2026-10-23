namespace AuraORM;

/// <summary>
/// The SQL text and the parameters of a statement, exactly as AuraORM sends them to the database.
/// </summary>
public sealed class SqlStatement
{
    internal SqlStatement(string text, SqlStatementParameter[] parameters)
    {
        Text = text;
        Parameters = parameters;
    }

    /// <summary>
    /// Gets the SQL text.
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// Gets the parameters in the order their placeholders appear in <see cref="Text"/>.
    /// </summary>
    public IReadOnlyList<SqlStatementParameter> Parameters { get; }

    /// <summary>
    /// Returns the SQL text.
    /// </summary>
    /// <returns>The SQL text.</returns>
    public override string ToString() => Text;
}
