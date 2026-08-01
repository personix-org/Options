using System.ComponentModel.DataAnnotations;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace Personix.Options.Tests;

public class OptionsStartupValidatorTests
{
    // Test options class with various validation attributes
    private class ValidTestOptions : IOption
    {
        public static string SectionName => "TestSection";

        [Required]
        [MinLength(5)]
        public string RequiredField { get; set; } = null!;

        [Range(1, 100)]
        public int RangeField { get; set; }

        [Url]
        public string? OptionalUrl { get; set; }
    }

    private class EmptyOptions : IOption
    {
        public static string SectionName => "EmptySection";
        public string? SomeProperty { get; set; }
    }

    private class ComplexValidationOptions : IOption
    {
        public static string SectionName => "ComplexSection";

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = null!;

        [RegularExpression(@"^\d{3}-\d{3}-\d{4}$", ErrorMessage = "Phone must be in format XXX-XXX-XXXX")]
        public string? Phone { get; set; }

        [MaxLength(50, ErrorMessage = "Name cannot exceed 50 characters")]
        public string? Name { get; set; }
    }

    [Fact]
    public void RegisterAndValidateOptions_WithValidConfiguration_ShouldReturnOptionsInstance()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "TestSection:RequiredField", "ValidValue" },
                { "TestSection:RangeField", "50" },
                { "TestSection:OptionalUrl", "https://example.com" }
            })
            .Build();

        var services = new ServiceCollection();

        // Act
        var options = services.RegisterAndValidateOptions<ValidTestOptions>(configuration);

        // Assert
        options.ShouldNotBeNull();
        options.RequiredField.ShouldBe("ValidValue");
        options.RangeField.ShouldBe(50);
        options.OptionalUrl.ShouldBe("https://example.com");
    }

    [Fact]
    public void RegisterAndValidateOptions_WithValidConfiguration_ShouldRegisterInDI()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "TestSection:RequiredField", "ValidValue" },
                { "TestSection:RangeField", "50" }
            })
            .Build();

        var services = new ServiceCollection();

        // Act
        services.RegisterAndValidateOptions<ValidTestOptions>(configuration);
        var serviceProvider = services.BuildServiceProvider();
        var optionsFromDI = serviceProvider.GetService<IOptions<ValidTestOptions>>();

        // Assert
        optionsFromDI.ShouldNotBeNull();
        optionsFromDI.Value.RequiredField.ShouldBe("ValidValue");
    }

    [Fact]
    public void RegisterAndValidateOptions_WithMissingRequiredField_ShouldThrowOptionsValidationException()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "TestSection:RangeField", "50" }
                // RequiredField is missing
            })
            .Build();

        var services = new ServiceCollection();

        // Act & Assert
        var exception = Should.Throw<OptionsValidationException>(
            () => services.RegisterAndValidateOptions<ValidTestOptions>(configuration));
        exception.Message.ShouldContain("RequiredField");
    }

    [Fact]
    public void RegisterAndValidateOptions_WithTooShortValue_ShouldThrowOptionsValidationException()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "TestSection:RequiredField", "123" }, // Only 3 chars, needs at least 5
                { "TestSection:RangeField", "50" }
            })
            .Build();

        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<OptionsValidationException>(
            () => services.RegisterAndValidateOptions<ValidTestOptions>(configuration));
    }

    [Fact]
    public void RegisterAndValidateOptions_WithOutOfRangeValue_ShouldThrowOptionsValidationException()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "TestSection:RequiredField", "ValidValue" },
                { "TestSection:RangeField", "999" } // Out of range (1-100)
            })
            .Build();

        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<OptionsValidationException>(
            () => services.RegisterAndValidateOptions<ValidTestOptions>(configuration));
    }

    [Fact]
    public void RegisterAndValidateOptions_WithInvalidUrl_ShouldThrowOptionsValidationException()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "TestSection:RequiredField", "ValidValue" },
                { "TestSection:RangeField", "50" },
                { "TestSection:OptionalUrl", "not-a-valid-url" }
            })
            .Build();

        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<OptionsValidationException>(
            () => services.RegisterAndValidateOptions<ValidTestOptions>(configuration));
    }

    [Fact]
    public void RegisterAndValidateOptions_WithMissingSection_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var services = new ServiceCollection();

        // Act & Assert
        var exception = Should.Throw<InvalidOperationException>(
            () => services.RegisterAndValidateOptions<ValidTestOptions>(configuration));
        exception.Message.ShouldContain("TestSection");
    }

    [Fact]
    public void RegisterAndValidateOptions_WithEmptySection_ShouldSucceedWithDefaults()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "EmptySection:SomeProperty", "" }
            })
            .Build();

        var services = new ServiceCollection();

        // Act
        var options = services.RegisterAndValidateOptions<EmptyOptions>(configuration);

        // Assert
        options.ShouldNotBeNull();
        options.SomeProperty.ShouldBeEmpty();
    }

    [Fact]
    public void RegisterAndValidateOptions_WithNullOptionalField_ShouldSucceed()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "TestSection:RequiredField", "ValidValue" },
                { "TestSection:RangeField", "50" }
                // OptionalUrl is not provided
            })
            .Build();

        var services = new ServiceCollection();

        // Act
        var options = services.RegisterAndValidateOptions<ValidTestOptions>(configuration);

        // Assert
        options.ShouldNotBeNull();
        options.OptionalUrl.ShouldBeNull();
    }

    [Fact]
    public void RegisterAndValidateOptions_WithMinimumRangeValue_ShouldSucceed()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "TestSection:RequiredField", "ValidValue" },
                { "TestSection:RangeField", "1" } // Minimum valid value
            })
            .Build();

        var services = new ServiceCollection();

        // Act
        var options = services.RegisterAndValidateOptions<ValidTestOptions>(configuration);

        // Assert
        options.RangeField.ShouldBe(1);
    }

    [Fact]
    public void RegisterAndValidateOptions_WithMaximumRangeValue_ShouldSucceed()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "TestSection:RequiredField", "ValidValue" },
                { "TestSection:RangeField", "100" } // Maximum valid value
            })
            .Build();

        var services = new ServiceCollection();

        // Act
        var options = services.RegisterAndValidateOptions<ValidTestOptions>(configuration);

        // Assert
        options.RangeField.ShouldBe(100);
    }

    [Fact]
    public void RegisterAndValidateOptions_WithZeroRangeValue_ShouldThrowOptionsValidationException()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "TestSection:RequiredField", "ValidValue" },
                { "TestSection:RangeField", "0" } // Below minimum
            })
            .Build();

        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<OptionsValidationException>(
            () => services.RegisterAndValidateOptions<ValidTestOptions>(configuration));
    }

    [Fact]
    public void RegisterAndValidateOptions_WithNegativeRangeValue_ShouldThrowOptionsValidationException()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "TestSection:RequiredField", "ValidValue" },
                { "TestSection:RangeField", "-5" }
            })
            .Build();

        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<OptionsValidationException>(
            () => services.RegisterAndValidateOptions<ValidTestOptions>(configuration));
    }

    [Fact]
    public void RegisterAndValidateOptions_WithComplexValidation_ValidEmail_ShouldSucceed()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ComplexSection:Email", "test@example.com" },
                { "ComplexSection:Phone", "123-456-7890" },
                { "ComplexSection:Name", "John Doe" }
            })
            .Build();

        var services = new ServiceCollection();

        // Act
        var options = services.RegisterAndValidateOptions<ComplexValidationOptions>(configuration);

        // Assert
        options.Email.ShouldBe("test@example.com");
        options.Phone.ShouldBe("123-456-7890");
        options.Name.ShouldBe("John Doe");
    }

    [Fact]
    public void RegisterAndValidateOptions_WithComplexValidation_InvalidEmail_ShouldThrowOptionsValidationException()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ComplexSection:Email", "not-an-email" }
            })
            .Build();

        var services = new ServiceCollection();

        // Act & Assert
        var exception = Should.Throw<OptionsValidationException>(
            () => services.RegisterAndValidateOptions<ComplexValidationOptions>(configuration));
        exception.Message.ShouldContain("Invalid email format");
    }

    [Fact]
    public void RegisterAndValidateOptions_WithComplexValidation_InvalidPhoneFormat_ShouldThrowOptionsValidationException()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ComplexSection:Email", "test@example.com" },
                { "ComplexSection:Phone", "1234567890" } // Wrong format
            })
            .Build();

        var services = new ServiceCollection();

        // Act & Assert
        var exception = Should.Throw<OptionsValidationException>(
            () => services.RegisterAndValidateOptions<ComplexValidationOptions>(configuration));
        exception.Message.ShouldContain("XXX-XXX-XXXX");
    }

    [Fact]
    public void RegisterAndValidateOptions_WithComplexValidation_NameTooLong_ShouldThrowOptionsValidationException()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ComplexSection:Email", "test@example.com" },
                { "ComplexSection:Name", new string('A', 51) } // 51 chars, max is 50
            })
            .Build();

        var services = new ServiceCollection();

        // Act & Assert
        var exception = Should.Throw<OptionsValidationException>(
            () => services.RegisterAndValidateOptions<ComplexValidationOptions>(configuration));
        exception.Message.ShouldContain("cannot exceed 50 characters");
    }

    [Fact]
    public void RegisterAndValidateOptions_WithExactlyMinLength_ShouldSucceed()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "TestSection:RequiredField", "12345" }, // Exactly 5 chars (minimum)
                { "TestSection:RangeField", "50" }
            })
            .Build();

        var services = new ServiceCollection();

        // Act
        var options = services.RegisterAndValidateOptions<ValidTestOptions>(configuration);

        // Assert
        options.RequiredField.ShouldBe("12345");
    }

    [Fact]
    public void RegisterAndValidateOptions_MultipleValidationErrors_ShouldThrowWithAllErrors()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "TestSection:RequiredField", "123" }, // Too short
                { "TestSection:RangeField", "999" }, // Out of range
                { "TestSection:OptionalUrl", "invalid-url" } // Invalid URL
            })
            .Build();

        var services = new ServiceCollection();

        // Act & Assert
        var exception = Should.Throw<OptionsValidationException>(
            () => services.RegisterAndValidateOptions<ValidTestOptions>(configuration));
        exception.Failures.Count().ShouldBeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public void RegisterAndValidateOptions_RegistersOnStartValidation_ThatFailsWithoutAnyComponentReadingOptionsValue()
    {
        // The eager check inside RegisterAndValidateOptions already forces validation once,
        // synchronously, against whatever the configuration holds right now. That alone cannot
        // prove the *separate* startup-time guard (ValidateOnStart) is actually wired into the
        // real `services` collection the caller keeps using. To observe ValidateOnStart's own
        // contribution we need configuration that changes to become invalid *after*
        // registration -- exactly what the host's IStartupValidator is meant to catch before any
        // component ever touches IOptions<T>.Value. A reloadable JSON file is the realistic way
        // that happens in production (e.g. a mounted config file changing between app startup
        // and IHost.StartAsync()).
        var configPath = Path.Combine(Path.GetTempPath(), $"personix-options-validate-on-start-{Guid.NewGuid():N}.json");
        File.WriteAllText(configPath, """{"TestSection":{"RequiredField":"ValidValue","RangeField":50}}""");

        try
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile(configPath, optional: false, reloadOnChange: false)
                .Build();
            var services = new ServiceCollection();

            // Arrange: registration succeeds now, because the file is currently valid.
            services.RegisterAndValidateOptions<ValidTestOptions>(configuration);

            // Configuration drifts to invalid *after* registration completed.
            File.WriteAllText(configPath, """{"TestSection":{"RequiredField":""}}""");
            ((IConfigurationRoot)configuration).Reload();

            var provider = services.BuildServiceProvider();
            var startupValidator = provider.GetRequiredService<IStartupValidator>();

            // Act & Assert: this is what IHost.StartAsync() invokes before the host starts
            // serving requests. Nothing here ever accessed IOptions<ValidTestOptions>.Value.
            Should.Throw<OptionsValidationException>(() => startupValidator.Validate());
        }
        finally
        {
            File.Delete(configPath);
        }
    }
}
