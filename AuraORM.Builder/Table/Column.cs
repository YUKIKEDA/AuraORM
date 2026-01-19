using AuraORM.Builder.Conditions;

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
    protected Column(string name, int ordinal = -1)
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

    /// <summary>
    /// 等号演算子をオーバーロードします。
    /// </summary>
    /// <param name="column">カラム</param>
    /// <param name="value">値</param>
    /// <returns>WHERE句の条件</returns>
    public static WhereCondition operator ==(Column<T> column, T? value)
    {
        return new WhereCondition(column.Name, "=", value);
    }

    /// <summary>
    /// 不等号演算子をオーバーロードします。
    /// </summary>
    /// <param name="column">カラム</param>
    /// <param name="value">値</param>
    /// <returns>WHERE句の条件</returns>
    public static WhereCondition operator !=(Column<T> column, T? value)
    {
        return new WhereCondition(column.Name, "<>", value);
    }

    /// <summary>
    /// より大きい演算子をオーバーロードします。
    /// </summary>
    /// <param name="column">カラム</param>
    /// <param name="value">値</param>
    /// <returns>WHERE句の条件</returns>
    public static WhereCondition operator >(Column<T> column, T? value)
    {
        return new WhereCondition(column.Name, ">", value);
    }

    /// <summary>
    /// より小さい演算子をオーバーロードします。
    /// </summary>
    /// <param name="column">カラム</param>
    /// <param name="value">値</param>
    /// <returns>WHERE句の条件</returns>
    public static WhereCondition operator <(Column<T> column, T? value)
    {
        return new WhereCondition(column.Name, "<", value);
    }

    /// <summary>
    /// より大きいか等しい演算子をオーバーロードします。
    /// </summary>
    /// <param name="column">カラム</param>
    /// <param name="value">値</param>
    /// <returns>WHERE句の条件</returns>
    public static WhereCondition operator >=(Column<T> column, T? value)
    {
        return new WhereCondition(column.Name, ">=", value);
    }

    /// <summary>
    /// より小さいか等しい演算子をオーバーロードします。    
    /// </summary>
    /// <param name="column">カラム</param>
    /// <param name="value">値</param>
    /// <returns>WHERE句の条件</returns>
    public static WhereCondition operator <=(Column<T> column, T? value)
    {
        return new WhereCondition(column.Name, "<=", value);
    }

    /// <summary>
    /// LIKE演算子をオーバーロードします。
    /// </summary>
    /// <param name="pattern">パターン</param>
    /// <returns>WHERE句の条件</returns>
    public WhereCondition Like(string pattern)
    {
        return new WhereCondition(Name, "LIKE", pattern);
    }

    /// <summary>
    /// IN演算子を使用したWHERE句の条件を表すクラス
    /// </summary>
    /// <param name="values">値の配列</param>
    /// <returns>WHERE句の条件</returns>
    public WhereCondition In(params T[] values)
    {
        return new WhereInCondition(Name, values?.Cast<object>().ToArray() ?? []);
    }

    /// <summary>
    /// NOT IN演算子を使用したWHERE句の条件を表すクラス
    /// </summary>
    /// <param name="values">値の配列</param>
    /// <returns>WHERE句の条件</returns>
    public WhereCondition NotIn(params T[] values)
    {
        return new WhereNotInCondition(Name, values?.Cast<object>().ToArray() ?? []);
    }

    /// <summary>
    /// IS NULL演算子を使用したWHERE句の条件を表すクラス
    /// </summary>
    /// <returns>WHERE句の条件</returns>
    public WhereCondition IsNull()
    {
        return new WhereIsNullCondition(Name);
    }

    /// <summary>
    /// IS NOT NULL演算子を使用したWHERE句の条件を表すクラス
    /// </summary>
    /// <returns>WHERE句の条件</returns>
    public WhereCondition IsNotNull()
    {
        return new WhereIsNotNullCondition(Name);
    }

    /// <summary>
    /// BETWEEN演算子を使用したWHERE句の条件を表すクラス
    /// </summary>
    /// <param name="min">最小値</param>
    /// <param name="max">最大値</param>
    /// <returns>WHERE句の条件</returns>
    public WhereCondition Between(T min, T max)
    {
        return new WhereBetweenCondition(Name, min, max);
    }

    /// <summary>
    /// 等号演算子をオーバーロードします。
    /// </summary>
    /// <param name="left">左辺のカラム</param>
    /// <param name="right">右辺のカラム</param>
    /// <returns>JOIN条件</returns>
    public static JoinCondition operator ==(Column<T> left, Column<T> right)
    {
        return new JoinCondition(left.Name, right.Name);
    }

    /// <summary>
    /// 不等号演算子をオーバーロードします。
    /// </summary>
    /// <param name="left">左辺のカラム</param>
    /// <param name="right">右辺のカラム</param>
    /// <returns>JOIN条件</returns>
    /// <exception cref="InvalidOperationException">JOIN条件では != 演算子は使用できません。== のみサポートされています。</exception>
    [Obsolete("JOIN条件では != 演算子は使用できません。== のみサポートされています。", error: true)]
    public static JoinCondition operator !=(Column<T> left, Column<T> right)
    {
        // TODO: コンパイルエラーにする方法を検討 (Obsoluteは本来の用途と異なる)
        throw new InvalidOperationException("JOIN条件では != 演算子は使用できません。== のみサポートされています。");
    }

    /// <summary>
    /// 等しいかどうかを比較します。
    /// </summary>
    /// <param name="obj">比較対象</param>
    /// <returns>等しいかどうか</returns>
    public override bool Equals(object? obj)
    {
        return obj is Column<T> column && Name == column.Name;
    }

    /// <summary>
    /// ハッシュコードを取得します。
    /// </summary>
    /// <returns>ハッシュコード</returns>
    public override int GetHashCode()
    {
        return Name.GetHashCode();
    }
}
