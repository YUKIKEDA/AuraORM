using static AuraORM.Sql;

namespace AuraORM.Integration.Tests;

/// <summary>
/// <c>INSERT</c>, <c>UPDATE</c>, and <c>DELETE</c> (design §6–§8) on one database. A derived class per database gives the
/// target.
/// </summary>
/// <typeparam name="TDialect">The dialect.</typeparam>
public abstract class MutationTests<TDialect>
    where TDialect : IDialect
{
    private static readonly DateOnly Day = new(2000, 1, 2);

    private protected abstract ITestTarget<TDialect> Target { get; }

    private protected static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>
    /// An insert of one row and of several rows returns the number of rows, and the rows read back with the values given;
    /// a column left out is null (design §6.2).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - An empty <c>people</c> table.
    ///
    /// Steps:
    /// - Insert Alice without a nickname; insert Bob and Carol in one statement; select all, ordered by the id.
    ///
    /// Expected:
    /// - 1 and 2; (1, Alice, null, 30, Active, 10.50, 2000-01-02), (2, Bob, "B", null, …), (3, Carol, …).
    /// </remarks>
    [Fact]
    public async Task Insert_OneAndSeveralRows_AreWritten()
    {
        Database<TDialect> db = await Target.CreateDatabaseAsync();

        int one = await db.Insert(Person.Table).Values(NewPerson(1, "Alice", age: 30)).ExecuteAsync(Ct);
        int two = await db.Insert(Person.Table).Values([NewPerson(2, "Bob", nickname: "B"), NewPerson(3, "Carol")]).ExecuteAsync(Ct);
        List<Person> people = await db.From(Person.Table).Select(p => p).OrderBy(p => [Asc(p.Id)]).ToListAsync(Ct);

        Assert.Equal(1, one);
        Assert.Equal(2, two);
        Assert.Equal(
            [
                new Person(1, "Alice", null, 30, PersonStatus.Active, 10.50m, Day),
                new Person(2, "Bob", "B", null, PersonStatus.Active, 10.50m, Day),
                new Person(3, "Carol", null, null, PersonStatus.Active, 10.50m, Day),
            ],
            people
        );
    }

    /// <summary>
    /// A row that sets no column takes the database's defaults and its assigned key, and a column left out takes its default
    /// (design §6.1, §6.2).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - An empty <c>notes</c> table: an auto-increment id, a nullable body, and a priority that defaults to 0.
    ///
    /// Steps:
    /// - Insert a note that sets nothing (<c>DEFAULT VALUES</c> or <c>() VALUES ()</c>); insert a note that sets only the
    ///   body; select all, ordered by the id.
    ///
    /// Expected:
    /// - 1 for each insert; (null, 0) and ("b", 0), with two different ids.
    /// </remarks>
    [Fact]
    public async Task Insert_ColumnsLeftOut_TakeTheirDefaults()
    {
        Database<TDialect> db = await Target.CreateDatabaseAsync();

        int empty = await db.Insert(Note.Table).Values(new Note.Insert()).ExecuteAsync(Ct);
        int body = await db.Insert(Note.Table).Values(new Note.Insert { Body = "b" }).ExecuteAsync(Ct);
        List<Note> notes = await db.From(Note.Table).Select(n => n).OrderBy(n => [Asc(n.Id)]).ToListAsync(Ct);

        Assert.Equal(1, empty);
        Assert.Equal(1, body);
        Assert.Equal([(null, 0), ("b", 0)], notes.Select(n => (n.Body, n.Priority)));
        Assert.NotEqual(notes[0].Id, notes[1].Id);
    }

    /// <summary>
    /// An update assigns values and expressions evaluated on the row before the update, returns the number of rows that met
    /// the condition (also when a value does not change), and <c>AllRows()</c> changes every row (design §6.2, §7).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Alice (age 30, no nickname), Bob (age null, nickname "B"), and Carol.
    ///
    /// Steps:
    /// - Update the people older than 20: nickname to the name, name to "Al", age to age + 1. Update Bob's nickname to "B",
    ///   its current value. Update every row's balance to 0. Select all.
    ///
    /// Expected:
    /// - 1, 1, 3; Alice is (Al, nickname Alice, 31, 0.00); Bob keeps "B"; every balance is 0.
    /// </remarks>
    [Fact]
    public async Task Update_AssignsAndCountsTheMatchedRows()
    {
        Database<TDialect> db = await SeedPeopleAsync();

        int older = await db.Update(Person.Table)
            .Set(p => [Set(p.Nickname, p.Name), Set(p.Name, "Al"), Set(p.Age, Add(p.Age, 1))])
            .Where(p => Gt(p.Age, 20))
            .ExecuteAsync(Ct);
        int same = await db.Update(Person.Table).Set(p => [Set(p.Nickname, "B")]).Where(p => Eq(p.Id, 2L)).ExecuteAsync(Ct);
        int all = await db.Update(Person.Table).Set(p => [Set(p.Balance, 0m)]).AllRows().ExecuteAsync(Ct);
        List<Person> people = await db.From(Person.Table).Select(p => p).OrderBy(p => [Asc(p.Id)]).ToListAsync(Ct);

        Assert.Equal(1, older);
        Assert.Equal(1, same);
        Assert.Equal(3, all);
        Assert.Equal(
            [
                new Person(1, "Al", "Alice", 31, PersonStatus.Active, 0m, Day),
                new Person(2, "Bob", "B", null, PersonStatus.Active, 0m, Day),
                new Person(3, "Carol", null, null, PersonStatus.Active, 0m, Day),
            ],
            people
        );
    }

    /// <summary>
    /// An update assigns null to a nullable column, and its condition can be a subquery on another table (design §7, §5.11).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Alice, Bob ("B"), and Carol; a pet owned by Bob.
    ///
    /// Steps:
    /// - Set the nickname to null for the people who own a pet; select the nicknames.
    ///
    /// Expected:
    /// - 1; every nickname is null.
    /// </remarks>
    [Fact]
    public async Task Update_NullAndSubquery_AreWritten()
    {
        Database<TDialect> db = await SeedPeopleAsync();
        await db.ExecuteAsync($"INSERT INTO pets (id, owner_id, name) VALUES (10, 2, 'Rex')", Ct);

        int owners = await db.Update(Person.Table)
            .Set(p => [Set(p.Nickname, null)])
            .Where(p => In(p.Id, db.From(Pet.Table).Select(x => x.OwnerId)))
            .ExecuteAsync(Ct);
        List<string?> nicknames = await db.From(Person.Table).Select(p => p.Nickname).ToListAsync(Ct);

        Assert.Equal(1, owners);
        Assert.All(nicknames, Assert.Null);
    }

    /// <summary>
    /// A delete removes the rows that meet its condition and returns their number; <c>AllRows()</c> removes the rest (design
    /// §8).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Alice, Bob, and Carol.
    ///
    /// Steps:
    /// - Delete the people whose id is over 1; select the names; delete every row; count the rows.
    ///
    /// Expected:
    /// - 2; Alice; 1; 0.
    /// </remarks>
    [Fact]
    public async Task Delete_RemovesAndCountsTheRows()
    {
        Database<TDialect> db = await SeedPeopleAsync();

        int some = await db.Delete(Person.Table).Where(p => Gt(p.Id, 1L)).ExecuteAsync(Ct);
        List<string> left = await db.From(Person.Table).Select(p => p.Name).ToListAsync(Ct);
        int rest = await db.Delete(Person.Table).AllRows().ExecuteAsync(Ct);
        long count = await db.From(Person.Table).Select(p => Count()).SingleAsync(Ct);

        Assert.Equal(2, some);
        Assert.Equal(["Alice"], left);
        Assert.Equal(1, rest);
        Assert.Equal(0, count);
    }

    private protected async Task<Database<TDialect>> SeedPeopleAsync()
    {
        Database<TDialect> db = await Target.CreateDatabaseAsync();
        await db.Insert(Person.Table)
            .Values([NewPerson(1, "Alice", age: 30), NewPerson(2, "Bob", nickname: "B"), NewPerson(3, "Carol")])
            .ExecuteAsync(Ct);
        return db;
    }

    private static Person.Insert NewPerson(long id, string name, string? nickname = null, int? age = null) =>
        new()
        {
            Id = id,
            Name = name,
            Nickname = nickname,
            Age = age,
            Status = PersonStatus.Active,
            Balance = 10.50m,
            Birthday = Day,
        };
}

