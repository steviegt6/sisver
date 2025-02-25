using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Semver.Ranges;

namespace Semver.Benchmarks.RangeBenchmarks.Npm
{
    [SimpleJob(RuntimeMoniker.Net461)]
    [SimpleJob(RuntimeMoniker.NetCoreApp21)]
    [SimpleJob(RuntimeMoniker.NetCoreApp31)]
    public abstract class NpmRangeParsing
    {
        protected const int RangeCount = 1_000;
        private readonly IReadOnlyList<string> ranges;

        protected abstract IReadOnlyList<string> GetRanges();

        protected NpmRangeParsing()
        {
            ranges = GetRanges();
        }

        [Benchmark(OperationsPerInvoke = RangeCount)]
        [Arguments(false)]
        [Arguments(true)]
        public long Parse(bool includePrerelease)
        {
            // The accumulator ensures the versions aren't dead code with minimal overhead
            long accumulator = 0;
            for (int i = 0; i < RangeCount; i++)
            {
                var range = SemVersionRange.ParseNpm(ranges[i], includePrerelease);
                accumulator += range.Count;
            }
            return accumulator;
        }
    }
}