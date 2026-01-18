using System.Collections;
using AuraORM.Builder.Table;

namespace AuraORM.Builder.Query;

/// <summary>
/// UPDATE句の値を表すクラス
/// </summary>
public class UpdateQueryValues : IEnumerable<ColumnValuePair>
{
    private readonly List<ColumnValuePair> _values = [];

    /// <summary>
    /// 値を追加します。
    /// </summary>
    /// <typeparam name="T">値の型</typeparam>
    /// <param name="pair">カラムと値のペア</param>
    public void Add<T>((Column<T> Column, T Value) pair)
    {
        _values.Add((pair.Column, pair.Value));
    }

    /// <summary>
    /// 値の列挙子を取得します。
    /// </summary>
    /// <returns>値の列挙子</returns>
    public IEnumerator<ColumnValuePair> GetEnumerator() => _values.GetEnumerator();

    /// <summary>
    /// 値の列挙子を取得します。
    /// </summary>
    /// <returns>値の列挙子</returns>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
