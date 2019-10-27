﻿using System;
using System.Globalization;
using System.Text;
#if !NETSTANDARD
using System.Runtime.Serialization;
using System.Security.Permissions;
#endif
using System.Text.RegularExpressions;

namespace Semver
{
    /// <summary>
    /// A semantic version implementation.
    /// Conforms with v2.0.0 of http://semver.org
    /// </summary>
#if NETSTANDARD
    public sealed class SemVersion : IComparable<SemVersion>, IComparable
#else
    [Serializable]
    public sealed class SemVersion : IComparable<SemVersion>, IComparable, ISerializable
#endif
    {
        private static readonly Regex ParseEx =
            new Regex(@"^(?<major>\d+)" +
                @"(\.(?<minor>\d+))?" +
                @"(\.(?<patch>\d+))?" +
                @"(\-(?<pre>[0-9A-Za-z\-\.]+))?" +
                @"(\+(?<metadata>[0-9A-Za-z\-\.]+))?$",
#if NETSTANDARD
                RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture);
#else
                RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.ExplicitCapture);
#endif

#if !NETSTANDARD
#pragma warning disable CA1801 // Parameter unused
        /// <summary>
        /// Deserialize a <see cref="SemVersion"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException">The <paramref name="info"/> parameter is null.</exception>
        private SemVersion(SerializationInfo info, StreamingContext context)
#pragma warning restore CA1801 // Parameter unused
        {
            if (info == null) throw new ArgumentNullException(nameof(info));
            var semVersion = Parse(info.GetString("SemVersion"));
            Major = semVersion.Major;
            Minor = semVersion.Minor;
            Patch = semVersion.Patch;
            Prerelease = semVersion.Prerelease;
            Metadata = semVersion.Metadata;
        }
#endif

        /// <summary>
        /// Constructs a new instance of the <see cref="SemVersion" /> class.
        /// </summary>
        /// <param name="major">The major version.</param>
        /// <param name="minor">The minor version.</param>
        /// <param name="patch">The patch version.</param>
        /// <param name="prerelease">The prerelease version (e.g. "alpha").</param>
        /// <param name="build">The build metadata (e.g. "nightly.232").</param>
        public SemVersion(int major, int minor = 0, int patch = 0, string prerelease = "", string build = "")
        {
            Major = major;
            Minor = minor;
            Patch = patch;

            Prerelease = prerelease ?? "";
            Metadata = build ?? "";
        }

        /// <summary>
        /// Constructs a new instance of the <see cref="SemVersion"/> class from
        /// a <see cref="System.Version"/>.
        /// </summary>
        /// <param name="version">The <see cref="Version"/> that is used to initialize
        /// the Major, Minor, and Patch versions and the build metadata.</param>
        /// <returns>A <see cref="SemVersion"/> with the same Major and Minor version.
        /// The Patch version will be the fourth component of the version number. The
        /// build meta data will contain the third component of the version number if
        /// it is greater than zero.</returns>
        [Obsolete("This constructor is obsolete. Call FromVersion instead.")]
        public SemVersion(Version version)
        {
            if (version == null)
                throw new ArgumentNullException(nameof(version));

            Major = version.Major;
            Minor = version.Minor;

            if (version.Revision >= 0)
                Patch = version.Revision;

            Prerelease = "";

            Metadata = version.Build > 0 ? version.Build.ToString(CultureInfo.InvariantCulture) : "";
        }

