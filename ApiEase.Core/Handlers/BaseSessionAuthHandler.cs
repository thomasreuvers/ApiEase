using System.Net;
using System.Net.Http.Headers;
using Microsoft.Extensions.Caching.Memory;

namespace ApiEase.Core.Handlers;

/// <summary>
/// A base handler for session-based authentication using cached tokens. Handles automatic token refresh upon receiving
/// an unauthorized (401) response from the API, and stores the session in cache for reuse.
/// </summary>
/// <typeparam name="TSession">The session type containing authentication data, such as tokens.</typeparam>
public abstract class BaseSessionAuthHandler<TSession>(IMemoryCache cache, string cacheKey) : DelegatingHandler
    where TSession : class
{
    
    /// <summary>
    /// Sends an HTTP request with a cached access token, refreshing the session token if a 401 Unauthorized response is received.
    /// </summary>
    /// <param name="request">The HTTP request message to send.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The HTTP response message.</returns>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (cache.TryGetValue(cacheKey, out TSession? session))
        {
            var accessToken = GetAccessToken(session);
            if (!string.IsNullOrEmpty(accessToken))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            }
        }
        
        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode != HttpStatusCode.Unauthorized) return response;
        
        var newSession = await RefreshSessionAsync();
        cache.Set(cacheKey, newSession);

        var newAccessToken = GetAccessToken(newSession);
        
        if (string.IsNullOrEmpty(newAccessToken)) return response;
        
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newAccessToken);
        response = await base.SendAsync(request, cancellationToken);

        return response;
    }
    
    /// <summary>
    /// Refreshes the session to obtain a new access token, which is invoked when a 401 Unauthorized response is received.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation, containing the refreshed session.</returns>
    protected abstract Task<TSession> RefreshSessionAsync();
    
    /// <summary>
    /// Retrieves the access token from the provided session instance.
    /// </summary>
    /// <param name="session">The session from which to retrieve the access token.</param>
    /// <returns>The access token as a string, or null if unavailable.</returns>
    protected abstract string? GetAccessToken(TSession? session);
}