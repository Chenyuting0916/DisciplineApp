using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using DisciplineApp.Models;

namespace DisciplineApp.Controllers;

[Route("[controller]/[action]")]
public class CultureController : Controller
{
    public IActionResult Set(string culture, string redirectUri)
    {
        if (InputGuard.IsAllowedCulture(culture))
        {
            HttpContext.Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(
                    new RequestCulture(culture, culture)),
                new CookieOptions
                {
                    HttpOnly = true,
                    IsEssential = true,
                    SameSite = SameSiteMode.Lax,
                    MaxAge = TimeSpan.FromDays(365)
                });
        }

        var target = Url.IsLocalUrl(redirectUri) ? redirectUri : "/";
        return LocalRedirect(target);
    }
}
