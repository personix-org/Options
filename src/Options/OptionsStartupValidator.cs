using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Personix.Options;

public static class OptionsStartupValidator
{
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
