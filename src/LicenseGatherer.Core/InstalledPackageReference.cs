using System;
using NuGet.Versioning;

namespace LicenseGatherer.Core
{
    public class InstalledPackageReference : IEquatable<InstalledPackageReference>
    {
        public string Name { get; }

        public NuGetVersion ResolvedVersion { get; }

        public InstalledPackageReference(string name, NuGetVersion resolvedVersion)
        {
            Name = name;
            ResolvedVersion = resolvedVersion;
        }

        public bool Equals(InstalledPackageReference? other)
        {
            return string.Equals(Name, other?.Name, StringComparison.Ordinal) && ResolvedVersion.Equals(other?.ResolvedVersion);
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as InstalledPackageReference);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name, ResolvedVersion);
        }

        public static bool operator ==(InstalledPackageReference? left, InstalledPackageReference? right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(InstalledPackageReference? left, InstalledPackageReference? right)
        {
            return !Equals(left, right);
        }
    }
}
