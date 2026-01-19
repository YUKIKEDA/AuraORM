using System.Text;

using AuraORM.Builder.Conditions;
using AuraORM.Builder.Dialect;
using AuraORM.Builder.Table;

namespace AuraORM.Builder.Query;

/// <summary>
/// SELECT JOINクエリを表すクラス
/// </summary>
/// <typeparam name="TTable">テーブルの型</typeparam>
/// <typeparam name="TJoinTable">JOIN先のテーブルの型</typeparam>
public sealed class SelectJoinQuery<TTable, TJoinTable> 
    where TTable : ITable
    where TJoinTable : ITable
{
    private readonly TTable _table;
    private readonly TJoinTable _joinTable;
    private readonly JoinType _joinType;
    private readonly JoinCondition _joinCondition;
    private readonly DbContext? _dbContext;
    private readonly Column[]? _columns;
    private readonly List<WhereCondition> _whereConditions = [];
    private readonly List<OrderByCondition> _orderByConditions = [];
    private int? _limit;
    private int? _offset;

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="table">テーブル</param>
    /// <param name="joinTable">JOIN先のテーブル</param>
    /// <param name="joinType">JOINの種類</param>
    /// <param name="joinCondition">JOIN条件</param>
    /// <param name="dbContext">データベースコンテキスト</param>
    /// <param name="columns">カラム</param>
    internal SelectJoinQuery(
        TTable table,
        TJoinTable joinTable,
        JoinType joinType,
        JoinCondition joinCondition,
        DbContext? dbContext = null,
        Column[]? columns = null)
    {
        _table = table;
        _joinTable = joinTable;
        _joinType = joinType;
        _joinCondition = joinCondition;
        _dbContext = dbContext;
        _columns = columns;
    }

    /// <summary>
    /// WHERE句を追加します。
    /// </summary>
    /// <param name="condition">WHERE句の条件</param>
    /// <returns>SELECT JOINクエリビルダー</returns>
    public SelectJoinQuery<TTable, TJoinTable> Where(WhereCondition condition)
    {
        _whereConditions.Add(condition);
        return this;
    }

    /// <summary>
    /// ORDER BY句を追加します。
    /// </summary>
    /// <param name="condition">ORDER BY句の条件</param>
    /// <returns>SELECT JOINクエリビルダー</returns>
    public SelectJoinQuery<TTable, TJoinTable> OrderBy(OrderByCondition condition)
    {
        _orderByConditions.Add(condition);
        return this;
    }

    /// <summary>
    /// LIMIT句を追加します。
    /// </summary>
    /// <param name="limit">LIMIT句の条件</param>
    /// <returns>SELECT JOINクエリビルダー</returns>
    public SelectJoinQuery<TTable, TJoinTable> Limit(int limit)
    {
        _limit = limit;
        return this;
    }

    /// <summary>
    /// OFFSET句を追加します。
    /// </summary>
    /// <param name="offset">OFFSET句の条件</param>
    /// <returns>SELECT JOINクエリビルダー</returns>
    public SelectJoinQuery<TTable, TJoinTable> Offset(int offset)
    {
        _offset = offset;
        return this;
    }

    /// <summary>
    /// 構築されたSELECT JOINクエリをSQLテキストとパラメータのリストに変換します。
    /// </summary>
    /// <returns>SQLテキストとパラメータのリストを含むタプル。</returns>
    public (string SqlText, List<(string Name, object? Value)> Parameters) Build()
    {
        var sqlBuilder = new StringBuilder();
        var dialect = _dbContext?.Dialect ?? DialectProvider.Default;

        // SELECT句の構築
        BuildSelectClause(sqlBuilder, dialect);

        // FROM句とJOIN句の構築
        BuildFromJoinClause(sqlBuilder, dialect);

        // WHERE句の構築
        var parameters = new List<(string Name, object? Value)>();
        BuildWhereClause(sqlBuilder, parameters, dialect);

        // ORDER BY句の構築
        BuildOrderByClause(sqlBuilder, dialect);

        // LIMIT/OFFSET句の構築
        var limitOffsetClause = dialect.BuildLimitOffset(_limit, _offset);
        if (!string.IsNullOrEmpty(limitOffsetClause))
        {
            sqlBuilder.Append(limitOffsetClause);
        }

        sqlBuilder.Append(";");

        return (sqlBuilder.ToString(), parameters);
    }

    /// <summary>
    /// SELECT句を構築します。
    /// </summary>
    /// <param name="sqlBuilder">SQLビルダー</param>
    /// <param name="dialect">SQL方言</param>
    private void BuildSelectClause(StringBuilder sqlBuilder, ISqlDialect dialect)
    {
        sqlBuilder.Append("SELECT ");
        
        if (_columns != null && _columns.Length > 0)
        {
            for (int i = 0; i < _columns.Length; i++)
            {
                if (i > 0)
                {
                    sqlBuilder.Append(", ");
                }
                sqlBuilder.Append(dialect.QuoteIdentifier(_columns[i].Name));
            }
        }
        else
        {
            sqlBuilder.Append("*");
        }
    }

    /// <summary>
    /// FROM句とJOIN句を構築します。
    /// </summary>
    /// <param name="sqlBuilder">SQLビルダー</param>
    /// <param name="dialect">SQL方言</param>
    private void BuildFromJoinClause(StringBuilder sqlBuilder, ISqlDialect dialect)
    {
        sqlBuilder.Append(" FROM ");
        sqlBuilder.Append(dialect.QuoteIdentifier(_table.TableName));

        // JOINタイプに応じた文字列を取得
        var joinTypeString = _joinType switch
        {
            JoinType.Inner => "INNER JOIN",
            JoinType.Left => "LEFT JOIN",
            JoinType.Right => "RIGHT JOIN",
            JoinType.Full => "FULL JOIN",
            _ => "INNER JOIN"
        };

        sqlBuilder.Append(" ");
        sqlBuilder.Append(joinTypeString);
        sqlBuilder.Append(" ");
        sqlBuilder.Append(dialect.QuoteIdentifier(_joinTable.TableName));

        // ON句の構築
        sqlBuilder.Append(" ON ");
        sqlBuilder.Append(dialect.QuoteIdentifier(_table.TableName));
        sqlBuilder.Append(".");
        sqlBuilder.Append(dialect.QuoteIdentifier(_joinCondition.LeftColumnName));
        sqlBuilder.Append(" = ");
        sqlBuilder.Append(dialect.QuoteIdentifier(_joinTable.TableName));
        sqlBuilder.Append(".");
        sqlBuilder.Append(dialect.QuoteIdentifier(_joinCondition.RightColumnName));
    }

    /// <summary>
    /// WHERE句を構築します。
    /// </summary>
    /// <param name="sqlBuilder">SQLビルダー</param>
    /// <param name="parameters">パラメータ</param>
    /// <param name="dialect">SQL方言</param>
    private void BuildWhereClause(
        StringBuilder sqlBuilder,
        List<(string Name, object? Value)> parameters,
        ISqlDialect dialect)
    {
        if (_whereConditions.Count == 0)
        {
            return;
        }

        sqlBuilder.Append(" WHERE ");

        int parameterIndex = 0;
        for (int i = 0; i < _whereConditions.Count; i++)
        {
            if (i > 0)
            {
                sqlBuilder.Append(" AND ");
            }
            BuildWhereCondition(sqlBuilder, _whereConditions[i], parameters, dialect, ref parameterIndex);
        }
    }

    /// <summary>
    /// WHERE句の条件を構築します。
    /// </summary>
    /// <param name="sqlBuilder">SQLビルダー</param>
    /// <param name="condition">WHERE句の条件</param>
    /// <param name="parameters">パラメータのリスト</param>
    /// <param name="dialect">SQL方言</param>
    /// <param name="parameterIndex">パラメータのインデックス</param>
    private void BuildWhereCondition(
        StringBuilder sqlBuilder,
        WhereCondition condition,
        List<(string Name, object? Value)> parameters,
        ISqlDialect dialect,
        ref int parameterIndex)
    {
        if (condition is WhereCompositeCondition composite)
        {
            sqlBuilder.Append("(");
            BuildWhereCondition(sqlBuilder, composite.Left, parameters, dialect, ref parameterIndex);
            sqlBuilder.Append(" ");
            sqlBuilder.Append(composite.LogicalOperator);
            sqlBuilder.Append(" ");
            BuildWhereCondition(sqlBuilder, composite.Right, parameters, dialect, ref parameterIndex);
            sqlBuilder.Append(")");
        }
        else if (condition is WhereInCondition inCondition)
        {
            sqlBuilder.Append(dialect.QuoteIdentifier(inCondition.ColumnName));
            sqlBuilder.Append(" IN (");

            for (int i = 0; i < inCondition.Values.Length; i++)
            {
                if (i > 0)
                {
                    sqlBuilder.Append(", ");
                }
                var parameterName = dialect.GetParameterPlaceholder(parameterIndex);
                sqlBuilder.Append(parameterName);
                parameters.Add((parameterName, inCondition.Values[i]));
                parameterIndex++;
            }

            sqlBuilder.Append(")");
        }
        else if (condition is WhereNotInCondition notInCondition)
        {
            sqlBuilder.Append(dialect.QuoteIdentifier(notInCondition.ColumnName));
            sqlBuilder.Append(" NOT IN (");

            for (int i = 0; i < notInCondition.Values.Length; i++)
            {
                if (i > 0)
                {
                    sqlBuilder.Append(", ");
                }
                var parameterName = dialect.GetParameterPlaceholder(parameterIndex);
                sqlBuilder.Append(parameterName);
                parameters.Add((parameterName, notInCondition.Values[i]));
                parameterIndex++;
            }

            sqlBuilder.Append(")");
        }
        else if (condition is WhereIsNullCondition)
        {
            sqlBuilder.Append(dialect.QuoteIdentifier(condition.ColumnName));
            sqlBuilder.Append(" IS NULL");
        }
        else if (condition is WhereIsNotNullCondition)
        {
            sqlBuilder.Append(dialect.QuoteIdentifier(condition.ColumnName));
            sqlBuilder.Append(" IS NOT NULL");
        }
        else if (condition is WhereBetweenCondition betweenCondition)
        {
            var minParameterName = dialect.GetParameterPlaceholder(parameterIndex);
            var maxParameterName = dialect.GetParameterPlaceholder(parameterIndex + 1);

            sqlBuilder.Append(dialect.QuoteIdentifier(betweenCondition.ColumnName));
            sqlBuilder.Append(" BETWEEN ");
            sqlBuilder.Append(minParameterName);
            sqlBuilder.Append(" AND ");
            sqlBuilder.Append(maxParameterName);

            parameters.Add((minParameterName, betweenCondition.Min));
            parameters.Add((maxParameterName, betweenCondition.Max));
            parameterIndex += 2;
        }
        else
        {
            var parameterName = dialect.GetParameterPlaceholder(parameterIndex);

            sqlBuilder.Append(dialect.QuoteIdentifier(condition.ColumnName));
            sqlBuilder.Append(" ");
            sqlBuilder.Append(condition.Operator);
            sqlBuilder.Append(" ");
            sqlBuilder.Append(parameterName);

            parameters.Add((parameterName, condition.Value));
            parameterIndex++;
        }
    }

    /// <summary>
    /// ORDER BY句を構築します。
    /// </summary>
    /// <param name="sqlBuilder">SQLビルダー</param>
    /// <param name="dialect">SQL方言</param>
    private void BuildOrderByClause(StringBuilder sqlBuilder, ISqlDialect dialect)
    {
        if (_orderByConditions.Count == 0)
        {
            return;
        }

        sqlBuilder.Append(" ORDER BY ");

        for (int i = 0; i < _orderByConditions.Count; i++)
        {
            if (i > 0)
            {
                sqlBuilder.Append(", ");
            }
            var condition = _orderByConditions[i];
            sqlBuilder.Append(dialect.QuoteIdentifier(condition.ColumnName));
            sqlBuilder.Append(condition.Descending ? " DESC" : " ASC");
        }
    }
}
