using LicenseGatherer.Core;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Build.Locator;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static System.FormattableString;
using Environment = LicenseGatherer.Core.Environment;

namespace LicenseGatherer
{
    public class Program
    {
        [Option(Description = "The subject", LongName = "path", ShortName = "p")]
        public string PathToProjectOrSolution { get; set; }

        [Option(Description = "The subject", LongName = "outputpath", ShortName = "o")]
        public string OutputPath { get; set; }

        public static async Task<int> Main(string[] args)
        {
            var exitCode = await new HostBuilder()
                .RunCommandLineApplicationAsync<Program>(args);
            return exitCode;
        }

        // ReSharper disable UnusedMember.Local
        private async Task<int> OnExecuteAsync()
        // ReSharper restore UnusedMember.Local
        {
            var cancellationToken = CancellationToken.None;
            var fileSystem = new FileSystem();
            var environment = new Environment();
            var instances = MSBuildLocator.QueryVisualStudioInstances().ToList();
            MSBuildLocator.RegisterMSBuildPath(instances.First().MSBuildPath);
            var projectDependencyResolver = new ProjectDependencyResolver(fileSystem, environment);
            Console.WriteLine("Resolving dependencies");
            var dependencies = projectDependencyResolver.ResolveDependencies(PathToProjectOrSolution);

            using (var httpClient = new HttpClient())
            {
                var licenseProvider = new LicenseLocator();
                Console.WriteLine("Extracting licensing information");
                var licenseSpecs = licenseProvider.Provide(dependencies);

                var uriCorrector = new UriCorrector();
                Console.WriteLine("Correcting license locations");
                var correctedLicenseLocations = uriCorrector.Correct(licenseSpecs.Values.Distinct(EqualityComparer<Uri>.Default));

                var downloader = new LicenseDownloader(httpClient);
                Console.WriteLine("Downloading licenses");
                var licenses = await downloader.DownloadAsync(correctedLicenseLocations.Values.Select(v => v.corrected), cancellationToken);

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
                    var outputFile = fileSystem.FileInfo.FromFileName(OutputPath);
                    if (outputFile.Exists)
                    {
                        Console.WriteLine("The file to write the output to already exists");
                        return 1;
                    }

                    var fileContent = JsonConvert.SerializeObject(licenseDependencyInformation, Formatting.Indented);

                    using (var writer = outputFile.OpenWrite())
                    {
                        var encoding = new UTF8Encoding(false, true);
                        var bytes = encoding.GetBytes(fileContent);
                        await writer.WriteAsync(bytes, 0, bytes.Length, cancellationToken);
                    }
                }
                else
                {
                    Console.WriteLine(Invariant($"Licenses of {PathToProjectOrSolution}"));
                    foreach (var dependencyInformation in licenseDependencyInformation)
                    {
                        Console.WriteLine(Invariant($"dependency {dependencyInformation.PackageReference.Name} (version {dependencyInformation.PackageReference.ResolvedVersion})"));
                    }
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
