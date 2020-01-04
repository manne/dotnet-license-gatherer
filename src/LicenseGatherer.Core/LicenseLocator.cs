using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
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

        public IImmutableDictionary<InstalledPackageReference, Uri> Provide(IImmutableDictionary<InstalledPackageReference, LocalPackageInfo> packages)
        {
            var result = new Dictionary<InstalledPackageReference, Uri>(packages.Count);
            foreach (var (installedPackageReference, localPackageInfo) in packages)
            {
                Uri licenseSource;
                if (localPackageInfo is null)
                {
                    continue;
                }

                Debug.Assert(localPackageInfo.Nuspec != null);
                var licenseMetadata = localPackageInfo.Nuspec.GetLicenseMetadata();
                if (licenseMetadata != null)
                {
                    licenseSource = licenseMetadata.LicenseUrl;
                }
                else
                {
                    var temp = localPackageInfo.Nuspec.GetLicenseUrl();
                    licenseSource = temp != null ? new Uri(temp) : null;
                }

                if (licenseSource is null)
                {
                    _logger.LogInformation("No license source provided for {Package}", localPackageInfo.Identity);
                    continue;
                }

                result.Add(installedPackageReference, licenseSource);
            }

            return ImmutableDictionary.CreateRange(result);
        }
    }
}
