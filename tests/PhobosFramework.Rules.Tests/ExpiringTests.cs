using Phobos.Framework.Rules;
using Xunit;

namespace Phobos.Framework.Rules.Tests;

public class ExpiringTests
{
    [Fact]
    public void NothingIsHeldAtFirst() => Assert.False(new Expiring<int>().TryGet(0, out _));

    [Fact]
    public void AValueIsHeldUntilItsTimePasses()
    {
        var cache = new Expiring<int>();
        cache.Set(7, 10, 0.5);
        Assert.True(cache.TryGet(10.4, out int found));
        Assert.Equal(7, found);
        Assert.False(cache.TryGet(10.5, out _));
    }

    [Fact]
    public void SetGivesTheValueBack() => Assert.Equal("x", new Expiring<string>().Set("x", 0, 1));

    [Fact]
    public void DropForgetsAtOnce()
    {
        var cache = new Expiring<bool>();
        cache.Set(true, 0, 100);
        cache.Drop();
        Assert.False(cache.TryGet(1, out _));
    }

    [Fact]
    public void AFreshValueReplacesAnOldOne()
    {
        var cache = new Expiring<int>();
        cache.Set(1, 0, 1);
        cache.Set(2, 5, 1);
        Assert.True(cache.TryGet(5.5, out int found));
        Assert.Equal(2, found);
    }
}