/// <summary>
/// <see cref="MutationTests{TDialect}"/> and the statements that only MySQL rejects: a subquery of an update or a delete
/// that reads the table being changed (design §7, §8).
/// </summary>
/// <typeparam name="TDialect">The dialect: every one but MySQL.</typeparam>
public abstract class TargetSubqueryMutationTests<TDialect> : MutationTests<TDialect>
    where TDialect : IDialect
{
    /// <summary>
    /// A subquery of an update and of a delete can read the table being changed, and sees the rows before the statement
    /// (design §7, §8).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Alice (30), Bob (null), and Carol (null).
    ///
    /// Steps:
    /// - Set every age to the largest age of <c>people AS x</c> plus 1; delete the people for whom a person
    ///   (<c>people AS x</c>) with a larger id exists; select the remaining ids and ages.
    ///
    /// Expected:
    /// - 3 and 2; only (3, 31) is left.
    /// </remarks>
    [Fact]
    public async Task Mutation_SubqueryOnTheTarget_ReadsTheRowsBefore()
    {
        Database<TDialect> db = await SeedPeopleAsync();

        int updated = await db.Update(Person.Table)
            .Set(p => [Set(p.Age, Add(Scalar(db.From(Person.Table.As("x")).Select(x => Max(x.Age))), 1))])
            .AllRows()
            .ExecuteAsync(Ct);
        int deleted = await db.Delete(Person.Table)
            .Where(p => Exists(db.From(Person.Table.As("x")).Where(x => Gt(x.Id, p.Id)).Select(x => x.Id)))
            .ExecuteAsync(Ct);
        List<(long, int?)> left = await db.From(Person.Table).Select(p => (p.Id, p.Age)).ToListAsync(Ct);

        Assert.Equal(3, updated);
        Assert.Equal(2, deleted);
        Assert.Equal([(3L, (int?)31)], left);
    }
}

/// <summary>
/// <see cref="TargetSubqueryMutationTests{TDialect}"/> on SQLite in memory.
/// </summary>
public sealed class SqliteMutationTests : TargetSubqueryMutationTests<SqliteDialect>, IAsyncLifetime
{
    private readonly SqliteTarget _target = new();

    private protected override ITestTarget<SqliteDialect> Target => _target;

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public ValueTask DisposeAsync() => _target.DisposeAsync();
}
