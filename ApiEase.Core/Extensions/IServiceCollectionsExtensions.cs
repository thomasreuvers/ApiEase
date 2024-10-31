using System.Diagnostics.CodeAnalysis;
using ApiEase.Core.Clients.Base;
using ApiEase.Core.Contracts;
using ApiEase.Core.Handlers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Polly;
using Refit;

namespace ApiEase.Core.Extensions;

[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "UnusedType.Global")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
public static class IServiceCollectionExtensions
{
    /// <summary>
    /// Registers an API client with specified settings and optional retry policy.
    /// </summary>
    /// <typeparam name="TClient">The type of the client that extends <see cref="BaseHttpClient{TApi}"/>.</typeparam>
    /// <typeparam name="TApi">The Refit interface that defines the API endpoints, must inherit <see cref="IBaseApi"/>.</typeparam>
    /// <typeparam name="TSettings">The type containing configuration settings for the API, must inherit <see cref="IBaseSettings"/>.</typeparam>
    /// <param name="serviceCollection">The service collection to which the client and dependencies will be added.</param>
    /// <param name="configuration">The configuration provider used to retrieve the settings.</param>
    /// <param name="policyFactory">Optional factory function to create an <see cref="IAsyncPolicy"/> for handling retries and fault tolerance.</param>
    /// <exception cref="InvalidOperationException">Thrown if the configuration section for <typeparamref name="TSettings"/> is not found in <c>ApiSettings</c>.</exception>
    public static void AddApiClient<TClient, TApi, TSettings>(
        this IServiceCollection serviceCollection,
        IConfiguration configuration,
        Func<IServiceProvider, IAsyncPolicy>? policyFactory = null)
    where TClient : BaseHttpClient<TApi>
    where TApi : class, IBaseApi
    where TSettings : class, IBaseSettings
    {
        AddApiClient<TClient, TApi, TSettings, DefaultDelegatingHandler<TSettings>>(
            serviceCollection, configuration, policyFactory);
    }
    
    /// <summary>
    /// Registers an API client with specified settings, authentication handler, and optional retry policy.
    /// </summary>
    /// <typeparam name="TClient">The type of the client that extends <see cref="BaseHttpClient{TApi}"/>.</typeparam>
    /// <typeparam name="TApi">The Refit interface that defines the API endpoints, must inherit <see cref="IBaseApi"/>.</typeparam>
    /// <typeparam name="TSettings">The type containing configuration settings for the API, must inherit <see cref="IBaseSettings"/>.</typeparam>
    /// <typeparam name="TDelegatingHandler">A custom <see cref="DelegatingHandler"/> for handling authentication or custom HTTP behavior.</typeparam>
    /// <param name="serviceCollection">The service collection to which the client and dependencies will be added.</param>
    /// <param name="configuration">The configuration provider used to retrieve the settings.</param>
    /// <param name="policyFactory">Optional factory function to create an <see cref="IAsyncPolicy"/> for handling retries and fault tolerance.</param>
    /// <exception cref="InvalidOperationException">Thrown if the configuration section for <typeparamref name="TSettings"/> is not found in <c>ApiSettings</c>.</exception>
    public static void AddApiClient<TClient, TApi, TSettings, TDelegatingHandler>(
            this IServiceCollection serviceCollection,
            IConfiguration configuration,
            Func<IServiceProvider, IAsyncPolicy>? policyFactory = null)
        where TClient : BaseHttpClient<TApi>
        where TApi : class, IBaseApi
        where TSettings : class, IBaseSettings
        where TDelegatingHandler : DelegatingHandler
    {
        // RegisterApiClient<TClient, TSettings>(serviceCollection, configuration);
        var sectionName = typeof(TSettings).Name.Replace("Settings", string.Empty);
        var section = configuration.GetSection($"ApiSettings:{sectionName}");
        
        // Ensure configuration section exists, otherwise throw exception
        if (!section.Exists())
        {
            throw new InvalidOperationException($"Configuration for 'ApiSettings:{sectionName}' is not set. Ensure it exists in the appsettings.");
        }

        
        // Register the settings for the API client
        serviceCollection.Configure<TSettings>(section);
        
        // Register TApi as the Refit client, applying authentication handler if provided
        serviceCollection.AddRefitClient<TApi>()
            .ConfigureHttpClient((provider, client) =>
            {
                var settings = provider.GetRequiredService<IOptions<TSettings>>().Value;
                client.BaseAddress = new Uri(settings.BaseUrl);
            })
            .AddHttpMessageHandler<TDelegatingHandler>();

        serviceCollection.AddScoped<TDelegatingHandler>();
        
        // Register TClient with the already-registered TApi and custom policies
        serviceCollection.AddTransient<TClient>(provider =>
        {
            var api = provider.GetRequiredService<TApi>(); // Use existing Refit client
            var policy = policyFactory?.Invoke(provider) ?? Policy.NoOpAsync();
            return (TClient)ActivatorUtilities.CreateInstance(provider, typeof(TClient), api, policy);
        });
    }
}