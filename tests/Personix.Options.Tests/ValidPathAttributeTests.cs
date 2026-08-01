using System.ComponentModel.DataAnnotations;
using Shouldly;
using Xunit;

namespace Personix.Options.Tests;

public sealed class ValidPathAttributeTests
{
    // Contains an embedded NUL character. Path.GetInvalidPathChars() differs per OS, but a NUL
    // byte is rejected by Path.GetFullPath on every platform .NET runs on, so this is the one
    // "invalid format" input the test suite can rely on everywhere -- unlike Windows-only
    // characters such as '<' or '|', which are perfectly legal Unix filenames.
    private const string PathWithEmbeddedNullCharacter = "bad\0path";

    /// <summary>
    /// Skips on non-Windows platforms by setting <see cref="TheoryAttribute.Skip"/> in the
    /// constructor (evaluated at test discovery), so the run is honestly reported as "Skipped"
    /// instead of silently reporting "Passed" for a test body that never executed an assertion.
    /// </summary>
    private sealed class WindowsOnlyTheoryAttribute : TheoryAttribute
    {
        public WindowsOnlyTheoryAttribute()
        {
            if (!OperatingSystem.IsWindows())
            {
                Skip = "Exercises Windows-specific invalid path characters ('<', '|'), which are legal Unix filenames.";
            }
        }
    }

    private static ValidationResult? Validate(object? value, string? memberName = "TestPath")
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
        result.ShouldBe(ValidationResult.Success);
    }

    [Fact]
    public void NullValue_ReturnsSuccess()
    {
        var result = Validate(null);
        result.ShouldBe(ValidationResult.Success);
    }

    [Fact]
    public void EmptyString_ReturnsSuccess()
    {
        var result = Validate("");
        result.ShouldBe(ValidationResult.Success);
    }

    [Fact]
    public void NonStringValue_ReturnsError()
    {
        var result = Validate(42);
        result.ShouldNotBe(ValidationResult.Success);
        result!.ErrorMessage.ShouldBe("Value must be of type string");
    }

    [Fact]
    public void InvalidPathFormat_WithEmbeddedNullCharacter_ReturnsError()
    {
        // This is the only test in the suite that proves IsValid actually rejects a malformed
        // path on every OS: replacing the whole validity check with "always valid" makes only
        // this assertion fail.
        var result = Validate(PathWithEmbeddedNullCharacter);

        result.ShouldNotBe(ValidationResult.Success);
        result!.ErrorMessage.ShouldBe("The path 'TestPath' has an invalid format");
    }

    [Fact]
    public void InvalidPathFormat_ErrorMessage_UsesConfiguredMemberName()
    {
        var result = Validate(PathWithEmbeddedNullCharacter, memberName: "ConfigPath");

        result!.ErrorMessage.ShouldBe("The path 'ConfigPath' has an invalid format");
    }

    [Fact]
    public void InvalidPathFormat_WithNullMemberName_FallsBackToGenericPathLabel()
    {
        var result = Validate(PathWithEmbeddedNullCharacter, memberName: null);

        result!.ErrorMessage.ShouldBe("The path 'path' has an invalid format");
    }

    [WindowsOnlyTheory]
    [InlineData("path<with>invalid")]
    [InlineData("path|with|pipes")]
    public void PathWithWindowsInvalidChars_OnWindows_ReturnsError(string path)
    {
        var result = Validate(path);
        result.ShouldNotBe(ValidationResult.Success);
    }
}
