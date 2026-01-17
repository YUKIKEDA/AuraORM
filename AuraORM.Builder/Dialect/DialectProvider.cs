namespace AuraORM.Builder.Dialect;

/// <summary>
/// デフォルトのSQL方言を管理するプロバイダー
/// </summary>
public static class DialectProvider
{
    private static ISqlDialect s_default = SqliteDialect.Instance;

    /// <summary>
    /// デフォルトのSQL方言を取得または設定する
    /// </summary>
    public static ISqlDialect Default
    {
        get => s_default;
        set => s_default = value ?? throw new System.ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// デフォルトの方言をSQLiteに設定する
    /// </summary>
    public static void UseSqlite() => Default = SqliteDialect.Instance;
}