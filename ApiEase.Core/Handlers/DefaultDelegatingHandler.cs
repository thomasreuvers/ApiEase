using System.Diagnostics.CodeAnalysis;
using ApiEase.Core.Contracts;
using ApiEase.Core.Helpers;
using Microsoft.Extensions.Options;

namespace ApiEase.Core.Handlers;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
internal class DefaultDelegatingHandler<TSettings>(IOptions<TSettings> settings) : DelegatingHandler
    where TSettings : class, IBaseSettings
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.Headers.Authorization = AuthenticationHeaderValueHelper.DetermineAuthenticationHeader(settings.Value);
        return await base.SendAsync(request, cancellationToken);
    }
}