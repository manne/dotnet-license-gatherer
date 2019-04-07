# dotnet-license-gatherer

[![Build Status](https://manne.visualstudio.com/public/_apis/build/status/manne.dotnet-license-gatherer?branchName=master)](https://manne.visualstudio.com/public/_build/latest?definitionId=1&branchName=master) ![GitHub](https://img.shields.io/github/license/manne/dotnet-license-gatherer.svg?style=flat-square)

> dotnet-license-gatherer provides license information from all transitive NuGet dependencies of your solution.

## Installation

> Coming soon!

## Usage

### Project

```batch
dotnet .\LicenseGatherer.dll -p c:\your\path\to\the\projectfile.csproj -o licenses.json
```

### Solution

```batch
dotnet .\LicenseGatherer.dll -p c:\your\path\to\the\solutionfile.sln -o licenses.json
```

## License

Licensed under [MIT](LICENSE.txt)