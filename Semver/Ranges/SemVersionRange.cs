﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Semver.Comparers;
using Semver.Ranges.Parsers;
using Semver.Utility;

namespace Semver.Ranges
{
    /// <summary>
    /// A range of <see cref="SemVersion"/> values. A range can have gaps in it and may include only
    /// some prerelease versions between included release versions. For a range that cannot have
    /// gaps see the <see cref="UnbrokenSemVersionRange"/> class.
    /// </summary>
    public sealed class SemVersionRange : IReadOnlyList<UnbrokenSemVersionRange>, IEquatable<SemVersionRange>
    {
        internal const int MaxRangeLength = 2048;
        internal const string InvalidOptionsMessage = "An invalid SemVersionRangeOptions value was used.";
        internal const string InvalidMaxLengthMessage = "Must not be negative.";

        public static SemVersionRange Empty { get; } = new SemVersionRange(ReadOnlyList<UnbrokenSemVersionRange>.Empty);

        public static SemVersionRange AllRelease { get; } = new SemVersionRange(UnbrokenSemVersionRange.AllRelease);
        public static SemVersionRange All { get; } = new SemVersionRange(UnbrokenSemVersionRange.All);

        public static SemVersionRange Equals(SemVersion version)
            => Create(UnbrokenSemVersionRange.Equals(version));

        public static SemVersionRange GreaterThan(SemVersion version, bool includeAllPrerelease = false)
            => Create(UnbrokenSemVersionRange.GreaterThan(version, includeAllPrerelease));

        public static SemVersionRange AtLeast(SemVersion version, bool includeAllPrerelease = false)
             => Create(UnbrokenSemVersionRange.AtLeast(version, includeAllPrerelease));

        public static SemVersionRange LessThan(SemVersion version, bool includeAllPrerelease = false)
             => Create(UnbrokenSemVersionRange.LessThan(version, includeAllPrerelease));

        public static SemVersionRange AtMost(SemVersion version, bool includeAllPrerelease = false)
            => Create(UnbrokenSemVersionRange.AtMost(version, includeAllPrerelease));

        public static SemVersionRange Inclusive(SemVersion start, SemVersion end, bool includeAllPrerelease = false)
            => Create(UnbrokenSemVersionRange.Inclusive(start, end, includeAllPrerelease));

        public static SemVersionRange InclusiveOfStart(SemVersion start, SemVersion end, bool includeAllPrerelease = false)
            => Create(UnbrokenSemVersionRange.InclusiveOfStart(start, end, includeAllPrerelease));

        public static SemVersionRange InclusiveOfEnd(SemVersion start, SemVersion end, bool includeAllPrerelease = false)
            => Create(UnbrokenSemVersionRange.InclusiveOfEnd(start, end, includeAllPrerelease));

        public static SemVersionRange Exclusive(SemVersion start, SemVersion end, bool includeAllPrerelease = false)
            => Create(UnbrokenSemVersionRange.Exclusive(start, end, includeAllPrerelease));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static SemVersionRange Create(UnbrokenSemVersionRange range)
            => UnbrokenSemVersionRange.Empty.Equals(range) ? Empty : new SemVersionRange(range);

        /// <remarks>Ownership of the <paramref name="ranges"/> list must be given to this method.
        /// The list will be mutated and used as the basis of an immutable list. It must not still
        /// be referenced by the caller.</remarks>
        internal static SemVersionRange Create(List<UnbrokenSemVersionRange> ranges)
        {
            DebugChecks.IsNotNull(ranges, nameof(ranges));

            // Remove empty ranges and see if the result is empty
            ranges.RemoveAll(range => UnbrokenSemVersionRange.Empty.Equals(range));
            if (ranges.Count == 0) return Empty;

            // Sort and merge ranges
            ranges.Sort(UnbrokenSemVersionRangeComparer.Instance);
            for (var i = 0; i < ranges.Count - 1; i++)
                for (var j = ranges.Count - 1; j > i; j--)
                    if (ranges[i].TryUnion(ranges[j], out var union))
                    {
                        ranges.RemoveAt(j);
                        ranges[i] = union;
                    }

            return new SemVersionRange(ranges.AsReadOnly());
        }

        public static SemVersionRange Create(IEnumerable<UnbrokenSemVersionRange> ranges)
            => Create((ranges ?? throw new ArgumentNullException(nameof(ranges))).ToList());

        public static SemVersionRange Create(params UnbrokenSemVersionRange[] ranges)
            => Create((ranges ?? throw new ArgumentNullException(nameof(ranges))).ToList());

        private readonly IReadOnlyList<UnbrokenSemVersionRange> ranges;

        /// <remarks>Parameter validation is not performed. The unbroken range must not be empty.</remarks>
        private SemVersionRange(UnbrokenSemVersionRange range)
            : this(new List<UnbrokenSemVersionRange>(1) { range }.AsReadOnly())
        {
        }

        /// <remarks>Parameter validation is not performed. The <paramref name="ranges"/> must be
        /// an immutable list of properly ordered and combined ranges.</remarks>
        private SemVersionRange(IReadOnlyList<UnbrokenSemVersionRange> ranges)
        {
            this.ranges = ranges;
        }

        public bool Contains(SemVersion version) => ranges.Any(r => r.Contains(version));

        public static implicit operator Predicate<SemVersion>(SemVersionRange range)
            => range.Contains;