        #region System.Version
        /// <summary>
        /// Converts a <see cref="System.Version"/> into the equivalent semantic version.
        /// </summary>
        /// <param name="version">The version to be converted to a semantic version.</param>
        /// <returns>The equivalent semantic version.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="version"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="version"/> has a Revision greater than zero.</exception>
        /// <remarks>
        /// <see cref="System.Version"/> numbers have the form <em>major</em>.<em>minor</em>[.<em>build</em>[.<em>revision</em>]]
        /// where square brackets ('[' and ']')  indicate optional components. The first three parts
        /// are converted to the Major, Minor, and Patch versions of a semantic version. If the build component
        /// is not defined (-1), the Patch number is assumed to be zero. <see cref="System.Version"/> numbers
        /// with a revision component greater than zero cannot be converted to semantic versions. An
        /// <see cref="ArgumentException"/> is thrown when this method is called with such a <see cref="Version"/>.
        /// </remarks>
        public static SemVersion FromVersion(Version version)
        {
            if (version == null) throw new ArgumentNullException(nameof(version));
            if (version.Revision > 0) throw new ArgumentException("Version with Revision number can't be converted to SemVer.", nameof(version));
            var patch = version.Build > 0 ? version.Build : 0;
            return new SemVersion(version.Major, version.Minor, patch);
        }

        /// <summary>
        /// Converts this semantic version to a <see cref="System.Version"/>.
        /// </summary>
        /// <returns>The equivalent <see cref="System.Version"/>.</returns>
        /// <remarks>
        /// A semantic version of the form <em>major</em>.<em>minor</em>.<em>patch</em>
        /// is converted to a <see cref="System.Version"/> of the form
        /// <em>major</em>.<em>minor</em>.<em>build</em> where the build number is the
        /// patch version of the semantic version. Prerelease versions and build metadata
        /// are not representable in a <see cref="System.Version"/>. This method throws
        /// an <see cref="InvalidOperationException"/> if the semantic version is a
        /// prerelease version or has build metadata.
        /// </remarks>
        /// <exception cref="InvalidOperationException">The semantic version is a prerelease version
        /// or has build metadata or has a negative major, minor, or patch version.</exception>
        public Version ToVersion()
        {
            if (Major < 0 || Minor < 0 || Patch < 0) throw new InvalidOperationException("Negative version numbers can't be converted to System.Version.");
            if (IsPrerelease) throw new InvalidOperationException("Prerelease version can't be converted to System.Version.");
            if (Metadata.Length != 0) throw new InvalidOperationException("Version with build metadata can't be converted to System.Version.");

            return new Version(Major, Minor, Patch);
        }
        #endregion

        /// <summary>
        /// Converts the string representation of a semantic version to its <see cref="SemVersion"/> equivalent.
        /// </summary>
        /// <param name="version">The version string.</param>
        /// <param name="strict">If set to <see langword="true"/> minor and patch version are required,
        /// otherwise they are optional.</param>
        /// <returns>The <see cref="SemVersion"/> object.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="version"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">The <paramref name="version"/> has an invalid format.</exception>
        /// <exception cref="InvalidOperationException">The <paramref name="version"/> is missing Minor or Patch versions and <paramref name="strict"/> is <see langword="true"/>.</exception>
        /// <exception cref="OverflowException">The Major, Minor, or Patch versions are larger than <code>int.MaxValue</code>.</exception>
        public static SemVersion Parse(string version, bool strict = false)
        {
            var match = ParseEx.Match(version);
            if (!match.Success)
                throw new ArgumentException("Invalid version.", nameof(version));

            var major = int.Parse(match.Groups["major"].Value, CultureInfo.InvariantCulture);

            var minorMatch = match.Groups["minor"];
            int minor = 0;
            if (minorMatch.Success)
                minor = int.Parse(minorMatch.Value, CultureInfo.InvariantCulture);
            else if (strict)
                throw new InvalidOperationException("Invalid version (no minor version given in strict mode)");

            var patchMatch = match.Groups["patch"];
            int patch = 0;
            if (patchMatch.Success)
                patch = int.Parse(patchMatch.Value, CultureInfo.InvariantCulture);
            else if (strict)
                throw new InvalidOperationException("Invalid version (no patch version given in strict mode)");

            var prerelease = match.Groups["pre"].Value;
            var metadata = match.Groups["metadata"].Value;

            return new SemVersion(major, minor, patch, prerelease, metadata);
        }

