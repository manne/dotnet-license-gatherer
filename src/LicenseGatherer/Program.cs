using System;
using System.Collections.Generic;
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

        [Option(Description = "The path of the project or solution to gather the licenses", LongName = "path", ShortName = "p")]
        public string PathToProjectOrSolution { get; set; }

        [Option(Description = "The path of the json content output", LongName = "outputpath", ShortName = "o")]
        public string OutputPath { get; set; }

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
                    logging.AddConsole();
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
            var cancellationToken = CancellationToken.None;
            var instances = MSBuildLocator.QueryVisualStudioInstances().ToList();
            MSBuildLocator.RegisterMSBuildPath(instances.First().MSBuildPath);

            await Console.Out.WriteLineAsync("Resolving dependencies");
            var dependencies = _projectDependencyResolver.ResolveDependencies(PathToProjectOrSolution);

            await Console.Out.WriteLineAsync("Extracting licensing information");
            var licenseSpecs = _licenseLocator.Provide(dependencies);

            await Console.Out.WriteLineAsync("Correcting license locations");
            var correctedLicenseLocations = _uriCorrector.Correct(licenseSpecs.Values.Distinct(EqualityComparer<Uri>.Default));

            await Console.Out.WriteLineAsync("Downloading licenses");
            var licenses = await _downloader.DownloadAsync(correctedLicenseLocations.Values.Select(v => v.corrected), cancellationToken);

            var licenseDependencyInformation = new List<LicenseDependencyInformation>();

            foreach (var (package, location) in licenseSpecs)
            {
                var correctedUrl = correctedLicenseLocations[location].corrected;
                var content = licenses.First(l => l.Key == correctedUrl);
                var dependencyInformation = new LicenseDependencyInformation(package, content.Value, location, correctedUrl);

                licenseDependencyInformation.Add(dependencyInformation);
            }

            if (OutputPath != null)
            {
                var outputFile = _fileSystem.FileInfo.FromFileName(OutputPath);
                if (outputFile.Exists)
                {
                    await Console.Out.WriteLineAsync("The file to write the output to already exists");
                    return 1;
                }

                var fileContent = JsonConvert.SerializeObject(licenseDependencyInformation, Formatting.Indented);

                using (var writer = outputFile.OpenWrite())
                {
                    var encoding = new UTF8Encoding(false, true);
                    var bytes = encoding.GetBytes(fileContent);
                    await writer.WriteAsync(bytes, 0, bytes.Length, cancellationToken);
                    await writer.FlushAsync(cancellationToken);
                }
            }
            else
            {
                await Console.Out.WriteLineAsync(Invariant($"Licenses of {PathToProjectOrSolution}"));
                foreach (var dependencyInformation in licenseDependencyInformation)
                {
                    await Console.Out.WriteLineAsync(Invariant($"dependency {dependencyInformation.PackageReference.Name} (version {dependencyInformation.PackageReference.ResolvedVersion})"));
                }
            }

            return 0;
        }
    }

    public static class Extensions
    {
        public static void Deconstruct<T1, T2>(this KeyValuePair<T1, T2> tuple, out T1 key, out T2 value)
        {
            key = tuple.Key;
            value = tuple.Value;
        }
    }
}
