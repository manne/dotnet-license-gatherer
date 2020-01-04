using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Xml;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Exceptions;
using NuGet.Common;
using NuGet.ProjectModel;
using NuGet.Protocol;

using static System.FormattableString;

namespace LicenseGatherer.Core
{
    public class ProjectDependencyResolver
    {
        private static readonly ILogger Logger = new DummyLogger();
        private readonly IFileSystem _fileSystem;
        private readonly IEnvironment _environment;

        public ProjectDependencyResolver(IFileSystem fileSystem, IEnvironment environment)
        {
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        }

        public IImmutableDictionary<InstalledPackageReference, LocalPackageInfo?> ResolveDependencies(string? projectOrSolutionPath)
        {
            IFileInfo file;
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
            }

            return AnalyzeFileInfo(file);
        }

        private IImmutableDictionary<InstalledPackageReference, LocalPackageInfo?> AnalyzeFileInfo(IFileInfo projectFile)
        {
            var isSolution = ".sln".Equals(projectFile.Extension, StringComparison.OrdinalIgnoreCase);
            return isSolution ? AnalyzeSolutionFile(projectFile) : AnalyzeProjectFile(projectFile);
        }

        private IImmutableDictionary<InstalledPackageReference, LocalPackageInfo?> AnalyzeSolutionFile(IFileInfo solutionFile)
        {
            var solution = SolutionFile.Parse(solutionFile.FullName);
            var projects = solution.ProjectsInOrder;
            var fileSystem = solutionFile.FileSystem;
            var result = new Dictionary<InstalledPackageReference, LocalPackageInfo?>(InstalledPackageReferenceEqualityComparer.Instance);
            foreach (var project in projects)
            {
                if (project.ProjectType == SolutionProjectType.SolutionFolder)
                {
                    continue;
                }

                var projectFile = fileSystem.FileInfo.FromFileName(project.AbsolutePath);
                var info = AnalyzeProjectFile(projectFile);
                result.SafeAddRange(info);
            }

            return ImmutableDictionary.CreateRange(InstalledPackageReferenceEqualityComparer.Instance, result);
        }

        private IImmutableDictionary<InstalledPackageReference, LocalPackageInfo?> AnalyzeProjectFile(IFileInfo projectFile)
        {
            const string projectAssetsPropertyName = "ProjectAssetsFile";

            string assetFileLocation;
            using (var stream = projectFile.OpenText())
            {
                using var xmlStream = XmlReader.Create(stream);
                var lastCurrentDirectory = _environment.CurrentDirectory;
                try
                {
                    _environment.CurrentDirectory = projectFile.DirectoryName;
                    using var projectCollection = new ProjectCollection();

                    var project = new Project(xmlStream, null, null, projectCollection);
                    var relativePath = project.GetPropertyValue(projectAssetsPropertyName);
                    assetFileLocation = _fileSystem.Path.Combine(projectFile.Directory.FullName, relativePath);
                }
                finally
                {
                    _environment.CurrentDirectory = lastCurrentDirectory;
                }
            }

            if (string.IsNullOrEmpty(assetFileLocation))
            {
                throw new InvalidProjectFileException(
                    Invariant($"The project file {projectFile.FullName} does not have the property {projectAssetsPropertyName}"));
            }

            var assetFile = _fileSystem.FileInfo.FromFileName(assetFileLocation);
            if (!assetFile.Exists)
            {
                throw new FileNotFoundException(
                    Invariant($"The asset file {assetFile.FullName} of the project {projectFile.FullName} does not exists. Please build the project first."));
            }

            LockFile lockFile;
            using (var stream = assetFile.OpenText())
            {
                lockFile = new LockFileFormat().Read(stream, assetFile.FullName);
            }

            var allReferencedPackages = lockFile.Targets.SelectMany(t => t.Libraries)
                .Where(l => l.Type == "package")
                .Select(p => new InstalledPackageReference(p.Name, p.Version));
            var referencedPackages = allReferencedPackages.Distinct(InstalledPackageReferenceEqualityComparer.Instance);
            var packageFolders = lockFile.PackageFolders;
            var localPackageInfos = new Dictionary<InstalledPackageReference, LocalPackageInfo?>();
            foreach (var referencedPackage in referencedPackages)
            {
                var packageInfo = GetPackageInfo(referencedPackage, packageFolders);
                localPackageInfos.Add(referencedPackage, packageInfo);
            }

            return ImmutableDictionary.CreateRange(localPackageInfos);
        }

        private static LocalPackageInfo? GetPackageInfo(InstalledPackageReference packageReference, IEnumerable<LockFileItem> folders)
        {
            var packageIdentity = new NuGet.Packaging.Core.PackageIdentity(packageReference.Name, packageReference.ResolvedVersion);
            foreach (var folder in folders)
            {
                var packageInfo = LocalFolderUtility.GetPackageV3(folder.Path, packageIdentity, Logger);
                return packageInfo;
            }

            return null;
        }
    }
}
