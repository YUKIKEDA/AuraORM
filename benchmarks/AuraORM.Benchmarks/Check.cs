namespace AuraORM.Benchmarks;

/// <summary>
/// Runs each benchmark once and checks that every library returns the same rows as the hand-written code, so the
/// benchmarks compare the same work (<c>--check</c>, benchmarks/README.md).
/// </summary>
internal static class Check
{
    public static async Task<int> RunAsync()
    {
        int failures = 0;

        var read = new ReadBenchmarks { Count = 100 };
        read.Setup();
        List<Person> people = await read.HandWritten();
        failures += Same("read: AuraORM", people, await read.AuraOrm());
        failures += Same("read: Dapper", people, await read.Dapper());
        failures += Same("read: Dapper.AOT", people, await read.DapperAot());
        List<Maybe> maybe = await read.HandWrittenNullable();
        failures += Same("read nullable: AuraORM", maybe, await read.AuraOrmNullable());
        failures += Same("read nullable: Dapper", maybe, await read.DapperNullable());
        failures += Same("read nullable: Dapper.AOT", maybe, await read.DapperAotNullable());

        using (var trip = new RoundTripBenchmarks { Count = 100 })
        {
            trip.Setup();
            List<Person> rows = await trip.HandWritten();
            failures += Same("round trip: AuraORM", rows, await trip.AuraOrm());
            failures += Same("round trip: Dapper", rows, Convert(await trip.Dapper()));
            failures += Same("round trip: Dapper.AOT", rows, Convert(await trip.DapperAot()));
            failures += Same("round trip: linq2db", rows, Convert(await trip.Linq2Db()));
            failures += Same("round trip: EF Core", rows, Convert(await trip.EfCore()));
            failures += Same("round trip: EF Core compiled", rows, Convert(await trip.EfCoreCompiled()));
        }

        var forms = new ValueReaderFormBenchmarks { Count = 100 };
        forms.Setup();
        failures += Same("forms: delegates", forms.Direct(), forms.Delegates());
        failures += Same("forms: branches", forms.Direct(), forms.Branches());
        failures += Same("forms nullable: delegates", forms.DirectNullable(), forms.DelegatesNullable());
        failures += Same("forms nullable: branches", forms.DirectNullable(), forms.BranchesNullable());

        using (var render = new RenderBenchmarks())
        using (var database = Statements.Open())
        {
            render.Setup();
            string select = render.AuraOrmSelect();
            failures += Statements.Same(database, "SELECT: linq2db", select, render.Linq2DbSelect());
            failures += Statements.Same(database, "SELECT: EF Core", select, render.EfCoreSelect());
            failures += Statements.Same(database, "SELECT: SqExpress", select, render.SqExpressSelect());
            failures += Statements.Same(database, "SELECT: SqlKata", select, render.SqlKataSelect());
            failures += Statements.Same(database, "SELECT: Sqlify", select, render.SqlifySelect());
            string joinGroup = render.AuraOrmJoinGroup();
            failures += Statements.InnerJoins("inner joins: AuraORM", joinGroup);
            failures += Statements.Same(database, "JOIN and GROUP BY: linq2db", joinGroup, render.Linq2DbJoinGroup());
            failures += Statements.InnerJoins("inner joins: linq2db", render.Linq2DbJoinGroup());
            failures += Statements.Same(database, "JOIN and GROUP BY: EF Core", joinGroup, render.EfCoreJoinGroup());
            failures += Statements.InnerJoins("inner joins: EF Core", render.EfCoreJoinGroup());
            failures += Statements.Same(database, "JOIN and GROUP BY: SqExpress", joinGroup, render.SqExpressJoinGroup());
            failures += Statements.InnerJoins("inner joins: SqExpress", render.SqExpressJoinGroup());
            failures += Statements.Same(database, "JOIN and GROUP BY: SqlKata", joinGroup, render.SqlKataJoinGroup());
            failures += Statements.InnerJoins("inner joins: SqlKata", render.SqlKataJoinGroup());
            failures += Statements.Same(database, "JOIN and GROUP BY: Sqlify", joinGroup, render.SqlifyJoinGroup());
            failures += Statements.InnerJoins("inner joins: Sqlify", render.SqlifyJoinGroup());
            Console.WriteLine(render.AuraOrmInsert());
            Console.WriteLine(render.AuraOrmUpdate());
        }

        Console.WriteLine(failures == 0 ? "Every library returned the same rows." : failures + " checks failed.");
        return failures == 0 ? 0 : 1;
    }

    private static List<Person> Convert(List<PersonRow> rows) =>
        rows.Select(r => new Person(r.Id, r.Name, r.Age, r.Balance, r.CreatedAt, r.Active)).ToList();

    private static int Same<T>(string name, List<T> expected, List<T> actual)
    {
        bool same = expected.SequenceEqual(actual);
        Console.WriteLine(
            (same ? "ok   " : "FAIL ") + name + (same ? string.Empty : $": {actual.FirstOrDefault()} instead of {expected.FirstOrDefault()}")
        );
        return same ? 0 : 1;
    }

    private static int Same(string name, long expected, long actual)
    {
        Console.WriteLine((expected == actual ? "ok   " : "FAIL ") + name);
        return expected == actual ? 0 : 1;
    }
}
