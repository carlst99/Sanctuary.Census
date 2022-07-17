using BenchmarkDotNet.Running;

#pragma warning disable CS1591

namespace Sanctuary.Census.Benchmarks;

public static class Program
{
    public static void Main()
        => BenchmarkRunner.Run<ObjectShaperBenchmarks>();
}
