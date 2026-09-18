using DisciplineApp.Models;
using Xunit;

namespace DisciplineApp.Tests.Services;

public class ErrorCopyTests
{
    [Fact]
    public void HtmlLang_OnlyAllowsKnownCultures()
    {
        Assert.Equal("zh-TW", ErrorCopy.HtmlLang("zh-TW"));
        Assert.Equal("en", ErrorCopy.HtmlLang("en"));
        Assert.Equal("ja", ErrorCopy.HtmlLang("ja"));
        Assert.Equal("zh-TW", ErrorCopy.HtmlLang(null));
        Assert.Equal("zh-TW", ErrorCopy.HtmlLang("fr"));
        Assert.Equal("zh-TW", ErrorCopy.HtmlLang("zh-CN"));
    }

    [Fact]
    public void ShowDevelopmentHelp_OnlyInDevelopment()
    {
        Assert.True(ErrorCopy.ShowDevelopmentHelp("Development"));
        Assert.False(ErrorCopy.ShowDevelopmentHelp("Production"));
        Assert.False(ErrorCopy.ShowDevelopmentHelp("Staging"));
        Assert.False(ErrorCopy.ShowDevelopmentHelp(null));
    }
}
