using NuGet.Packaging.Licenses;

namespace LicenseGatherer.Core
{
    public class NoLicense : NuGetLicenseExpression
    {
        static NoLicense()
        {
            Instance = new NoLicense();
        }

        public static NoLicense Instance { get; }

        private NoLicense()
        {
            Type = LicenseExpressionType.License;
        }

        public override string ToString() => "/";
    }
}