        #region Standard Parsing
#pragma warning disable RS0026 // Do not add multiple public overloads with optional parameters
        public static SemVersionRange Parse(
            string range,
            SemVersionRangeOptions options,
            int maxLength = MaxRangeLength)
#pragma warning restore RS0026 // Do not add multiple public overloads with optional parameters
        {
            if (!options.IsValid())
                throw new ArgumentException(InvalidOptionsMessage, nameof(options));
            if (maxLength < 0)
                throw new ArgumentOutOfRangeException(nameof(maxLength), maxLength, InvalidMaxLengthMessage);

            var ex = StandardRangeParser.Parse(range, options, null, maxLength, out var semverRange);
            if (ex != null) throw ex;
            return semverRange;
        }

#pragma warning disable RS0026 // Do not add multiple public overloads with optional parameters
        public static SemVersionRange Parse(
            string range,
            int maxLength = MaxRangeLength)
#pragma warning restore RS0026 // Do not add multiple public overloads with optional parameters
            => Parse(range, SemVersionRangeOptions.Strict, maxLength);

#pragma warning disable RS0026 // Do not add multiple public overloads with optional parameters
        public static bool TryParse(
            string range,
            SemVersionRangeOptions options,
            out SemVersionRange semverRange,
            int maxLength = MaxRangeLength)
#pragma warning restore RS0026 // Do not add multiple public overloads with optional parameters
        {
            if (!options.IsValid()) throw new ArgumentException(InvalidOptionsMessage, nameof(options));
            if (maxLength < 0)
                throw new ArgumentOutOfRangeException(nameof(maxLength), maxLength, InvalidMaxLengthMessage);

            var exception = StandardRangeParser.Parse(range, options, Parsing.FailedException, maxLength, out semverRange);

            DebugChecks.IsNotFailedException(exception, nameof(SemVersionParser), nameof(SemVersionParser.Parse));

            return exception is null;
        }

#pragma warning disable RS0026 // Do not add multiple public overloads with optional parameters
        public static bool TryParse(
            string range,
            out SemVersionRange semverRange,
            int maxLength = MaxRangeLength)
#pragma warning restore RS0026 // Do not add multiple public overloads with optional parameters
            => TryParse(range, SemVersionRangeOptions.Strict, out semverRange, maxLength);
        #endregion

        #region npm Parsing
        /// <summary>
        /// Parse a range string following the npm range rules into a <see cref="SemVersionRange"/>.
        /// </summary>
        /// <remarks>The npm "loose" option is not supported.</remarks>
#pragma warning disable RS0026 // Do not add multiple public overloads with optional parameters
        public static SemVersionRange ParseNpm(
#pragma warning restore RS0026 // Do not add multiple public overloads with optional parameters
            string range,
            bool includeAllPrerelease,
            int maxLength = MaxRangeLength)
        {
            if (maxLength < 0)
                throw new ArgumentOutOfRangeException(nameof(maxLength), maxLength, InvalidMaxLengthMessage);

            var ex = NpmRangeParser.Parse(range, includeAllPrerelease, null, maxLength, out var semverRange);
            if (ex != null) throw ex;
            return semverRange;
        }

#pragma warning disable RS0026 // Do not add multiple public overloads with optional parameters
        public static SemVersionRange ParseNpm(string range, int maxLength = MaxRangeLength)
#pragma warning restore RS0026 // Do not add multiple public overloads with optional parameters
            => ParseNpm(range, false, maxLength);

#pragma warning disable RS0026 // Do not add multiple public overloads with optional parameters
        public static bool TryParseNpm(
#pragma warning restore RS0026 // Do not add multiple public overloads with optional parameters
            string range,
            bool includeAllPrerelease,
            out SemVersionRange semverRange,
            int maxLength = MaxRangeLength)
        {
            if (maxLength < 0)
                throw new ArgumentOutOfRangeException(nameof(maxLength), maxLength, InvalidMaxLengthMessage);

            var exception = NpmRangeParser.Parse(range, includeAllPrerelease, Parsing.FailedException, maxLength, out semverRange);

            DebugChecks.IsNotFailedException(exception, nameof(NpmRangeParser), nameof(NpmRangeParser.Parse));

            return exception is null;
        }

#pragma warning disable RS0026 // Do not add multiple public overloads with optional parameters
        public static bool TryParseNpm(
#pragma warning restore RS0026 // Do not add multiple public overloads with optional parameters
            string range,
            out SemVersionRange semverRange,
            int maxLength = MaxRangeLength)
            => TryParseNpm(range, false, out semverRange, maxLength);
        #endregion

        #region IReadOnlyList<UnbrokenSemVersionRange>
        public int Count => ranges.Count;

        public UnbrokenSemVersionRange this[int index] => ranges[index];

        public IEnumerator<UnbrokenSemVersionRange> GetEnumerator() => ranges.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        #endregion

        #region Equality
        public bool Equals(SemVersionRange other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return ranges.SequenceEqual(other.ranges);
        }

        public override bool Equals(object obj)
            => obj is SemVersionRange other && Equals(other);

        public override int GetHashCode() => CombinedHashCode.CreateForItems(ranges);

        public static bool operator ==(SemVersionRange left, SemVersionRange right)
            => Equals(left, right);

        public static bool operator !=(SemVersionRange left, SemVersionRange right)
            => !Equals(left, right);
        #endregion

        public override string ToString() => string.Join(" || ", this);
    }
}
