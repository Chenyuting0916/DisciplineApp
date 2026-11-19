using System.Text.Json;
using DisciplineApp.Services;
using Xunit;

namespace DisciplineApp.Tests.Services;

public class SecurityConfigTests
{
    [Fact]
    public void TrackedAppsettings_HasEmptyGoogleClientSecret()
    {
        var path = LocateRepoFile("appsettings.json");
        var raw = File.ReadAllText(path);
        Assert.DoesNotContain("GOCSPX-", raw, StringComparison.Ordinal);

        using var doc = JsonDocument.Parse(raw);
        var secret = doc.RootElement
            .GetProperty("Authentication")
            .GetProperty("Google")
            .GetProperty("ClientSecret")
            .GetString();
        Assert.True(string.IsNullOrEmpty(secret));
    }

    [Fact]
    public void ShareToken_Is128BitsOfHex()
    {
        var first = ChallengeService.CreateShareToken();
        var second = ChallengeService.CreateShareToken();
        Assert.Equal(32, first.Length);
        Assert.NotEqual(first, second);
        Assert.All(first, c => Assert.True(Uri.IsHexDigit(c)));
    }

    private static string LocateRepoFile(string fileName)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, fileName);
            if (File.Exists(candidate) && File.Exists(Path.Combine(dir.FullName, "DisciplineApp.csproj")))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        throw new FileNotFoundException(fileName);
    }
}
