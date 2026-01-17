using System.Text;

using AuraORM.Builder.Conditions;
using AuraORM.Builder.Dialect;
using AuraORM.Builder.Table;

namespace AuraORM.Builder.Query;

/// <summary>
/// SELECTクエリを表すクラス
/// </summary>
public class SelectQuery
{
    private readonly DbContext? _context;
    private readonly Column[] _columns;

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="context">データベースコンテキスト</param>
    /// <param name="columns">カラム</param>
    internal SelectQuery(DbContext? context = null, params Column[] columns)
    {
        _context = context;
        _columns = columns;
    }

    /// <summary>
    /// データベースコンテキストを取得します。
    /// </summary>
    /// <returns>データベースコンテキスト。関連付けられていない場合は null。</returns>
    public DbContext? GetContext() => _context;

    /// <summary>
    /// カラムを取得します。
    /// </summary>
    /// <returns>カラムの配列</returns>
    public Column[] GetColumns() => _columns;

    /// <summary>
    /// テーブルを指定してSELECTクエリを生成します。
    /// </summary>
    /// <typeparam name="TEntity">エンティティの型</typeparam>
    /// <typeparam name="TTable">テーブルの型</typeparam>
    /// <param name="table">テーブル</param>
    /// <returns>SELECTクエリビルダー</returns>
    public SelectQuery<TEntity, TTable> From<TEntity, TTable>(TTable table)
        where TTable : ITable
    {
        return new SelectQuery<TEntity, TTable>(table, _context, _columns);
    }
}

/// <summary>
/// SELECTクエリビルダーを表すクラス
/// </summary>
/// <typeparam name="TEntity">エンティティの型</typeparam>
/// <typeparam name="TTable">テーブルの型</typeparam>
public sealed class SelectQuery<TEntity, TTable>where TTable : ITable
{
    private readonly TTable _table;
    private readonly DbContext? _dbContext;
    private readonly Column[]? _columns;
    private readonly List<WhereCondition> _conditions = [];
    private readonly List<OrderByCondition> _orderByConditions = [];

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="table">テーブル</param>
    /// <param name="dbContext">データベースコンテキスト</param>
    /// <param name="columns">カラム</param>
    public SelectQuery(TTable table, DbContext? dbContext = null, Column[]? columns = null)
    {
        _table = table;
        _dbContext = dbContext;
        _columns = columns;
    }

    /// <summary>
    /// このビルダーに関連付けられているデータベースコンテキストを取得します。
    /// </summary>
    /// <returns>データベースコンテキスト。関連付けられていない場合は null。</returns>
    public DbContext? GetDbContext() => _dbContext;

    /// <summary>
    /// WHERE句を追加します。
    /// </summary>
    /// <param name="condition">WHERE句の条件</param>
    /// <returns>SELECTクエリビルダー</returns>
    public SelectQuery<TEntity, TTable> Where(WhereCondition condition)
    {
        _conditions.Add(condition);
        return this;
    }

    /// <summary>
    /// ORDER BY句を追加します。
    /// </summary>
    /// <param name="orderBy">ORDER BY句</param>
    /// <returns>SELECTクエリビルダー</returns>
    public SelectQuery<TEntity, TTable> OrderBy(OrderByCondition orderBy)
    {
        _orderByConditions.Add(orderBy);
        return this;
    }

    /// <summary>
    /// 構築されたSELECTクエリをSQLテキストとパラメータのリストに変換します。
    /// </summary>
    /// <returns>SQLテキストとパラメータのリストを含むタプル。</returns>
    public (string SqlText, List<(string Name, object? Value)> Parameters) Build()
    {
        var sqlBuilder = new StringBuilder();
        var dialect = _dbContext?.Dialect ?? DialectProvider.Default;

        // SELECT句の構築
        BuildSelectClause(sqlBuilder, dialect);

        // FROM句の構築
        BuildFromClause(sqlBuilder, dialect);

        // WHERE句の構築
        var parameters = new List<(string Name, object? Value)>();
        BuildWhereClause(sqlBuilder, parameters, dialect);

        // ORDER BY句の構築
        BuildOrderByClause(sqlBuilder, dialect);

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
    /// FROM句を構築します。
    /// </summary>
    /// <param name="sqlBuilder">SQLビルダー</param>
    /// <param name="dialect">SQL方言</param>
    private void BuildFromClause(StringBuilder sqlBuilder, ISqlDialect dialect)
    {
        sqlBuilder.Append(" FROM ");
        sqlBuilder.Append(dialect.QuoteIdentifier(_table.TableName));
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
        if (_conditions.Count == 0)
        {
            return;
        }

        sqlBuilder.Append(" WHERE ");

        int parameterIndex = 0;
        for (int i = 0; i < _conditions.Count; i++)
        {
            if (i > 0)
            {
                sqlBuilder.Append(" AND ");
            }
            BuildWhereCondition(sqlBuilder, _conditions[i], parameters, dialect, ref parameterIndex);
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