using System.Data.Common;

using AuraORM.Builder.Query;
using AuraORM.Builder.Table;

namespace AuraORM.Builder;

/// <summary>
/// 
/// </summary>
public class DbContext
{
    /// <summary>
    /// 
    /// </summary>
    public DbConnection Connection { get; }

    /// <summary>
    /// 
    /// </summary>
    public Schema Schema { get; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="connection"></param>
    /// <param name="schema"></param>
    public DbContext(DbConnection connection, Schema schema)
    {
        Connection = connection ?? throw new ArgumentNullException(nameof(connection));
        Schema = schema ?? throw new ArgumentNullException(nameof(schema));
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="columns"></param>
    /// <returns></returns>
    public SelectQuery Select(params Column[] columns)
    {
        return new SelectQuery(this, columns);
    }

}
