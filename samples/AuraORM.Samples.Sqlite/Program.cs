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

List<Book> selected = await db.From(Book.Table).Where(b => Eq(b.Genre, Genre.Fiction)).Select(b => b).OrderBy(b => [Asc(b.Id)]).ToListAsync();
checks.SameItems("select rows", Book.Samples.Where(b => b.Genre == Genre.Fiction).ToList(), selected);

List<(string, int?)> page = await db.From(Book.Table).Select(b => (b.Title, b.Pages)).OrderBy(b => [Desc(b.Id)]).Limit(1).Offset(1).ToListAsync();
checks.SameItems("select page", [("Anonymous Tales", (int?)null)], page);

(long count, int? longest) = await db.From(Book.Table).Select(b => (Count(), Max(b.Pages))).SingleAsync();

string? author = null;
List<long> filtered = await db.From(Book.Table)
    .Where(b => And([author is null ? null : Eq(b.Author, author), Gt(b.Pages, 300)]))
    .Select(b => b.Id)
    .OrderBy(b => [Asc(b.Id)])
    .ToListAsync();
checks.SameItems("optional conditions", [1L, 3L], filtered);

List<Genre> genres = await db.From(Book.Table).SelectDistinct(b => b.Genre).OrderBy(g => [Desc(g)]).ToListAsync();
checks.SameItems("distinct ordered", [Genre.Science, Genre.Fiction], genres);

// Joins, with the nullable whole row of an outer join (NullableColumns.ReadRow) and an aliased self-join.
await db.ExecuteAsync($"CREATE TABLE {Loan.Table} (id INTEGER PRIMARY KEY, book_id INTEGER NOT NULL, borrower TEXT NOT NULL)");
await db.ExecuteAsync($"INSERT INTO {Loan.Table} (id, book_id, borrower) VALUES ({1L}, {3L}, {"Ada"})");
List<(string, Loan?)> loans = await db.From(Book.Table)
    .LeftJoin(Loan.Table, (b, l) => Eq(l.BookId, b.Id))
    .Select((b, l) => (b.Title, l))
    .OrderBy((b, l) => [Asc(b.Id)])
    .ToListAsync();
checks.SameItems("left join", [("Dune", (Loan?)null), ("Anonymous Tales", null), ("Cosmos", new Loan(1, 3, "Ada"))], loans);
long later = await db.From(Book.Table).InnerJoin(Book.Table.As("later"), (b, x) => Gt(x.Id, b.Id)).Select((b, x) => Count()).SingleAsync();
checks.Equal("self join", 3L, later);

// GROUP BY with HAVING and ORDER BY on aggregates, and a key with a value, which is one parameter wherever it is written.
List<(Genre, long, int?)> perGenre = await db.From(Book.Table)
    .GroupBy(b => b.Genre)
    .Having((k, b) => Ge(Count(), 1L))
    .Select((k, b) => (k, Count(), Max(b.Pages)))
    .OrderBy((k, b) => [Desc(Count()), Asc(k)])
    .ToListAsync();
checks.SameItems("group by", [(Genre.Fiction, 2L, (int?)412), (Genre.Science, 1L, (int?)365)], perGenre);
List<(int?, long)> keyed = await db.From(Book.Table)
    .GroupBy(b => Add(b.Pages, 1))
    .Having((k, b) => Gt(k, 400))
    .Select((k, b) => (k, Count()))
    .ToListAsync();
checks.SameItems("group by a key with a value", [((int?)413, 1L)], keyed);

// Subqueries: IN, a correlated EXISTS and scalar, and a group subquery correlated through Outer(k).
List<string> lent = await db.From(Book.Table)
    .Where(b => In(b.Id, db.From(Loan.Table).Select(l => l.BookId)))
    .Select(b => b.Title)
    .ToListAsync();
checks.SameItems("in subquery", ["Cosmos"], lent);
List<(string, string?)> borrowers = await db.From(Book.Table)
    .Where(b => Exists(db.From(Loan.Table).Where(l => Eq(l.BookId, b.Id)).Select(l => l.Id)))
    .Select(b => (b.Title, Scalar(db.From(Loan.Table).Where(l => Eq(l.BookId, b.Id)).Select(l => l.Borrower))))
    .ToListAsync();
checks.SameItems("correlated subqueries", [("Cosmos", (string?)"Ada")], borrowers);
List<long> loanedIds = await db.From(Book.Table)
    .GroupBy(b => b.Id)
    .Having((k, b) => GroupExists(db.From(Loan.Table).Where(l => Eq(l.BookId, Outer(k))).Select(l => l.Id)))
    .Select((k, b) => k)
    .ToListAsync();
