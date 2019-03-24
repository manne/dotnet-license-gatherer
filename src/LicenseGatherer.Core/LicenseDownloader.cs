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

        public LicenseDownloader(HttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
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
                using (var requestMessage = new HttpRequestMessage(HttpMethod.Get, licenseLocation))
                {
                    var responseMessage = await _httpClient.SendAsync(requestMessage, cancellationToken);
                    var rawLicenseText = await responseMessage.Content.ReadAsStringAsync();
                    licenseContent = rawLicenseText;
                }

                result.Add(licenseLocation, licenseContent);
            }

            return ImmutableDictionary.CreateRange(result);
        }
    }
}
