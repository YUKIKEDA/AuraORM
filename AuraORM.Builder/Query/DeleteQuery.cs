using System.Text;

using AuraORM.Builder.Conditions;
using AuraORM.Builder.Dialect;
using AuraORM.Builder.Table;
using AuraORM.Builder.Transaction;

namespace AuraORM.Builder.Query;

/// <summary>
/// DELETEクエリビルダー
/// </summary>
/// <typeparam name="TTable">テーブルの型</typeparam>
public sealed class DeleteQuery<TTable> where TTable : ITable
{
    private readonly TTable _table;
    private readonly DbContext? _dbContext;
    private readonly TransactionContext? _transactionContext;
    private readonly List<WhereCondition> _whereConditions = [];

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="table">テーブル</param>
    /// <param name="dbContext">データベースコンテキスト</param>
    /// <param name="transactionContext">トランザクションコンテキスト</param>
    internal DeleteQuery(TTable table, DbContext? dbContext = null, TransactionContext? transactionContext = null)
    {
        _table = table;
        _dbContext = dbContext;
        _transactionContext = transactionContext;
    }

    /// <summary>
    /// WHERE句を追加します。
    /// </summary>
    /// <param name="condition">条件</param>
    /// <returns>DELETEクエリビルダー</returns>
    public DeleteQuery<TTable> Where(WhereCondition condition)
    {
        _whereConditions.Add(condition);
        return this;
    }

    /// <summary>
    /// SQLクエリをビルドします。
    /// </summary>
    /// <returns>SQLテキストとパラメータのタプル</returns>
    public (string SqlText, List<(string Name, object? Value)> Parameters) Build()
    {
        var sqlBuilder = new StringBuilder();
        var dialect = _dbContext?.Dialect ?? DialectProvider.Default;
        var parameters = new List<(string Name, object? Value)>();

        // DELETE句の構築
        sqlBuilder.Append("DELETE FROM ");
        sqlBuilder.Append(dialect.QuoteIdentifier(_table.TableName));

        // WHERE句の構築
        int parameterIndex = 0;
        BuildWhereClause(sqlBuilder, parameters, dialect, ref parameterIndex);

        sqlBuilder.Append(";");

        return (sqlBuilder.ToString(), parameters);
    }

    /// <summary>
    /// WHERE句を構築します。
    /// </summary>
    /// <param name="sqlBuilder">SQLビルダー</param>
    /// <param name="parameters">パラメータ</param>
    /// <param name="dialect">SQL方言</param>
    /// <param name="parameterIndex">パラメータのインデックス</param>
    private void BuildWhereClause(
        StringBuilder sqlBuilder,
        List<(string Name, object? Value)> parameters,
        ISqlDialect dialect,
        ref int parameterIndex)
    {
        if (_whereConditions.Count == 0)
        {
            return;
        }

        sqlBuilder.Append(" WHERE ");

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
}