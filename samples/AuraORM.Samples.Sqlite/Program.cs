// Runs AuraORM on SQLite in memory and checks every result. It is published with NativeAOT in CI (docs/design.ja.md §16.3);
// the exit code says whether every check passed, so the published executable is tested, not only compiled.
using AuraORM;
using AuraORM.Samples.Sqlite;
using Microsoft.Data.Sqlite;
using static AuraORM.Sql;

await using var connection = new SqliteConnection("Data Source=:memory:");
await connection.OpenAsync();
var db = new Database<SqliteDialect>(connection);
var checks = new Checks();

await db.ExecuteAsync(
    $"CREATE TABLE {Book.Table} (id INTEGER PRIMARY KEY, title TEXT NOT NULL, author TEXT, pages INTEGER, genre INTEGER NOT NULL, published TEXT NOT NULL)"
);

await db.InTransactionAsync(async tx =>
{
    foreach (Book book in Book.Samples)
    {
        await tx.ExecuteAsync(
            $"INSERT INTO {Book.Table} (id, title, author, pages, genre, published) VALUES ({book.Id}, {book.Title}, {book.Author}, {book.Pages}, {book.Genre}, {book.Published})"
        );
    }
});

List<Book> books = await db.QueryAsync<Book>($"SELECT id, title, author, pages, genre, published FROM {Book.Table} ORDER BY id");
checks.SameItems("rows by column name", Book.Samples, books);

List<(string, int?)> pages = await db.QueryAsync<string, int?>($"SELECT title, pages FROM {Book.Table} WHERE {Gt(Value(1), 0)} ORDER BY id");
checks.SameItems("tuples", Book.Samples.Select(b => (b.Title, b.Pages)).ToList(), pages);

long fiction = await db.QueryScalarAsync<long>($"SELECT {Count()} FROM {Book.Table} WHERE genre = {Genre.Fiction}");
checks.Equal("scalar", 2L, fiction);

try
{
    await db.InTransactionAsync(async tx =>
    {
        await tx.ExecuteAsync($"DELETE FROM {Book.Table}");
        throw new InvalidOperationException("roll back");
    });
}
catch (InvalidOperationException) { }

checks.Equal("rollback", 3L, await db.QueryScalarAsync<long>($"SELECT {Count()} FROM {Book.Table}"));

return checks.Report();

namespace AuraORM.Samples.Sqlite
{
    /// <summary>
    /// The genre of a book, stored as its underlying integer.
    /// </summary>
    public enum Genre
    {
        Fiction = 1,
        Science = 2,
    }

    /// <summary>
    /// A table of the sample.
    /// </summary>
    [Table("books", Naming = ColumnNaming.SnakeCase)]
    public sealed partial record Book([PrimaryKey] long Id, string Title, string? Author, int? Pages, Genre Genre, DateOnly Published)
    {
        public static readonly IReadOnlyList<Book> Samples =
        [
            new(1, "Dune", "Frank Herbert", 412, Genre.Fiction, new DateOnly(1965, 8, 1)),
            new(2, "Anonymous Tales", null, null, Genre.Fiction, new DateOnly(1900, 1, 1)),
            new(3, "Cosmos", "Carl Sagan", 365, Genre.Science, new DateOnly(1980, 1, 1)),
        ];
    }

    /// <summary>
    /// Collects the checks of the sample and reports them.
    /// </summary>
    internal sealed class Checks
    {
        private int _failures;

        public void Equal<T>(string name, T expected, T actual)
        {
            bool same = EqualityComparer<T>.Default.Equals(expected, actual);
            Report(name, same, $"expected {expected}, actual {actual}");
        }

        public void SameItems<T>(string name, IReadOnlyList<T> expected, IReadOnlyList<T> actual)
        {
            bool same = expected.SequenceEqual(actual);
            Report(name, same, $"expected [{string.Join(", ", expected)}], actual [{string.Join(", ", actual)}]");
        }

        public int Report()
        {
            Console.WriteLine(_failures == 0 ? "All checks passed." : $"{_failures} checks failed.");
            return _failures == 0 ? 0 : 1;
        }

        private void Report(string name, bool passed, string detail)
        {
            Console.WriteLine($"{(passed ? "ok  " : "FAIL")} {name}{(passed ? string.Empty : ": " + detail)}");
            _failures += passed ? 0 : 1;
        }
    }
}
