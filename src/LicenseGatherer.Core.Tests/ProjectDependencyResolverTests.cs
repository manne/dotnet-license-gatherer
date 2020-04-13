using System;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Build.Locator;
using Moq;
using Xunit;

namespace LicenseGatherer.Core.Tests
{
    public class ProjectDependencyResolverTests
    {
        public ProjectDependencyResolverTests()
        {
            if (MSBuildLocator.CanRegister)
            {
                var instances = MSBuildLocator.QueryVisualStudioInstances().ToList();
                MSBuildLocator.RegisterMSBuildPath(instances.First().MSBuildPath);
            }
        }

        [Fact(Skip = ".NET SDK is not found")]
        public async Task GivenOnePathToOneExistingProjectFile_WhenThisProjectDoesNotHaveOneProjectAssetsFileProperty_ThenOneException_ShouldBeThrown()
        {
            var mockFileSystem = new MockFileSystem();
            mockFileSystem.AddFile(@"c:\foo\bar\xyz.csproj", new MockFileData(@"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFrameworks>netstandard1.6;net462;net452</TargetFrameworks>
    <AssemblyName>Lokad.AzureEventStore</AssemblyName>
    <RuntimeIdentifiers>win</RuntimeIdentifiers>
    <OutputType>Library</OutputType>
    <Company>Lokad</Company>
    <Copyright>Copyright © Lokad 2019</Copyright>
    <AssemblyVersion>2.1.0.0</AssemblyVersion>
    <FileVersion>2.1.0.0</FileVersion>
    <PackageId>Lokad.AzureEventStore</PackageId>
    <PackageVersion>2.1.0.0</PackageVersion>
    <GeneratePackageOnBuild>true</GeneratePackageOnBuild>
    <Authors>Lokad</Authors>
    <Description>Simple, low-maintenance event sourcing backed by Azure Blob Storage.</Description>
    <PackageLicenseUrl>https://github.com/Lokad/AzureEventStore/blob/master/LICENSE.txt</PackageLicenseUrl>
    <PackageProjectUrl>https://github.com/Lokad/AzureEventStore</PackageProjectUrl>
    <PackageIconUrl>https://raw.githubusercontent.com/Lokad/AzureEventStore/master/lokad.png</PackageIconUrl>
    <Version>2.1.0</Version>
  </PropertyGroup>
  <ItemGroup Condition=""'$(TargetFramework)'=='netstandard1.6'"">
    <PackageReference Include=""System.Diagnostics.Contracts"" Version=""4.3.0"" />
    <PackageReference Include=""System.Diagnostics.TraceSource"" Version=""4.3.0"" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include=""Microsoft.Azure.Storage.Blob"" Version=""9.4.1"" />
    <PackageReference Include=""Newtonsoft.Json"" Version=""12.0.1"" />
  </ItemGroup>
</Project>"));
            var entryPoint = new EntryPoint(mockFileSystem.FileInfo.FromFileName(@"c:\foo\bar\xyz.csproj"), EntryPointType.Project);
            var cut = new ProjectDependencyResolver(mockFileSystem, Mock.Of<IEnvironment>());

            Func<Task> action = async () => await cut.ResolveDependenciesAsync(entryPoint);
            (await action.Should().ThrowAsync<FileNotFoundException>()).And.Message.Should().StartWith("The file does not exist");
        }

        [Fact(Skip = ".NET SDK is not found")]
        public async Task GivenOnePathToOneExistingProjectFile_WhenThisProjectDoesHaveOneProjectAssetsFileProperty_ThenNoException_ShouldBeThrown()
        {
            var mockFileSystem = new MockFileSystem();
            mockFileSystem.AddFile(@"c:\foo\bar\xyz.csproj", new MockFileData(@"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFrameworks>netstandard1.6;net462;net452</TargetFrameworks>
    <AssemblyName>Lokad.AzureEventStore</AssemblyName>
    <RuntimeIdentifiers>win</RuntimeIdentifiers>
    <OutputType>Library</OutputType>
    <Company>Lokad</Company>
    <Copyright>Copyright © Lokad 2019</Copyright>
    <AssemblyVersion>2.1.0.0</AssemblyVersion>
    <FileVersion>2.1.0.0</FileVersion>
    <PackageId>Lokad.AzureEventStore</PackageId>
    <PackageVersion>2.1.0.0</PackageVersion>
    <GeneratePackageOnBuild>true</GeneratePackageOnBuild>
    <Authors>Lokad</Authors>
    <Description>Simple, low-maintenance event sourcing backed by Azure Blob Storage.</Description>
    <PackageLicenseUrl>https://github.com/Lokad/AzureEventStore/blob/master/LICENSE.txt</PackageLicenseUrl>
    <PackageProjectUrl>https://github.com/Lokad/AzureEventStore</PackageProjectUrl>
    <PackageIconUrl>https://raw.githubusercontent.com/Lokad/AzureEventStore/master/lokad.png</PackageIconUrl>
    <Version>2.1.0</Version>
<ProjectAssetsFile Condition="" '$(ProjectAssetsFile)' == '' "">c:\foo\bar\obj\project.assets.json</ProjectAssetsFile>
  </PropertyGroup>
  <ItemGroup Condition=""'$(TargetFramework)'=='netstandard1.6'"">
    <PackageReference Include=""System.Diagnostics.Contracts"" Version=""4.3.0"" />
    <PackageReference Include=""System.Diagnostics.TraceSource"" Version=""4.3.0"" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include=""Microsoft.Azure.Storage.Blob"" Version=""9.4.1"" />
    <PackageReference Include=""Newtonsoft.Json"" Version=""12.0.1"" />
  </ItemGroup>
</Project>"));

            var entryPoint = new EntryPoint(mockFileSystem.FileInfo.FromFileName(@"c:\foo\bar\xyz.csproj"), EntryPointType.Project);
            var cut = new ProjectDependencyResolver(mockFileSystem, Mock.Of<IEnvironment>());
            mockFileSystem.AddFile(@"c:\foo\bar\obj\project.assets.json", new MockFileData(""));

           Func<Task> action = async () => await cut.ResolveDependenciesAsync(entryPoint);
           await action.Should().NotThrowAsync();
        }

        [Fact(Skip = ".NET SDK is not found")]
        public void GivenOnePathToOneExistingProjectFile_WhenThisProjectDoesHaveOneProjectAssetsFileProperty_ButTheAssetFileDoesNotExist_ThenOneFileNotFoundException_ShouldBeThrown()
        {
            var mockFileSystem = new MockFileSystem();
            mockFileSystem.AddFile(@"c:\foo\bar\xyz.csproj", new MockFileData(@"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFrameworks>netstandard1.6;net462;net452</TargetFrameworks>
    <AssemblyName>Lokad.AzureEventStore</AssemblyName>
    <RuntimeIdentifiers>win</RuntimeIdentifiers>
    <OutputType>Library</OutputType>
    <Company>Lokad</Company>
    <Copyright>Copyright © Lokad 2019</Copyright>
    <AssemblyVersion>2.1.0.0</AssemblyVersion>
    <FileVersion>2.1.0.0</FileVersion>
    <PackageId>Lokad.AzureEventStore</PackageId>
    <PackageVersion>2.1.0.0</PackageVersion>
    <GeneratePackageOnBuild>true</GeneratePackageOnBuild>
    <Authors>Lokad</Authors>
    <Description>Simple, low-maintenance event sourcing backed by Azure Blob Storage.</Description>
    <PackageLicenseUrl>https://github.com/Lokad/AzureEventStore/blob/master/LICENSE.txt</PackageLicenseUrl>
    <PackageProjectUrl>https://github.com/Lokad/AzureEventStore</PackageProjectUrl>
    <PackageIconUrl>https://raw.githubusercontent.com/Lokad/AzureEventStore/master/lokad.png</PackageIconUrl>
    <Version>2.1.0</Version>
<ProjectAssetsFile Condition="" '$(ProjectAssetsFile)' == '' "">c:\foo\bar\obj\project.assets.json</ProjectAssetsFile>
  </PropertyGroup>
  <ItemGroup Condition=""'$(TargetFramework)'=='netstandard1.6'"">
    <PackageReference Include=""System.Diagnostics.Contracts"" Version=""4.3.0"" />
    <PackageReference Include=""System.Diagnostics.TraceSource"" Version=""4.3.0"" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include=""Microsoft.Azure.Storage.Blob"" Version=""9.4.1"" />
    <PackageReference Include=""Newtonsoft.Json"" Version=""12.0.1"" />
  </ItemGroup>
</Project>"));
            var entryPoint = new EntryPoint(mockFileSystem.FileInfo.FromFileName(@"c:\foo\bar\xyz.csproj"), EntryPointType.Project);
            var cut = new ProjectDependencyResolver(mockFileSystem, Mock.Of<IEnvironment>());

            Func<Task> action = async () => await cut.ResolveDependenciesAsync(entryPoint);
            action.Should().ThrowAsync<FileNotFoundException>();
        }
    }
}
