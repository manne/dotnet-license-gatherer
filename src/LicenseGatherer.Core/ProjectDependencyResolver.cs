using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Xml;

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

        public IImmutableDictionary<InstalledPackageReference, LocalPackageInfo> ResolveDependencies(string projectOrSolutionPath)
        {
            if (!_fileSystem.File.Exists(projectOrSolutionPath))
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

                throw new FileNotFoundException("The file does not exist", projectOrSolutionPath);
            }

            return AnalyzeProject(_fileSystem.FileInfo.FromFileName(projectOrSolutionPath));
        }

        private IImmutableDictionary<InstalledPackageReference, LocalPackageInfo> AnalyzeProject(FileInfoBase projectFile)
        {
            const string projectAssetsPropertyName = "ProjectAssetsFile";

            string assetFileLocation;
            using (var stream = projectFile.OpenText())
            using (var xmlStream = XmlReader.Create(stream))
            {
                string lastCurrentDirectory = null;

                try
                {
                    lastCurrentDirectory = _environment.CurrentDirectory;
                    _environment.CurrentDirectory = projectFile.DirectoryName;

                    var project = new Project(xmlStream, null, null, new ProjectCollection());
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
            var referencedPackages = allReferencedPackages.Distinct(new InstalledPackageReferenceEqualityComparer());
            var packageFolders = lockFile.PackageFolders;
            var localPackageInfos = new Dictionary<InstalledPackageReference, LocalPackageInfo>();
            foreach (var referencedPackage in referencedPackages)
            {
                var packageInfo = GetPackageInfo(referencedPackage, packageFolders);
                localPackageInfos.Add(referencedPackage, packageInfo);
            }

            return ImmutableDictionary.CreateRange(localPackageInfos);
        }

        private LocalPackageInfo GetPackageInfo(InstalledPackageReference packageReference, IEnumerable<LockFileItem> folders)
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
