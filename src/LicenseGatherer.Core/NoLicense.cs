using NuGet.Packaging.Licenses;

namespace LicenseGatherer.Core
{
    public class NoLicense : NuGetLicenseExpression
    {
        public static NoLicense Instance { get; } = new NoLicense();

        private NoLicense()
        {
            Type = LicenseExpressionType.License;
        }

        public override string ToString() => "/";
    }
}
