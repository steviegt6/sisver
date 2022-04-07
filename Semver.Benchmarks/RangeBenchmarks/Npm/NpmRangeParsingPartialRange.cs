using System;
using System.Collections.Generic;
using Semver.Benchmarks.Builders;
using Semver.Utility;

namespace Semver.Benchmarks.RangeBenchmarks.Npm
{
    public class NpmRangeParsingPartialRange : NpmRangeParsing
    {
        protected override IReadOnlyList<string> GetRanges()
        {
            var random = new Random();

            return Enumerables.Generate(NumRanges, () => random.RandomPartialVersion(prependOperator: true)).ToReadOnlyList();
        }
    }
}