using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using DisciplineApp.Models;

namespace DisciplineApp.Controllers;

[Route("[controller]/[action]")]
public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly DisciplineApp.Services.TokenProvider _tokenProvider;

    public AccountController(
        SignInManager<ApplicationUser> signInManager, 
        UserManager<ApplicationUser> userManager,
        DisciplineApp.Services.TokenProvider tokenProvider)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _tokenProvider = tokenProvider;
    }

    [HttpGet]
    public IActionResult LoginWithGoogle(string returnUrl = "/")
    {
        var redirectUrl = Url.Action("GoogleCallback", "Account", new { returnUrl });
        var properties = _signInManager.ConfigureExternalAuthenticationProperties(GoogleDefaults.AuthenticationScheme, redirectUrl);
        
        // Request offline access to get refresh token and access to Calendar API
        properties.Items["scope"] = "openid profile email https://www.googleapis.com/auth/calendar.readonly https://www.googleapis.com/auth/tasks.readonly";
        properties.Items["access_type"] = "offline";
        properties.Items["prompt"] = "consent";
        
        return new ChallengeResult(GoogleDefaults.AuthenticationScheme, properties);
    }

    [HttpGet]
    public async Task<IActionResult> GoogleCallback(string returnUrl = "/", string? remoteError = null)
    {
        if (remoteError != null)
        {
            Console.WriteLine($"GoogleCallback - Remote error: {remoteError}");
            return Redirect("/"); // Handle error appropriately
        }

        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info == null)
        {
            Console.WriteLine("GoogleCallback - No external login info");
            return Redirect("/");
        }

        // Sign in the user with this external login provider if the user already has a login.
        var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: true, bypassTwoFactor: true);
        
        ApplicationUser? user = null;

        if (result.Succeeded)
        {
            var email = info.Principal.FindFirstValue(ClaimTypes.Email);
            if (email != null)
            {
                user = await _userManager.FindByEmailAsync(email);
                if (user != null)
                {
                    // Update DisplayName if it's not set
                    if (string.IsNullOrEmpty(user.DisplayName))
                    {
                        var name = info.Principal.FindFirstValue(ClaimTypes.Name);
                        user.DisplayName = name ?? email;
                        
                        // Update PhotoUrl
                        var photoUrl = info.Principal.FindFirstValue("picture");
                        if (!string.IsNullOrEmpty(photoUrl) && user.PhotoUrl != photoUrl)
                        {
                            user.PhotoUrl = photoUrl;
                        }

                        await _userManager.UpdateAsync(user);
                    }
                    else
                    {
                        // Update PhotoUrl even if DisplayName is already set
                        var photoUrl = info.Principal.FindFirstValue("picture");
                        if (!string.IsNullOrEmpty(photoUrl) && user.PhotoUrl != photoUrl)
                        {
                            user.PhotoUrl = photoUrl;
                            await _userManager.UpdateAsync(user);
                        }
                    }
                }
            }
        }
        else
        {
            // If the user does not have an account, then ask the user to create an account.
            // Here we automatically create it for a smoother "popup" experience.
            if (result.IsLockedOut)
            {
                return Redirect("/"); // Handle lockout
            }
            else
            {
                var email = info.Principal.FindFirstValue(ClaimTypes.Email);
                var name = info.Principal.FindFirstValue(ClaimTypes.Name);
                
                if (email != null)
                {
                    user = new ApplicationUser 
                    { 
                        UserName = email, 
                        Email = email,
                        DisplayName = name ?? email,
                        PhotoUrl = info.Principal.FindFirstValue("picture")
                    };
                    var createResult = await _userManager.CreateAsync(user);
                    if (createResult.Succeeded)
                    {
                        createResult = await _userManager.AddLoginAsync(user, info);
                        if (createResult.Succeeded)
                        {
                            await _signInManager.SignInAsync(user, isPersistent: true);
                        }
                        else
                        {
                             user = null; // Failed to add login
                        }
                    }
                    else
                    {
                        user = null; // Failed to create user
                    }
                }
            }
        }

        // Store tokens if user exists (either logged in or just created)
        if (user != null)
        {
            var accessToken = info.AuthenticationTokens?.FirstOrDefault(t => t.Name == "access_token")?.Value;
            var refreshToken = info.AuthenticationTokens?.FirstOrDefault(t => t.Name == "refresh_token")?.Value;

            if (!string.IsNullOrEmpty(accessToken))
            {
                await _userManager.SetAuthenticationTokenAsync(user, "Google", "access_token", accessToken);
                _tokenProvider.AccessToken = accessToken; // Also update session cache
                Console.WriteLine($"GoogleCallback - Access token stored in DB");
            }

            if (!string.IsNullOrEmpty(refreshToken))
            {
                await _userManager.SetAuthenticationTokenAsync(user, "Google", "refresh_token", refreshToken);
                Console.WriteLine($"GoogleCallback - Refresh token stored in DB");
            }
        }

        return LocalRedirect(returnUrl);
    }
    
    [HttpGet]
    public async Task<IActionResult> LogOut(string returnUrl = "/")
    {
        await _signInManager.SignOutAsync();
        _tokenProvider.AccessToken = null; // Clear the token on logout
        return LocalRedirect(returnUrl);
    }
}

