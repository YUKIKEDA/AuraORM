using static AuraORM.Sql;

namespace AuraORM.Integration.Tests;

/// <summary>
/// <c>Returning</c> of <c>INSERT</c> and <c>DELETE</c> (design §9) on one database. A derived class per database gives the
/// target.
/// </summary>
/// <typeparam name="TDialect">The dialect.</typeparam>
public abstract class ReturningTests<TDialect>
    where TDialect : IInsertReturningDialect, IDeleteReturningDialect
{
    private static readonly DateOnly Day = new(2000, 1, 2);

    private protected abstract ITestTarget<TDialect> Target { get; }

    private protected static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>
    /// An insert returns the values that the database gave the rows: the assigned key and the defaults (design §6.1, §9).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - An empty <c>notes</c> table: an auto-increment id, a nullable body, and a priority that defaults to 0.
    ///
    /// Steps:
    /// - Insert a note that sets nothing, returning the whole row; insert two notes with bodies, returning the id and the
    ///   body; select the ids.
    ///
    /// Expected:
    /// - One row (id, null, 0); two rows with the bodies "a" and "b"; the three returned ids are the ids in the table.
    /// </remarks>
    [Fact]
    public async Task Insert_Returning_ReadsTheAssignedValues()
    {
        Database<TDialect> db = await Target.CreateDatabaseAsync();

        Note empty = await db.Insert(Note.Table).Values(new Note.Insert()).Returning(n => n).SingleAsync(Ct);
        List<(long, string?)> two = await db.Insert(Note.Table)
            .Values([new Note.Insert { Body = "a" }, new Note.Insert { Body = "b" }])
            .Returning(n => (n.Id, n.Body))
            .ToListAsync(Ct);
        List<long> ids = await db.From(Note.Table).Select(n => n.Id).OrderBy(n => [Asc(n.Id)]).ToListAsync(Ct);

        Assert.Equal((null, 0), (empty.Body, empty.Priority));
        Assert.Equal(["a", "b"], two.Select(r => r.Item2).Order());
        Assert.Equal(ids, two.Select(r => r.Item1).Append(empty.Id).Order());
    }

    /// <summary>
    /// A delete returns the values of the rows before they were deleted, and <c>FirstAsync</c> still deletes every row that
    /// meets the condition (design §9, §12.2).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Alice, Bob, and Carol.
    ///
    /// Steps:
    /// - Delete the people whose id is over 1, returning the name and the age plus one; delete every row with
    ///   <c>FirstAsync</c> returning the id; count the rows.
    ///
    /// Expected:
    /// - (Bob, null) and (Carol, 41); one id; 0 rows left.
    /// </remarks>
    [Fact]
    public async Task Delete_Returning_ReadsTheDeletedRows()
    {
        Database<TDialect> db = await SeedAsync();

        List<(string, int?)> deleted = await db.Delete(Person.Table).Where(p => Gt(p.Id, 1L)).Returning(p => (p.Name, Add(p.Age, 1))).ToListAsync(Ct);
        long first = await db.Delete(Person.Table).AllRows().Returning(p => p.Id).FirstAsync(Ct);
        long left = await db.From(Person.Table).Select(p => Count()).SingleAsync(Ct);

        Assert.Equal([("Bob", (int?)null), ("Carol", 41)], deleted.OrderBy(r => r.Item1));
        Assert.Equal(1L, first);
        Assert.Equal(0, left);
    }

    /// <summary>
    /// A subquery of <c>Returning</c> on another table reads it for each row, with the target's columns qualified inside it;
    /// SQL Server rejects any subquery there before the statement is sent (design §9, §14.3).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Alice, Bob, and Carol; Bob owns Rex.
    ///
    /// Steps:
    /// - Delete Bob, returning the id and the name of his pet.
    ///
    /// Expected:
    /// - (2, Rex), and Bob is deleted; on SQL Server, <see cref="InvalidOperationException"/> and nothing is deleted.
    /// </remarks>
    [Fact]
    public async Task Returning_SubqueryOnAnotherTable_ReadsPerRow()
    {
        Database<TDialect> db = await SeedAsync();
        await db.ExecuteAsync($"INSERT INTO pets (id, owner_id, name) VALUES (10, 2, 'Rex')", Ct);
        var statement = db.Delete(Person.Table)
            .Where(p => Eq(p.Id, 2L))
            .Returning(p => (p.Id, Scalar(db.From(Pet.Table).Where(x => Eq(x.OwnerId, p.Id)).Select(x => x.Name))));

        if (typeof(TDialect) == typeof(SqlServerDialect))
        {
            Assert.Throws<InvalidOperationException>(() => statement.ToSql());
            Assert.Equal(3, await db.From(Person.Table).Select(p => Count()).SingleAsync(Ct));
            return;
        }

        List<(long, string?)> rows = await statement.ToListAsync(Ct);

        Assert.Equal([(2L, "Rex")], rows);
        Assert.Equal(2, await db.From(Person.Table).Select(p => Count()).SingleAsync(Ct));
    }

    private protected async Task<Database<TDialect>> SeedAsync()
    {
        Database<TDialect> db = await Target.CreateDatabaseAsync();
        await db.Insert(Person.Table).Values([NewPerson(1, "Alice", 30), NewPerson(2, "Bob", null), NewPerson(3, "Carol", 40)]).ExecuteAsync(Ct);
        return db;
    }

    private static Person.Insert NewPerson(long id, string name, int? age) =>
        new()
        {
            Id = id,
            Name = name,
            Age = age,
            Status = PersonStatus.Active,
            Balance = 1m,
            Birthday = Day,
        };
}

/// <summary>
/// <see cref="ReturningTests{TDialect}"/> and <c>Returning</c> of <c>UPDATE</c> (design §9): every dialect with it, which is
/// not MariaDB.
/// </summary>
/// <typeparam name="TDialect">The dialect.</typeparam>
public abstract class UpdateReturningTests<TDialect> : ReturningTests<TDialect>
    where TDialect : IInsertReturningDialect, IUpdateReturningDialect, IDeleteReturningDialect
{
    /// <summary>
    /// An update returns the values of the rows after it (design §9).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Alice (30), Bob (null), and Carol (40).
    ///
    /// Steps:
    /// - Set the age to age + 1 and the nickname to the name for the people with an age, returning the id, the age, and
    ///   the nickname.
    ///
    /// Expected:
    /// - (1, 31, Alice) and (3, 41, Carol).
    /// </remarks>
    [Fact]
    public async Task Update_Returning_ReadsTheNewValues()
    {
        Database<TDialect> db = await SeedAsync();

        List<(long, int?, string?)> updated = await db.Update(Person.Table)
            .Set(p => [Set(p.Age, Add(p.Age, 1)), Set(p.Nickname, p.Name)])
            .Where(p => IsNotNull(p.Age))
            .Returning(p => (p.Id, p.Age, p.Nickname))
            .ToListAsync(Ct);

        Assert.Equal([(1L, (int?)31, "Alice"), (3L, 41, "Carol")], updated.OrderBy(r => r.Item1));
    }
}

/// <summary>
/// <see cref="UpdateReturningTests{TDialect}"/> on SQLite in memory.
/// </summary>
public sealed class SqliteReturningTests : UpdateReturningTests<SqliteDialect>, IAsyncLifetime
{
    private readonly SqliteTarget _target = new();

    private protected override ITestTarget<SqliteDialect> Target => _target;

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public ValueTask DisposeAsync() => _target.DisposeAsync();
}
