using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using NuGet.Packaging.Licenses;
using NuGet.Protocol;

namespace LicenseGatherer.Core
{
    public class LicenseLocator
    {
        private readonly ILogger<LicenseLocator> _logger;

        public LicenseLocator(ILogger<LicenseLocator> logger)
        {
            _logger = logger;
        }

        public IImmutableDictionary<InstalledPackageReference, (Uri, NuGetLicenseExpression)> Provide(IImmutableDictionary<InstalledPackageReference, LocalPackageInfo?> packages)
        {
            var result = new Dictionary<InstalledPackageReference, (Uri, NuGetLicenseExpression)>(packages.Count);
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

                if (licenseSource is null)
                {
                    _logger.LogInformation("No license source provided for {Package}", localPackageInfo.Identity);
                    continue;
                }

                result.Add(installedPackageReference, (licenseSource, license));
            }

            return ImmutableDictionary.CreateRange(result);
        }
    }
}
