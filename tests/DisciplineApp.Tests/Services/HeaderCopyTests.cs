using DisciplineApp.Models;
using Xunit;

namespace DisciplineApp.Tests.Services;

public class HeaderCopyTests
{
    [Fact]
    public void DisplayName_PrefersDisplayThenIdentityThenFallback()
    {
        Assert.Equal("Ada", HeaderCopy.DisplayName(" Ada ", "id", "User"));
        Assert.Equal("login", HeaderCopy.DisplayName("  ", "login", "User"));
        Assert.Equal("用戶", HeaderCopy.DisplayName(null, null, "用戶"));
        Assert.Equal("", HeaderCopy.DisplayName(null, "  ", " "));
    }

    [Fact]
    public void LevelBadge_KeepsLocalizedLabel()
    {
        Assert.Equal("等級 3", HeaderCopy.LevelBadge("等級", 3));
        Assert.Equal("レベル 1", HeaderCopy.LevelBadge(" レベル ", 1));
        Assert.Equal("7", HeaderCopy.LevelBadge(" ", 7));
        Assert.Equal("main-content", HeaderCopy.MainContentId);
    }
}
