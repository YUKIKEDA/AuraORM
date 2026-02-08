using System.Data.Common;

using AuraORM.Builder.Query;
using AuraORM.Builder.Table;

namespace AuraORM.Builder.Transaction;

/// <summary>
/// トランザクションコンテキストを表すクラス
/// </summary>
public class TransactionContext
{
    /// <summary>
    /// データベーストランザクション
    /// </summary>
    public DbTransaction Transaction { get; }

    /// <summary>
    /// トランザクション用のDbContext
    /// </summary>
    private readonly DbContext _dbContext;

    /// <summary>
    /// トランザクションコンテキストを生成します。
    /// </summary>
    /// <param name="transaction">データベーストランザクション</param>
    /// <param name="parentContext">親のDbContext</param>
    /// <exception cref="ArgumentNullException"></exception>
    internal TransactionContext(DbTransaction transaction, DbContext parentContext)
    {
        Transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
        _dbContext = new DbContext(parentContext.Connection, parentContext.Schema, parentContext.Dialect, this);
    }

    /// <summary>
    /// INSERTクエリを生成します。
    /// </summary>
    /// <typeparam name="TTable">テーブル</typeparam>
    /// <param name="table">テーブル</param>
    /// <returns>INSERTクエリビルダー</returns>
    public InsertQuery<TTable> Insert<TTable>(TTable table) where TTable : ITable
    {
        return new InsertQuery<TTable>(table, _dbContext);
    }

    /// <summary>
    /// UPDATEクエリを生成します。
    /// </summary>
    /// <typeparam name="TTable">テーブル</typeparam>
    /// <param name="table">テーブル</param>
    /// <returns>UPDATEクエリビルダー</returns>
    public UpdateQuery<TTable> Update<TTable>(TTable table) where TTable : ITable
    {
        return new UpdateQuery<TTable>(table, _dbContext);
    }

    /// <summary>
    /// DELETEクエリを生成します。
    /// </summary>
    /// <typeparam name="TTable">テーブル</typeparam>
    /// <param name="table">テーブル</param>
    /// <returns>DELETEクエリビルダー</returns>
    public DeleteQuery<TTable> Delete<TTable>(TTable table) where TTable : ITable
    {
        return new DeleteQuery<TTable>(table, _dbContext);
    }
}
