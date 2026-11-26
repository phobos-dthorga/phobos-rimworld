using System.Collections.Generic;
using Phobos.Radio.Physics;
using Xunit;

namespace Phobos.Radio.Physics.Tests;

public class RelayLedgerTests
{
    // The radio's three kinds as ChannelKinds.xml and Modes.xml set them: control (12.5 kHz, keyed half the time, pre-empts), picture
    // (50 kHz, always keyed), voice (12.5 kHz, keyed a fifth of the time). 25 kHz channels.
    const int Control = 0, Picture = 1, Voice = 2;
    static readonly IReadOnlyList<KindShape> Kinds = new[]
    {
        new KindShape(1, 2, Preempts: true),
        new KindShape(2, 1),
        new KindShape(1, 5),
    };
    const int TwoChannels = 2;

    static void Hold(RelayLedger ledger, string caller, int kind, string relay, int now = 0, int until = 100) =>
        ledger.Hold(caller, kind, new object[] { relay }, Kinds, _ => TwoChannels, now, until);

    [Fact]
    public void ARelayUseTakesTwiceTheChannelsOneInOneOut()
    {
        Assert.Equal(2, RelayLedger.AtRelay(Kinds[Voice]).ChannelsPerUse);
        Assert.Equal(4, RelayLedger.AtRelay(Kinds[Picture]).ChannelsPerUse);
        Assert.Equal(5, RelayLedger.AtRelay(Kinds[Voice]).RadiosPerUse);
    }

    [Fact]
    public void TwoChannelsCarryFiveTalkingConvoysTakingTurns()
    {
        var ledger = new RelayLedger();
        for (var i = 0; i < 5; i++)
        {
            Assert.True(ledger.HasRoom("mast", "convoy" + i, Voice, Kinds, TwoChannels, 0));
            Hold(ledger, "convoy" + i, Voice, "mast");
        }
        Assert.False(ledger.HasRoom("mast", "convoy5", Voice, Kinds, TwoChannels, 0));
        Assert.Equal((5, 2), ledger.Use("mast", Kinds, 0));
    }

    [Fact]
    public void TwoChannelsCarryTwoDrivenTrucks()
    {
        var ledger = new RelayLedger();
        Hold(ledger, "truck1", Control, "mast");
        Hold(ledger, "truck2", Control, "mast");
        Assert.False(ledger.HasRoom("mast", "truck3", Control, Kinds, TwoChannels, 0));
    }

    [Fact]
    public void APictureNeedsMoreThanARepeaterHas()
    {
        Assert.False(new RelayLedger().HasRoom("mast", "camera", Picture, Kinds, TwoChannels, 0));
    }

    [Fact]
    public void ACallerAlreadyThroughAFullRelayKeepsItsPlace()
    {
        var ledger = new RelayLedger();
        Hold(ledger, "truck1", Control, "mast");
        Hold(ledger, "truck2", Control, "mast");
        Assert.True(ledger.HasRoom("mast", "truck1", Control, Kinds, TwoChannels, 0));
    }

    [Fact]
    public void ALeaseLapsesWhenItsCallerStopsAsking()
    {
        var ledger = new RelayLedger();
        Hold(ledger, "truck1", Control, "mast", now: 0, until: 100);
        Hold(ledger, "truck2", Control, "mast", now: 0, until: 100);
        Assert.False(ledger.HasRoom("mast", "truck3", Control, Kinds, TwoChannels, 50));
        Assert.True(ledger.HasRoom("mast", "truck3", Control, Kinds, TwoChannels, 101));
    }

    [Fact]
    public void HoldingANewPathLetsGoOfTheOldOne()
    {
        var ledger = new RelayLedger();
        Hold(ledger, "truck1", Control, "mast");
        Hold(ledger, "truck1", Control, "station");
        Assert.Equal((0, 0), ledger.Use("mast", Kinds, 0));
        Assert.Equal(1, ledger.Use("station", Kinds, 0).Calls);
    }

    [Fact]
    public void OrdersPushTalkAsideAtAFullRelay()
    {
        var ledger = new RelayLedger();
        for (var i = 0; i < 5; i++) Hold(ledger, "convoy" + i, Voice, "mast", now: i);
        Assert.True(ledger.HasRoom("mast", "truck", Control, Kinds, TwoChannels, 10));
        Hold(ledger, "truck", Control, "mast", now: 10);
        Assert.True(ledger.Holds("mast", "truck", 10));
        // The talkers' whole use went: the newest first, until the truck had its two channels.
        Assert.False(ledger.Holds("mast", "convoy4", 10));
        Assert.True(ledger.HasRoom("mast", "truck2", Control, Kinds, TwoChannels, 10));
    }

    [Fact]
    public void TalkNeverPushesOrdersAside()
    {
        var ledger = new RelayLedger();
        Hold(ledger, "truck1", Control, "mast");
        Hold(ledger, "truck2", Control, "mast");
        Assert.False(ledger.HasRoom("mast", "convoy", Voice, Kinds, TwoChannels, 0));
    }

    [Fact]
    public void ReleaseFreesEveryPlace()
    {
        var ledger = new RelayLedger();
        ledger.Hold("truck", Control, new object[] { "mast", "station" }, Kinds, _ => TwoChannels, 0, 100);
        ledger.Release("truck");
        Assert.Equal((0, 0), ledger.Use("mast", Kinds, 0));
        Assert.Equal((0, 0), ledger.Use("station", Kinds, 0));
    }
}
