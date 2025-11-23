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
        properties.Items["scope"] = "openid profile email https://www.googleapis.com/auth/calendar.readonly";
        properties.Items["access_type"] = "offline";
        properties.Items["prompt"] = "consent";
        
        return new ChallengeResult(GoogleDefaults.AuthenticationScheme, properties);
    }

    [HttpGet]
    public async Task<IActionResult> GoogleCallback(string returnUrl = "/", string remoteError = null)
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

        // Extract and store the access token
        var accessToken = info.AuthenticationTokens?.FirstOrDefault(t => t.Name == "access_token")?.Value;
        if (!string.IsNullOrEmpty(accessToken))
        {
            _tokenProvider.AccessToken = accessToken;
            Console.WriteLine($"GoogleCallback - Access token stored: {accessToken.Substring(0, Math.Min(20, accessToken.Length))}...");
        }
        else
        {
            Console.WriteLine("GoogleCallback - No access token found in authentication tokens");
        }

        // Sign in the user with this external login provider if the user already has a login.
        var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: true, bypassTwoFactor: true);
        if (result.Succeeded)
        {
            // Update DisplayName if it's not set
            var email = info.Principal.FindFirstValue(ClaimTypes.Email);
            if (email != null)
            {
                var existingUser = await _userManager.FindByEmailAsync(email);
                if (existingUser != null && string.IsNullOrEmpty(existingUser.DisplayName))
                {
                    var name = info.Principal.FindFirstValue(ClaimTypes.Name);
                    existingUser.DisplayName = name ?? email;
                    await _userManager.UpdateAsync(existingUser);
                }
            }
            return LocalRedirect(returnUrl);
        }
        
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
                var user = new ApplicationUser 
                { 
                    UserName = email, 
                    Email = email,
                    DisplayName = name ?? email
                };
                var createResult = await _userManager.CreateAsync(user);
                if (createResult.Succeeded)
                {
                    createResult = await _userManager.AddLoginAsync(user, info);
                    if (createResult.Succeeded)
                    {
                        await _signInManager.SignInAsync(user, isPersistent: true);
                        return LocalRedirect(returnUrl);
                    }
                }
            }
            
            return Redirect("/");
        }
    }
    
    [HttpGet]
    public async Task<IActionResult> LogOut(string returnUrl = "/")
    {
        await _signInManager.SignOutAsync();
        _tokenProvider.AccessToken = null; // Clear the token on logout
        return LocalRedirect(returnUrl);
    }
}

