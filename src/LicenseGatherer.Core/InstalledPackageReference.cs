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

        public bool Equals(InstalledPackageReference other)
        {
            return string.Equals(Name, other?.Name) && ResolvedVersion.Equals(other?.ResolvedVersion);
        }
    }
}
