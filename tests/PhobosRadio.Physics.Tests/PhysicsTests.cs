using System.Collections.Generic;
using System;
using Phobos.Radio.Physics;
using Xunit;

namespace Phobos.Radio.Physics.Tests;

public class DecibelsTests
{
    [Fact]
    public void AShorterWavelengthGetsDiscountThroughWallsNotRock()
    {
        Assert.Equal(15, Propagation.ObstructionDb(20, 0, 0.75), 6);
        Assert.Equal(60, Propagation.ObstructionDb(0, 60, 0.75), 6);
        Assert.Equal(75, Propagation.ObstructionDb(20, 60, 0.75), 6);
    }

    [Fact]
    public void FiveWattsIsThirtySevenDbm() => Assert.Equal(36.99, Decibels.FromWatts(5), 2);

    [Fact]
    public void DoublingDistanceCostsSixDbAndFourTimesThePower()
    {
        Assert.Equal(6.02, Decibels.DoublingDistanceLossDb, 2);
        Assert.Equal(4.0, Decibels.ToMilliwatts(Decibels.DoublingDistanceLossDb), 2);
    }

    [Fact]
    public void TwoEqualNoisesSumToThreeDbMore() => Assert.Equal(-97.0, Decibels.PowerSumDbm(-100, -100), 1);

    [Fact]
    public void NothingSumsToNegativeInfinity() => Assert.True(double.IsNegativeInfinity(Decibels.PowerSumDbm(new double[0])));
}

public class PropagationTests
{
    [Fact]
    public void FreeSpaceLossAtOneKilometreAndOneHundredFortySixMegahertz()
    {
        // 20 log10(1) + 20 log10(146) + 32.45 = 75.74 dB
        Assert.Equal(75.74, Propagation.FreeSpacePathLossDb(1000, 146), 2);
    }

    [Fact]
    public void DoublingDistanceAddsSixDb()
    {
        var near = Propagation.FreeSpacePathLossDb(200, 146);
        var far = Propagation.FreeSpacePathLossDb(400, 146);
        Assert.Equal(Decibels.DoublingDistanceLossDb, far - near, 3);
    }

    [Fact]
    public void DistancesUnderOneMetreClampToOneMetre()
    {
        Assert.Equal(Propagation.FreeSpacePathLossDb(1, 146), Propagation.FreeSpacePathLossDb(0, 146), 6);
    }

    [Fact]
    public void AMastSeesOverAWallUntilTheRayDropsNearTheHandheld()
    {
        // Mast at 10 m to a hand-held at 1.5 m; wall height 2.5 m. The ray is above the wall for the
        // first 88 % of the way and below it for the last 12 %.
        Assert.False(Propagation.RayBlockedByWall(0.5, 10, 1.5, 2.5));
        Assert.False(Propagation.RayBlockedByWall(0.88, 10, 1.5, 2.5));
        Assert.True(Propagation.RayBlockedByWall(0.9, 10, 1.5, 2.5));
        Assert.True(Propagation.RayBlockedByWall(1.0, 10, 1.5, 2.5));
    }

    [Fact]
    public void TwoHandheldsAreBlockedByEveryWall()
    {
        Assert.True(Propagation.RayBlockedByWall(0.0, 1.5, 1.5, 2.5));
        Assert.True(Propagation.RayBlockedByWall(0.5, 1.5, 1.5, 2.5));
    }
}

public class NoiseFloorTests
{
    [Fact]
    public void ThermalNoiseInTwoAndAHalfKilohertzIsMinusOneHundredAndForty()
    {
        Assert.Equal(-140.0, NoiseFloor.ThermalDbm(2500, 0), 1);
    }

    [Fact]
    public void NoiseFigureAddsDirectly() => Assert.Equal(-130.0, NoiseFloor.ThermalDbm(2500, 10), 1);

    [Fact]
    public void NoLocalNoiseLeavesTheThermalFloor()
    {
        Assert.Equal(NoiseFloor.ThermalDbm(12500, 10), NoiseFloor.TotalDbm(12500, 10, double.NegativeInfinity), 6);
    }

