using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using LicenseGatherer.Core;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Build.Locator;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

using static System.FormattableString;

using Environment = LicenseGatherer.Core.Environment;
using IReporter = LicenseGatherer.Core.IReporter;
using CommandLineUtils = McMaster.Extensions.CommandLineUtils;

namespace LicenseGatherer
{
    public class Program
    {
        private readonly UriCorrector _uriCorrector;
        private readonly PackageLocator _packageLocator;
        private readonly IFileSystem _fileSystem;
        private readonly ProjectDependencyResolver _projectDependencyResolver;
        private readonly LicenseDownloader _downloader;
        private readonly IReporter _reporter;
        private readonly IEntryPointLocator _entryPointLocator;

        [Option(Description = "The path of the project or solution to gather the licenses. A directory can be specified, the value must end with \\, then for a solution in the working directory is searched. (optional)", LongName = "path", ShortName = "p", ShowInHelpText = true)]
        public string? PathToProjectOrSolution { get; set; }

        [Option(Description = "The path of the JSON content output. If the no value is specified some information is printed into the console. (optional)", LongName = "outputpath", ShortName = "o", ShowInHelpText = true)]
        public string? OutputPath { get; set; }

        [Option(Description = "Skip the download of licenses", LongName = "skipdownload", ShortName = "s", ShowInHelpText = true)]
        public bool SkipDownloadOfLicenses { get; set; }

        public static async Task<int> Main(string[] args)
        {
            var exitCode = await new HostBuilder()
                .ConfigureAppConfiguration((context, config) =>
                {
                    var env = context.HostingEnvironment;
                    config
                        .SetBasePath(Directory.GetCurrentDirectory())
                        .AddJsonFile("appsettings.json", optional: true)
                        .AddJsonFile(Invariant($"appsettings.{env.EnvironmentName}.json"), optional: true);
                })
                .ConfigureLogging((context, logging) =>
                {
                    if (context.HostingEnvironment.IsDevelopment())
                    {
                        logging.AddConsole();
                    }
                })
                .ConfigureServices(services =>
                {
                    services
                        .AddSingleton<UriCorrector>()
                        .AddSingleton<PackageLocator>()
                        .AddSingleton<IFileSystem, FileSystem>()
                        .AddSingleton<IEnvironment, Environment>()
                        .AddSingleton<ProjectDependencyResolver>()
                        .AddSingleton<CommandLineUtils.IReporter, ConsoleReporter>()
                        .AddSingleton<IReporter, Reporter>()
                        .AddSingleton<IEntryPointLocator, EntryPointLocator>();
                    services.AddHttpClient<LicenseDownloader>();
                })
                .RunCommandLineApplicationAsync<Program>(args);
            return exitCode;
        }

        public Program(UriCorrector uriCorrector, PackageLocator packageLocator, IFileSystem fileSystem,
            ProjectDependencyResolver projectDependencyResolver, LicenseDownloader licenseDownloader, IReporter reporter, IEntryPointLocator entryPointLocator)
        {
            _uriCorrector = uriCorrector;
            _packageLocator = packageLocator;
            _fileSystem = fileSystem;
            _projectDependencyResolver = projectDependencyResolver;
            _downloader = licenseDownloader;
            _reporter = reporter;
            _entryPointLocator = entryPointLocator;
        }

        // ReSharper disable UnusedMember.Local
#pragma warning disable IDE0051 // Remove unused private members
        private async Task<int> OnExecuteAsync(CancellationToken cancellationToken)
#pragma warning restore IDE0051 // Remove unused private members
        // ReSharper restore UnusedMember.Local
        {
            if (OutputPath is null)
            {
                SkipDownloadOfLicenses = true;
            }

            IFileInfo? outputFile;
            if (OutputPath != null)
            {
                outputFile = _fileSystem.FileInfo.FromFileName(OutputPath);
                if (outputFile.Exists)
                {
                    _reporter.Error("The file to write the output to already exists. Specify another output path or delete the file");
                    return 1;
                }
            }
            else
            {
                outputFile = null;
            }

            var instances = MSBuildLocator.QueryVisualStudioInstances().ToList();
            MSBuildLocator.RegisterMSBuildPath(instances.First().MSBuildPath);

            var entryPoint = _entryPointLocator.GetEntryPoint(PathToProjectOrSolution);

            var dependencies = _projectDependencyResolver.ResolveDependencies(entryPoint);
            _reporter.OutputInvariant($"Resolving dependencies of {entryPoint.File.FullName}");
            _reporter.OutputInvariant($"\tcount {dependencies.Count}");

            _reporter.Output("Extracting licensing information");
            var packageSpec = _packageLocator.Provide(dependencies);

            _reporter.Output("Correcting license locations");
            var existingLicenses = packageSpec.Values
                .Where(v => v.LicenseLocation != null)
                .Select(v => v.LicenseLocation!)
                .Distinct(EqualityComparer<Uri>.Default);
            var correctedLicenseLocations = _uriCorrector.Correct(existingLicenses);

            _reporter.OutputInvariant($"Downloading licenses (total {correctedLicenseLocations.Count})");
            IImmutableDictionary<Uri, string> licenses;
            if (SkipDownloadOfLicenses)
            {
                _reporter.Output("\tSkipping download");
                licenses = ImmutableDictionary<Uri, string>.Empty;
            }
            else
            {
                licenses = await _downloader.DownloadAsync(correctedLicenseLocations.Values.Select(v => v.corrected), cancellationToken);
            }

            var licenseDependencyInformation = new List<LicenseDependencyInformation>();

            foreach (var (package, (location, licenseExpression, authors)) in packageSpec)
            {
                Uri? correctedUrl;
                string licenseContent;
                if (!(location is null))
                {
                    correctedUrl = correctedLicenseLocations[location].corrected;
                    licenseContent = licenses.FirstOrDefault(l => l.Key == correctedUrl).Value ?? "";
                }
                else
                {
                    correctedUrl = null;
                    licenseContent = "";
                }

                var dependencyInformation = new LicenseDependencyInformation(package, licenseContent, location, correctedUrl, licenseExpression, authors);

                licenseDependencyInformation.Add(dependencyInformation);
            }

            if (outputFile != null)
            {
                var fileContent = JsonConvert.SerializeObject(licenseDependencyInformation, Formatting.Indented);

                await using var writer = outputFile.OpenWrite();
                var encoding = new UTF8Encoding(false, true);
                var bytes = encoding.GetBytes(fileContent);
                await writer.WriteAsync(bytes, 0, bytes.Length, cancellationToken);
                await writer.FlushAsync(cancellationToken);
            }
            else
            {
                _reporter.OutputInvariant($"Licenses:");
                foreach (var dependencyInformation in licenseDependencyInformation)
                {
                    _reporter.OutputInvariant($"dependency {dependencyInformation.PackageReference.Name} (version: {dependencyInformation.PackageReference.ResolvedVersion}, license expression: {dependencyInformation.LicenseExpression})");
                }
            }

            return 0;
        }
    }
}
