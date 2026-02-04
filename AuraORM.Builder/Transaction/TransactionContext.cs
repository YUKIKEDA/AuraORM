using System.Data.Common;

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
    /// 
    /// </summary>
    /// <param name="transaction"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public TransactionContext(DbTransaction transaction)
    {
        Transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
    }
}
