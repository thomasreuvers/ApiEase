namespace ApiEase.Core.Contracts;

public interface IBaseSettings
{
    public string BaseUrl { get; set; }
}

public interface IBasicAuthBaseSettings : IBaseSettings
{
    public string Username { get; set; }
    
    public string Password { get; set; }
}

public interface IBearerAuthBaseSettings : IBaseSettings
{
    public string Token { get; set; }
}