        /// <summary>
        /// Converts the string representation of a semantic version to its <see cref="SemVersion"/>
        /// equivalent and returns a value that indicates whether the conversion succeeded.
        /// </summary>
        /// <param name="version">The version string.</param>
        /// <param name="semver">When the method returns, contains a <see cref="SemVersion"/> instance equivalent
        /// to the version string passed in, if the version string was valid, or <see langword="null"/> if the
        /// version string was not valid.</param>
        /// <param name="strict">If set to <see langword="true"/> minor and patch version are required,
        /// otherwise they are optional.</param>
        /// <returns><see langword="false"/> when a invalid version string is passed, otherwise <see langword="true"/>.</returns>
        public static bool TryParse(string version, out SemVersion semver, bool strict = false)
        {
            semver = null;
            if (version is null) return false;

            var match = ParseEx.Match(version);
            if (!match.Success) return false;

            if (!int.TryParse(match.Groups["major"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var major))
                return false;

            var minorMatch = match.Groups["minor"];
            int minor = 0;
            if (minorMatch.Success)
            {
                if (!int.TryParse(minorMatch.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out minor))
                    return false;
            }
            else if (strict) return false;

            var patchMatch = match.Groups["patch"];
            int patch = 0;
            if (patchMatch.Success)
            {
                if (!int.TryParse(patchMatch.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out patch))
                    return false;
            }
            else if (strict) return false;

            var prerelease = match.Groups["pre"].Value;
            var metadata = match.Groups["metadata"].Value;

            semver = new SemVersion(major, minor, patch, prerelease, metadata);
            return true;
        }

        /// <summary>
        /// Checks whether two semantic versions are equal.
        /// </summary>
        /// <param name="versionA">The first version to compare.</param>
        /// <param name="versionB">The second version to compare.</param>
        /// <returns><see langword="true"/> if the two values are equal, otherwise <see langword="false"/>.</returns>
        public static bool Equals(SemVersion versionA, SemVersion versionB)
        {
            if (ReferenceEquals(versionA, versionB)) return true;
            if (versionA is null || versionB is null) return false;
            return versionA.Equals(versionB);
        }

        /// <summary>
        /// Compares the specified versions.
        /// </summary>
        /// <param name="versionA">The first version to compare.</param>
        /// <param name="versionB">The second version to compare.</param>
        /// <returns>A signed number indicating the relative values of <paramref name="versionA"/> and <paramref name="versionB"/>.</returns>
        public static int Compare(SemVersion versionA, SemVersion versionB)
        {
            if (ReferenceEquals(versionA, versionB)) return 0;
            if (versionA is null) return -1;
            if (versionB is null) return 1;
            return versionA.CompareTo(versionB);
        }

        /// <summary>
        /// Make a copy of the current instance with changed properties.
        /// </summary>
        /// <param name="major">The value to replace the major version or <see langword="null"/> to leave it unchanged.</param>
        /// <param name="minor">The value to replace the minor version or <see langword="null"/> to leave it unchanged.</param>
        /// <param name="patch">The value to replace the patch version or <see langword="null"/> to leave it unchanged.</param>
        /// <param name="prerelease">The value to replace the prerelease version or <see langword="null"/> to leave it unchanged.</param>
        /// <param name="build">The value to replace the build metadata or <see langword="null"/> to leave it unchanged.</param>
        /// <returns>The new version object.</returns>
        /// <remarks>
        /// The change method is intended to be called using named argument syntax, passing only
        /// those fields to be changed.
        /// </remarks>
        /// <example>
        /// To change only the patch version:
        /// <code>version.Change(patch: 4)</code>
        /// </example>
        public SemVersion Change(int? major = null, int? minor = null, int? patch = null,
            string prerelease = null, string build = null)
        {
            return new SemVersion(
                major ?? Major,
                minor ?? Minor,
                patch ?? Patch,
                prerelease ?? Prerelease,
                build ?? Metadata);
        }

        /// <summary>
        /// Gets the major version.
        /// </summary>
        /// <value>
        /// The major version.
        /// </value>
        public int Major { get; }

        /// <summary>
        /// Gets the minor version.
        /// </summary>
        /// <value>
        /// The minor version.
        /// </value>
        public int Minor { get; }

        /// <summary>
        /// Gets the patch version.
        /// </summary>
        /// <value>
        /// The patch version.
        /// </value>
        public int Patch { get; }

        /// <summary>
        /// Gets the prerelease label of this semantic version.
        /// </summary>
        /// <value>
        /// The prerelease label or empty string if this is a release version.
        /// </value>
        /// <remarks>
        /// A prerelease version label follows the main version number separated
        /// by a dash ('-'). It is a series of identifiers each of which may either
        /// be alphanumeric or numeric. Prerelease versions have lower precedence
        /// than release versions.
        /// </remarks>
        public string Prerelease { get; }

        /// <summary>
        /// Indicates whether this semantic version is a prerelease version.
        /// </summary>
        /// <value><see langword="true"/> if the <see cref="Prerelease"/> property
        /// is non-empty; <see langword="false"/> if it is empty.</value>
        public bool IsPrerelease => Prerelease.Length != 0;

        /// <summary>
        /// Gets the build metadata.
        /// </summary>
        /// <value>
        /// The build metadata or empty string if there is no build metadata.
        /// </value>
        [Obsolete("This property is obsolete. Use Metadata instead.")]
        public string Build => Metadata;

        /// <summary>
        /// Gets the build metadata of this semantic version.
        /// </summary>
        /// <value>The build metadata of this version or empty string if there
        /// is no metadata.</value>
        /// <remarks>
        /// The build metadata is a series of dot separated alphanumeric identifiers separated
        /// from the rest of the version number with a plus sign ('+').
        ///
        /// The metadata does not affect precedence. Two versions with different
        /// build metadata have the same precedence. However, metadata does affect
        /// sort order. A version without metadata sorts before one that has metadata.
        /// </remarks>
        public string Metadata { get; }

        /// <summary>
        /// Returns the <see cref="string" /> equivalent of this version.
        /// </summary>
        /// <returns>
        /// The <see cref="string" /> equivalent of this version.
        /// </returns>
        public override string ToString()
        {
            // Assume all separators ("..-+"), at most 2 extra chars
            var estimatedLength = 4 + Major.Digits() + Minor.Digits() + Patch.Digits()
                                  + Prerelease.Length + Metadata.Length;
            var version = new StringBuilder(estimatedLength);
            version.Append(Major);
            version.Append('.');
            version.Append(Minor);
            version.Append('.');
            version.Append(Patch);
            if (Prerelease.Length > 0)
            {
                version.Append('-');
                version.Append(Prerelease);
            }
            if (Metadata.Length > 0)
            {
                version.Append('+');
                version.Append(Metadata);
            }
            return version.ToString();
        }

        /// <summary>
        /// Compares the current instance with another object of the same type and returns an integer that indicates
        /// whether the current instance precedes, follows, or occurs in the same position in the sort order as the
        /// other object.
        /// </summary>
        /// <param name="obj">An object to compare with this instance.</param>
        /// <returns>
        /// A value that indicates the relative order of the objects being compared.
        /// The return value has these meanings:
        ///  Less than zero: This instance precedes <paramref name="obj" /> in the sort order.
        ///  Zero: This instance occurs in the same position in the sort order as <paramref name="obj" />.
        ///  Greater than zero: This instance follows <paramref name="obj" /> in the sort order.
        /// </returns>
        /// <exception cref="InvalidCastException">The <paramref name="obj"/> is not a <see cref="SemVersion"/>.</exception>
        public int CompareTo(object obj)
        {
            return CompareTo((SemVersion)obj);
        }

        /// <summary>
        /// Compares the current instance with another object of the same type and returns an integer that indicates
        /// whether the current instance precedes, follows, or occurs in the same position in the sort order as the
        /// other object.
        /// </summary>
        /// <param name="other">An object to compare with this instance.</param>
        /// <returns>
        /// A value that indicates the relative order of the objects being compared.
        /// The return value has these meanings:
        ///  Less than zero: This instance precedes <paramref name="other" /> in the sort order.
        ///  Zero: This instance occurs in the same position in the sort order as <paramref name="other" />.
        ///  Greater than zero: This instance follows <paramref name="other" /> in the sort order.
        /// </returns>
        public int CompareTo(SemVersion other)
        {
            var r = CompareByPrecedence(other);
            if (r != 0) return r;

#pragma warning disable CA1062 // Validate arguments of public methods
            // If other is null, CompareByPrecedence() returns 1
            return CompareComponent(Metadata, other.Metadata);
#pragma warning restore CA1062 // Validate arguments of public methods
        }

        /// <summary>
        /// Returns whether two semantic versions have the same precedence. Versions
        /// that differ only by build metadata have the same precedence.
        /// </summary>
        /// <param name="other">The semantic version to compare to.</param>
        /// <returns><see langword="true"/> if the version precedences are equal.</returns>
        public bool PrecedenceMatches(SemVersion other)
        {
            return CompareByPrecedence(other) == 0;
        }

        /// <summary>
        /// Compares two semantic versions by precedence as defined in the SemVer spec. Versions
        /// that differ only by build metadata have the same precedence.
        /// </summary>
        /// <param name="other">The semantic version.</param>
        /// <returns>
        /// A value that indicates the relative order of the objects being compared.
        /// The return value has these meanings:
        ///  Less than zero: This instance precedes <paramref name="other" /> in the sort order.
        ///  Zero: This instance occurs in the same position in the sort order as <paramref name="other" />.
        ///  Greater than zero: This instance follows <paramref name="other" /> in the sort order.
        /// </returns>
        public int CompareByPrecedence(SemVersion other)
        {
            if (other is null)
                return 1;

            var r = Major.CompareTo(other.Major);
            if (r != 0) return r;

            r = Minor.CompareTo(other.Minor);
            if (r != 0) return r;

            r = Patch.CompareTo(other.Patch);
            if (r != 0) return r;

            return CompareComponent(Prerelease, other.Prerelease, true);
        }

        private static int CompareComponent(string a, string b, bool nonemptyIsLower = false)
        {
            var aEmpty = string.IsNullOrEmpty(a);
            var bEmpty = string.IsNullOrEmpty(b);
            if (aEmpty && bEmpty)
                return 0;

            if (aEmpty)
                return nonemptyIsLower ? 1 : -1;
            if (bEmpty)
                return nonemptyIsLower ? -1 : 1;

            var aComps = a.Split('.');
            var bComps = b.Split('.');

            var minLen = Math.Min(aComps.Length, bComps.Length);
            for (int i = 0; i < minLen; i++)
            {
                var ac = aComps[i];
                var bc = bComps[i];
                var aIsNum = int.TryParse(ac, out var aNum);
                var bIsNum = int.TryParse(bc, out var bNum);
                int r;
                if (aIsNum && bIsNum)
                {
                    r = aNum.CompareTo(bNum);
                    if (r != 0) return r;
                }
                else
                {
                    if (aIsNum)
                        return -1;
                    if (bIsNum)
                        return 1;
                    r = string.CompareOrdinal(ac, bc);
                    if (r != 0)
                        return r;
                }
            }

            return aComps.Length.CompareTo(bComps.Length);
        }

        /// <summary>
        /// Determines whether the specified <see cref="object" /> is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="object" /> to compare with this instance.</param>
        /// <returns>
        ///   <see langword="true"/> if the specified <see cref="object" /> is equal to this instance, otherwise <see langword="false"/>.
        /// </returns>
        /// <exception cref="InvalidCastException">The <paramref name="obj"/> is not a <see cref="SemVersion"/>.</exception>
        public override bool Equals(object obj)
        {
            if (obj is null)
                return false;

            if (ReferenceEquals(this, obj))
                return true;

            var other = (SemVersion)obj;

            return Major == other.Major
                && Minor == other.Minor
                && Patch == other.Patch
                && string.Equals(Prerelease, other.Prerelease, StringComparison.Ordinal)
                && string.Equals(Metadata, other.Metadata, StringComparison.Ordinal);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.
        /// </returns>
        public override int GetHashCode()
        {
            unchecked
            {
                // TODO verify this. Some versions start result = 17. Some use 37 instead of 31
                int result = Major.GetHashCode();
                result = result * 31 + Minor.GetHashCode();
                result = result * 31 + Patch.GetHashCode();
                result = result * 31 + Prerelease.GetHashCode();
                result = result * 31 + Metadata.GetHashCode();
                return result;
            }
        }

#if !NETSTANDARD
        /// <summary>
        /// Populates a <see cref="SerializationInfo"/> with the data needed to serialize the target object.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> to populate with data.</param>
        /// <param name="context">The destination (see <see cref="SerializationInfo"/>) for this serialization.</param>
        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null) throw new ArgumentNullException(nameof(info));
            info.AddValue("SemVersion", ToString());
        }
#endif

#pragma warning disable CA2225 // Operator overloads have named alternates
        /// <summary>
        /// Implicit conversion from <see cref="string"/> to <see cref="SemVersion"/>.
        /// </summary>
        /// <param name="version">The semantic version.</param>
        /// <returns>The <see cref="SemVersion"/> object.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="version"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">The version number has an invalid format.</exception>
        /// <exception cref="OverflowException">The Major, Minor, or Patch versions are larger than <code>int.MaxValue</code>.</exception>
        public static implicit operator SemVersion(string version)
#pragma warning restore CA2225 // Operator overloads have named alternates
        {
            return Parse(version);
        }

