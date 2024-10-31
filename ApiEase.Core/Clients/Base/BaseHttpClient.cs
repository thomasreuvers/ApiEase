using System.Diagnostics.CodeAnalysis;
using ApiEase.Core.Contracts;
using Polly;

namespace ApiEase.Core.Clients.Base;

/// <summary>
/// Initializes a new instance of the <see cref="BaseHttpClient{TApi}"/> class with a specified API interface and retry policy.
/// </summary>
/// <param name="api">The Refit-defined API interface to interact with the endpoints.</param>
/// <param name="retryPolicy">An <see cref="IAsyncPolicy"/> to apply retry logic to API requests.</param>
[SuppressMessage("ReSharper", "UnusedMember.Global")]
[SuppressMessage("ReSharper", "VirtualMemberNeverOverridden.Global")]
public abstract class BaseHttpClient<TApi>(TApi api, IAsyncPolicy retryPolicy)
    where TApi : IBaseApi
{
    protected TApi Api { get; } = api;
    
    /// <summary>
    /// Executes an asynchronous API call with a retry policy and returns a result.
    /// </summary>
    /// <typeparam name="TResult">The type of the result returned by the API call.</typeparam>
    /// <param name="action">The asynchronous function representing the API call to execute.</param>
    /// <returns>
    /// A <see cref="Task{TResult}"/> representing the asynchronous operation, which contains the API response result or 
    /// the default value of <typeparamref name="TResult"/> if an exception is thrown.
    /// </returns>
    protected async Task<TResult?> ExecuteWithPolicyAsync<TResult>(Func<Task<TResult>> action)
    {
        try
        {
            return await retryPolicy.ExecuteAsync(action);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
        
        return default;
    }
    
    /// <summary>
    /// Executes an asynchronous API call with a retry policy, intended for methods that do not return a result.
    /// </summary>
    /// <param name="action">The asynchronous function representing the API call to execute.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous operation.</returns>
    protected async Task ExecuteWithPolicyAsync(Func<Task> action)
    {
        try
        {
            await retryPolicy.ExecuteAsync(action);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }
    
    /// <summary>
    /// Handles exceptions that occur during API requests, allowing for custom exception handling.
    /// </summary>
    /// <param name="exception">The exception that was thrown during the API request.</param>
    /// <returns>A <see cref="Task"/> representing the completion of the exception handling process.</returns>
    /// <exception cref="Exception">Re-throws the exception by default unless overridden in a derived class.</exception>
    protected virtual Task HandleExceptionAsync(Exception exception)
    {
        // Default behavior, extend for specific APIs
        throw exception;
    }
}