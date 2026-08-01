# Personix.Options

Shared DI options validation infrastructure for .NET services.

## Contents

- `IOption` – interface with `static abstract string SectionName` for strongly-typed config sections.
- `OptionsStartupValidator` – extension method `RegisterAndValidateOptions<T>()` that binds, validates (data annotations), and returns the options instance at startup.
- `ConnectionStringsOption` – abstract base for options bound to the `ConnectionStrings` config section.
- `ValidPathAttribute` – data annotation for validating path format (no invalid characters, parseable by `Path.GetFullPath`). Does **not** verify existence — that must be done separately at startup.

## Usage

### 1. Installation

```xml
<PackageReference Include="Personix.Options" Version="1.0.0" />
```

### 2. Define your options class

```csharp
using System.ComponentModel.DataAnnotations;
using Personix.Options;

public class MyOptions : IOption
{
    public static string SectionName => "MySection";

    [Required]
    [MinLength(10)]
    public string ApiKey { get; set; } = null!;

    [Range(1, 100)]
    public int MaxRetries { get; set; } = 3;

    [Url]
    public string? BaseUrl { get; set; }
}
```

### 3. Configure in appsettings.json

```json
{
  "MySection": {
    "ApiKey": "your-api-key-here",
    "MaxRetries": 5,
    "BaseUrl": "https://api.example.com"
  }
}
```

### 4. Register in Program.cs

```csharp
using Personix.Options;

// Registers, validates (data annotations), and returns the options instance
// Throws OptionsValidationException at startup if validation fails
var myOptions = services.RegisterAndValidateOptions<MyOptions>(configuration);

// The options are also registered in DI and can be injected:
// - IOptions<MyOptions>
// - IOptionsSnapshot<MyOptions>
// - IOptionsMonitor<MyOptions>
```

### Using ConnectionStringsOption

For connection strings, inherit from `ConnectionStringsOption`:

```csharp
using System.ComponentModel.DataAnnotations;
using Personix.Options;

public class DatabaseOptions : ConnectionStringsOption
{
    [Required]
    public string DefaultConnection { get; set; } = null!;
}
```

Configure in appsettings.json:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=mydb;..."
  }
}
```

Register in Program.cs:

```csharp
var dbOptions = services.RegisterAndValidateOptions<DatabaseOptions>(configuration);
```

## Validation

- Uses standard **Data Annotations** attributes: `[Required]`, `[Range]`, `[MinLength]`, `[MaxLength]`, `[RegularExpression]`, `[Url]`, `[EmailAddress]`, `[ValidPath]`, etc.
- Validation occurs **at startup** (via `ValidateOnStart()`)
- Throws `OptionsValidationException` if validation fails, preventing the application from starting with invalid configuration
- Configuration section is bound using `IConfiguration.GetRequiredSection()` – throws if the section is missing

### `ValidPathAttribute`

```csharp
public sealed class MyOptions : IOption
{
    public static string SectionName => "MySection";

    [Required]
    [ValidPath]
    public string OutputDirectory { get; set; } = string.Empty;
}
```

The attribute validates path **format** only (invalid characters, `Path.GetFullPath` parseability). To verify that the path actually exists, check separately after `RegisterAndValidateOptions` returns:

```csharp
var options = services.RegisterAndValidateOptions<MyOptions>(configuration);
if (!Directory.Exists(options.OutputDirectory))
    throw new DirectoryNotFoundException($"Directory not found: {options.OutputDirectory}");
```

## Project Structure

```
Options/
├── src/
│   └── Options/           # Main library code
└── tests/
    └── Options.Tests/     # Unit tests
```

## Development

### Running Tests

```bash
dotnet test
```

### Building

```bash
dotnet build
```

### Packing

```bash
dotnet pack -c Release
```