    [Fact]
    public void StrongLocalNoiseDominates()
    {
        Assert.Equal(-90.0, NoiseFloor.TotalDbm(12500, 10, -90), 1);
    }

    [Fact]
    public void NoiseFallsSixDbPerDoubling()
    {
        Assert.Equal(-60.0, NoiseFloor.AtDistanceDbm(-60, 1), 6);
        Assert.Equal(-66.02, NoiseFloor.AtDistanceDbm(-60, 2), 2);
        Assert.Equal(-100.0, NoiseFloor.AtDistanceDbm(-60, 100), 2);
    }

    [Fact]
    public void NoiseIsUntouchedInsideTheFade()
    {
        Assert.Equal(0.0, NoiseFloor.FadeDb(0, 12, 2.0 / 3.0));
        Assert.Equal(0.0, NoiseFloor.FadeDb(8, 12, 2.0 / 3.0));
    }

    [Fact]
    public void NoiseFadesSmoothlyToNothingAtTheRadius()
    {
        // Halfway through the fade the power weight is a half: −3 dB.
        Assert.Equal(-3.01, NoiseFloor.FadeDb(10, 12, 2.0 / 3.0), 2);
        Assert.True(NoiseFloor.FadeDb(11.9, 12, 2.0 / 3.0) < -20);
        Assert.True(double.IsNegativeInfinity(NoiseFloor.FadeDb(12, 12, 2.0 / 3.0)));
        Assert.True(double.IsNegativeInfinity(NoiseFloor.FadeDb(13, 12, 2.0 / 3.0)));
    }

    [Fact]
    public void TheFadeNeverRises()
    {
        var last = 0.0;
        for (var d = 0.0; d < 12; d += 0.25)
        {
            var fade = NoiseFloor.FadeDb(d, 12, 2.0 / 3.0);
            Assert.True(fade <= last + 1e-12, $"fade rose at {d}");
            last = fade;
        }
    }

    [Fact]
    public void AFadeStartOfOneKeepsTheHardEdge()
    {
        Assert.Equal(0.0, NoiseFloor.FadeDb(11.99, 12, 1.0));
        Assert.True(double.IsNegativeInfinity(NoiseFloor.FadeDb(12, 12, 1.0)));
    }

    [Fact]
    public void TwoLevelsSumWithoutTheListForm()
    {
        Assert.Equal(Decibels.PowerSumDbm(new[] { -100.0, -97.0 }), Decibels.PowerSumDbm(-100, -97), 9);
        Assert.Equal(-100.0, Decibels.PowerSumDbm(-100, double.NegativeInfinity), 9);
        Assert.True(double.IsNegativeInfinity(Decibels.PowerSumDbm(double.NegativeInfinity, double.NegativeInfinity)));
    }
}

public class AmplifierTests
{
    [Fact]
    public void TenDbOfGainIsTenTimesThePowerUpToItsRating()
    {
        Assert.Equal(100.0, Amplifier.OutputWatts(10, 10, 100), 6);   // the terminal's 10 W through the 100 W amplifier
        Assert.Equal(100.0, Amplifier.OutputWatts(25, 10, 100), 6);   // the desk's 25 W would make 250: capped
        Assert.Equal(250.0, Amplifier.OutputWatts(25, 10, 400), 6);   // the heavy amplifier's 400 W rating leaves room
        Assert.Equal(0.0, Amplifier.OutputWatts(0, 10, 100));
    }

    [Fact]
    public void TheStrongestPoweredAmplifierDecides()
    {
        var both = new (double, double, double, bool)[] { (10, 100, 0.5, true), (10, 400, 0.5, true) };
        Assert.Equal(250.0, Amplifier.ThroughWatts(25, both), 6);
    }

    [Fact]
    public void AnIdleAmplifierOnlyCostsItsLossOnTheWayThrough()
    {
        var idle = new (double, double, double, bool)[] { (10, 100, 0.5, false) };
        Assert.Equal(25.0 * Math.Pow(10, -0.05), Amplifier.ThroughWatts(25, idle), 6);
        Assert.Equal(25.0, Amplifier.ThroughWatts(25, Array.Empty<(double, double, double, bool)>()), 6);
    }
}

