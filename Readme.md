# LicenseGatherer

[![Build Status](https://manne.visualstudio.com/public/_apis/build/status/manne.dotnet-license-gatherer?branchName=master&stageName=Build)](https://manne.visualstudio.com/public/_build/latest?definitionId=1&branchName=master) [![Nuget](https://img.shields.io/nuget/v/LicenseGatherer?style=flat-square)](https://www.nuget.org/packages/LicenseGatherer/) [![GitHub License](https://img.shields.io/github/license/manne/dotnet-license-gatherer.svg?style=flat-square)](https://github.com/manne/dotnet-license-gatherer/blob/master/LICENSE.txt)

> LicenseGatherer provides license information from all transitive NuGet dependencies of your solution.

## Limitation

This tool only gathers licenses from projects using the project SDK.

## Installation

As **global** tool

```batch
dotnet tool install --global LicenseGatherer
```

As **local** tool

```batch
dotnet tool install LicenseGatherer
```

## Usage

```text
Usage: LicenseGatherer [options]

Options:
  -p|--path <PATH_TO_PROJECT_OR_SOLUTION>  The path of the project or solution to gather the licenses. A directory can be specified, the value
                                           must end with \, then for a solution in the working directory is searched. (optional)
  -o|--outputpath <OUTPUT_PATH>            The path of the JSON content output. If the no value is specified some information is printed into
                                           the console. (optional)
  -s|--skipdownload                        Skip the download of licenses
  -?|-h|--help                             Show help information
```

### JSON Output

The generated JSON file consists of the following schema:

An array of **package**s.

#### Package

One package has these properties

| Name                                    | Type   | Explanation                                                                          |
|-----------------------------------------|--------|--------------------------------------------------------------------------------------|
| [PackageReference](#packagereference)   | object | An object containing information of the package.                                     |
| LicenseContent                          | string | The content of the license.                                                          |
| OriginalLicenseLocation                 | string | The url of the given license.                                                        |
| DownloadedLicenseLocation               | string | The corrected url of the license. E.g. It replaces the github url with the raw once. |
| [LicenseExpression](#licenseexpression) | object | The license expression of the package.                                               |
| Authors                                 | string | The authors of the package.                                                          |

#### LicenseExpression

Contains an object of the type [NuGet.Packaging.Licenses.NuGetLicense](https://github.com/NuGet/NuGet.Client/blob/dev/src/NuGet.Core/NuGet.Packaging/Licenses/NuGetLicense.cs)

| Name              | Type    | Explanation                                                                            |
|-------------------|---------|----------------------------------------------------------------------------------------|
| Identifier        | string  | The identifier according to [spdx](https://spdx.org/spdx-specification-21-web-version) |
| Plus              | boolean | Signifies whether the plus operator has been specified on this license                 |
| IsStandardLicense | boolean | Signifies whether this is a standard license known by the NuGet APIs                   |
| Type              | integer | 0: License, 1: Operator                                                                |

#### PackageReference

| Name                                | Type   | Explanation                                                                      |
|-------------------------------------|--------|----------------------------------------------------------------------------------|
| Name                                | string | The name of the package dependency                                               |
| [ResolvedVersion](#resolvedversion) | object | The runtime version. This value can differ from the version in the configuration |

#### ResolvedVersion

Contains an object of the type [NuGet.Versioning.NuGetVersion](https://github.com/NuGet/NuGet.Client/blob/dev/src/NuGet.Core/NuGet.Versioning/NuGetVersion.cs).

| Name            | Type            | Explanation                                                                        |
|-----------------|-----------------|------------------------------------------------------------------------------------|
| Version         | string          | A System.Version representation of the version without metadata or release labels. |
| IsLegacyVersion | boolean         | True if the NuGetVersion is using legacy behavior.                                 |
| Revision        | integer         | Revision version R (x.y.z.R)                                                       |
| IsSemVer2       | boolean         | Returns true if version is a SemVer 2.0.0 version                                  |
| OriginalVersion | string          | Returns the original, non-normalized version string.                               |
| Major           | integer         | Major version X (X.y.z)                                                            |
| Minor           | integer         | Minor version Y (x.Y.z)                                                            |
| Patch           | integer         | Patch version Z (x.y.Z)                                                            |
| ReleaseLabels   | array of string | A collection of pre-release labels attached to the version.                        |
| Release         | string          | The full pre-release label for the version.                                        |
| IsPrerelease    | boolean         | True if pre-release labels exist for the version.                                  |
| HasMetadata     | boolean         | True if metadata exists for the version.                                           |
| Metadata        | string          | Build metadata attached to the version.                                            |

## Contribution

* Create a fork and make a Pull Request
* Submit a bug
* Submit an idea

## License

Licensed under [MIT](LICENSE.txt)
