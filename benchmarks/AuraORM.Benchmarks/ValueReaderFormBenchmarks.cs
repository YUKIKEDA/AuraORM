using System.Data.Common;
using AuraORM.Tests;
using BenchmarkDotNet.Attributes;

namespace AuraORM.Benchmarks;

/// <summary>
/// The two forms of reading one typed value that design §16.1 asks row P1 to compare: a <c>static readonly</c> delegate
/// per type, chosen once (the form of <c>ValueReader&lt;T&gt;</c>), and the <c>typeof(T)</c> tests written into the
/// reading method, which the JIT folds for each value type. Both are copies of the shape, over the same in-memory reader,
/// against the typed getters called directly.
/// </summary>
[MemoryDiagnoser]
public class ValueReaderFormBenchmarks
{
    private FakeConnection _people = null!;
    private FakeConnection _maybe = null!;

    [Params(10000)]
    public int Count { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _people = Rows.People(Count);
        _maybe = Rows.Maybe(Count);
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("People")]
    public long Direct()
    {
        using DbDataReader reader = Open(_people);
        long sum = 0;
        while (reader.Read())
        {
            sum += Weigh(
                reader.GetInt64(0),
                reader.GetString(1),
                reader.GetInt32(2),
                reader.GetDecimal(3),
                reader.GetDateTime(4),
                reader.GetBoolean(5)
            );
        }

        return sum;
    }

    [Benchmark]
    [BenchmarkCategory("People")]
    public long Delegates()
    {
        using DbDataReader reader = Open(_people);
        long sum = 0;
        while (reader.Read())
        {
            sum += Weigh(
                DelegateForm<long>.Read(reader, 0),
                DelegateForm<string>.Read(reader, 1),
                DelegateForm<int>.Read(reader, 2),
                DelegateForm<decimal>.Read(reader, 3),
                DelegateForm<DateTime>.Read(reader, 4),
                DelegateForm<bool>.Read(reader, 5)
            );
        }

        return sum;
    }

    [Benchmark]
    [BenchmarkCategory("People")]
    public long Branches()
    {
        using DbDataReader reader = Open(_people);
        long sum = 0;
        while (reader.Read())
        {
            sum += Weigh(
                BranchForm.Read<long>(reader, 0),
                BranchForm.Read<string>(reader, 1),
                BranchForm.Read<int>(reader, 2),
                BranchForm.Read<decimal>(reader, 3),
                BranchForm.Read<DateTime>(reader, 4),
                BranchForm.Read<bool>(reader, 5)
            );
        }

        return sum;
    }

    [Benchmark]
    [BenchmarkCategory("Maybe")]
    public long DirectNullable()
    {
        using DbDataReader reader = Open(_maybe);
        long sum = 0;
        while (reader.Read())
        {
            sum += WeighMaybe(
                reader.GetInt64(0),
                reader.IsDBNull(1) ? null : reader.GetString(1),
                reader.IsDBNull(2) ? null : reader.GetInt32(2),
                reader.IsDBNull(3) ? null : reader.GetDecimal(3),
                reader.IsDBNull(4) ? null : reader.GetDateTime(4),
                reader.IsDBNull(5) ? null : reader.GetBoolean(5)
            );
        }

        return sum;
    }

    [Benchmark]
    [BenchmarkCategory("Maybe")]
    public long DelegatesNullable()
    {
        using DbDataReader reader = Open(_maybe);
        long sum = 0;
        while (reader.Read())
        {
            sum += WeighMaybe(
                DelegateForm<long>.Read(reader, 0),
                DelegateForm<string?>.Read(reader, 1),
                DelegateForm<int?>.Read(reader, 2),
                DelegateForm<decimal?>.Read(reader, 3),
                DelegateForm<DateTime?>.Read(reader, 4),
                DelegateForm<bool?>.Read(reader, 5)
            );
        }

        return sum;
    }

    [Benchmark]
    [BenchmarkCategory("Maybe")]
    public long BranchesNullable()
    {
        using DbDataReader reader = Open(_maybe);
        long sum = 0;
        while (reader.Read())
        {
            sum += WeighMaybe(
                BranchForm.Read<long>(reader, 0),
                BranchForm.Read<string?>(reader, 1),
                BranchForm.Read<int?>(reader, 2),
                BranchForm.Read<decimal?>(reader, 3),
                BranchForm.Read<DateTime?>(reader, 4),
                BranchForm.Read<bool?>(reader, 5)
            );
        }

        return sum;
    }

