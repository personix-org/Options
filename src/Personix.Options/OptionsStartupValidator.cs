using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Personix.Options;

/// <summary>
/// Registers strongly-typed options and validates them while the application is still starting,
/// so invalid configuration stops the process instead of surfacing on the first request.
/// </summary>
public static class OptionsStartupValidator
{
    /// <summary>
    /// Binds <typeparamref name="TOptions"/> to its configuration section, validates it against its
    /// data annotations, registers it in <paramref name="services"/>, and returns the validated instance.
    /// </summary>
    /// <typeparam name="TOptions">The options type to register.</typeparam>
    /// <param name="services">The service collection the options are registered in.</param>
    /// <param name="configuration">The configuration the options are bound to.</param>
    /// <returns>The validated options instance, usable before the host is built.</returns>
    /// <exception cref="InvalidOperationException">
    /// The configuration section named by <c>TOptions.SectionName</c> is missing.
    /// </exception>
    /// <exception cref="OptionsValidationException">
    /// The bound values fail at least one data annotation.
    /// </exception>
    public static TOptions RegisterAndValidateOptions<TOptions>(this IServiceCollection services,
        IConfiguration configuration)
        where TOptions : class, IOption
    {
        var temporaryServiceCollection = new ServiceCollection();
        _ = temporaryServiceCollection.RegisterOptions<TOptions>(configuration);

        var sp = temporaryServiceCollection.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptions<TOptions>>();

        var result = options.Value;

        services.RegisterOptions<TOptions>(configuration);

        return result;
    }

    private static IServiceCollection RegisterOptions<TOptions>(this IServiceCollection services,
        IConfiguration configuration)
        where TOptions : class, IOption
    {
        services.AddOptions<TOptions>()
            .Bind(configuration.GetRequiredSection(TOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        return services;
    }
}