        /// <summary>
        /// Compares two semantic versions for equality.
        /// </summary>
        /// <param name="left">The left value.</param>
        /// <param name="right">The right value.</param>
        /// <returns>If left is equal to right <see langword="true"/>, otherwise <see langword="false"/>.</returns>
        public static bool operator ==(SemVersion left, SemVersion right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Compares two semantic versions for inequality.
        /// </summary>
        /// <param name="left">The left value.</param>
        /// <param name="right">The right value.</param>
        /// <returns>If left is not equal to right <see langword="true"/>, otherwise <see langword="false"/>.</returns>
        public static bool operator !=(SemVersion left, SemVersion right)
        {
            return !Equals(left, right);
        }

        /// <summary>
        /// Compares two semantic versions.
        /// </summary>
        /// <param name="left">The left value.</param>
        /// <param name="right">The right value.</param>
        /// <returns>If left is greater than right <see langword="true"/>, otherwise <see langword="false"/>.</returns>
        public static bool operator >(SemVersion left, SemVersion right)
        {
            return Compare(left, right) > 0;
        }

        /// <summary>
        /// Compares two semantic versions.
        /// </summary>
        /// <param name="left">The left value.</param>
        /// <param name="right">The right value.</param>
        /// <returns>If left is greater than or equal to right <see langword="true"/>, otherwise <see langword="false"/>.</returns>
        public static bool operator >=(SemVersion left, SemVersion right)
        {
            return Equals(left, right) || Compare(left, right) > 0;
        }

        /// <summary>
        /// Compares two semantic versions.
        /// </summary>
        /// <param name="left">The left value.</param>
        /// <param name="right">The right value.</param>
        /// <returns>If left is less than right <see langword="true"/>, otherwise <see langword="false"/>.</returns>
        public static bool operator <(SemVersion left, SemVersion right)
        {
            return Compare(left, right) < 0;
        }

        /// <summary>
        /// Compares two semantic versions.
        /// </summary>
        /// <param name="left">The left value.</param>
        /// <param name="right">The right value.</param>
        /// <returns>If left is less than or equal to right <see langword="true"/>, otherwise <see langword="false"/>.</returns>
        public static bool operator <=(SemVersion left, SemVersion right)
        {
            return Equals(left, right) || Compare(left, right) < 0;
        }
    }
}
