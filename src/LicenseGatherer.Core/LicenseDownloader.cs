using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace LicenseGatherer.Core
{
    public class LicenseDownloader
    {
        private readonly HttpClient _httpClient;
        private readonly IReporter _reporter;

        public LicenseDownloader(HttpClient httpClient, IReporter reporter)
        {
            _httpClient = httpClient;
            _reporter = reporter;
        }

        public async Task<IImmutableDictionary<Uri, string>> DownloadAsync(IEnumerable<Uri> licenseLocations, CancellationToken cancellationToken)
        {
            var equalityComparer = EqualityComparer<Uri>.Default;
            var uniqueLocations = licenseLocations.Distinct(equalityComparer);
            var result = new Dictionary<Uri, string>(equalityComparer);

            foreach (var licenseLocation in uniqueLocations)
            {
                cancellationToken.ThrowIfCancellationRequested();
                string licenseContent;
                _reporter.OutputInvariant($"Downloading license file form {licenseLocation}");
                using (var requestMessage = new HttpRequestMessage(HttpMethod.Get, licenseLocation))
                {
                    string rawLicenseText;
                    using (var responseMessage = await _httpClient.SendAsync(requestMessage, cancellationToken))
                    {
                        rawLicenseText = await responseMessage.Content.ReadAsStringAsync();
                    }

                    licenseContent = rawLicenseText;
                }

                result.Add(licenseLocation, licenseContent);
            }

            return ImmutableDictionary.CreateRange(result);
        }
    }
}
