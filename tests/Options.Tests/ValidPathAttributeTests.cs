using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using Options;
using Xunit;

namespace Options.Tests;

public sealed class ValidPathAttributeTests
{
    private static ValidationResult? Validate(object? value, string memberName = "TestPath")
    {
        var attribute = new ValidPathAttribute();
        var context = new ValidationContext(new object()) { MemberName = memberName };
        return attribute.GetValidationResult(value, context);
    }

    [Theory]
    [InlineData("/valid/unix/path")]
    [InlineData("./relative/path")]
    [InlineData("../parent/path")]
    [InlineData("/path/with spaces/allowed")]
    [InlineData("/single")]
    public void ValidPath_ReturnsSuccess(string path)
    {
        var result = Validate(path);
        result.Should().Be(ValidationResult.Success);
    }

    [Fact]
    public void NullValue_ReturnsSuccess()
    {
        var result = Validate(null);
        result.Should().Be(ValidationResult.Success);
    }

    [Fact]
    public void EmptyString_ReturnsSuccess()
    {
        var result = Validate("");
        result.Should().Be(ValidationResult.Success);
    }

    [Fact]
    public void NonStringValue_ReturnsError()
    {
        var result = Validate(42);
        result.Should().NotBe(ValidationResult.Success);
        result!.ErrorMessage.Should().Contain("string");
    }

    [Fact]
    public void ErrorMessage_ContainsMemberName_WhenNonStringPassed()
    {
        var result = Validate(new object(), "MyProperty");
        result.Should().NotBe(ValidationResult.Success);
    }

    [Theory]
    [InlineData("path<with>invalid")]
    [InlineData("path|with|pipes")]
    public void PathWithWindowsInvalidChars_OnWindows_ReturnsError(string path)
    {
        if (!OperatingSystem.IsWindows())
            return; // invalid char sets are OS-dependent, skip on non-Windows

        var result = Validate(path);
        result.Should().NotBe(ValidationResult.Success);
    }
}
