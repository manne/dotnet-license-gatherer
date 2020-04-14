using System.IO.Abstractions;

namespace LicenseGatherer.Core
{
    public sealed class EntryPoint
    {
        public EntryPoint(IFileInfo file, EntryPointType type)
        {
            File = file;
            Type = type;
        }

        public IFileInfo File { get; }

        public EntryPointType Type { get; }
    }

    public enum EntryPointType
    {
        Solution = 0,
        Project = 1
    }
}
