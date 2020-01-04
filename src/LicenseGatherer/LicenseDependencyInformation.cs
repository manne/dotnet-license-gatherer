using System;

using LicenseGatherer.Core;
using NuGet.Packaging.Licenses;

namespace LicenseGatherer
{
    public class LicenseDependencyInformation
    {
        public LicenseDependencyInformation(InstalledPackageReference packageReference, string licenseContent,
            Uri originalLicenseLocation, Uri downloadedLicenseLocation, NuGetLicenseExpression licenseExpression)
        {
            PackageReference = packageReference ?? throw new ArgumentNullException(nameof(packageReference));
            LicenseContent = licenseContent ?? throw new ArgumentNullException(nameof(licenseContent));
            OriginalLicenseLocation = originalLicenseLocation ?? throw new ArgumentNullException(nameof(originalLicenseLocation));
            DownloadedLicenseLocation = downloadedLicenseLocation ?? throw new ArgumentNullException(nameof(downloadedLicenseLocation));
            LicenseExpression = licenseExpression ?? throw new ArgumentNullException(nameof(licenseExpression));
        }

        public InstalledPackageReference PackageReference { get; }

        public string LicenseContent { get; }

        public Uri OriginalLicenseLocation { get; }

        public Uri DownloadedLicenseLocation { get; }

        public NuGetLicenseExpression LicenseExpression { get; }
    }
}
