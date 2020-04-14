using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using MaxKagamine.Moq.HttpClient;
using Moq;
using Xunit;

namespace LicenseGatherer.Core.Tests
{
    public class LicenseDownloaderTests
    {
        [Fact]
        public async Task GivenTwoSameLicenseLocations_WhenDownloadingThem_ThenTheLicense_ShouldOnlyDownloadedOnce()
        {
            // Given
            var uri = new Uri("https://example.com/MIT");
            var mock = new Mock<HttpMessageHandler>();
            mock
                .SetupRequest(HttpMethod.Get, uri)
                .ReturnsResponse(HttpStatusCode.OK, r => r.Content = new StringContent(""))
                .Verifiable();

            using var httpClient = mock.CreateClient();
            var cut = new LicenseDownloader(httpClient, Mock.Of<IReporter>());

            // When
            _ = await cut.DownloadAsync(new[] { uri, uri }, CancellationToken.None);

            // Then
            mock.VerifyAnyRequest(Times.Once());
        }
    }
}
