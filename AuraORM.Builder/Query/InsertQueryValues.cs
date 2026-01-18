using System.Collections;

namespace AuraORM.Builder.Query;

/// <summary>
/// INSERT句の値を表すクラス
/// </summary>
public partial class InsertQueryValues : IEnumerable<ColumnValuePair[]>
{
    private readonly List<ColumnValuePair[]> _values = [];
    private int? _columnCount = null;

    /// <summary>
    /// 値を追加します。
    /// </summary>
    /// <param name="cells">値</param>
    /// <exception cref="ArgumentException">列数が一致しない場合</exception>
    private void AddRow(params ColumnValuePair[] cells)
    {
        // 1行目の列数を基準として記録する
        _columnCount ??= cells.Length;

        // 2行目以降、列数が一致しない場合は例外を投げる
        if (cells.Length != _columnCount)
        {
            throw new ArgumentException($"列数が一致しません。期待値: {_columnCount}, 実際: {cells.Length}");
        }

        _values.Add(cells);
    }

    /// <summary>
    /// 値の列挙子を取得します。
    /// </summary>
    /// <returns>値の列挙子</returns>
    public IEnumerator<ColumnValuePair[]> GetEnumerator() => _values.GetEnumerator();

    /// <summary>
    /// 値の列挙子を取得します。
    /// </summary>
    /// <returns>値の列挙子</returns>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
