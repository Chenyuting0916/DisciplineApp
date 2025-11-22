using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using DisciplineApp.Data;
using Microsoft.EntityFrameworkCore;
using DisciplineApp.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddControllers(); // Required for CultureController
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

builder.Services.AddSingleton<WeatherForecastService>();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddScoped<DisciplineApp.Services.GamificationService>();
builder.Services.AddScoped<DisciplineApp.Services.TaskService>();
builder.Services.AddScoped<DisciplineApp.Services.TokenProvider>();
builder.Services.AddScoped<DisciplineApp.Services.CalendarService>();
builder.Services.AddScoped<DisciplineApp.Services.LocalStorageService>();
builder.Services.AddScoped<DisciplineApp.Services.ToastService>();

builder.Services.AddAuthentication()
    .AddGoogle(options =>
    {
        IConfigurationSection googleAuthNSection =
            builder.Configuration.GetSection("Authentication:Google");

        options.ClientId = googleAuthNSection["ClientId"];
        options.ClientSecret = googleAuthNSection["ClientSecret"];
        options.Scope.Add("https://www.googleapis.com/auth/calendar.readonly");
        options.SaveTokens = true;
        
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

app.Run();
