using Phobos.Framework.Rules;
using Xunit;

namespace Phobos.Framework.Rules.Tests;

public class ErrorAttributionRuleTests
{
    // Shapes copied from the owner's Player.log of 19 Nov 2026, with our names put in where a test needs them.
    const string GameException = "Exception in Verse.AI.ThinkNode_Tagger TryIssueJobPackage: System.NullReferenceException: Object reference not set to an instance of an object.\n"
        + "[Ref 692179C4]\n"
        + "   at RimWorld.ThinkNode_ConditionalHasLordDuty.Satisfied(Pawn pawn) in Assembly-CSharp.dll:token 0x6005f18+0x8\n"
        + "   at Verse.AI.ThinkNode_Conditional.TryIssueJobPackage(Pawn pawn, JobIssueParams jobParams) in Assembly-CSharp.dll:token 0x600459c+0x0";

    const string CallSite = "    at .Verse.AI.Pawn_JobTracker.DetermineNextConstantThinkTreeJob_Patch2(Pawn_JobTracker this) [0xFFFFFFFF]\n"
        + "    at Verse.Pawn.TickInterval(Int32 delta) [0x46]";

    const string OurFrameInException = "Exception ticking Phobos_Osprey: System.NullReferenceException: Object reference not set to an instance of an object.\n"
        + "   at Phobos.VehicleSystems.CompPhobosFlight.CompTick() in PhobosFramework.Vehicles.dll:token 0x6000123+0x10\n"
        + "   at Vehicles.VehiclePawn.Tick() in Vehicles.dll:token 0x6000456+0x0";

    const string OurPatchAnnotation = "    at Verse.AI.Group.Lord.ExposeData_Patch1(Lord this)\n"
        + "    - POSTFIX PhobosDthorga.Patches: Verse.AI.Group.Lord.ExposeData <- Void Phobos.ClassicalPatches.RaidOrdersOnLoadPatch:Postfix(LordJob& ___curJob)\n"
        + "    - PREFIX cbf.custombaseframework: Verse.Map.MapPreTick <- Boolean CustomBaseFramework.Patch_CBFEbenenruheVorTick:Prefix(Map __instance)";

    [Fact]
    public void AnErrorWithNoSignOfUsIsLeftAlone() => Assert.Equal(AttributionKind.None, ErrorAttributionRule.Find(GameException, CallSite).Kind);

    [Fact]
    public void AFrameOfOursInTheExceptionIsFound()
    {
        Attribution found = ErrorAttributionRule.Find(OurFrameInException, CallSite);
        Assert.Equal(AttributionKind.OurFrame, found.Kind);
        Assert.Equal("Phobos.VehicleSystems.CompPhobosFlight.CompTick", found.Where);
    }

    [Fact]
    public void AFrameOfOursInTheCallSiteIsFound()
    {
        Attribution found = ErrorAttributionRule.Find("Dropped Banuk in a null map.", "    at .Phobos.Framework.TopUp.TopUps.Fill(Thing thing) [0x12]\n" + CallSite);
        Assert.Equal(AttributionKind.OurFrame, found.Kind);
        Assert.Equal("Phobos.Framework.TopUp.TopUps.Fill", found.Where);
    }

    [Fact]
    public void APatchOfOursOnThePathIsFound()
    {
        Attribution found = ErrorAttributionRule.Find(GameException, OurPatchAnnotation);
        Assert.Equal(AttributionKind.OurPatch, found.Kind);
        Assert.Equal("Phobos.ClassicalPatches.RaidOrdersOnLoadPatch.Postfix", found.Where);
    }

    [Fact]
    public void OurFrameOutranksOurPatch()
    {
        Attribution found = ErrorAttributionRule.Find(OurFrameInException, OurPatchAnnotation);
        Assert.Equal(AttributionKind.OurFrame, found.Kind);
    }

    [Fact]
    public void AnotherModsPatchIsNotOurs() => Assert.Null(ErrorAttributionRule.OurPatch(
        "- PREFIX cbf.custombaseframework: Verse.Map.MapPreTick <- Boolean CustomBaseFramework.Patch_CBFEbenenruheVorTick:Prefix(Map __instance)"));

    [Fact]
    public void OurLoggingFramesAreNotCounted()
    {
        Assert.Null(ErrorAttributionRule.OurFrame("at Phobos.Framework.Logging.HostLog.Write(String category, HostLevel level, String text)"));
        Assert.Null(ErrorAttributionRule.OurFrame("at Phobos.Logging.PhobosLog.Error(String text)"));
    }

    [Fact]
    public void AMessageOfOursNeedsNoNote()
    {
        Assert.True(ErrorAttributionRule.IsOurOwnMessage("[D'thorga Patches] Could not apply X; that part is switched off."));
        Assert.Equal(AttributionKind.None, ErrorAttributionRule.Find("[D'thorga Framework] something", "at Phobos.Framework.FrameworkMod.Load()").Kind);
    }

    [Fact]
    public void ANamespaceThatOnlyStartsLikeOursIsNotOurs() => Assert.Null(ErrorAttributionRule.OurFrame("at PhobosOther.Thing.Run()"));

    [Fact]
    public void ALambdaFrameIsTidied() => Assert.Equal("Phobos.VehicleSystems.FlightJourneys.Launch",
        ErrorAttributionRule.OurFrame("at Phobos.VehicleSystems.FlightJourneys.<>c.<Launch>b__4_0(Thing t) in x.dll"));

    [Fact]
    public void ADisplayClassFrameIsTidied() => Assert.Equal("Phobos.VehicleSystems.FlightJourneys.Launch",
        ErrorAttributionRule.OurFrame("at Phobos.VehicleSystems.FlightJourneys+<>c__DisplayClass3_0.<Launch>b__0()"));

    [Fact]
    public void TheTypeOfAMemberIsEverythingBeforeTheLastDot() => Assert.Equal("Phobos.A.B", ErrorAttributionRule.TypeOf("Phobos.A.B.Member"));

    [Fact]
    public void AQuoteIsTheFirstLineCut()
    {
        Assert.Equal("Dropped Banuk in a null map.", ErrorAttributionRule.Quote("Dropped Banuk in a null map.\n   at X"));
        string longLine = new string('x', 300);
        Assert.Equal(ErrorAttributionRule.QuoteLength + 1, ErrorAttributionRule.Quote(longLine).Length);
    }
}
