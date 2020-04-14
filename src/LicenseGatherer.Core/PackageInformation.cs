using System;
using NuGet.Packaging.Licenses;

namespace LicenseGatherer.Core
{
    public sealed class PackageInformation
    {
        public PackageInformation(Uri? licenseLocation, NuGetLicenseExpression licenseExpression, string authors)
        {
            LicenseLocation = licenseLocation;
            LicenseExpression = licenseExpression;
            Authors = authors;
        }

        public Uri? LicenseLocation { get; }

        public NuGetLicenseExpression LicenseExpression { get; }

        public string Authors { get; }

        public void Deconstruct(out Uri? licenseLocation, out NuGetLicenseExpression licenseExpression, out string authors)
        {
            licenseLocation = LicenseLocation;
            licenseExpression = LicenseExpression;
            authors = Authors;
        }
    }
}