public class ChannelTests
{
    private static readonly Dictionary<int, int> None = new();
    private static readonly KindShape Voice = new(1, 5);           // FM voice: one channel, five taking turns
    private static readonly KindShape WideData = new(4, 1);        // a 100 kHz link keyed all the time
    private static readonly KindShape[] VoiceOnly = { Voice };
    private static readonly KindShape[] VoiceAndData = { WideData, Voice };

    [Fact]
    public void VoiceTakesOneTwentyFiveKilohertzChannelAndFiveShareIt()
    {
        Assert.Equal(1, Channels.PerUse(12500, 25000));      // narrowband FM voice
        Assert.Equal(1, Channels.PerUse(25000, 25000));
        Assert.Equal(4, Channels.PerUse(100000, 25000));     // a wide data mode takes four
        Assert.Equal(5, Channels.RadiosPerUse(0.2));
        Assert.Equal(1, Channels.ChannelsHeld(5, Voice));
        Assert.Equal(2, Channels.ChannelsHeld(6, Voice));
        Assert.Equal(0, Channels.ChannelsHeld(0, Voice));
        Assert.Equal(8, Channels.ChannelsHeld(2, WideData));
    }

    [Fact]
    public void KindsFillBlocksOfTheirOwn()
    {
        // One data link and three voices on the terminal's four channels: the link takes all four, so the voices find no room.
        Assert.Equal(5, Channels.ChannelsHeld(new[] { 1, 3 }, VoiceAndData));
        // Each kind's room, with the other as it is.
        Assert.Equal(20, Channels.Room(1, new[] { 0, 0 }, VoiceAndData, 4));   // the terminal, empty: twenty voices
        Assert.Equal(17, Channels.Room(1, new[] { 0, 3 }, VoiceAndData, 4));   // two turns left on channel 1, three channels free
        Assert.Equal(0, Channels.Room(0, new[] { 0, 3 }, VoiceAndData, 4));    // a data link needs four free channels
        Assert.Equal(1, Channels.Room(0, new[] { 0, 0 }, VoiceAndData, 4));
        Assert.Equal(5, Channels.Room(0, new[] { 0 }, VoiceOnly, 1));          // the desk, empty: five voices
        Assert.Equal(0, Channels.Room(3, new[] { 0 }, VoiceOnly, 1));          // a kind the ledger does not know has no room
    }

    [Fact]
    public void TheBestStationWithRoomServesEachRadio()
    {
        var channels = new Dictionary<int, int> { [100] = 1, [200] = 1 };
        var candidates = new List<ChannelCandidate>();
        for (var radio = 1; radio <= 6; radio++) candidates.Add(new ChannelCandidate(radio, 0, new[] { 100, 200 }));
        candidates.Add(new ChannelCandidate(7, 0, new[] { 100 }));
        var served = Channels.Allocate(channels, VoiceOnly, candidates, None);
        for (var radio = 1; radio <= 5; radio++) Assert.Equal(100, served[radio]);
        Assert.Equal(200, served[6]);        // station 100 is full: the next best
        Assert.False(served.ContainsKey(7)); // only station 100 hears it, and it is full: the net is busy
    }

    [Fact]
    public void AKindServedFirstCanCrowdTheOthersOut()
    {
        // The caller puts the data link first (its kind's order): it takes the terminal's four channels and the voice waits.
        var channels = new Dictionary<int, int> { [100] = 4 };
        var candidates = new[] { new ChannelCandidate(1, 0, new[] { 100 }), new ChannelCandidate(2, 1, new[] { 100 }) };
        var served = Channels.Allocate(channels, VoiceAndData, candidates, None);
        Assert.Equal(100, served[1]);
        Assert.False(served.ContainsKey(2));
        // On a desk of one channel the link cannot fit at all, so the voice is served.
        served = Channels.Allocate(new Dictionary<int, int> { [100] = 1 }, VoiceAndData, candidates, None);
        Assert.False(served.ContainsKey(1));
        Assert.Equal(100, served[2]);
    }

