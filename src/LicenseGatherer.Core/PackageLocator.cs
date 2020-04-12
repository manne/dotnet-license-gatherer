using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using NuGet.Packaging.Licenses;
using NuGet.Protocol;

namespace LicenseGatherer.Core
{
    public class PackageLocator
    {
        private readonly ILogger<PackageLocator> _logger;

        public PackageLocator(ILogger<PackageLocator> logger)
        {
            _logger = logger;
        }

        public IImmutableDictionary<InstalledPackageReference, PackageInformation> Provide(IImmutableDictionary<InstalledPackageReference, LocalPackageInfo?> packages)
        {
            var result = new Dictionary<InstalledPackageReference, PackageInformation>(packages.Count);
            foreach (var (installedPackageReference, localPackageInfo) in packages)
            {
                Uri? licenseSource;
                if (localPackageInfo is null)
                {
                    continue;
                }

                NuGetLicenseExpression license;
                Debug.Assert(localPackageInfo.Nuspec != null);
                var licenseMetadata = localPackageInfo.Nuspec.GetLicenseMetadata();
                if (licenseMetadata != null)
                {
                    license = licenseMetadata.LicenseExpression ?? NoLicense.Instance;
                    licenseSource = licenseMetadata.LicenseUrl;
                }
                else
                {
                    license = NoLicense.Instance;
                    var temp = localPackageInfo.Nuspec.GetLicenseUrl();
                    licenseSource = temp != null ? new Uri(temp) : null;
                }

                var authors = localPackageInfo.Nuspec.GetAuthors() ?? "";

                if (licenseSource is null)
                {
                    _logger.LogInformation("No license source provided for {Package}", localPackageInfo.Identity);
                }

                var information = new PackageInformation(licenseSource, license, authors);
                result.Add(installedPackageReference, information);
            }

            return ImmutableDictionary.CreateRange(result);
        }
    }
}
