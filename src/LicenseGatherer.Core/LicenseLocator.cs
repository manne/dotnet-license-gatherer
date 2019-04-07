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
            foreach (var package in packages)
            {
                Uri licenseSource;
                if (package.Value == null)
                {
                    continue;
                }

                Debug.Assert(package.Value.Nuspec != null);
                var licenseMetadata = package.Value.Nuspec.GetLicenseMetadata();
                if (licenseMetadata != null)
                {
                    licenseSource = licenseMetadata.LicenseUrl;
                }
                else
                {
                    var temp = package.Value.Nuspec.GetLicenseUrl();
                    licenseSource = temp != null ? new Uri(temp) : null;
                }

                if (licenseSource == null)
                {
                    _logger.LogInformation("No license source provided for {Package}", package.Value.Identity);
                    continue;
                }

                result.Add(package.Key, licenseSource);
            }

            return ImmutableDictionary.CreateRange(result);
        }
    }
}
