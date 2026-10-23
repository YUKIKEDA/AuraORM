using System.Globalization;

namespace AuraORM;

/// <summary>
/// Writes the parts of SQL that differ by dialect. The rules are in <c>docs/design.ja.md</c>; each override names the
/// section it follows.
/// </summary>
internal abstract class DialectRenderer
{
    private const int CachedParameterNames = 64;

    private readonly string?[] _parameterNames = new string?[CachedParameterNames];

    /// <summary>
    /// Gets the name of the database, for messages.
    /// </summary>
    public abstract string Name { get; }

    /// <summary>
    /// Gets the largest number of parameters one statement may have (§12.4).
    /// </summary>
    public abstract int MaxParameters { get; }

    /// <summary>
    /// Gets a value indicating whether parameters are unnamed and bound by position (PostgreSQL, §5.12).
    /// </summary>
    public virtual bool UsesPositionalParameters => false;

    /// <summary>
    /// Gets the function that counts the characters of a string (§4.2).
    /// </summary>
    protected virtual string LengthFunction => "CHAR_LENGTH";

    /// <summary>
    /// Returns the type that a parameter is sent with, or <see langword="null"/> to let the provider infer it (§12.4). A null
    /// <see cref="byte"/> array is sent as binary on every dialect: SqlClient takes an untyped null for <c>nvarchar</c>, which
    /// does not convert to <c>varbinary</c>.
    /// </summary>
    /// <param name="parameter">The parameter.</param>
    /// <returns>The type, or <see langword="null"/>.</returns>
    public virtual System.Data.DbType? ParameterDbType(ParameterNode parameter) =>
        parameter.IsNull && parameter.ValueType == typeof(byte[]) ? System.Data.DbType.Binary : null;

    /// <summary>
    /// Writes the length of a string in characters (§4.2).
    /// </summary>
    /// <param name="context">The rendering context.</param>
    /// <param name="argument">The string.</param>
    public virtual void RenderLength(SqlRenderContext context, SqlNode argument)
    {
        context.Write(LengthFunction);
        context.Write('(');
        argument.Render(context);
        context.Write(')');
    }

    protected abstract char IdentifierOpen { get; }

    protected abstract char IdentifierClose { get; }

    /// <summary>
    /// Returns the renderer of a dialect type.
    /// </summary>
    /// <typeparam name="TDialect">The dialect type.</typeparam>
    /// <returns>The renderer.</returns>
    public static DialectRenderer For<TDialect>()
        where TDialect : IDialect => DialectCache<TDialect>.Renderer;

    /// <summary>
    /// Writes a quoted identifier. The closing quote inside the name is doubled (§5.12).
    /// </summary>
    /// <param name="writer">The writer.</param>
    /// <param name="name">The identifier.</param>
    public void WriteIdentifier(SqlWriter writer, string name)
    {
        writer.Append(IdentifierOpen);
        ReadOnlySpan<char> rest = name;
        int quote;
        while ((quote = rest.IndexOf(IdentifierClose)) >= 0)
        {
            writer.Append(rest[..(quote + 1)]);
            writer.Append(IdentifierClose);
            rest = rest[(quote + 1)..];
        }

        writer.Append(rest);
        writer.Append(IdentifierClose);
    }

    /// <summary>
    /// Returns the placeholder of the parameter at an index (§5.12).
    /// </summary>
    /// <param name="index">The zero-based index of the parameter in the statement.</param>
    /// <returns>The placeholder.</returns>
    public string ParameterName(int index)
    {
        if (index >= CachedParameterNames)
        {
            return FormatParameterName(index);
        }

        // Filling the cache twice from two threads stores equal strings, so no lock is needed.
        return _parameterNames[index] ??= FormatParameterName(index);
    }

    /// <summary>
    /// Returns the type name for <c>CAST</c> (§4.3).
    /// </summary>
    /// <param name="target">The target type.</param>
    /// <returns>The type name.</returns>
    public abstract string CastTypeName(CastTarget target);

    /// <summary>
    /// Writes the concatenation of strings (§4.2). The default is <c>(a || b || …)</c>.
    /// </summary>
    /// <param name="context">The rendering context.</param>
    /// <param name="arguments">The strings.</param>
    public virtual void RenderConcat(SqlRenderContext context, SqlNode[] arguments) => RenderInfix(context, arguments, " || ");

    /// <summary>
    /// Writes the division of integers (§4.2). The default is <c>(a / b)</c>.
    /// </summary>
    /// <param name="context">The rendering context.</param>
    /// <param name="left">The dividend.</param>
    /// <param name="right">The divisor.</param>
    public virtual void RenderIntegerDivide(SqlRenderContext context, SqlNode left, SqlNode right) => RenderInfix(context, [left, right], " / ");

    /// <summary>
    /// Writes <c>IS [NOT] DISTINCT FROM</c> (§4.2). The default is the standard form.
    /// </summary>
    /// <param name="context">The rendering context.</param>
    /// <param name="node">The node.</param>
    public virtual void RenderDistinctFrom(SqlRenderContext context, DistinctFromNode node)
    {
        node.Left.Render(context);
        context.Write(node.Distinct ? " IS DISTINCT FROM " : " IS NOT DISTINCT FROM ");
        node.Right.Render(context);
    }

