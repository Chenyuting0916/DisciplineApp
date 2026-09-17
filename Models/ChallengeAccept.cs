namespace DisciplineApp.Models;

public static class ChallengeAccept
{
    public static bool ClaimsOnServer(string? userId)
        => !string.IsNullOrWhiteSpace(userId);
}
