using static AuraORM.Sql;

namespace AuraORM.Integration.Tests;

/// <summary>
/// Raw queries and commands (design §11.3) on one database. A derived class per database gives the target.
/// </summary>
/// <typeparam name="TDialect">The dialect.</typeparam>
public abstract class RawQueryTests<TDialect>
    where TDialect : IDialect
{
    private static readonly Person Alice = new(1, "Alice", "Al", 30, PersonStatus.Active, 12.50m, new DateOnly(1996, 4, 1));
    private static readonly Person Bob = new(2, "O'Brien; DROP TABLE people; --", null, null, PersonStatus.Inactive, 0m, new DateOnly(2000, 1, 2));

    private protected abstract ITestTarget<TDialect> Target { get; }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>
    /// Rows inserted with a raw command are read back into the table type by column name, every column type included.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - An empty <c>people</c> table.
    ///
    /// Steps:
    /// - Insert two people with raw commands whose values are holes: one with every column set, one with nulls and a name
    ///   that holds a quote and SQL text.
    /// - Read them with <c>QueryAsync&lt;Person&gt;</c>, ordered by id.
    ///
    /// Expected:
    /// - Each command affects one row.
    /// - The rows equal the inserted values: the name with SQL text is data, the enum and the date are read back.
    /// </remarks>
    [Fact]
    public async Task QueryAsync_TableType_ReadsInsertedRows()
    {
        Database<TDialect> db = await Target.CreateDatabaseAsync();

        Assert.Equal(1, await InsertAsync(db, Alice));
        Assert.Equal(1, await InsertAsync(db, Bob));
        List<Person> people = await db.QueryAsync<Person>(
            $"SELECT id, name, nickname, age, status, balance, birthday FROM {Person.Table} ORDER BY id",
            Ct
        );

        Assert.Equal([Alice, Bob], people);
    }

    /// <summary>
    /// A result type is read by column name, without regard to case, and the columns it does not name are ignored.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Alice in <c>people</c>.
    ///
    /// Steps:
    /// - Select the name as <c>PERSON_NAME</c>, the id, and the age, into <see cref="NameAndAge"/> (whose name column is
    ///   <c>person_name</c>).
    ///
    /// Expected:
    /// - One result with Alice's name and age.
    /// </remarks>
    [Fact]
    public async Task QueryAsync_ResultType_MatchesNamesIgnoringCase()
    {
        Database<TDialect> db = await Target.CreateDatabaseAsync();
        await InsertAsync(db, Alice);

        List<NameAndAge> rows = await db.QueryAsync<NameAndAge>($"SELECT id, name AS PERSON_NAME, age FROM {Person.Table}", Ct);

        Assert.Equal([new NameAndAge("Alice", 30)], rows);
    }

    /// <summary>
    /// A result type with a member that the result has no column for throws before the first row is read.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Alice in <c>people</c>.
    ///
    /// Steps:
    /// - Select only the name into <see cref="NameAndAge"/>.
    ///
    /// Expected:
    /// - <see cref="InvalidOperationException"/> that names the missing column <c>Age</c>.
    /// </remarks>
    [Fact]
    public async Task QueryAsync_MissingColumn_Throws()
    {
        Database<TDialect> db = await Target.CreateDatabaseAsync();
        await InsertAsync(db, Alice);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            db.QueryAsync<NameAndAge>($"SELECT name AS person_name FROM {Person.Table}", Ct)
        );

        Assert.Contains("Age", error.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// Two columns with the name of a member make the result ambiguous and throw; two columns with a name that no member
    /// needs are ignored (design §11.3).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Alice in <c>people</c>.
    ///
    /// Steps:
    /// - Select two columns named <c>age</c> into <see cref="NameAndAge"/>; then two columns named <c>id</c> with the members'
    ///   columns.
    ///
    /// Expected:
    /// - <see cref="InvalidOperationException"/> that names <c>age</c>; then Alice's name and age.
    /// </remarks>
    [Fact]
    public async Task QueryAsync_DuplicateColumnNames_ThrowOnlyWhenUsed()
    {
        Database<TDialect> db = await Target.CreateDatabaseAsync();
        await InsertAsync(db, Alice);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            db.QueryAsync<NameAndAge>($"SELECT name AS person_name, age, id AS age FROM {Person.Table}", Ct)
        );
        List<NameAndAge> rows = await db.QueryAsync<NameAndAge>($"SELECT id, name AS person_name, age, id FROM {Person.Table}", Ct);

        Assert.Contains("age", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal([new NameAndAge("Alice", 30)], rows);
    }

    /// <summary>
    /// Tuples read columns by position, nullable ones included.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Alice and Bob in <c>people</c>.
    ///
    /// Steps:
    /// - Select id, nickname, and age as a tuple, ordered by id.
    ///
    /// Expected:
    /// - (1, "Al", 30) and (2, null, null).
    /// </remarks>
    [Fact]
    public async Task QueryAsync_Tuple_ReadsByPosition()
    {
        Database<TDialect> db = await Target.CreateDatabaseAsync();
        await InsertAsync(db, Alice);
        await InsertAsync(db, Bob);

        List<(long, string?, int?)> rows = await db.QueryAsync<long, string?, int?>($"SELECT id, nickname, age FROM {Person.Table} ORDER BY id", Ct);

        Assert.Equal([(1L, "Al", 30), (2L, null, null)], rows);
    }

    /// <summary>
    /// A scalar query returns the first column of the first row; with no row it throws, or returns the default.
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Alice in <c>people</c>.
    ///
    /// Steps:
    /// - Count the rows (with <c>Count()</c>, a 64-bit count on every dialect); query the age of a person who does not exist with both scalar methods.
    ///
    /// Expected:
    /// - The count is 1; <c>QueryScalarAsync</c> throws <see cref="InvalidOperationException"/>; the other returns null.
    /// </remarks>
    [Fact]
    public async Task QueryScalarAsync_RowOrNoRow_ValueOrThrows()
    {
        Database<TDialect> db = await Target.CreateDatabaseAsync();
        await InsertAsync(db, Alice);
        const long missing = 99;

        Assert.Equal(1L, await db.QueryScalarAsync<long>($"SELECT {Count()} FROM {Person.Table}", Ct));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            db.QueryScalarAsync<int?>($"SELECT age FROM {Person.Table} WHERE id = {missing}", Ct)
        );
        Assert.Null(await db.QueryScalarOrDefaultAsync<int?>($"SELECT age FROM {Person.Table} WHERE id = {missing}", Ct));
    }

    /// <summary>
    /// <c>Identifier</c> quotes a name chosen at run time, and <c>Unsafe</c> puts text in unchanged (design §11.1).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - Alice and Bob in <c>people</c>.
    ///
    /// Steps:
    /// - Select the column named by <c>Identifier("name")</c>, with <c>Unsafe("ORDER BY id DESC")</c> at the end.
    ///
    /// Expected:
    /// - Bob's name first, then Alice's.
    /// </remarks>
    [Fact]
    public async Task ExecuteAsync_IdentifierAndUnsafe_WriteSql()
    {
        Database<TDialect> db = await Target.CreateDatabaseAsync();
        await InsertAsync(db, Alice);
        await InsertAsync(db, Bob);

        List<(string, long)> rows = await db.QueryAsync<string, long>(
            $"SELECT {Identifier("name")}, id FROM {Person.Table} {Unsafe("ORDER BY id DESC")}",
            Ct
        );

        Assert.Equal([Bob.Name, Alice.Name], rows.Select(r => r.Item1));
    }

    /// <summary>
    /// <c>IsDistinctFrom</c> and <c>IsNotDistinctFrom</c> are two-valued: when one side is null they are true or false, never
    /// unknown, alone and under <c>Not</c>, on every dialect (design §4.2; SQL Server writes an expansion).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - People aged 30, null, and 5 (ids 1, 2, 3).
    ///
    /// Steps:
    /// - Select the ids where the age is distinct from 30, not distinct from 30, distinct from null, not distinct from null,
    ///   and the <c>Not</c> of the first two, each ordered by id.
    ///
    /// Expected:
    /// - [2, 3], [1], [1, 3], [2], [1], [2, 3].
    /// </remarks>
    [Fact]
    public async Task DistinctFrom_OneSideNull_IsTwoValued()
    {
        Database<TDialect> db = await Target.CreateDatabaseAsync();
        await InsertAsync(db, Alice);
        await InsertAsync(db, Bob);
        await InsertAsync(db, Alice with { Id = 3, Age = 5 });
        IExpr<int?, PerRow> age = Raw<int?>($"{Identifier("age")}");
        int? none = null;

        Assert.Equal([2L, 3L], await IdsAsync(db, IsDistinctFrom(age, 30)));
        Assert.Equal([1L], await IdsAsync(db, IsNotDistinctFrom(age, 30)));
        Assert.Equal([1L, 3L], await IdsAsync(db, IsDistinctFrom(age, none)));
        Assert.Equal([2L], await IdsAsync(db, IsNotDistinctFrom(age, none)));
        Assert.Equal([1L], await IdsAsync(db, Not(IsDistinctFrom(age, 30))));
        Assert.Equal([2L, 3L], await IdsAsync(db, Not(IsNotDistinctFrom(age, 30))));
    }

    /// <summary>
    /// <c>Like</c> with an escape character finds <c>%</c> as a plain character, with the escape character bound as a
    /// parameter, on every dialect (design §4.2).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - People named <c>50%</c> and <c>500</c>.
    ///
    /// Steps:
    /// - Select the ids whose name is like <c>50!%</c> with <c>!</c> as the escape character, and those not like it.
    ///
    /// Expected:
    /// - Only the person named <c>50%</c>, then only the other.
    /// </remarks>
    [Fact]
    public async Task Like_EscapeCharacter_MatchesWildcardAsText()
    {
        Database<TDialect> db = await Target.CreateDatabaseAsync();
        await InsertAsync(db, Alice with { Id = 1, Name = "50%" });
        await InsertAsync(db, Alice with { Id = 2, Name = "500" });
        IExpr<string, PerRow> name = Raw<string>($"{Identifier("name")}");

        Assert.Equal([1L], await IdsAsync(db, Like(name, "50!%", '!')));
        Assert.Equal([2L], await IdsAsync(db, NotLike(name, "50!%", '!')));
    }

    private static async Task<List<long>> IdsAsync(Database<TDialect> db, Condition<PerRow> condition)
    {
        List<(long, int?)> rows = await db.QueryAsync<long, int?>($"SELECT id, age FROM {Person.Table} WHERE {condition} ORDER BY id", Ct);
        return [.. rows.Select(r => r.Item1)];
    }

    private static Task<int> InsertAsync(Database<TDialect> db, Person person) =>
        db.ExecuteAsync(
            $"INSERT INTO {Person.Table} (id, name, nickname, age, status, balance, birthday) VALUES ({person.Id}, {person.Name}, {person.Nickname}, {person.Age}, {person.Status}, {person.Balance}, {person.Birthday})",
            Ct
        );
}

/// <summary>
/// <see cref="RawQueryTests{TDialect}"/> on SQLite.
/// </summary>
public sealed class SqliteRawQueryTests : RawQueryTests<SqliteDialect>, IAsyncLifetime
{
    private readonly SqliteTarget _target = new();

    private protected override ITestTarget<SqliteDialect> Target => _target;

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public ValueTask DisposeAsync() => _target.DisposeAsync();
}