    /// <summary>
    /// Gets the smallest row limit that the dialect accepts (§5.9, §14.3): 0, or 1 on SQL Server, whose
    /// <c>FETCH NEXT n ROWS</c> needs at least one row.
    /// </summary>
    public virtual int MinimumRowLimit => 0;

    /// <summary>
    /// Writes the row limit and the offset of a SELECT (§5.9). The default is <c>LIMIT n OFFSET m</c>; the builder lets only
    /// the dialects that accept it write an offset without a limit.
    /// </summary>
    /// <param name="context">The rendering context.</param>
    /// <param name="limit">The row limit, or <see langword="null"/>.</param>
    /// <param name="offset">The offset, or <see langword="null"/>.</param>
    public virtual void RenderLimitOffset(SqlRenderContext context, ParameterNode? limit, ParameterNode? offset)
    {
        if (limit is not null)
        {
            context.Write(" LIMIT ");
            limit.Render(context);
        }

        if (offset is not null)
        {
            context.Write(" OFFSET ");
            offset.Render(context);
        }
    }

    /// <summary>
    /// Writes an aggregate (§4.2).
    /// </summary>
    /// <param name="context">The rendering context.</param>
    /// <param name="node">The node.</param>
    public void RenderAggregate(SqlRenderContext context, AggregateNode node)
    {
        switch (node.Function)
        {
            case AggregateFunction.CountAll:
                context.Write(CountFunction);
                context.Write("(*)");
                return;
            case AggregateFunction.Count:
                RenderCall(context, CountFunction, node.Argument!);
                return;
            case AggregateFunction.CountDistinct:
                context.Write(CountFunction);
                context.Write("(DISTINCT ");
                node.Argument!.Render(context);
                context.Write(')');
                return;
            case AggregateFunction.SumInteger:
                RenderSumOfIntegers(context, node.Argument!);
                return;
            case AggregateFunction.Sum:
                RenderCall(context, "SUM", node.Argument!);
                return;
            case AggregateFunction.AverageAsDouble:
                RenderAverageAsDouble(context, node.Argument!);
                return;
            case AggregateFunction.Average:
                RenderCall(context, "AVG", node.Argument!);
                return;
            case AggregateFunction.Min:
                RenderCall(context, "MIN", node.Argument!);
                return;
            default:
                RenderCall(context, "MAX", node.Argument!);
                return;
        }
    }

    /// <summary>
    /// Gets the function that counts rows. The default is <c>COUNT</c>.
    /// </summary>
    protected virtual string CountFunction => "COUNT";

    /// <summary>
    /// Writes the sum of integers, read as <c>long</c>. The default is <c>SUM(a)</c>.
    /// </summary>
    /// <param name="context">The rendering context.</param>
    /// <param name="argument">The summed expression.</param>
    protected virtual void RenderSumOfIntegers(SqlRenderContext context, SqlNode argument) => RenderCall(context, "SUM", argument);

    /// <summary>
    /// Writes the average of numbers, read as <c>double</c>. The default is <c>AVG(a)</c>.
    /// </summary>
    /// <param name="context">The rendering context.</param>
    /// <param name="argument">The averaged expression.</param>
    protected virtual void RenderAverageAsDouble(SqlRenderContext context, SqlNode argument) => RenderCall(context, "AVG", argument);

    protected virtual string FormatParameterName(int index) => "@p" + index.ToString(CultureInfo.InvariantCulture);

    protected static void RenderCall(SqlRenderContext context, string function, SqlNode argument)
    {
        context.Write(function);
        context.Write('(');
        argument.Render(context);
        context.Write(')');
    }

    protected static void RenderCast(SqlRenderContext context, SqlNode value, string type)
    {
        context.Write("CAST(");
        value.Render(context);
        context.Write(" AS ");
        context.Write(type);
        context.Write(')');
    }

    protected static void RenderInfix(SqlRenderContext context, SqlNode[] operands, string separator)
    {
        context.Write('(');
        for (int i = 0; i < operands.Length; i++)
        {
            if (i > 0)
            {
                context.Write(separator);
            }

            operands[i].Render(context);
        }

        context.Write(')');
    }

    private static class DialectCache<TDialect>
        where TDialect : IDialect
    {
        public static readonly DialectRenderer Renderer = Create();

        private static DialectRenderer Create()
        {
            if (typeof(TDialect) == typeof(SqliteDialect))
            {
                return new SqliteRenderer();
            }

            if (typeof(TDialect) == typeof(PostgreSqlDialect))
            {
                return new PostgreSqlRenderer();
            }

            if (typeof(TDialect) == typeof(MySqlDialect))
            {
                return new MySqlRenderer(isMariaDb: false);
            }

            if (typeof(TDialect) == typeof(MariaDbDialect))
            {
                return new MySqlRenderer(isMariaDb: true);
            }

            if (typeof(TDialect) == typeof(SqlServerDialect))
            {
                return new SqlServerRenderer();
            }

            throw ThrowHelper.UnsupportedDialect(typeof(TDialect));
        }
    }
}
