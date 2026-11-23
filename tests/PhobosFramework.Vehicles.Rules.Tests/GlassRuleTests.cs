using Phobos.Framework.Vehicles.Rules;
using Xunit;

namespace Phobos.Framework.Vehicles.Rules.Tests;

public class GlassRuleTests
{
    [Fact]
    public void WholeGlassIsSound() => Assert.Equal(GlassState.Sound, GlassRule.StateOf(1));

    [Fact]
    public void ChippedGlassIsStillSound() => Assert.Equal(GlassState.Sound, GlassRule.StateOf(0.8));

    [Fact]
    public void BadlyDamagedGlassIsCracked() => Assert.Equal(GlassState.Cracked, GlassRule.StateOf(0.3));

    [Fact]
    public void GlassAtNothingIsGone() => Assert.Equal(GlassState.Gone, GlassRule.StateOf(0));

    [Fact]
    public void OrdinaryGlassTakesMoreOfAPlateHitThanArmoured() =>
        Assert.True(GlassRule.PlateHitShare(GlassKind.Ordinary) > 3 * GlassRule.PlateHitShare(GlassKind.Armoured));

    [Fact]
    public void ABlastBreaksGlassBeyondItsRadius() => Assert.True(GlassRule.BlastDamage(50, 8, 4, GlassKind.Ordinary) > 0);

    [Fact]
    public void ABlastBreaksNothingPastItsReach() => Assert.Equal(0, GlassRule.BlastDamage(50, 10, 4, GlassKind.Ordinary), 9);

    [Fact]
    public void NearerIsWorse() =>
        Assert.True(GlassRule.BlastDamage(50, 1, 4, GlassKind.Ordinary) > GlassRule.BlastDamage(50, 6, 4, GlassKind.Ordinary));

    [Fact]
    public void ArmouredGlassShrugsOffMoreOfABlast() =>
        Assert.True(GlassRule.BlastDamage(50, 2, 4, GlassKind.Armoured) < GlassRule.BlastDamage(50, 2, 4, GlassKind.Ordinary));

    [Fact]
    public void NothingMissingNeedsNothing() => Assert.Equal((0, 0, 0, 0), GlassRule.Goods(GlassKind.Ordinary, 0));

    [Fact]
    public void ACrackedWindscreenTakesALaminatedPane() => Assert.Equal((1, 0, 0, 0), GlassRule.Goods(GlassKind.Ordinary, 0.3));

    [Fact]
    public void AllOrdinaryGlassGoneTakesAWindscreenAndThreePanes() => Assert.Equal((1, 3, 0, 0), GlassRule.Goods(GlassKind.Ordinary, 1));

    [Fact]
    public void ArmouredGlassTakesArmouredPanes() => Assert.Equal((0, 0, 3, 0), GlassRule.Goods(GlassKind.Armoured, 1));

    [Fact]
    public void VisionBlocksTakeBlocks() => Assert.Equal((0, 0, 0, 2), GlassRule.Goods(GlassKind.VisionBlocks, 0.5));

    [Fact]
    public void AWindscreenAloneTakesOneLaminatedPaneHoweverMuchIsGone()
    {
        Assert.Equal((1, 0, 0, 0), GlassRule.Goods(GlassKind.Windscreen, 0.3));
        Assert.Equal((1, 0, 0, 0), GlassRule.Goods(GlassKind.Windscreen, 1));
    }

    [Fact]
    public void AWindscreenAloneThrowsOneSplinterOnlyAsItGoes()
    {
        Assert.Equal(0, GlassRule.Splinters(GlassKind.Windscreen, 0.5, false));
        Assert.Equal(1, GlassRule.Splinters(GlassKind.Windscreen, 1, true));
    }

    [Fact]
    public void AWindscreenAloneTakesLessOfACabsHitsThanGlassAllRound() =>
        Assert.True(GlassRule.PlateHitShare(GlassKind.Windscreen) < GlassRule.PlateHitShare(GlassKind.Ordinary));

    [Fact]
    public void OrdinaryGlassGoingThrowsTwoSplinters() => Assert.Equal(2, GlassRule.Splinters(GlassKind.Ordinary, 1, true));

    [Fact]
    public void ASmallCrackThrowsNone() => Assert.Equal(0, GlassRule.Splinters(GlassKind.Ordinary, 0.1, false));

    [Fact]
    public void ArmouredGlassThrowsNoneUntilItGoes()
    {
        Assert.Equal(0, GlassRule.Splinters(GlassKind.Armoured, 0.5, false));
        Assert.Equal(1, GlassRule.Splinters(GlassKind.Armoured, 1, true));
    }

    [Fact]
    public void SoundGlassSlowsNobody() => Assert.Equal(1, GlassRule.ViewFactor(GlassKind.Ordinary, GlassState.Sound, true, true), 9);

    [Fact]
    public void CrackedGlassAtNightIsSlower() =>
        Assert.True(GlassRule.ViewFactor(GlassKind.Ordinary, GlassState.Cracked, true, false) < GlassRule.ViewFactor(GlassKind.Ordinary, GlassState.Cracked, false, false));

    [Fact]
    public void NoGlassOnAFineDaySlowsNobody() => Assert.Equal(1, GlassRule.ViewFactor(GlassKind.Ordinary, GlassState.Gone, false, false), 9);

    [Fact]
    public void ATankWithItsBlocksGoneCrawls() => Assert.True(GlassRule.ViewFactor(GlassKind.VisionBlocks, GlassState.Gone, false, false) <= 0.6);

    [Fact]
    public void MoreMissingIsMoreWork() => Assert.True(GlassRule.ReglazeWork(1, 400, 800) > GlassRule.ReglazeWork(0.3, 400, 800));
}
