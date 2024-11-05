# ApiEase

`ApiEase` is a .NET library designed to simplify API client creation with built-in support for session-based authentication, retry policies, and flexible configuration. Leveraging [Refit](https://github.com/reactiveui/refit) for defining API interfaces, the package provides base classes and extension methods that streamline the registration of custom HTTP clients with dependency injection.

## Features

- **Dynamic API Client Registration:**: Automatically register multiple API clients with custom settings.
- **Session-Based Authentication**: Automatically refresh session tokens on `401 Unauthorized` responses.
- **Retry Policies**: Integrated retry mechanism using `Polly`.
- **Flexible Configuration**: Configure base URLs, authentication handlers, and settings using `IOptions`.

## Installation

To install `ApiEase` via NuGet, use:

```bash
dotnet add package ApiEase
```

## Getting Started

### 1. Define Your API Settings
Each API client requires its own configuration class that implements `IBaseSettings`:

```csharp
public class ExampleSettings : IBaseSettings
{
    public string? BaseUrl { get; set; }
    public string? Token { get; set; } // Additional configuration properties as needed
}
```

### 2. Define Your API Interface
Define an interface for your API using `Refit` attributes to specify the endpoints, and implement `IBaseClient` or one of its generic variants for greater control over settings and handler types.
```csharp
using Refit;

public interface IExampleClient : IBaseClient<ExampleSettings>
{
    [Get("/items")]
    Task<ApiResponse<List<Item>>> GetItemsAsync();
}
```
The `IBaseClient` interfaces come in three variations for different levels of customization:
- `IBaseClient`: Basic interface, serves as a marker for all clients.
- `IBaseClient<TSettings>`: Allows you to specify a settings class, which should implement `IBaseSettings`.
- `IBaseClient<TSettings, TDelegatingHandler>`: Adds support for a custom delegating handler. `TDelegatingHandler` must inherit from `DelegatingHandler`, which can be used for custom authentication or middleware.

Example:
```csharp
public interface IExampleClient : IBaseClient<ExampleSettings, CustomAuthHandler>
{
    [Get("/items")]
    Task<ApiResponse<List<Item>>> GetItemsAsync();
}
```

### 3. Register Clients in `Program.cs`
In your `Program.cs` or `Startup.cs`, register the clients using `AddDynamicClients`:
```csharp
using ApiEase.Core.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Register dynamic API clients
builder.Services.AddDynamicClients(builder.Configuration);

var app = builder.Build();
```

### 4. Configure Settings
In your `appsettings.json`, add the configuration settings for your API client(s).
```json
{
  "ApiSettings": {
    "Example": {
      "BaseUrl": "https://api.example.com",
      "Token": "thisIsAToken"
    }
  }
}
```

### 5. Usage
Once registered, inject your client into any service or controller:
```csharp
public class MyService
{
    private readonly IExampleClient _exampleClient;

    public MyService(IExampleClient exampleClient)
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
## Advanced Usage
### Custom Delegating Handlers
You can create a custom delegating handler by implementing `DelegatingHandler` and specifying it when registering the client. For example:
```csharp
public class CustomAuthHandler<TSettings> : DelegatingHandler where TSettings : IBaseSettings
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "your-token");
        return await base.SendAsync(request, cancellationToken);
    }
}
```
In AddDynamicClients, ApiEase will automatically detect and apply these handlers. If your settings implement IBasicAuth or IBearerAuthSettings, the appropriate HTTP header will also be added automatically.

## Contributing
Contributions are welcome! For major changes, please open an issue first to discuss what you would like to change.
