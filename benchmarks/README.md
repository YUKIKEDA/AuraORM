# Benchmarks

The micro-benchmarks of row P1 (`docs/design.ja.md` §16.1, `.cursor/rules/bench.mdc`). CI builds them and does not run
them: shared runners are too noisy for a timing.

## Run

```powershell
# Check first that every library returns the same rows.
dotnet run -c Release --project benchmarks/AuraORM.Benchmarks -- --check

# Every benchmark, three processes each.
dotnet run -c Release --project benchmarks/AuraORM.Benchmarks -- --filter "*" --launchCount 3

# One class, with the EventPipe profiler (a speedscope file per benchmark in BenchmarkDotNet.Artifacts).
dotnet run -c Release --project benchmarks/AuraORM.Benchmarks -- --filter "*ReadBenchmarks*" --profiler EP
```

## What each class measures

| Class | Measures | Compared with |
| --- | --- | --- |
| `ReadBenchmarks` | Reading 1, 100, and 10000 rows from an in-memory reader whose getters do not allocate (`tests/AuraORM.Tests/Support/ArrayDataReader.cs`): six columns that do not allow null, and six of which five allow null and are NULL in every other row | A loop written by hand with the typed getters by ordinal (the baseline), Dapper, Dapper.AOT |
| `RenderBenchmarks` | Building a statement and rendering its SQL without a connection: a SELECT with WHERE, two joins with GROUP BY, an INSERT of one row, an UPDATE | linq2db (`ToSqlQuery`) and EF Core (`ToQueryString`) for the queries; neither renders an INSERT or UPDATE without running it. Dapper takes the SQL text as it is |
| `RoundTripBenchmarks` | A query of every row of a table of 1 and 100 rows on SQLite in memory, on one open connection | Hand-written ADO.NET (the baseline), Dapper, Dapper.AOT, linq2db, EF Core with and without a compiled query |
| `ValueReaderFormBenchmarks` | The two forms of reading one typed value that §16.1 names: a `static readonly` delegate per type, and the `typeof(T)` tests in the method | The typed getters called directly |

The in-memory reader and the fake connection are the files of the allocation test of requirement N4
(`tests/AuraORM.Tests/Reading/RowAllocationTests.cs`), linked into this project, so both read the same rows the same way.
