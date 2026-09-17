using DisciplineApp.Models;
using Xunit;

namespace DisciplineApp.Tests.Services;

public class ShortcutCopyTests
{
    [Fact]
    public void Rows_MatchTimerKeys()
    {
        Assert.Equal(4, ShortcutCopy.Rows.Count);
        Assert.Equal("Space", ShortcutCopy.Rows[0].Keys);
        Assert.Equal("1 / 2 / 3", ShortcutCopy.Rows[1].Keys);
        Assert.Equal("S", ShortcutCopy.Rows[2].Keys);
        Assert.Equal("Esc", ShortcutCopy.Rows[3].Keys);
        Assert.All(ShortcutCopy.Rows, row => Assert.False(string.IsNullOrWhiteSpace(row.ActionKey)));
        Assert.DoesNotContain(ShortcutCopy.Rows, row => row.ActionKey.Contains('<') || row.Keys.Contains('<'));
    }
}