    [Fact]
    public void AHolderKeepsItsChannelSoNobodySwaps()
    {
        var channels = new Dictionary<int, int> { [100] = 4 };
        var candidates = new[] { new ChannelCandidate(1, 0, new[] { 100 }), new ChannelCandidate(2, 1, new[] { 100 }) };
        var served = Channels.Allocate(channels, VoiceAndData, candidates, new Dictionary<int, int> { [2] = 100 });
        Assert.Equal(100, served[2]);       // the voice held it, though the link's kind comes first
        Assert.False(served.ContainsKey(1)); // three channels left: not enough for the link
    }

    // The remote control of vehicles (Radio 0.9.0): orders first, and they pre-empt; pictures next; talk last.
    private static readonly KindShape Control = new(1, 2, Preempts: true);   // 12.5 kHz data keyed half the time: two vehicles a channel
    private static readonly KindShape Picture = new(2, 1);                   // 50 kHz video keyed all the time: two channels, one vehicle
    private static readonly KindShape[] Vehicles = { Control, Picture, Voice };

    private static List<ChannelCandidate> TalkersThenOthers(int talkers, int talkerKind, IReadOnlyList<int> talkerStations,
        params ChannelCandidate[] first)
    {
        var candidates = new List<ChannelCandidate>(first);
        for (var radio = 1; radio <= talkers; radio++) candidates.Add(new ChannelCandidate(radio, talkerKind, talkerStations));
        return candidates;
    }

    private static Dictionary<int, int> AllOn(int station, int radios)
    {
        var held = new Dictionary<int, int>();
        for (var radio = 1; radio <= radios; radio++) held[radio] = station;
        return held;
    }

    [Fact]
    public void ControlTakesAChannelFromTheWeakestTalkersOnly()
    {
        // Six colonists hold both of a desk's channels; a truck asks for orders. Talk is put off a channel's worth at a time, the
        // weakest first (last in the order), and no more than the truck needs: one colonist here, as five still fit on one channel.
        var channels = new Dictionary<int, int> { [100] = 2 };
        var candidates = TalkersThenOthers(6, 2, new[] { 100 }, new ChannelCandidate(10, 0, new[] { 100 }));
        var served = Channels.Allocate(channels, Vehicles, candidates, AllOn(100, 6));
        Assert.Equal(100, served[10]);
        for (var radio = 1; radio <= 5; radio++) Assert.Equal(100, served[radio]);
        Assert.False(served.ContainsKey(6));

        // The next pass, with the truck holding: nothing changes, so nobody swaps back and forth.
        var again = Channels.Allocate(channels, Vehicles, candidates, served);
        Assert.Equal(served, again);

        // On a desk of one channel the truck needs all of it: five talking colonists go "net busy".
        served = Channels.Allocate(new Dictionary<int, int> { [100] = 1 }, Vehicles,
            TalkersThenOthers(5, 2, new[] { 100 }, new ChannelCandidate(10, 0, new[] { 100 })), AllOn(100, 5));
        Assert.Equal(100, served[10]);
        Assert.Single(served);
    }

    [Fact]
    public void ATalkerPutOffMovesToAnotherStationWithRoom()
    {
        var channels = new Dictionary<int, int> { [100] = 2, [200] = 1 };
        var candidates = TalkersThenOthers(6, 2, new[] { 100, 200 }, new ChannelCandidate(10, 0, new[] { 100 }));
        var served = Channels.Allocate(channels, Vehicles, candidates, AllOn(100, 6));
        Assert.Equal(100, served[10]);
        Assert.Equal(200, served[6]);   // put off at the desk, served by the other station
    }

    [Fact]
    public void APictureTakesNothingFromTalkers()
    {
        var channels = new Dictionary<int, int> { [100] = 2 };
        var candidates = TalkersThenOthers(6, 2, new[] { 100 }, new ChannelCandidate(10, 1, new[] { 100 }));
        var served = Channels.Allocate(channels, Vehicles, candidates, AllOn(100, 6));
        Assert.False(served.ContainsKey(10));
        for (var radio = 1; radio <= 6; radio++) Assert.Equal(100, served[radio]);
    }

