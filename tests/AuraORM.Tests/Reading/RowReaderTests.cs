namespace AuraORM.Tests;

/// <summary>
/// A row of a string and a byte array that are not nullable, and of their nullable forms, for the typed reading methods.
/// </summary>
[Table("texts")]
public sealed partial record TextRow([PrimaryKey] long Id, string Name, string? Note, byte[] Data, byte[]? MaybeData);

public sealed class RowReaderTests
{
    /// <summary>
    /// The typed methods that generated code calls read NULL as null for a nullable member, and throw for a member that is
    /// not nullable, naming the column (design §16.1).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - A reader whose first row has a value in every column and whose second row has NULL in the nullable columns; and
    ///   readers with NULL in <c>Name</c> and in <c>Data</c>.
    ///
    /// Steps:
    /// - Read the whole row of <see cref="TextRow"/> from each.
    ///
    /// Expected:
    /// - The values and nulls of the first reader.
    /// - <see cref="InvalidOperationException"/> naming position 1 (<c>Name</c>) and position 3 (<c>Data</c>).
    /// </remarks>
    [Fact]
    public async Task ReadRow_NullInNotNullableColumn_Throws()
    {
        byte[] data = [1, 2];
        string[] names = ["Id", "Name", "Note", "Data", "MaybeData"];
        FakeConnection Rows(string?[] name, byte[]?[] blob) =>
            new()
            {
                Reader = () =>
                    new ArrayDataReader(
                        names,
                        [new long[] { 1, 2 }, name, new string?[] { "n", null }, blob, new byte[]?[] { data, null }],
                        new bool[]?[5]
                    ),
            };

        List<TextRow> rows = await new Database<SqliteDialect>(Rows(["a", "b"], [data, data]))
            .From(TextRow.Table)
            .Select(t => t)
            .ToListAsync(TestContext.Current.CancellationToken);
        Assert.Equal(("a", "n", data, data), (rows[0].Name, rows[0].Note, rows[0].Data, rows[0].MaybeData));
        Assert.Equal(("b", (string?)null, data, (byte[]?)null), (rows[1].Name, rows[1].Note, rows[1].Data, rows[1].MaybeData));

        InvalidOperationException name = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new Database<SqliteDialect>(Rows(["a", null], [data, data]))
                .From(TextRow.Table)
                .Select(t => t)
                .ToListAsync(TestContext.Current.CancellationToken)
        );
        Assert.Contains("position 1 (Name)", name.Message, StringComparison.Ordinal);
        InvalidOperationException blob = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new Database<SqliteDialect>(Rows(["a", "b"], [data, null]))
                .From(TextRow.Table)
                .Select(t => t)
                .ToListAsync(TestContext.Current.CancellationToken)
        );
        Assert.Contains("position 3 (Data)", blob.Message, StringComparison.Ordinal);
    }
}
