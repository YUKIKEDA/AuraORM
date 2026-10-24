namespace AuraORM.Integration.Tests;

/// <summary>
/// Writes <see cref="TypedRow"/> rows with raw SQL, so that a test of reading does not depend on the INSERT builder.
/// </summary>
internal static class TypedRows
{
    /// <summary>
    /// Inserts one row.
    /// </summary>
    /// <typeparam name="TDialect">The dialect.</typeparam>
    /// <param name="db">The database.</param>
    /// <param name="row">The row.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of rows inserted.</returns>
    public static Task<int> InsertAsync<TDialect>(Database<TDialect> db, TypedRow row, CancellationToken cancellationToken)
        where TDialect : IDialect =>
        db.ExecuteAsync(
            $"INSERT INTO {TypedRow.Table} (id, flag_value, tiny_value, small_value, number_value, big_value, single_value, double_value, money_value, text_value, blob_value, key_value, at_value, at_offset_value, day_value, time_value, state_value) VALUES ({row.Id}, {row.FlagValue}, {row.TinyValue}, {row.SmallValue}, {row.NumberValue}, {row.BigValue}, {row.SingleValue}, {row.DoubleValue}, {row.MoneyValue}, {row.TextValue}, {row.BlobValue}, {row.KeyValue}, {row.AtValue}, {row.AtOffsetValue}, {row.DayValue}, {row.TimeValue}, {row.StateValue})",
            cancellationToken
        );
}
