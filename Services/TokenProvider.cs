using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace DisciplineApp.Services;

public class TokenProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly AuthenticationStateProvider _authenticationStateProvider;
    private readonly UserManager<DisciplineApp.Models.ApplicationUser> _userManager;
    private const string TokenKey = "GoogleAccessToken";

    public TokenProvider(
        IHttpContextAccessor httpContextAccessor,
        AuthenticationStateProvider authenticationStateProvider,
        UserManager<DisciplineApp.Models.ApplicationUser> userManager)
    {
        _httpContextAccessor = httpContextAccessor;
        _authenticationStateProvider = authenticationStateProvider;
        _userManager = userManager;
    }

    public string? AccessToken
    {
        get
        {
            // 1. Try Session (fastest)
            var token = _httpContextAccessor.HttpContext?.Session.GetString(TokenKey);
            if (!string.IsNullOrEmpty(token))
            {
                return token;
            }

            // 2. Try DB (fallback)
            try 
            {
                // Try to get user from AuthenticationStateProvider (Blazor context)
                // Note: In a pure HTTP controller context, this might not return the user if not set up for it,
                // but we have HttpContextAccessor for that if needed.
                // However, the session check above covers the HTTP context usually.
                // If session is empty, we might be in a fresh Blazor circuit.
                
                var authStateTask = _authenticationStateProvider.GetAuthenticationStateAsync();
                if (authStateTask.IsCompleted)
                {
                    var user = authStateTask.Result.User;
                    if (user.Identity?.IsAuthenticated == true)
                    {
                        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                        if (!string.IsNullOrEmpty(userId))
                        {
                            // Use .Result carefully here as we are in a property getter
                            var appUser = _userManager.FindByIdAsync(userId).Result;
                            if (appUser != null)
                            {
                                var dbToken = _userManager.GetAuthenticationTokenAsync(
                                    appUser, 
                                    "Google", 
                                    "access_token").Result;

                                if (!string.IsNullOrEmpty(dbToken))
                                {
                                    // Cache it back in session if possible
                                    if (_httpContextAccessor.HttpContext != null)
                                    {
                                        _httpContextAccessor.HttpContext.Session.SetString(TokenKey, dbToken);
                                    }
                                    return dbToken;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving token from DB in TokenProvider: {ex.Message}");
            }

            return null;
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
