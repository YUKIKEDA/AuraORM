namespace AuraORM.Tests;

public sealed class ValueConverterTypingTests
{
    /// <summary>
    /// A converted column takes comparisons, <c>In</c>, ordering, assignments, and inserts of its own type, and its values
    /// read as that type (design §3.6).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - <see cref="Order"/>, whose columns are converted.
    ///
    /// Steps:
    /// - Compile a select with <c>Eq</c>, <c>Lt</c>, <c>In</c>, <c>IsNull</c>, and <c>OrderBy</c> on converted columns, read
    ///   as <c>List&lt;OrderId&gt;</c>; an insert; an update; and a raw query into <see cref="OrderLink"/>.
    ///
    /// Expected:
    /// - No error and no warning.
    /// </remarks>
    [Fact]
    public void ConvertedColumn_ValidForms_Compile()
    {
        CompileAssert.Compiles(
            """
            System.Threading.Tasks.Task<List<OrderId>> ids = sqlite.From(Order.Table)
                .Where(o => And(Eq(o.Id, new OrderId(1)), Lt(o.Id, new OrderId(9)), In(o.Id, [new OrderId(2)]), IsNull(o.Parent)))
                .Select(o => o.Id)
                .OrderBy(o => [Asc(o.Id)])
                .ToListAsync();
            var insert = sqlite.Insert(Order.Table).Values(new Order.Insert { Id = new OrderId(1), Link = new Uri("https://a.example/") });
            var update = sqlite.Update(Order.Table).Set(o => [Set(o.Parent, new OrderId(1)), Set(o.Total, new Price(1m))]).AllRows();
            System.Threading.Tasks.Task<List<OrderLink>> links = sqlite.QueryAsync<OrderLink>($"SELECT id, link FROM orders WHERE id = {new OrderId(1)}");
            """
        );
    }

    /// <summary>
    /// Functions with a type constraint do not take a converted type: arithmetic, string functions, and <c>Min</c> /
    /// <c>Max</c> do not compile (design §3.6).
    /// </summary>
    /// <remarks>
    /// Preconditions:
    /// - <see cref="Order"/>'s <see cref="OrderId"/> key and <see cref="Uri"/>? link.
    ///
    /// Steps:
    /// - Compile <c>Add(o.Id, new OrderId(1))</c>, <c>Like(o.Link, "x%")</c>, and <c>Max(o.Id)</c>.
    ///
    /// Expected:
    /// - An error for each.
    /// </remarks>
    [Theory]
    [InlineData("sqlite.From(Order.Table).Select(o => Add(o.Id, new OrderId(1)));")]
    [InlineData("sqlite.From(Order.Table).Where(o => Like(o.Link, \"x%\")).Select(o => o.Id);")]
    [InlineData("sqlite.From(Order.Table).Select(o => Max(o.Id));")]
    public void ConvertedColumn_ConstrainedFunctions_DoNotCompile(string body) => CompileAssert.Rejects(body);
}
