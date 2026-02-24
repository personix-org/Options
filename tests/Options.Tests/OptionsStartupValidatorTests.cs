using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Options;
using Xunit;

namespace Options.Tests;

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
        options.Should().NotBeNull();
        options.RequiredField.Should().Be("ValidValue");
        options.RangeField.Should().Be(50);
        options.OptionalUrl.Should().Be("https://example.com");
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
        optionsFromDI.Should().NotBeNull();
        optionsFromDI!.Value.RequiredField.Should().Be("ValidValue");
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
        var act = () => services.RegisterAndValidateOptions<ValidTestOptions>(configuration);
        act.Should().Throw<OptionsValidationException>()
            .WithMessage("*RequiredField*");
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
        var act = () => services.RegisterAndValidateOptions<ValidTestOptions>(configuration);
        act.Should().Throw<OptionsValidationException>();
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
        var act = () => services.RegisterAndValidateOptions<ValidTestOptions>(configuration);
        act.Should().Throw<OptionsValidationException>();
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
        var act = () => services.RegisterAndValidateOptions<ValidTestOptions>(configuration);
        act.Should().Throw<OptionsValidationException>();
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
        var act = () => services.RegisterAndValidateOptions<ValidTestOptions>(configuration);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*TestSection*");
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
        options.Should().NotBeNull();
        options.SomeProperty.Should().BeEmpty();
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
        options.Should().NotBeNull();
        options.OptionalUrl.Should().BeNull();
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
        options.RangeField.Should().Be(1);
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
        options.RangeField.Should().Be(100);
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
        var act = () => services.RegisterAndValidateOptions<ValidTestOptions>(configuration);
        act.Should().Throw<OptionsValidationException>();
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
        var act = () => services.RegisterAndValidateOptions<ValidTestOptions>(configuration);
        act.Should().Throw<OptionsValidationException>();
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
        options.Email.Should().Be("test@example.com");
        options.Phone.Should().Be("123-456-7890");
        options.Name.Should().Be("John Doe");
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
        var act = () => services.RegisterAndValidateOptions<ComplexValidationOptions>(configuration);
        act.Should().Throw<OptionsValidationException>()
            .WithMessage("*Invalid email format*");
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
        var act = () => services.RegisterAndValidateOptions<ComplexValidationOptions>(configuration);
        act.Should().Throw<OptionsValidationException>()
            .WithMessage("*XXX-XXX-XXXX*");
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
        var act = () => services.RegisterAndValidateOptions<ComplexValidationOptions>(configuration);
        act.Should().Throw<OptionsValidationException>()
            .WithMessage("*cannot exceed 50 characters*");
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
        options.RequiredField.Should().Be("12345");
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
        var act = () => services.RegisterAndValidateOptions<ValidTestOptions>(configuration);
        var exception = act.Should().Throw<OptionsValidationException>().Which;
        exception.Failures.Should().HaveCountGreaterOrEqualTo(2);
    }
}

