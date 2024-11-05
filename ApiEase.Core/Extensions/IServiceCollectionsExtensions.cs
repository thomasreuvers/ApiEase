using System.Diagnostics.CodeAnalysis;
using ApiEase.Core.Contracts;
using ApiEase.Core.Handlers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Refit;

namespace ApiEase.Core.Extensions;

[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "UnusedType.Global")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
public static class IServiceCollectionExtensions
{
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configuration"></param>
    public static void AddDynamicClients(this IServiceCollection services, IConfiguration configuration)
    {
        var clientInterfaces = GetDynamicClientInterfaces();

        foreach (var interfaceType in clientInterfaces)
        {
            var settingsType = GetSettingsType(interfaceType);
            var delegatingHandlerType = GetDelegatingHandlerType(interfaceType);

            if (settingsType != null)
            {
                RegisterSettings(services, configuration, settingsType);
                
                var clientBuilder = ConfigureRefitClient(services, interfaceType, settingsType);

                ConfigureDelegatingHandler(services, clientBuilder, settingsType, delegatingHandlerType);
            }
            // TODO: Implement cases where there are no settings or delegating handler later.
            // else
            // {
            //     ConfigureRefitClient(services, interfaceType);
            // }
        }
    }

    // Helper Methods
    private static IEnumerable<Type> GetDynamicClientInterfaces()
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(t => 
                t.IsInterface && 
                typeof(IBaseClient).IsAssignableFrom(t) &&
                IsNotABaseType(t))
            .ToArray();
    }

    private static bool IsNotABaseType(Type t)
    {
        var baseInterfaces = new[]
        {
            typeof(IBaseClient),
            typeof(IBaseClient<>),
            typeof(IBaseClient<,>)
        };

        return !baseInterfaces.Contains(t);
    }

    private static Type? GetSettingsType(Type clientType)
    {
        var interfaceWithSettings = clientType.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && 
                                 i.GetGenericTypeDefinition() == typeof(IBaseClient<>));
        
        return interfaceWithSettings?.GetGenericArguments()[0];
    }

    private static Type? GetDelegatingHandlerType(Type clientType)
    {
        var interfaceWithHandler = clientType.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && 
                                 i.GetGenericTypeDefinition() == typeof(IBaseClient<,>));
        
        return interfaceWithHandler?.GetGenericArguments()[1];
    }

    private static void RegisterSettings(IServiceCollection services, IConfiguration configuration, Type settingsType)
    {
        var sectionName = settingsType.Name.Replace("Settings", string.Empty);
        var section = configuration.GetSection($"ApiSettings:{sectionName}");

        if (!section.Exists())
        {
            throw new InvalidOperationException($"Configuration for 'ApiSettings:{sectionName}' is not set. Ensure it exists in the appsettings.");
        }
        
        var method = typeof(OptionsConfigurationServiceCollectionExtensions)
            .GetMethods()
            .First(m => m.Name == nameof(OptionsConfigurationServiceCollectionExtensions.Configure)
                        && m.GetParameters().Length == 2);
        var generic = method.MakeGenericMethod(settingsType);
        generic.Invoke(null, new object[] {services, section});
        Console.WriteLine($"Registered settings for: {settingsType.FullName}");
    }

    private static IHttpClientBuilder ConfigureRefitClient(IServiceCollection services, Type interfaceType, Type settingsType)
    {
        return services.AddRefitClient(interfaceType)
            .ConfigureHttpClient((provider, client) =>
            {
                client.BaseAddress = GetBaseAddress(provider, settingsType, interfaceType);
            });
    }
    
    // TODO: Implement further
    private static void ConfigureRefitClient(IServiceCollection services, Type interfaceType)
    {
        services.AddRefitClient(interfaceType)
            .ConfigureHttpClient((_, client) =>
            {
                client.BaseAddress = new Uri("https://localhost:5001");
            });
    }

    private static Uri GetBaseAddress(IServiceProvider provider, Type settingsType, Type interfaceType)
    {
        // Get the options type dynamically (e.g., IOptions<ExampleSettings>)
        var optionsType = typeof(IOptions<>).MakeGenericType(settingsType);

        // Retrieve the options instance from the provider
        var options = provider.GetService(optionsType);
        if (options == null)
        {
            throw new InvalidOperationException($"Failed to resolve options for settings type '{settingsType.FullName}'. Ensure it is registered in the configuration.");
        }

        // Retrieve the Value property using reflection
        if (optionsType.GetProperty("Value")?.GetValue(options) is not IBaseSettings settings || string.IsNullOrEmpty(settings.BaseUrl))
        {
            throw new ArgumentException($"Missing or invalid BaseAddress for client '{interfaceType.Name}' in settings type '{settingsType.FullName}'.");
        }

        return new Uri(settings.BaseUrl);
    }

    private static void ConfigureDelegatingHandler(
        IServiceCollection services, IHttpClientBuilder clientBuilder, Type settingsType, Type? delegatingHandlerType)
    {
        if (delegatingHandlerType != null)
        {
            services.AddTransient(delegatingHandlerType);
            clientBuilder.AddHttpMessageHandler(provider => 
                (DelegatingHandler)provider.GetRequiredService(delegatingHandlerType));
        }
        else
        {
            var defaultHandlerType = typeof(DefaultDelegatingHandler<>).MakeGenericType(settingsType);
            services.AddTransient(defaultHandlerType);
            clientBuilder.AddHttpMessageHandler(provider => 
                (DelegatingHandler)provider.GetRequiredService(defaultHandlerType));
        }
    }
}