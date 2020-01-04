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

namespace LicenseGatherer
{
    public class Program
    {
        private readonly UriCorrector _uriCorrector;
        private readonly LicenseLocator _licenseLocator;
        private readonly IFileSystem _fileSystem;
        private readonly ProjectDependencyResolver _projectDependencyResolver;
        private readonly LicenseDownloader _downloader;

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
                    services.AddSingleton<UriCorrector>();
                    services.AddSingleton<LicenseLocator>();
                    services.AddSingleton<IFileSystem, FileSystem>();
                    services.AddSingleton<IEnvironment, Environment>();
                    services.AddSingleton<ProjectDependencyResolver>();
                    services.AddHttpClient<LicenseDownloader>();
                })
                .RunCommandLineApplicationAsync<Program>(args);
            return exitCode;
        }

        public Program(UriCorrector uriCorrector, LicenseLocator licenseLocator, IFileSystem fileSystem,
            ProjectDependencyResolver projectDependencyResolver, LicenseDownloader licenseDownloader)
        {
            _uriCorrector = uriCorrector;
            _licenseLocator = licenseLocator;
            _fileSystem = fileSystem;
            _projectDependencyResolver = projectDependencyResolver;
            _downloader = licenseDownloader;
        }

        // ReSharper disable UnusedMember.Local
#pragma warning disable IDE0051 // Remove unused private members
        private async Task<int> OnExecuteAsync()
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
                    Console.ForegroundColor = ConsoleColor.Red;
                    await Console.Out.WriteLineAsync("The file to write the output to already exists. Specify another output path or delete the file");
                    Console.ResetColor();
                    return 1;
                }
            }
            else
            {
                outputFile = null;
            }

            var cancellationToken = CancellationToken.None;
            var instances = MSBuildLocator.QueryVisualStudioInstances().ToList();
            MSBuildLocator.RegisterMSBuildPath(instances.First().MSBuildPath);

            await Console.Out.WriteLineAsync("Resolving dependencies");
            var dependencies = _projectDependencyResolver.ResolveDependencies(PathToProjectOrSolution);
            await Console.Out.WriteLineAsync(Invariant($"\tcount {dependencies.Count}"));

            await Console.Out.WriteLineAsync("Extracting licensing information");
            var licenseSpecs = _licenseLocator.Provide(dependencies);

            await Console.Out.WriteLineAsync("Correcting license locations");
            var correctedLicenseLocations = _uriCorrector.Correct(licenseSpecs.Values.Select(v => v.Item1).Distinct(EqualityComparer<Uri>.Default));

            await Console.Out.WriteLineAsync(Invariant($"Downloading licenses (total {correctedLicenseLocations.Count})"));
            IImmutableDictionary<Uri, string> licenses;
            if (SkipDownloadOfLicenses)
            {
                await Console.Out.WriteLineAsync("\tSkipping download");
                licenses = ImmutableDictionary<Uri, string>.Empty;
            }
            else
            {
                licenses = await _downloader.DownloadAsync(correctedLicenseLocations.Values.Select(v => v.corrected), cancellationToken);
            }

            var licenseDependencyInformation = new List<LicenseDependencyInformation>();

            foreach (var (package, (location, licenseExpression)) in licenseSpecs)
            {
                var correctedUrl = correctedLicenseLocations[location].corrected;
                var content = licenses.FirstOrDefault(l => l.Key == correctedUrl);
                var dependencyInformation = new LicenseDependencyInformation(package, content.Value ?? string.Empty, location, correctedUrl, licenseExpression);

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
                await Console.Out.WriteLineAsync(Invariant($"Licenses of {PathToProjectOrSolution}"));
                foreach (var dependencyInformation in licenseDependencyInformation)
                {
                    await Console.Out.WriteLineAsync(Invariant($"dependency {dependencyInformation.PackageReference.Name} (version: {dependencyInformation.PackageReference.ResolvedVersion}, license expression: {dependencyInformation.LicenseExpression})"));
                }
            }

            return 0;
        }
    }
}
