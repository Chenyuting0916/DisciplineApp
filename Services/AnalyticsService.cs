using DisciplineApp.Data;
using DisciplineApp.Models;
using DisciplineApp.Services.Interfaces;
using Microsoft.AspNetCore.Components.Authorization;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace DisciplineApp.Services;

public class AnalyticsService : IAnalyticsService
{
    private readonly ApplicationDbContext _context;
    private readonly AuthenticationStateProvider _authenticationStateProvider;

    public AnalyticsService(ApplicationDbContext context, AuthenticationStateProvider authenticationStateProvider)
    {
        _context = context;
        _authenticationStateProvider = authenticationStateProvider;
    }

    public async Task TrackEventAsync(string eventName, string? category = null, string? data = null)
    {
        var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;
        string? userId = user.Identity?.IsAuthenticated == true ? user.FindFirst(ClaimTypes.NameIdentifier)?.Value : null;

        var analyticsEvent = new AnalyticsEvent
        {
            UserId = userId,
            EventName = eventName,
            Category = category,
            Data = data,
            Timestamp = DateTime.UtcNow
        };

        _context.AnalyticsEvents.Add(analyticsEvent);
        await _context.SaveChangesAsync();
    }
}
