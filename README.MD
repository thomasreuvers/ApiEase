# ApiEase

`ApiEase` is a .NET library designed to simplify API client creation with built-in support for session-based authentication, retry policies, and flexible configuration. Leveraging [Refit](https://github.com/reactiveui/refit) for defining API interfaces, the package provides base classes and extension methods that streamline the registration of custom HTTP clients with dependency injection.

## Features

- **Easy Client Registration**: Seamlessly register custom API clients with `IServiceCollection` extensions.
- **Session-Based Authentication**: Automatically refresh session tokens on `401 Unauthorized` responses.
- **Retry Policies**: Integrated retry mechanism using `Polly`.
- **Flexible Configuration**: Configure base URLs, authentication handlers, and settings using `IOptions`.

## Installation

To install `ApiEase`, use:

```bash
dotnet add package ApiEase
```

## Getting Started

### 1. Define Your API Interface
Define the endpoints of your API using a Refit interface. Each endpoint should be decorated with the appropriate HTTP attribute.

```csharp
public interface IExampleApi : IBaseApi
{
    [Get("/items")]
    Task<List<Item>> GetItemsAsync();

    [Post("/items")]
    Task CreateItemAsync([Body] Item item);
}
```

### 2. Create a Custom API Client
Create a custom API client that extends `BaseHttpClient<TApi>`. Use `ExecuteWithPolicyAsync` to wrap each API call with the retry policy.
```csharp
public class ExampleHttpClient : BaseHttpClient<IExampleApi>
{
    public ExampleHttpClient(IExampleApi api, IAsyncPolicy retryPolicy = null)
        : base(api, retryPolicy)
    {
    }

    public Task<List<Item>> GetItemsAsync() => ExecuteWithPolicyAsync(() => Api.GetItemsAsync());
    public Task CreateItemAsync(Item item) => ExecuteWithPolicyAsync(() => Api.CreateItemAsync(item));
}
```

### 3. Register the Client in Dependency Injection
Use the `AddApiClient` extension method to register your client in the `IServiceCollection`. Specify retry policies and optional authentication handlers.
```csharp
builder.Services.AddApiClient<ExampleHttpClient, IExampleApi, ExampleApiSettings>(
    builder.Configuration,
    provider => Policy.Handle<HttpRequestException>().RetryAsync(3));
```

### 4. Implement Session-Based Authentication (Optional)
For session-based authentication, create a custom `DelegatingHandler` that extends `BaseSessionAuthHandler<TSession>`, which will handle token refresh on 401 Unauthorized responses.
```csharp
public class ExampleSessionAuthHandler : BaseSessionAuthHandler<ExampleSession>
{
    public ExampleSessionAuthHandler(IMemoryCache cache, IOptions<ExampleApiSettings> settings) 
        : base(cache, nameof(ExampleSession))
    {
    }

    protected override Task<ExampleSession> RefreshSessionAsync()
    {
        // Logic to refresh the session token
    }

    protected override string? GetAccessToken(ExampleSession session)
    {
        return session.AccessToken;
    }
}
```

### 5. Configure Settings
In your `appsettings.json`, add the configuration settings for your API client.
```json
{
  "ApiSettings": {
    "ExampleApi": {
      "BaseUrl": "https://api.example.com",
      "Username": "user",
      "Password": "pass"
    }
  }
}
```

### 6. Usage
Once registered, inject your client into any service or controller:
```csharp
public class MyService
{
    private readonly ExampleHttpClient _exampleClient;

    public MyService(ExampleHttpClient exampleClient)
    {
        _exampleClient = exampleClient;
    }

    public async Task DoSomethingAsync()
    {
        var items = await _exampleClient.GetItemsAsync();
        // Process items
    }
}
```

## Contributing
Contributions are welcome! For major changes, please open an issue first to discuss what you would like to change.
