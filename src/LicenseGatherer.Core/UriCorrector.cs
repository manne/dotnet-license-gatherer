using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace LicenseGatherer.Core
{
    public class UriCorrector
    {
        public IImmutableDictionary<Uri, (Uri corrected, bool wasCorrected)> Correct(IEnumerable<Uri> licenseLocations)
        {
            var result = new Dictionary<Uri, (Uri corrected, bool wasCorrected)>(EqualityComparer<Uri>.Default);
            foreach (var location in licenseLocations)
            {
                Uri correctedLocation = null;
                bool wasCorrected = false;
                if ("github.com" == location.Host || "www.github.com" == location.Host)
                {
                    var segments = location.Segments;
                    if (segments.Length > 3 && segments[4] != "raw")
                    {
                        var li = segments.ToList();
                        li.RemoveAt(3);
                        correctedLocation = new Uri(new Uri("https://raw.githubusercontent.com"), string.Join("", li.Select(s => s)));
                        wasCorrected = true;
                    }
                }

                if (!wasCorrected)
                {
                    correctedLocation = location;
                }

                result.Add(location, (correctedLocation, wasCorrected));
            }

            return ImmutableDictionary.CreateRange(result);
        }
    }
}
