using Phobos.Framework.Rules;
using Xunit;

namespace Phobos.Framework.Rules.Tests;

public class HistoryTests
{
    [Fact]
    public void TwentyFourHoursAtOneSamplePerRareTickIs240Samples() => Assert.Equal(240, HistoryRing.SamplesFor(24, 250));

    [Fact]
    public void TheRingKeepsTheNewestSamplesOldestFirst()
    {
        var ring = new HistoryRing(3);
        Assert.Equal(0, ring.Count);
        ring.Push(1f);
        ring.Push(2f);
        ring.Push(3f);
        ring.Push(4f);
        Assert.Equal(3, ring.Count);
        Assert.Equal(2f, ring[0]);
        Assert.Equal(4f, ring[2]);
        Assert.Equal(4f, ring.Newest);
        var copy = new float[3];
        ring.CopyTo(copy);
        Assert.Equal(new[] { 2f, 3f, 4f }, copy);
        ring.Clear();
        Assert.Equal(0, ring.Count);
    }

    [Fact]
    public void TheCodecRoundTripsWithinTheScaleAndClampsOutside()
    {
        var ring = new HistoryRing(4);
        foreach (float v in new[] { -60f, 0f, 37.5f, 95f }) ring.Push(v);
        string text = HistoryCodec.Encode(ring, -60f, 80f);
        HistoryRing back = HistoryCodec.Decode(text, 4, -60f, 80f);
        Assert.Equal(4, back.Count);
        Assert.Equal(-60f, back[0], 2);
        Assert.Equal(0f, back[1], 2);
        Assert.Equal(37.5f, back[2], 2);
        Assert.Equal(80f, back[3], 2);
    }

    [Fact]
    public void MalformedOrMissingTextLoadsAsAnEmptyRing()
    {
        Assert.Equal(0, HistoryCodec.Decode(null, 4, 0f, 1f).Count);
        Assert.Equal(0, HistoryCodec.Decode("not base64!", 4, 0f, 1f).Count);
        Assert.Equal(4, HistoryCodec.Decode("", 4, 0f, 1f).Capacity);
    }
}
