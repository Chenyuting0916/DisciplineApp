using Microsoft.AspNetCore.Http;

namespace DisciplineApp.Services;

public class TokenProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private const string TokenKey = "GoogleAccessToken";

    public TokenProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? AccessToken
    {
        get
        {
            var token = _httpContextAccessor.HttpContext?.Session.GetString(TokenKey);
            return token;
        }
        set
        {
            if (_httpContextAccessor.HttpContext != null)
            {
                if (string.IsNullOrEmpty(value))
                {
                    _httpContextAccessor.HttpContext.Session.Remove(TokenKey);
                }
                else
                {
                    _httpContextAccessor.HttpContext.Session.SetString(TokenKey, value);
                }
            }
        }
    }
}
