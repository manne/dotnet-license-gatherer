# Manual Testing

## Install globally from Azure Artifact

```batch
dotnet tool install LicenseGatherer --add-source https://manne.pkgs.visualstudio.com/public/_packaging/Preview/nuget/v3/index.json --global --version ZZZZ
```

## Execute tool

```batch
license-gatherer -p ".\license-gatherer.sln" -o "src\LicenseGatherer\bin\Debug\netcoreapp3.1\licenses3.json"
```

## Uninstall globally

```batch
dotnet tool uninstall LicenseGatherer --global
```
