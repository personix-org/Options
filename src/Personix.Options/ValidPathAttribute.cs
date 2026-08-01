using System.ComponentModel.DataAnnotations;

namespace Personix.Options;

/// <summary>
/// Validates that a path has a valid format (no invalid characters, parseable by Path APIs).
/// Does NOT verify existence — that must be done separately at application startup.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public sealed class ValidPathAttribute : ValidationAttribute
{
    /// <summary>
    /// Initialises the attribute with the default error message.
    /// </summary>
    public ValidPathAttribute()
    {
        ErrorMessage = "The path '{0}' has an invalid format";
    }

    /// <summary>
    /// Validates that the value is either absent or a string holding a well-formed path.
    /// </summary>
    /// <param name="value">The value under validation.</param>
    /// <param name="validationContext">The context the value is validated in.</param>
    /// <returns>
    /// <see cref="ValidationResult.Success"/> when the value is acceptable, otherwise a failure result.
    /// </returns>
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is null or "")
        {
            return ValidationResult.Success;
        }

        if (value is not string path)
        {
            return new ValidationResult("Value must be of type string");
        }

        if (!IsValidPath(path))
        {
            var displayName = validationContext.MemberName ?? "path";
            return new ValidationResult(string.Format(ErrorMessageString, displayName));
        }

        return ValidationResult.Success;
    }

    private static bool IsValidPath(string path)
    {
        try
        {
            if (path.Any(c => Path.GetInvalidPathChars().Contains(c)))
            {
                return false;
            }

            _ = Path.GetFullPath(path);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
