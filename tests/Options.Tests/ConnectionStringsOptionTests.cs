using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Options;
using Xunit;

namespace Options.Tests;

public class ConnectionStringsOptionTests
{
    private class TestConnectionStringsOption : ConnectionStringsOption
    {
        [Required(ErrorMessage = "DefaultConnection is required")]
        public string DefaultConnection { get; set; } = null!;

        [MinLength(10, ErrorMessage = "Connection string must be at least 10 characters")]
        public string? SecondaryConnection { get; set; }
    }

    private class MultiConnectionOption : ConnectionStringsOption
    {
        [Required]
        public string DatabaseA { get; set; } = null!;

        [Required]
        public string DatabaseB { get; set; } = null!;

        [Url]
        public string? RedisUrl { get; set; }
    }

    [Fact]
    public void ConnectionStringsOption_ShouldHaveCorrectSectionName()
    {
        // Assert
        ConnectionStringsOption.SectionName.Should().Be("ConnectionStrings");
    }

    [Fact]
    public void ConnectionStringsOption_WithValidConfiguration_ShouldBindCorrectly()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ConnectionStrings:DefaultConnection", "Server=localhost;Database=test;User=sa;Password=Pass123;" },
                { "ConnectionStrings:SecondaryConnection", "Server=remote;Database=backup;" }
            })
            .Build();

        var services = new ServiceCollection();

        // Act
        var options = services.RegisterAndValidateOptions<TestConnectionStringsOption>(configuration);

        // Assert
        options.DefaultConnection.Should().Be("Server=localhost;Database=test;User=sa;Password=Pass123;");
        options.SecondaryConnection.Should().Be("Server=remote;Database=backup;");
    }

    [Fact]
    public void ConnectionStringsOption_WithMissingRequiredConnection_ShouldThrowOptionsValidationException()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ConnectionStrings:SecondaryConnection", "Server=remote;Database=backup;" }
                // DefaultConnection is missing
            })
            .Build();

        var services = new ServiceCollection();

        // Act & Assert
        var act = () => services.RegisterAndValidateOptions<TestConnectionStringsOption>(configuration);
        act.Should().Throw<Microsoft.Extensions.Options.OptionsValidationException>()
            .WithMessage("*DefaultConnection is required*");
    }

    [Fact]
    public void ConnectionStringsOption_WithTooShortConnectionString_ShouldThrowOptionsValidationException()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ConnectionStrings:DefaultConnection", "Server=localhost;Database=test;" },
                { "ConnectionStrings:SecondaryConnection", "short" } // Less than 10 chars
            })
            .Build();

        var services = new ServiceCollection();

        // Act & Assert
        var act = () => services.RegisterAndValidateOptions<TestConnectionStringsOption>(configuration);
        act.Should().Throw<Microsoft.Extensions.Options.OptionsValidationException>()
            .WithMessage("*must be at least 10 characters*");
    }

    [Fact]
    public void ConnectionStringsOption_WithMissingSection_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var services = new ServiceCollection();

        // Act & Assert
        var act = () => services.RegisterAndValidateOptions<TestConnectionStringsOption>(configuration);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*ConnectionStrings*");
    }

    [Fact]
    public void ConnectionStringsOption_WithEmptyConnectionString_ShouldThrowOptionsValidationException()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ConnectionStrings:DefaultConnection", "" }
            })
            .Build();

        var services = new ServiceCollection();

        // Act & Assert
        var act = () => services.RegisterAndValidateOptions<TestConnectionStringsOption>(configuration);
        act.Should().Throw<Microsoft.Extensions.Options.OptionsValidationException>();
    }

    [Fact]
    public void ConnectionStringsOption_WithMultipleConnections_ShouldBindAllCorrectly()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ConnectionStrings:DatabaseA", "Server=serverA;Database=dbA;" },
                { "ConnectionStrings:DatabaseB", "Server=serverB;Database=dbB;" },
                { "ConnectionStrings:RedisUrl", "https://redis.example.com:6379" }
            })
            .Build();

        var services = new ServiceCollection();

        // Act
        var options = services.RegisterAndValidateOptions<MultiConnectionOption>(configuration);

        // Assert
        options.DatabaseA.Should().Be("Server=serverA;Database=dbA;");
        options.DatabaseB.Should().Be("Server=serverB;Database=dbB;");
        options.RedisUrl.Should().Be("https://redis.example.com:6379");
    }

    [Fact]
    public void ConnectionStringsOption_WithInvalidRedisUrl_ShouldThrowOptionsValidationException()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ConnectionStrings:DatabaseA", "Server=serverA;Database=dbA;" },
                { "ConnectionStrings:DatabaseB", "Server=serverB;Database=dbB;" },
                { "ConnectionStrings:RedisUrl", "not-a-valid-url" }
            })
            .Build();

        var services = new ServiceCollection();

        // Act & Assert
        var act = () => services.RegisterAndValidateOptions<MultiConnectionOption>(configuration);
        act.Should().Throw<Microsoft.Extensions.Options.OptionsValidationException>();
    }

    [Fact]
    public void ConnectionStringsOption_WithOptionalConnectionNotProvided_ShouldSucceed()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ConnectionStrings:DefaultConnection", "Server=localhost;Database=test;" }
                // SecondaryConnection not provided
            })
            .Build();

        var services = new ServiceCollection();

        // Act
        var options = services.RegisterAndValidateOptions<TestConnectionStringsOption>(configuration);

        // Assert
        options.DefaultConnection.Should().NotBeNullOrEmpty();
        options.SecondaryConnection.Should().BeNull();
    }

    [Fact]
    public void ConnectionStringsOption_WithExactlyMinLength_ShouldSucceed()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ConnectionStrings:DefaultConnection", "Server=localhost;" },
                { "ConnectionStrings:SecondaryConnection", "1234567890" } // Exactly 10 chars
            })
            .Build();

        var services = new ServiceCollection();

        // Act
        var options = services.RegisterAndValidateOptions<TestConnectionStringsOption>(configuration);

        // Assert
        options.SecondaryConnection.Should().Be("1234567890");
    }

    [Fact]
    public void ConnectionStringsOption_WithWhitespaceOnly_ShouldThrowOptionsValidationException()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ConnectionStrings:DefaultConnection", "   " }
            })
            .Build();

        var services = new ServiceCollection();

        // Act & Assert
        var act = () => services.RegisterAndValidateOptions<TestConnectionStringsOption>(configuration);
        act.Should().Throw<Microsoft.Extensions.Options.OptionsValidationException>();
    }

    [Theory]
    [InlineData("Server=localhost;Database=mydb;Integrated Security=true;")]
    [InlineData("Server=tcp:server.database.windows.net,1433;Database=mydb;User ID=user@server;Password=pass;Encrypt=True;")]
    [InlineData("mongodb://localhost:27017/mydb")]
    [InlineData("Host=localhost;Port=5432;Database=mydb;Username=postgres;Password=pass;")]
    public void ConnectionStringsOption_WithVariousValidFormats_ShouldSucceed(string connectionString)
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ConnectionStrings:DefaultConnection", connectionString }
            })
            .Build();

        var services = new ServiceCollection();

        // Act
        var options = services.RegisterAndValidateOptions<TestConnectionStringsOption>(configuration);

        // Assert
        options.DefaultConnection.Should().Be(connectionString);
    }
}


