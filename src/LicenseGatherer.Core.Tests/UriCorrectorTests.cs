using System;

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LicenseGatherer.Core.Tests
{
    public class UriCorrectorTests
    {
        [Fact]
        public void GivenOneHtmlGithubUrl_WhenCorrection_ThenTheCorrectedUrl_ShouldBeOnPointingToItsDownload()
        {
            var uriCorrector = new UriCorrector(Mock.Of<ILogger<UriCorrector>>());
            var input = new Uri("https://github.com/dotnet/corefx/blob/master/LICENSE.TXT");
            var corrected = uriCorrector.Correct(new[] {input});

            corrected.Should().OnlyContain(i =>
                i.Value.corrected == new Uri("https://raw.githubusercontent.com/dotnet/corefx/master/LICENSE.TXT")
                && i.Value.wasCorrected);
        }
    }
}
