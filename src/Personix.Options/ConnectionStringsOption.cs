namespace Personix.Options;

/// <summary>
/// Base class for options bound to the standard <c>ConnectionStrings</c> configuration section.
/// </summary>
public abstract class ConnectionStringsOption : IOption
{
    /// <summary>
    /// Gets the configuration section name, fixed to <c>ConnectionStrings</c>.
    /// </summary>
    public static string SectionName => "ConnectionStrings";
}
