using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

// Runs the benchmarks that the arguments select, or --check (benchmarks/README.md), for example:
//   dotnet run -c Release --project benchmarks/AuraORM.Benchmarks -- --filter "*ReadBenchmarks*"
if (args is ["--check"])
{
    return await AuraORM.Benchmarks.Check.RunAsync();
}

BenchmarkSwitcher
    .FromAssembly(typeof(AuraORM.Benchmarks.ReadBenchmarks).Assembly)
    .Run(args, DefaultConfig.Instance.WithOptions(ConfigOptions.JoinSummary).AddLogicalGroupRules(BenchmarkLogicalGroupRule.ByCategory));
return 0;
