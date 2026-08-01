namespace Personix.Options;

/// <summary>
/// Marks an options class as bindable to a named configuration section.
/// </summary>
public interface IOption
{
    /// <summary>
    /// Gets the name of the configuration section this options class binds to.
    /// </summary>
    static abstract string SectionName { get; }
}
