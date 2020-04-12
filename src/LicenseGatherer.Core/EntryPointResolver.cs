using System;
using System.IO;
using System.IO.Abstractions;
using System.Linq;

using static System.FormattableString;

namespace LicenseGatherer.Core
{
    public interface IEntryPointLocator
    {
        EntryPoint GetEntryPoint(string? projectOrSolutionPath);
    }

    public class EntryPointLocator : IEntryPointLocator
    {
        private readonly IFileSystem _fileSystem;

        public EntryPointLocator(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }

        public EntryPoint GetEntryPoint(string? projectOrSolutionPath)
        {
            IFileInfo file;
            EntryPointType entryPointType;
            bool searchDirectory;
            if (projectOrSolutionPath is null)
            {
                searchDirectory = true;
                projectOrSolutionPath = _fileSystem.Directory.GetCurrentDirectory();
            }
            else
            {
                var lastCharacter = projectOrSolutionPath.TrimEnd().Last();
                if (lastCharacter == _fileSystem.Path.AltDirectorySeparatorChar || lastCharacter == _fileSystem.Path.DirectorySeparatorChar)
                {
                    searchDirectory = true;
                }
                else
                {
                    searchDirectory = false;
                }
            }

            if (!searchDirectory)
            {
                var fromFileName = _fileSystem.FileInfo.FromFileName(projectOrSolutionPath);
                if (!fromFileName.Exists)
                {
                    throw new FileNotFoundException("The file does not exist", projectOrSolutionPath);
                }

                file = fromFileName;
                entryPointType = EntryPointType.Project;
            }
            else
            {
                var directory = _fileSystem.DirectoryInfo.FromDirectoryName(projectOrSolutionPath);
                if (!directory.Exists)
                {
                    throw new DirectoryNotFoundException(Invariant($"The directory {projectOrSolutionPath} does not exist"));
                }

                var solutionFiles = directory.GetFiles("*.sln", SearchOption.TopDirectoryOnly);
                if (solutionFiles.Length == 0)
                {
                    throw new InvalidOperationException(Invariant($"The directory {directory.FullName} does not have one solution file"));
                }

                if (solutionFiles.Length > 1)
                {
                    throw new InvalidOperationException(Invariant($"The directory {directory.FullName} does have more than one solution file. Please specify the solution."));
                }

                file = solutionFiles[0];
                entryPointType = EntryPointType.Solution;
            }

            return new EntryPoint(file, entryPointType);
        }
    }
}