    [Fact]
    public void PreemptionTakesNothingWhereItCannotMakeRoomOrFromKindsBeforeIt()
    {
        // A use wider than the station: putting everyone off would not make room, so nobody is put off.
        var wideControl = new KindShape(4, 1, Preempts: true);
        var served = Channels.Allocate(new Dictionary<int, int> { [100] = 2 }, new[] { wideControl, Voice },
            TalkersThenOthers(6, 1, new[] { 100 }, new ChannelCandidate(10, 0, new[] { 100 })), AllOn(100, 6));
        Assert.False(served.ContainsKey(10));
        Assert.Equal(6, served.Count);

        // A kind that pre-empts takes nothing from a kind served before it.
        served = Channels.Allocate(new Dictionary<int, int> { [100] = 1 }, new[] { Voice, Control },
            TalkersThenOthers(5, 0, new[] { 100 }, new ChannelCandidate(10, 1, new[] { 100 })), AllOn(100, 5));
        Assert.False(served.ContainsKey(10));
        Assert.Equal(5, served.Count);

        // Nor from another kind that pre-empts.
        var otherControl = new KindShape(1, 1, Preempts: true);
        served = Channels.Allocate(new Dictionary<int, int> { [100] = 1 }, new[] { Control, otherControl },
            new[] { new ChannelCandidate(10, 0, new[] { 100 }), new ChannelCandidate(1, 1, new[] { 100 }) },
            new Dictionary<int, int> { [1] = 100 });
        Assert.False(served.ContainsKey(10));
        Assert.Equal(100, served[1]);
    }

    [Fact]
    public void RadiosOnAChannelTakeTurns()
    {
        // Five radios at a fifth each: in any one tick exactly one of them is keyed.
        for (var tick = 0; tick < 600; tick += 7)
        {
            var keyed = 0;
            for (var place = 0; place < 5; place++) if (Channels.KeyedNow(place, 5, tick, 600, 0.2)) keyed++;
            Assert.Equal(1, keyed);
        }
        Assert.Equal(0, Channels.ChannelOf(4, Voice, 0));
        Assert.Equal(1, Channels.ChannelOf(5, Voice, 0));
        Assert.Equal(4, Channels.ChannelOf(0, Voice, 4));    // after a data link's block of four
        Assert.Equal(4, Channels.ChannelOf(1, WideData, 0)); // the second link starts on the fifth channel
    }
}

public class OtherModeTests
{
    [Fact]
    public void APictureNeedsFifteenDecibelsMoreThanOrders()
    {
        // Control: 12.5 kHz, 6 dB asked. Picture: 50 kHz (four times as wide, 6 dB more noise), 15 dB asked (9 dB more): 15 dB less margin.
        Assert.Equal(5.0, OtherMode.MarginDb(20, 12500, 6, 50000, 15), 1);
        Assert.Equal(-15.0, OtherMode.MarginDb(0, 12500, 6, 50000, 15), 1);
    }

    [Fact]
    public void TheSameModeKeepsItsMargin() => Assert.Equal(12.0, OtherMode.MarginDb(12, 12500, 12, 12500, 12), 6);

    [Fact]
    public void ItAgreesWithTheFloorWhereThermalNoiseRules()
    {
        // Worked from the floors themselves: the difference in thermal floors plus the difference in what each mode asks.
        var floors = NoiseFloor.ThermalDbm(50000, 8) - NoiseFloor.ThermalDbm(12500, 8);
        Assert.Equal(30 - floors - 9, OtherMode.MarginDb(30, 12500, 6, 50000, 15), 6);
    }
}

public class CoaxTests
{
    [Fact]
    public void LossScalesWithLengthAndRootFrequency()
    {
        // RG-213-like: 8 dB per 100 m at 146 MHz. 50 m at 146 MHz = 4 dB; at 4 × the frequency, 2 × the loss.
        Assert.Equal(4.0, Coax.LossDb(50, 8, 146, 146), 6);
        Assert.Equal(8.0, Coax.LossDb(50, 8, 146, 584), 6);
    }

    [Fact]
    public void ZeroLengthIsFree() => Assert.Equal(0.0, Coax.LossDb(0, 8, 146, 146));
}