checks.SameItems("group subquery", [3L], loanedIds);

SqlStatement offline = new SqlBuilder<SqliteDialect>().From(Book.Table).Select(b => b.Title).Limit(1).ToSql();
checks.Equal("sql builder", "SELECT \"books\".\"title\" FROM \"books\" LIMIT @p0", offline.Text);
checks.Equal("select aggregates", (3L, (int?)412), (count, longest));

// Every reading path of the generated code and of ValueReader<T> that a type of design §3.3 takes, with values and nulls.
await db.ExecuteAsync(
    $"CREATE TABLE {Gadget.Table} (id INTEGER PRIMARY KEY, flag INTEGER, price TEXT, key TEXT, made TEXT, opens TEXT, image BLOB, rating REAL, shade INTEGER)"
);
foreach (Gadget gadget in Gadget.Samples)
{
    await db.ExecuteAsync(
        $"INSERT INTO {Gadget.Table} (id, flag, price, key, made, opens, image, rating, shade) VALUES ({gadget.Id}, {gadget.Flag}, {gadget.Price}, {gadget.Key}, {gadget.Made}, {gadget.Opens}, {gadget.Image}, {gadget.Rating}, {gadget.Shade})"
    );
}

List<Gadget> gadgets = await db.QueryAsync<Gadget>($"SELECT id, flag, price, key, made, opens, image, rating, shade FROM {Gadget.Table} ORDER BY id");
checks.Equal("every column type", true, gadgets.Count == 2 && Gadget.Samples.Zip(gadgets).All(p => p.First.SameAs(p.Second)));

List<TitleAndPages> results = await db.QueryAsync<TitleAndPages>(
    $"SELECT {Identifier("title")} AS heading, pages FROM {Book.Table} WHERE {Raw($"pages > {300}")} {Unsafe("ORDER BY id")}"
);
checks.SameItems("result type", [new TitleAndPages("Dune", 412), new TitleAndPages("Cosmos", 365)], results);

checks.Equal("raw expression", 777L, await db.QueryScalarAsync<long>($"SELECT {Raw<long>($"{700} + {77}")}"));
checks.Equal("scalar or default", null, await db.QueryScalarOrDefaultAsync<int?>($"SELECT pages FROM {Book.Table} WHERE id = {99}"));

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
    /// The shade of a gadget, stored as its underlying byte.
    /// </summary>
    public enum Shade : byte
    {
        Dark = 1,
        Light = 2,
    }

    /// <summary>
    /// A table with the column types that <see cref="Book"/> does not have, each nullable.
    /// </summary>
    [Table("gadgets")]
    public sealed partial record Gadget(
        [PrimaryKey] long Id,
        [Column("flag")] bool? Flag,
        [Precision(10, 2), Column("price")] decimal? Price,
        [Column("key")] Guid? Key,
        [Column("made")] DateTimeOffset? Made,
        [Column("opens")] TimeOnly? Opens,
        [Column("image")] byte[]? Image,
        [Column("rating")] double? Rating,
        [Column("shade")] Shade? Shade
    )
    {
        public static readonly IReadOnlyList<Gadget> Samples =
        [
            new(
                1,
                true,
                19.99m,
                new Guid("6f9619ff-8b86-d011-b42d-00c04fc964ff"),
                new DateTimeOffset(2024, 5, 6, 7, 8, 9, TimeSpan.FromHours(9)),
                new TimeOnly(9, 30),
                [1, 2, 3],
                4.5,
                Sqlite.Shade.Light
            ),
            new(2, null, null, null, null, null, null, null, null),
        ];

        public bool SameAs(Gadget other) =>
            this with { Image = null } == other with { Image = null }
            && (Image is null ? other.Image is null : other.Image is not null && Image.AsSpan().SequenceEqual(other.Image));
    }

    /// <summary>
    /// A loan of a book, joined to <see cref="Book"/>.
    /// </summary>
    [Table("loans", Naming = ColumnNaming.SnakeCase)]
    public sealed partial record Loan([PrimaryKey] long Id, long BookId, string Borrower);

    /// <summary>
    /// A result type read by column name from a raw query.
    /// </summary>
    [QueryResult]
    public sealed partial record TitleAndPages([Column("heading")] string Title, int? Pages);

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
