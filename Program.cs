using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using DisciplineApp.Data;
using Microsoft.EntityFrameworkCore;
using DisciplineApp.Models;
using DisciplineApp.Services;
using DisciplineApp.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddControllers(); // Required for CultureController
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

// Add session support for token storage
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(24);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
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

builder.Services.AddAuthentication()
    .AddGoogle(options =>
    {
        IConfigurationSection googleAuthNSection =
            builder.Configuration.GetSection("Authentication:Google");

        options.ClientId = googleAuthNSection["ClientId"] ?? "";
        options.ClientSecret = googleAuthNSection["ClientSecret"] ?? "";
        // Removed calendar and tasks scopes to avoid "Unverified App" warning
        // options.Scope.Add("https://www.googleapis.com/auth/calendar.readonly");
        // options.Scope.Add("https://www.googleapis.com/auth/tasks.readonly");
        options.SaveTokens = true;
        
        // Removed forced consent prompt to reduce verification warnings
        // Users will only see the consent screen on first login
        // options.Events.OnRedirectToAuthorizationEndpoint = context =>
        // {
        //     var uri = context.RedirectUri;
        //     if (uri.Contains("prompt="))
        //     {
        //         uri = System.Text.RegularExpressions.Regex.Replace(uri, "prompt=[^&]*", "prompt=consent");
        //     }
        //     else
        //     {
        //         uri += "&prompt=consent";
        //     }
        //     context.Response.Redirect(uri);
        //     return Task.CompletedTask;
        // };
        
        // Fix for "Correlation failed" on localhost/HTTP
        options.CorrelationCookie.SameSite = SameSiteMode.Lax;
        options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    });

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
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseCookiePolicy();

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
app.MapControllers(); // Required for CultureController

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
