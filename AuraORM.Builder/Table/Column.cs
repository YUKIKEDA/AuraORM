using System;

namespace AuraORM.Builder.Table;

/// <summary>
/// カラムを表す基底クラス
/// </summary>
public abstract class Column
{
    /// <summary>
    /// カラム名
    /// </summary>
    public string Name { get; protected set; }

    /// <summary>
    /// カラムの序数（0ベース）
    /// </summary>
    public int Ordinal { get; protected set; }

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="name">カラム名</param>
    /// <param name="ordinal">カラムの序数</param>
    protected Column(string name, int ordinal)
    {
        Name = name;
        Ordinal = ordinal;
    }
}

/// <summary>
/// カラムを表すクラス
/// </summary>
/// <typeparam name="T">カラムの型</typeparam>
public class Column<T> : Column
{
    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="name">カラム名</param>
    /// <param name="ordinal">カラムの序数</param>
    public Column(string name, int ordinal)
        : base(name, ordinal)
    {
    }
}
