using System;
using System.Collections.Generic;
using System.Collections.Immutable;

using NuGet.Protocol;

namespace LicenseGatherer.Core
{
    public class LicenseLocator
    {
        public IImmutableDictionary<InstalledPackageReference, Uri> Provide(IImmutableDictionary<InstalledPackageReference, LocalPackageInfo> packages)
        {
            var result = new Dictionary<InstalledPackageReference, Uri>(packages.Count);
            foreach (var package in packages)
            {
                Uri licenseSource;
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

                result.Add(package.Key, licenseSource);
            }

            return ImmutableDictionary.CreateRange(result);
        }
    }
}
