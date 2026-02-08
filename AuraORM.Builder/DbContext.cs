using System.Data;
using System.Data.Common;

using AuraORM.Builder.Dialect;
using AuraORM.Builder.Query;
using AuraORM.Builder.Table;
using AuraORM.Builder.Transaction;

namespace AuraORM.Builder;

/// <summary>
/// データベースコンテキストを表すクラス
/// </summary>
public class DbContext
{
    /// <summary>
    /// データベース接続
    /// </summary>
    public DbConnection Connection { get; }

    /// <summary>
    /// スキーマ
    /// </summary>
    public Schema Schema { get; }

    /// <summary>
    /// SQL方言
    /// </summary>
    public ISqlDialect Dialect { get; }

    /// <summary>
    /// トランザクションコンテキスト
    /// </summary>
    public TransactionContext? TransactionContext { get; }

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="connection">データベース接続</param>
    /// <param name="schema">スキーマ</param>
    /// <param name="sqlDialect">SQL方言</param>
    public DbContext(DbConnection connection, Schema schema, ISqlDialect? sqlDialect = null)
    {
        Connection = connection ?? throw new ArgumentNullException(nameof(connection));
        Schema = schema ?? throw new ArgumentNullException(nameof(schema));
        Dialect = sqlDialect ?? DialectProvider.Default;
    }

    /// <summary>
    /// トランザクションコンテキスト付きの内部コンストラクタ
    /// </summary>
    /// <param name="connection">データベース接続</param>
    /// <param name="schema">スキーマ</param>
    /// <param name="dialect">SQL方言</param>
    /// <param name="transactionContext">トランザクションコンテキスト</param>
    internal DbContext(DbConnection connection, Schema schema, ISqlDialect dialect, TransactionContext transactionContext)
    {
        Connection = connection ?? throw new ArgumentNullException(nameof(connection));
        Schema = schema ?? throw new ArgumentNullException(nameof(schema));
        Dialect = dialect ?? throw new ArgumentNullException(nameof(dialect));
        TransactionContext = transactionContext;
    }

    /// <summary>
    /// SELECTクエリを生成します。
    /// </summary>
    /// <returns>SELECTクエリビルダー</returns>
    public SelectQuery Select()
    {
        return new SelectQuery(this);
    }

    /// <summary>
    /// SELECT COLUMNSクエリを生成します。
    /// </summary>
    /// <param name="columns">カラム</param>
    /// <returns>SELECT COLUMNSクエリビルダー</returns>
    public SelectColumnsQuery Select(params Column[] columns)
    {
        return new SelectColumnsQuery(this, columns);
    }

    /// <summary>
    /// INSERTクエリを生成します。
    /// </summary>
    /// <typeparam name="TTable">テーブル</typeparam>
    /// <param name="table">テーブル</param>
    /// <returns>INSERTクエリビルダー</returns>
    public InsertQuery<TTable> Insert<TTable>(TTable table) where TTable : ITable
    {
        return new InsertQuery<TTable>(table, this);
    }

    /// <summary>
    /// UPDATEクエリを生成します。
    /// </summary>
    /// <typeparam name="TTable">テーブル</typeparam>
    /// <param name="table">テーブル</param>
    /// <returns>UPDATEクエリビルダー</returns>
    public UpdateQuery<TTable> Update<TTable>(TTable table) where TTable : ITable
    {
        return new UpdateQuery<TTable>(table, this);
    }

    /// <summary>
    /// DELETEクエリを生成します。
    /// </summary>
    /// <typeparam name="TTable">テーブル</typeparam>
    /// <param name="table">テーブル</param>
    /// <returns>DELETEクエリビルダー</returns>
    public DeleteQuery<TTable> Delete<TTable>(TTable table) where TTable : ITable
    {
        return new DeleteQuery<TTable>(table, this);
    }

    /// <summary>
    /// RAW SQLクエリを生成します。
    /// </summary>
    /// <param name="rawSqlText">SQLテキスト</param>
    /// <param name="parameters">パラメータ</param>
    /// <returns>RAW SQLクエリビルダー</returns>
    public RawSqlQuery RawSql(string rawSqlText, List<(string Name, object? Value)>? parameters = null)
    {
        return new RawSqlQuery(rawSqlText, parameters, this);
    }

    /// <summary>
    /// トランザクションを開始し、非同期処理を実行します。
    /// 正常終了時はコミット、例外発生時はロールバックされます。
    /// </summary>
    /// <param name="func">トランザクション内で実行する処理</param>
    /// <param name="isolationLevel">分離レベル</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    public async Task TransactionAsync(
        Func<TransactionContext, Task> func,
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
        CancellationToken cancellationToken = default)
    {
        if (Connection.State != ConnectionState.Open)
        {
            await Connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        }

        using var transaction = Connection.BeginTransaction(isolationLevel);

        try
        {
            var txContext = new TransactionContext(transaction, this);
            
            await func(txContext).ConfigureAwait(false);

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    /// <summary>
    /// トランザクションを開始し、戻り値を返す非同期処理を実行します。
    /// 正常終了時はコミット、例外発生時はロールバックされます。
    /// </summary>
    /// <typeparam name="T">戻り値の型</typeparam>
    /// <param name="func">トランザクション内で実行する処理</param>
    /// <param name="isolationLevel">分離レベル</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>処理結果</returns>
    public async Task<T> TransactionAsync<T>(
        Func<TransactionContext, Task<T>> func,
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
        CancellationToken cancellationToken = default)
    {
        if (Connection.State != ConnectionState.Open)
        {
            await Connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        }

        using var transaction = Connection.BeginTransaction(isolationLevel);

        try
        {
            var txContext = new TransactionContext(transaction, this);
            
            var result = await func(txContext).ConfigureAwait(false);

            transaction.Commit();
            
            return result;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }
}
