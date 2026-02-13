# Nwo.Options

Shared DI options validation infrastructure for new-world-order services.

## Contents

- `IOption` – interface with `static abstract string SectionName` for strongly-typed config sections.
- `OptionsStartupValidator` – extension method `RegisterAndValidateOptions<T>()` that binds, validates (data annotations), and returns the options instance at startup.
- `ConnectionStringsOption` – abstract base for options bound to the `ConnectionStrings` config section.

## Usage

```xml
<PackageReference Include="Nwo.Options" Version="1.0.0" />
```

```csharp
using Options;

public class MyOptions : IOption
{
    public static string SectionName => "MySection";

    [Required]
    public string ApiKey { get; set; } = null!;
}

// In Program.cs – throws at startup if validation fails
var opts = services.RegisterAndValidateOptions<MyOptions>(configuration);
```

## Part of the NWO package family

| Package | Description |
|---------|-------------|
| Nwo.Constants | Shared constants |
| **Nwo.Options** | DI options validation |
| Nwo.StartUp | Startup coordination |
| Nwo.Persistence | EF Core / SQLite base |
| Nwo.ServiceDefaults | Aspire service defaults (OTel, Serilog, health checks) |
