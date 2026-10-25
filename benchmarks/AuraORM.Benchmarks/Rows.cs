using AuraORM.Tests;

namespace AuraORM.Benchmarks;

/// <summary>
/// The rows of the benchmarks, as in-memory readers whose typed getters do not allocate.
/// </summary>
internal static class Rows
{
    public const string SelectPeople = "SELECT \"Id\", \"Name\", \"Age\", \"Balance\", \"CreatedAt\", \"Active\" FROM \"people\"";

    public const string SelectMaybe = "SELECT \"Id\", \"Nickname\", \"Age\", \"Balance\", \"Seen\", \"Flag\" FROM \"maybe\"";

    private static readonly string[] PeopleNames = ["Id", "Name", "Age", "Balance", "CreatedAt", "Active"];

    private static readonly string[] MaybeNames = ["Id", "Nickname", "Age", "Balance", "Seen", "Flag"];

    // A connection whose every reader returns the rows of the people.
    public static FakeConnection People(int count)
    {
        long[] ids = Enumerable.Range(1, count).Select(i => (long)i).ToArray();
        string[] names = Enumerable.Range(1, count).Select(i => "name" + i).ToArray();
        int[] ages = Enumerable.Range(1, count).Select(i => 20 + (i % 50)).ToArray();
        decimal[] balances = Enumerable.Range(1, count).Select(i => i * 1.5m).ToArray();
        DateTime[] times = Enumerable.Range(1, count).Select(i => new DateTime(2024, 1, 1).AddMinutes(i)).ToArray();
        bool[] active = Enumerable.Range(1, count).Select(i => i % 2 == 0).ToArray();
        return new FakeConnection { Reader = () => new ArrayDataReader(PeopleNames, [ids, names, ages, balances, times, active], new bool[]?[6]) };
    }

    // A connection whose every reader returns rows with NULL in every other row of the nullable columns.
    public static FakeConnection Maybe(int count)
    {
        bool[] nulls = Enumerable.Range(1, count).Select(i => i % 2 == 0).ToArray();
        long[] ids = Enumerable.Range(1, count).Select(i => (long)i).ToArray();
        string?[] names = Enumerable.Range(1, count).Select(i => i % 2 == 0 ? null : "nick" + i).ToArray();
        int[] ages = Enumerable.Range(1, count).Select(i => 20 + (i % 50)).ToArray();
        decimal[] balances = Enumerable.Range(1, count).Select(i => i * 1.5m).ToArray();
        DateTime[] times = Enumerable.Range(1, count).Select(i => new DateTime(2024, 1, 1).AddMinutes(i)).ToArray();
        bool[] flags = Enumerable.Range(1, count).Select(i => i % 3 == 0).ToArray();
        return new FakeConnection
        {
            Reader = () => new ArrayDataReader(MaybeNames, [ids, names, ages, balances, times, flags], [null, null, nulls, nulls, nulls, nulls]),
        };
    }
}