public class LinkSheetTests
{
    private static LinkInputs Handheld250mOpenGround() => new(
        TransmitPowerDbm: Decibels.FromWatts(5),
        TransmitAntennaGainDbi: 0,
        TransmitFeedLossDb: 0,
        PathLossDb: Propagation.FreeSpacePathLossDb(250, 146),
        ObstructionLossDb: 0,
        ReceiveAntennaGainDbi: 0,
        ReceiveFeedLossDb: 0,
        NoiseFloorDbm: NoiseFloor.ThermalDbm(12500, 10),
        RequiredSnrDb: 12);

    [Fact]
    public void TheSheetAddsUp()
    {
        var direction = new LinkDirection(Handheld250mOpenGround());
        // 37.0 − 63.7 = −26.7 dBm received; floor −123.0 dBm; SNR 96.3; margin 84.3.
        Assert.Equal(-26.7, direction.ReceivedDbm, 1);
        Assert.Equal(84.3, direction.MarginDb, 1);
        Assert.Equal(LinkDirection.KeyMargin, direction.Terms[^1].Key);
        Assert.Equal(direction.MarginDb, direction.Terms[^1].Value);
    }

    [Fact]
    public void TheLargestLossIsThePlainReason()
    {
        var inputs = Handheld250mOpenGround() with { ObstructionLossDb = 80 };
        Assert.Equal(LinkDirection.KeyObstruction, new LinkDirection(inputs).LargestLoss().Key);
        Assert.Equal(LinkDirection.KeyPath, new LinkDirection(Handheld250mOpenGround()).LargestLoss().Key);
    }

    [Fact]
    public void TheWorseDirectionDecides()
    {
        var forward = new LinkDirection(Handheld250mOpenGround());
        var reverse = new LinkDirection(Handheld250mOpenGround() with { ObstructionLossDb = 80 });
        var link = new LinkBudget(forward, reverse, MarginBands.Default);
        Assert.True(link.ReverseLimits);
        Assert.Equal(reverse.MarginDb, link.MarginDb, 6);
        Assert.Equal(LinkOutcome.Marginal, link.Outcome);
    }

    [Fact]
    public void TheOverlaysArithmeticIsTheSheets()
    {
        var forwardInputs = Handheld250mOpenGround();
        var reverseInputs = Handheld250mOpenGround() with { ObstructionLossDb = 80 };
        var sheet = new LinkBudget(new LinkDirection(forwardInputs), new LinkDirection(reverseInputs), MarginBands.Default);
        Assert.Equal(sheet.MarginDb, LinkMath.TwoWayMarginDb(forwardInputs, reverseInputs), 12);
        Assert.Equal(new LinkDirection(forwardInputs).ReceivedDbm, LinkMath.ReceivedDbm(forwardInputs), 12);
    }

    [Theory]
    [InlineData(-0.1, LinkOutcome.NoContact)]
    [InlineData(0.0, LinkOutcome.Marginal)]
    [InlineData(5.9, LinkOutcome.Marginal)]
    [InlineData(6.0, LinkOutcome.Workable)]
    [InlineData(14.9, LinkOutcome.Workable)]
    [InlineData(15.0, LinkOutcome.Clean)]
    public void MarginBandsFollowTheDesign(double margin, LinkOutcome expected)
    {
        Assert.Equal(expected, MarginBands.Default.Classify(margin));
    }
}

public class RemoteTurretRuleTests
{
    [Fact]
    public void ATurretFiresWatchedOrWithTheSetting()
    {
        // Radio 0.18.0 (the owner: "Picture link, or the setting").
        Assert.False(RemoteTurretRule.MayFire(watched: false, fireAlone: false));
        Assert.True(RemoteTurretRule.MayFire(watched: true, fireAlone: false));
        Assert.True(RemoteTurretRule.MayFire(watched: false, fireAlone: true));
    }

    [Fact]
    public void ATurretTakesOneOfTheOperatorsHandsAfterTheVehicles()
    {
        // Two hands: two vehicles leave none for a turret; one vehicle leaves one.
        Assert.False(RemoteTurretRule.HasHand(operatorAtConsole: true, place: 2, handsPerOperator: 2));
        Assert.True(RemoteTurretRule.HasHand(operatorAtConsole: true, place: 1, handsPerOperator: 2));
        Assert.False(RemoteTurretRule.HasHand(operatorAtConsole: false, place: 0, handsPerOperator: 2));
    }
}
