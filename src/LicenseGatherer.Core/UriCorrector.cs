using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.Extensions.Logging;

namespace LicenseGatherer.Core
{
    public class UriCorrector
    {
        private readonly ILogger<UriCorrector> _logger;

        public UriCorrector(ILogger<UriCorrector> logger)
        {
            _logger = logger;
        }

        public IImmutableDictionary<Uri, (Uri corrected, bool wasCorrected)> Correct(IEnumerable<Uri> licenseLocations)
        {
            var result = new Dictionary<Uri, (Uri corrected, bool wasCorrected)>(EqualityComparer<Uri>.Default);
            using (_logger.BeginScope("Correcting URLs"))
            {
                foreach (var location in licenseLocations)
                {
                    Uri? correctedLocation = null;
                    bool wasCorrected;
                    if (location == null)
                    {
                        _logger.LogInformation("no uri");
                        continue;
                    }

                    if ("github.com" == location.Host || "www.github.com" == location.Host)
                    {
                        var segments = location.Segments;
                        if (segments.Length > 3 && segments[4] != "raw")
                        {
                            var li = segments.ToList();
                            li.RemoveAt(3);
                            correctedLocation = new Uri(new Uri("https://raw.githubusercontent.com"), string.Join("", li.Select(s => s)));
                            wasCorrected = true;

                            _logger.LogInformation("Corrected url from {SourceLicenseUrl} to {TargetLicenseUrl}", location, correctedLocation);
                        }
                        else
                        {
                            wasCorrected = false;
                        }
                    }
                    else
                    {
                        wasCorrected = false;
                    }

                    if (!wasCorrected)
                    {
                        correctedLocation = location;
                    }

                    result.Add(location, (correctedLocation!, wasCorrected));
                }
            }

            return ImmutableDictionary.CreateRange(result);
        }
    }
}