    private static DbDataReader Open(FakeConnection connection)
    {
        using DbCommand command = connection.CreateCommand();
        return command.ExecuteReader();
    }

    // Uses every value so the JIT cannot drop a read.
    private static long Weigh(long id, string name, int age, decimal balance, DateTime at, bool active) =>
        id + name.Length + age + (long)balance + at.Ticks + (active ? 1 : 0);

    private static long WeighMaybe(long id, string? name, int? age, decimal? balance, DateTime? at, bool? flag) =>
        id + (name?.Length ?? 0) + (age ?? 0) + (long)(balance ?? 0) + (at?.Ticks ?? 0) + (flag == true ? 1 : 0);

    // The form of ValueReader<T>: one delegate per type, chosen when the type is first used.
    private static class DelegateForm<T>
    {
        public static readonly Func<DbDataReader, int, T> Read = Create();

        private static Func<DbDataReader, int, T> Create()
        {
            if (typeof(T) == typeof(long))
            {
                return As(static (r, i) => r.GetInt64(i));
            }

            if (typeof(T) == typeof(int))
            {
                return As(static (r, i) => r.GetInt32(i));
            }

            if (typeof(T) == typeof(int?))
            {
                return As(static (r, i) => r.IsDBNull(i) ? (int?)null : r.GetInt32(i));
            }

            if (typeof(T) == typeof(decimal))
            {
                return As(static (r, i) => r.GetDecimal(i));
            }

            if (typeof(T) == typeof(decimal?))
            {
                return As(static (r, i) => r.IsDBNull(i) ? (decimal?)null : r.GetDecimal(i));
            }

            if (typeof(T) == typeof(DateTime))
            {
                return As(static (r, i) => r.GetDateTime(i));
            }

            if (typeof(T) == typeof(DateTime?))
            {
                return As(static (r, i) => r.IsDBNull(i) ? (DateTime?)null : r.GetDateTime(i));
            }

            if (typeof(T) == typeof(bool))
            {
                return As(static (r, i) => r.GetBoolean(i));
            }

            if (typeof(T) == typeof(bool?))
            {
                return As(static (r, i) => r.IsDBNull(i) ? (bool?)null : r.GetBoolean(i));
            }

            return As(static (r, i) => r.IsDBNull(i) ? null : r.GetString(i));
        }

        private static Func<DbDataReader, int, T> As<TValue>(Func<DbDataReader, int, TValue> read) => (Func<DbDataReader, int, T>)(object)read;
    }

    // The other form: the tests are in the method; for a value type the JIT keeps only the branch of T.
    private static class BranchForm
    {
        public static T Read<T>(DbDataReader r, int i)
        {
            if (typeof(T) == typeof(long))
            {
                return (T)(object)r.GetInt64(i);
            }

            if (typeof(T) == typeof(int))
            {
                return (T)(object)r.GetInt32(i);
            }

            if (typeof(T) == typeof(int?))
            {
                return (T)(object)(r.IsDBNull(i) ? (int?)null : r.GetInt32(i))!;
            }

            if (typeof(T) == typeof(decimal))
            {
                return (T)(object)r.GetDecimal(i);
            }

            if (typeof(T) == typeof(decimal?))
            {
                return (T)(object)(r.IsDBNull(i) ? (decimal?)null : r.GetDecimal(i))!;
            }

            if (typeof(T) == typeof(DateTime))
            {
                return (T)(object)r.GetDateTime(i);
            }

            if (typeof(T) == typeof(DateTime?))
            {
                return (T)(object)(r.IsDBNull(i) ? (DateTime?)null : r.GetDateTime(i))!;
            }

            if (typeof(T) == typeof(bool))
            {
                return (T)(object)r.GetBoolean(i);
            }

            if (typeof(T) == typeof(bool?))
            {
                return (T)(object)(r.IsDBNull(i) ? (bool?)null : r.GetBoolean(i))!;
            }

            return (T)(object)(r.IsDBNull(i) ? null : r.GetString(i))!;
        }
    }
}
