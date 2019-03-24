using System.Collections.Generic;

namespace LicenseGatherer.Core
{
    public class InstalledPackageReferenceEqualityComparer : IEqualityComparer<InstalledPackageReference>
    {
        public int GetHashCode(InstalledPackageReference co)
        {
            return co.Name.GetHashCode() ^ co.ResolvedVersion.GetHashCode();
        }

        public bool Equals(InstalledPackageReference x1, InstalledPackageReference x2)
        {
            if (ReferenceEquals(x1, x2))
            {
                return true;
            }

            if (x1 is null || x2 is null)
            {
                return false;
            }

            return x1.Equals(x2);
        }
    }
}
