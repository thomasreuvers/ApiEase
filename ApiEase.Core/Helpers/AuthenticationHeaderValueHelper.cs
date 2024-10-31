using System.Net.Http.Headers;
using System.Text;
using ApiEase.Core.Contracts;

namespace ApiEase.Core.Helpers;

public static class AuthenticationHeaderValueHelper
{
    private static AuthenticationHeaderValue GetBasicAuthenticationHeader(string username, string password)
    {
        return new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"{username}:{password}")));
    }
    
    private static AuthenticationHeaderValue GetBearerAuthenticationHeader(string token)
    {
        return new AuthenticationHeaderValue("Bearer", token);
    }
    
    public static AuthenticationHeaderValue? DetermineAuthenticationHeader(IBaseSettings settings)
    {
        return settings switch
        {
            IBasicAuthBaseSettings basicAuthSettings => GetBasicAuthenticationHeader(basicAuthSettings.Username,
                basicAuthSettings.Password),
            IBearerAuthBaseSettings bearerAuthSettings => GetBearerAuthenticationHeader(bearerAuthSettings.Token),
            _ => null
        };
    }
}