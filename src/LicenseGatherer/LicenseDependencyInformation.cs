using System;
using LicenseGatherer.Core;
using NuGet.Packaging.Licenses;

namespace LicenseGatherer
{
    public class LicenseDependencyInformation
    {
        public LicenseDependencyInformation(InstalledPackageReference packageReference, string licenseContent,
            Uri? originalLicenseLocation, Uri? downloadedLicenseLocation, NuGetLicenseExpression licenseExpression, string authors)
        {
            if (string.IsNullOrEmpty(authors))
            {
                throw new ArgumentException("Value cannot be null or empty.", nameof(authors));
            }

            PackageReference = packageReference ?? throw new ArgumentNullException(nameof(packageReference));
            LicenseContent = licenseContent;
            OriginalLicenseLocation = originalLicenseLocation;
            DownloadedLicenseLocation = downloadedLicenseLocation;
            LicenseExpression = licenseExpression ?? throw new ArgumentNullException(nameof(licenseExpression));
            Authors = authors;
        }

        public InstalledPackageReference PackageReference { get; }

        public string LicenseContent { get; }

        public Uri? OriginalLicenseLocation { get; }

        public Uri? DownloadedLicenseLocation { get; }

        public NuGetLicenseExpression LicenseExpression { get; }

        public string Authors { get; }
    }
}
