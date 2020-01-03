# LicenseGatherer

[![Build Status](https://manne.visualstudio.com/public/_apis/build/status/manne.dotnet-license-gatherer?branchName=master)](https://manne.visualstudio.com/public/_build/latest?definitionId=1&branchName=master) ![Nuget](https://img.shields.io/nuget/v/LicenseGatherer?style=flat-square) ![GitHub](https://img.shields.io/github/license/manne/dotnet-license-gatherer.svg?style=flat-square)

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

### Project

From **global** tool

```batch
license-gatherer -p c:\your\path\to\the\projectfile.csproj -o licenses.json
```

From **local** tool

```batch
dotnet tool run license-gatherer -p c:\your\path\to\the\projectfile.csproj -o licenses.json
```

### Solution

From **global** tool

```batch
license-gatherer -p c:\your\path\to\the\solutionfile.sln -o licenses.json
```

From **local** tool

```batch
dotnet tool run license-gatherer -p c:\your\path\to\the\solutionfile.sln -o licenses.json
```

## License

Licensed under [MIT](LICENSE.txt)
