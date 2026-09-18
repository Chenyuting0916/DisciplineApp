using DisciplineApp.Models;
using Xunit;

namespace DisciplineApp.Tests.Services;

public class ManifestAndLevelUpTests
{
    [Fact]
    public void Manifest_ClampsLangAndDescription()
    {
        using var zh = System.Text.Json.JsonDocument.Parse(
            ManifestCopy.Json("zh-TW", "專注、連續天數，和每天小小的承諾。"));
        Assert.Equal("zh-TW", zh.RootElement.GetProperty("lang").GetString());
        Assert.Equal("專注、連續天數，和每天小小的承諾。", zh.RootElement.GetProperty("description").GetString());
        Assert.Equal("favicon.png", zh.RootElement.GetProperty("icons")[0].GetProperty("src").GetString());

        using var en = System.Text.Json.JsonDocument.Parse(ManifestCopy.Json("en", "<b>ok</b>"));
        Assert.Equal("en", en.RootElement.GetProperty("lang").GetString());
        Assert.Equal("<b>ok</b>", en.RootElement.GetProperty("description").GetString());

        Assert.Equal("en", ManifestCopy.Lang("en"));
        Assert.Equal("ja", ManifestCopy.Lang("ja"));
        Assert.Equal("zh-TW", ManifestCopy.Lang("fr"));
        Assert.Equal("zh-TW", ManifestCopy.Lang(null));

        var longDesc = new string('a', ManifestCopy.DescriptionMax + 40);
        using var clamped = System.Text.Json.JsonDocument.Parse(ManifestCopy.Json("en", longDesc));
        Assert.Equal(ManifestCopy.DescriptionMax, clamped.RootElement.GetProperty("description").GetString()!.Length);
    }

    [Fact]
    public void LevelUp_FormatsCurrentLevel()
    {
        Assert.Equal("你現在是 4 級", LevelUpCopy.NowAt("你現在是 {0} 級", 4));
        Assert.Equal("You are now level 2.", LevelUpCopy.NowAt("You are now level {0}.", 2));
        Assert.Equal("7", LevelUpCopy.NowAt("  ", 7));
        Assert.Equal("3", LevelUpCopy.NowAt("{0} {1}", 3));
    }
}
