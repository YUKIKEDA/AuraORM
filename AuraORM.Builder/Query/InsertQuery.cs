using System.Text;

using AuraORM.Builder.Dialect;
using AuraORM.Builder.Table;
using AuraORM.Builder.Transaction;

namespace AuraORM.Builder.Query;

/// <summary>
/// INSERTクエリビルダー
/// </summary>
/// <typeparam name="TTable">テーブル</typeparam>
public sealed class InsertQuery<TTable> where TTable : ITable
{
    private readonly TTable _table;
    private readonly ISqlDialect? _dialect;
    private readonly DbContext? _dbContext;
    private readonly TransactionContext? _transactionContext;

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="table">テーブル</param>
    /// <param name="dbContext">データベースコンテキスト</param>
    /// <param name="dialect">SQL方言</param>
    /// <param name="transactionContext">トランザクションコンテキスト</param>
    internal InsertQuery(TTable table, DbContext? dbContext = null, ISqlDialect? dialect = null, TransactionContext? transactionContext = null)
    {
        _table = table;
        _dbContext = dbContext;
        _dialect = dialect;
        _transactionContext = transactionContext;
    }


    /// <summary>
    /// テーブルメタデータを取得
    /// </summary>
    public TTable GetTable() => _table;

    /// <summary>
    /// Dialectを取得
    /// </summary>
    public ISqlDialect? GetDialect() => _dialect;

    /// <summary>
    /// DbContextを取得
    /// </summary>
    public DbContext? GetDbContext() => _dbContext;

    /// <summary>
    /// TransactionContextを取得
    /// </summary>
    public TransactionContext? GetTransactionContext() => _transactionContext;

    /// <summary>
    /// エンティティを指定してINSERTクエリを生成します。
    /// </summary>
    /// <typeparam name="TEntity">エンティティの型</typeparam>
    /// <returns>INSERTクエリビルダー</returns>
    public InsertQuery<TEntity, TTable> For<TEntity>()
    {
        return new InsertQuery<TEntity, TTable>(_table, _dbContext, _dialect, _transactionContext);
    }
}

/// <summary>
/// INSERTクエリビルダー
/// </summary>
/// <typeparam name="TEntity">エンティティの型</typeparam>
/// <typeparam name="TTable">テーブルの型</typeparam>
public sealed class InsertQuery<TEntity, TTable> where TTable : ITable
{
    private readonly TTable _table;
    private readonly DbContext? _dbContext;
    private readonly TransactionContext? _transactionContext;

    private InsertQueryValues _values = [];

    /// <summary>
    /// 使用しているDialectを取得
    /// </summary>
    public ISqlDialect Dialect { get; }

    /// <summary>
    /// DbContextを取得
    /// </summary>
    public DbContext? GetDbContext() => _dbContext;

    /// <summary>
    /// TransactionContextを取得
    /// </summary>
    public TransactionContext? GetTransactionContext() => _transactionContext;

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="table">テーブル</param>
    /// <param name="dbContext">データベースコンテキスト</param>
    /// <param name="dialect">SQL方言</param>
    /// <param name="transactionContext">トランザクションコンテキスト</param>
    public InsertQuery(TTable table, DbContext? dbContext, ISqlDialect? dialect, TransactionContext? transactionContext)
    {
        _table = table;
        _dbContext = dbContext;
        Dialect = dialect ?? DialectProvider.Default;
        _transactionContext = transactionContext;
    }

    /// <summary>
    /// 値を指定してINSERTクエリを生成します。
    /// </summary>
    /// <param name="values">値</param>
    /// <returns>INSERTクエリビルダー</returns>
    public InsertQuery<TEntity, TTable> Values(InsertQueryValues values)
    {
        _values = values;
        return this;
    }

    /// <summary>
    /// SQLクエリをビルドします。
    /// </summary>
    /// <returns>SQLテキストとパラメータのタプル</returns>
    public (string SqlText, List<(string Name, object? Value)> Parameters) Build()
    {
        var sqlBuilder = new StringBuilder();
        var parameters = new List<(string Name, object? Value)>();

        // INSERT INTO句の構築
        sqlBuilder.Append("INSERT INTO ");
        sqlBuilder.Append(Dialect.QuoteIdentifier(_table.TableName));

        // 値が空の場合は特殊な形式を返す
        if (_values.Count() == 0)
        {
            sqlBuilder.Append(" () VALUES ();");
            return (sqlBuilder.ToString(), parameters);
        }

        // 最初の行からカラム名を取得
        var firstRow = _values.First();
        var columnNames = firstRow.Select(pair => pair.Column.Name).ToArray();

        // カラム名リストの構築
        sqlBuilder.Append(" (");
        for (int i = 0; i < columnNames.Length; i++)
        {
            if (i > 0)
            {
                sqlBuilder.Append(", ");
            }
            sqlBuilder.Append(Dialect.QuoteIdentifier(columnNames[i]));
        }
        sqlBuilder.Append(")");

        // VALUES句の構築
        sqlBuilder.Append(" VALUES ");
        int parameterIndex = 0;

        foreach (var row in _values)
        {
            if (parameterIndex > 0)
            {
                sqlBuilder.Append(", ");
            }

            sqlBuilder.Append("(");
            for (int i = 0; i < row.Length; i++)
            {
                if (i > 0)
                {
                    sqlBuilder.Append(", ");
                }

                var parameterName = Dialect.GetParameterPlaceholder(parameterIndex);
                sqlBuilder.Append(parameterName);
                parameters.Add((parameterName, row[i].Value));
                parameterIndex++;
            }
            sqlBuilder.Append(")");
        }

        sqlBuilder.Append(";");

        return (sqlBuilder.ToString(), parameters);
    }
}