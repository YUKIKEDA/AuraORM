namespace AuraORM;

/// <content>
/// The assignments of <c>UPDATE</c> (<c>docs/design.ja.md</c> §7). Only a <see cref="Column{T}"/> can be assigned: an
/// <c>[AutoIncrement]</c> column (<see cref="ReadOnlyColumn{T}"/>), a column of the nullable side of a join, and an
/// expression do not compile.
/// </content>
public static partial class Sql
{
    private const string ReadOnlyAssignment = "The column is [AutoIncrement], and the database assigns its values; UPDATE cannot assign it.";

    /// <summary>
    /// Assigns a value to a column: <c>"column" = @p0</c>. <c>Set(u.Nickname, null)</c> assigns <c>NULL</c> to a nullable
    /// column.
    /// </summary>
    /// <typeparam name="T">The CLR type of the column.</typeparam>
    /// <param name="column">The column.</param>
    /// <param name="value">The value, bound as a parameter.</param>
    /// <returns>The assignment.</returns>
    public static Assignment Set<T>(Column<T> column, T value)
    {
        ArgumentNullException.ThrowIfNull(column);
        return new Assignment(column.Node, Param(value));
    }

    /// <summary>
    /// Assigns an expression to a column: <c>"column" = expression</c>. The expression is evaluated on the row before the
    /// update, as SQL defines it; MySQL and MariaDB reject an expression that reads a column assigned before it in the same
    /// list (<c>docs/design.ja.md</c> §7).
    /// </summary>
    /// <typeparam name="T">The CLR type of the column.</typeparam>
    /// <typeparam name="TExpr">The type of the expression. It is a type parameter so that a <see langword="null"/> argument
    /// selects <see cref="Set{T}(Column{T}, T)"/>.</typeparam>
    /// <param name="column">The column.</param>
    /// <param name="value">The expression.</param>
    /// <returns>The assignment.</returns>
    public static Assignment Set<T, TExpr>(Column<T> column, TExpr value)
        where TExpr : IExpr<T, PerRow>
    {
        ArgumentNullException.ThrowIfNull(column);
        ArgumentNullException.ThrowIfNull(value);
        return new Assignment(column.Node, value.Node);
    }

    /// <inheritdoc cref="Set{T, TExpr}(Column{T}, TExpr)"/>
    public static Assignment Set<T, TExpr>(Column<T?> column, TExpr value)
        where T : struct
        where TExpr : IExpr<T, PerRow>
    {
        ArgumentNullException.ThrowIfNull(column);
        ArgumentNullException.ThrowIfNull(value);
        return new Assignment(column.Node, value.Node);
    }

    /// <summary>Not accepted: the database assigns the values of an <c>[AutoIncrement]</c> column.</summary>
    /// <typeparam name="T">The CLR type of the column.</typeparam>
    /// <param name="column">The column.</param>
    /// <param name="value">The value.</param>
    /// <returns>Nothing; the call does not compile.</returns>
    [Obsolete(ReadOnlyAssignment, error: true)]
    public static Assignment Set<T>(ReadOnlyColumn<T> column, T value) => throw new InvalidOperationException(ReadOnlyAssignment);

    /// <summary>Not accepted: the database assigns the values of an <c>[AutoIncrement]</c> column.</summary>
    /// <typeparam name="T">The CLR type of the column.</typeparam>
    /// <typeparam name="TExpr">The type of the expression.</typeparam>
    /// <param name="column">The column.</param>
    /// <param name="value">The expression.</param>
    /// <returns>Nothing; the call does not compile.</returns>
    [Obsolete(ReadOnlyAssignment, error: true)]
    public static Assignment Set<T, TExpr>(ReadOnlyColumn<T> column, TExpr value)
        where TExpr : IExpr<T, PerRow> => throw new InvalidOperationException(ReadOnlyAssignment);
}
