using System;

using LicenseGatherer.Core;

namespace LicenseGatherer
{
    public class LicenseDependencyInformation
    {
        public LicenseDependencyInformation(InstalledPackageReference packageReference, string licenseContent,
            Uri originalLicenseLocation, Uri downloadedLicenseLocation)
        {
            PackageReference = packageReference ?? throw new ArgumentNullException(nameof(packageReference));
            LicenseContent = licenseContent ?? throw new ArgumentNullException(nameof(licenseContent));
            OriginalLicenseLocation = originalLicenseLocation ??
                                      throw new ArgumentNullException(nameof(originalLicenseLocation));
            DownloadedLicenseLocation = downloadedLicenseLocation ??
                                        throw new ArgumentNullException(nameof(downloadedLicenseLocation));
        }

        public InstalledPackageReference PackageReference { get; }

        public string LicenseContent { get; }

        public Uri OriginalLicenseLocation { get; }

        public Uri DownloadedLicenseLocation { get; }
    }
}
