using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using DisciplineApp.Data;
using Microsoft.EntityFrameworkCore;
using DisciplineApp.Models;
using DisciplineApp.Services;
using DisciplineApp.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// SameAsRequest stays valid on local HTTP. In Azure, forwarded headers mark the
// public HTTPS request so cookies are still issued with Secure.
var secureCookies = CookieSecurePolicy.SameAsRequest;

builder.WebHost.ConfigureKestrel(options => options.AddServerHeader = false);

// Azure App Service terminates TLS. Trust only the platform forwarded scheme and address
// so HSTS and Secure cookies see the original HTTPS request.
if (!builder.Environment.IsDevelopment())
{
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.ForwardLimit = 1;
        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();
    });
}

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddControllers(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

builder.Services.AddAntiforgery(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = secureCookies;
});

// Add session support for token storage
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(24);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = secureCookies;
});

// Add HttpContextAccessor for TokenProvider
builder.Services.AddHttpContextAccessor();

// Configure DB Context with writable path for Azure
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Check if running in Azure (HOME environment variable exists)
var homePath = Environment.GetEnvironmentVariable("HOME");
if (!string.IsNullOrEmpty(homePath) && !builder.Environment.IsDevelopment())
{
    // In Azure, use a writable path
    var dbPath = Path.Combine(homePath, "app.db");
    connectionString = $"Data Source={dbPath}";
    Console.WriteLine($"Using Azure DB Path: {dbPath}");
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = secureCookies;
    options.SlidingExpiration = true;
});



builder.Services.AddScoped<IGamificationService, GamificationService>();
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IChallengeService, ChallengeService>();
builder.Services.AddScoped<TokenProvider>();
builder.Services.AddScoped<CalendarService>();
builder.Services.AddScoped<DisciplineApp.Services.LocalStorageService>();
builder.Services.AddScoped<DisciplineApp.Services.ToastService>();
builder.Services.AddScoped<DisciplineApp.Services.TimerService>();
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();
builder.Services.AddScoped<GuestTaskService>();
builder.Services.AddScoped<GuestHabitService>();
builder.Services.AddScoped<GuestIfThenService>();
builder.Services.AddScoped<GuestJournalService>();
builder.Services.AddScoped<GuestFocusService>();
builder.Services.AddScoped<GuestExportService>();
builder.Services.AddScoped<IHabitService, HabitService>();
builder.Services.AddScoped<IReflectionService, ReflectionService>();
builder.Services.AddScoped<IShopService, ShopService>();
builder.Services.AddScoped<IIntentionService, IntentionService>();
builder.Services.AddScoped<IIfThenService, IfThenService>();
builder.Services.AddScoped<IDataExportService, DataExportService>();
builder.Services.AddScoped<QuoteService>();

var googleAuthNSection = builder.Configuration.GetSection("Authentication:Google");
var googleClientId = googleAuthNSection["ClientId"];
var googleClientSecret = googleAuthNSection["ClientSecret"];
var authentication = builder.Services.AddAuthentication();
if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret))
{
    authentication.AddGoogle(options =>
    {
        options.ClientId = googleClientId;
        options.ClientSecret = googleClientSecret;
        // Identity scopes only. Calendar and Tasks scopes were removed.
        // Tokens stay in the encrypted auth cookie / user store and are not rendered into HTML.
        options.SaveTokens = true;

        // Lax + the request scheme keeps the OAuth correlation cookie working on localhost HTTP.
        options.CorrelationCookie.SameSite = SameSiteMode.Lax;
        options.CorrelationCookie.SecurePolicy = secureCookies;
        options.CorrelationCookie.HttpOnly = true;
    });
}
else
{
    Console.WriteLine("Google OAuth is not configured. Set Authentication:Google:ClientId and Authentication:Google:ClientSecret with user-secrets or environment variables.");
}

// Add CookiePolicy to handle SameSite=None correctly if needed generally
builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.MinimumSameSitePolicy = SameSiteMode.Unspecified;
    options.OnAppendCookie = cookieContext =>
    {
        CheckSameSite(cookieContext.Context, cookieContext.CookieOptions);
    };
    options.OnDeleteCookie = cookieContext =>
    {
        CheckSameSite(cookieContext.Context, cookieContext.CookieOptions);
    };
});

void CheckSameSite(HttpContext httpContext, CookieOptions options)
{
    if (options.SameSite == SameSiteMode.None)
    {
        var userAgent = httpContext.Request.Headers["User-Agent"].ToString();
        // For simplicity in this dev environment, we won't do full UA sniffing 
        // but we must ensure Secure is true if SameSite is None.
        // However, if we are on HTTP, we can't set Secure=true.
        // So we force SameSite=Lax if we are not on HTTPS.
        if (!httpContext.Request.IsHttps)
        {
            options.SameSite = SameSiteMode.Lax;
        }
    }
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseForwardedHeaders();
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseCookiePolicy();

app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers["X-Content-Type-Options"] = "nosniff";
    headers["X-Frame-Options"] = "DENY";
    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=()";
    headers["X-Permitted-Cross-Domain-Policies"] = "none";
    headers["Cross-Origin-Opener-Policy"] = "same-origin";
    var csp =
        "default-src 'self'; " +
        "base-uri 'self'; " +
        "object-src 'none'; " +
        "frame-ancestors 'none'; " +
        "script-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net; " +
        "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://fonts.googleapis.com; " +
        "font-src 'self' data: https://cdn.jsdelivr.net https://fonts.gstatic.com; " +
        "img-src 'self' data: https:; " +
        "connect-src 'self'; " +
        "worker-src 'self' blob:; " +
        "form-action 'self' https://accounts.google.com; " +
        "manifest-src 'self'";
    if (!app.Environment.IsDevelopment())
    {
        csp += "; upgrade-insecure-requests";
    }

    headers["Content-Security-Policy"] = csp;
    await next();
});

app.UseStaticFiles();

// Enable session middleware
app.UseSession();

var supportedCultures = new[] { "zh-TW", "en", "ja" };
var localizationOptions = new RequestLocalizationOptions()
    .SetDefaultCulture("zh-TW")
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures);

app.UseRequestLocalization(localizationOptions);

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers(); // Required for CultureController
app.MapGet("/manifest.json", (Microsoft.Extensions.Localization.IStringLocalizer<DisciplineApp.App> localizer) =>
{
    var json = ManifestCopy.Json(
        System.Globalization.CultureInfo.CurrentUICulture.Name,
        localizer["PwaDescription"]);
    return Results.Content(json, "application/manifest+json; charset=utf-8");
});

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

// Apply migrations on startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        context.Database.Migrate();
        Console.WriteLine("Database migrated successfully.");
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating the database.");
    }
}

app.Run();
