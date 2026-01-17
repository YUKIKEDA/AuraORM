using System;

using AuraORM.Builder.Table;

namespace AuraORM.Builder.Query;

/// <summary>
/// 
/// </summary>
public class SelectQuery
{
    private readonly DbContext _context;

    private readonly Column[] _columns;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="context"></param>
    /// <param name="columns"></param>
    internal SelectQuery(DbContext context, params Column[] columns)
    {
        _context = context;
        _columns = columns;
    }

    public SelectQuery From(Type tableType)
    {
        throw new NotImplementedException();
    }
    
    public Task<T[]> ToListAsync<T>()
    {
        throw new NotImplementedException();
    }
}
