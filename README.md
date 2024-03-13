<p align="center">
  <picture>
    <source media="(prefers-color-scheme: dark)" srcset="https://support.crowdin.com/assets/logos/symbol/png/crowdin-symbol-cWhite.png">
    <source media="(prefers-color-scheme: light)" srcset="https://support.crowdin.com/assets/logos/symbol/png/crowdin-symbol-cDark.png">
    <img width="150" height="150" width=""src="[https://support.crowdin.com/assets/logos/symbol/png/crowdin-symbol-cDark.png](https://crowdin.com)">
  </picture>
</p>

# Crowdin .NET SDK

Unofficial fork of https://github.com/crowdin/xamarin-sdk that targets *.NET Standard 2.0* and has a more generic working logic.

<div align="center">
  
[![Nuget](https://img.shields.io/nuget/v/Crowdin.Xamarin.Forms?cacheSeconds=5000&logo=nuget)](https://www.nuget.org/packages/Crowdin.Net/)
[![Nuget](https://img.shields.io/nuget/dt/Crowdin.Xamarin.Forms?cacheSeconds=800&logo=nuget)](https://www.nuget.org/packages/Crowdin.Net/)
[![GitHub](https://img.shields.io/github/license/crowdin/xamarin-sdk?cacheSeconds=20000)](https://github.com/crowdin/xamarin-sdk/blob/master/LICENSE)

</div>

### Features

+ Load remote strings from Crowdin Over-The-Air Content Delivery Network
+ Built-in translations caching mechanism (enabled by default, can be disabled)
+ Network usage configuration (All, only Wi-Fi or Cellular, Forbidden)
+ Load static strings from the bundled RESX/RESW files (usable as fallback before the CDN strings loaded)


### Requirements

* .NET Standard 2.0 support

### Installation

Install via NuGet:

```
// Package Manager
Install-Package Crowdin.Net -Version 1.0.0

// .Net CLI
dotnet add package Crowdin.Net --version 1.0.0

// Package Reference
<PackageReference Include="Crowdin.Net" Version="1.0.0" />

// Paket CLI
paket add Crowdin.Net --version 1.0.0
```

### Quick start

For applications using the XML resource localization files (RESX/RESW)

1) Add Crowdin Distribution Hash before any modules initialization:

    ```C#
    DynamicResourcesLoader.GlobalOptions.DistributionHash = "{your_distribution_hash}";
    ```

2) Load static strings from app resource files to use as fallback:

    ```C#
    DynamicResourcesLoader.LoadStaticStrings(Translations.ResourceManager, Current.Resources);
    ```

    The first argument is the source - `ResourceManager` of the generated class from resources group (`Translations.resx` and descendants).
    The second argument is the destination - `ResourceDictionary` where to place loaded strings:

    * Global: in `Application.Current.Resources`
    * Per-view: in `ContentPage.Resources`

3) Load strings from Crowdin Distributions CDN:

    ```C#
    string langCode = DynamicResourcesLoader.CurrentCulture.TwoLetterISOLanguageName;
    DynamicResourcesLoader.LoadCrowdinStrings($"Translations.{langCode}.resx", Current.Resources);
    ```

    The property `CurrentCulture` provides end-user OS locale by default.
    It can be overridden by the developer if needed.

    The method `LoadCrowdinStrings` is async and can be awaited if needed.

    In this example, the method is not “awaited” not to block the rendering of the user’s interface.

### Configuration

The SDK provides developers two ways for resources loading configuration: global and per-request:

```C#
var options = new CrowdinOptions
{
    DistributionHash = "<your_distribution_hash>",
    NetworkPolicy = NetworkPolicy.OnlyWiFi,
    UseCache = true,
    FileName = "Translations.resx"
};
```

+ `NetworkPolicy` - for network restrictions
  + `All` - all network types allowed
  + `OnlyWiFi` or `OnlyCellular` - only needed type allowed
  + `Forbidden`

+ `UseCache` - turn on or off built-in translations caching mechanism.

For global configuration override default `GlobalOptions`:

```C#
DynamicResourcesLoader.GlobalOptions = options;
```

For per-request configuration pass `options` as the first parameter:

```C#
DynamicResourcesLoader.LoadCrowdinStrings(options, Current.Resources);
```

In a last way don't forget to add `options.FileName` value. Please note - for this example we used the 'Translations.resx' file name. This name should correspond to the file name in Crowdin.

### Translations caching algorithm

Every time the method `DynamicResourcesLoader.LoadCrowdinStrings` is executed the module checks the conditions for obtaining resources, updating or creating a cache (if it does not exist).

1) If the cache is disabled and the network is unavailable - exit;
2) If the cache is disabled, the network is available - resources are downloaded directly from the Crowdin CDN every time;
3) If the cache is enabled, not yet created, the network is not available - exit;
4) If the cache is enabled, not yet created, the network is available - resources are downloaded from the CDN and stored in the cache;
5) If the cache is enabled, created - the cache is used first. The following is executed in the background thread:
    * Check network availability. If not - exit;
    * Update Crowdin manifest with updated translation date;
    * Comparison of creation dates of cached and remote resources. If they are the same - exit;
    * Update resources from Crowdin CDN;

P.S. The network is considered unavailable even if the user has forbidden its use in the loader settings.

