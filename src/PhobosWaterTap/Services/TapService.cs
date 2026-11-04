using DubsBadHygiene;
using Phobos.WaterTap.Rules;
using RimWorld;
using Verse;

namespace Phobos.WaterTap;

/// <summary>
/// Fills a tap's bottles from its pipe network. Water is drawn the way Dubs Bad Hygiene's own fixtures draw it
/// (PlumbingNet.PullWater, from the water towers on the net), and each bottle carries the quality of that water
/// (CompContamination, which Dubs Bad Hygiene reads when someone drinks).
/// </summary>
public static class TapService
{
    /// <summary>What the tap can see now. Reads only, so the inspect pane can call it.</summary>
    public static TapReading Read(CompBottleTap tap)
    {
        Thing? held = HeldBottles(tap, out bool blocked);
        PlumbingNet? net = tap.Pipe?.pipeNet;
        bool hasTower = net != null && net.WaterTowers.Count > 0;
        return new TapReading(
            Held: held?.stackCount ?? 0,
            Target: tap.Target,
            Blocked: blocked,
            HeldIsDifferentWater: held != null && hasTower && WaterQualityOf(held) != WorstWater(net!),
            HasWaterTower: hasTower,
            Contaminated: net != null && net.IsNetContaminated(),
            WaterStored: net?.WaterStorage ?? 0f,
            WaterPerBottle: tap.Props.waterPerBottle);
    }

    /// <summary>One fill: draws water and adds bottles if the rules say the tap should.</summary>
    public static void Fill(CompBottleTap tap)
    {
        int bottles = TapRule.BottlesToMake(Read(tap), tap.Props.bottlesPerFill);
        if (bottles <= 0) return;
        PlumbingNet net = tap.Pipe!.pipeNet;
        ContaminationLevel water = WorstWater(net);
        if (!net.PullWater(bottles * tap.Props.waterPerBottle, out _)) return;

        Thing fresh = ThingMaker.MakeThing(tap.Props.bottle);
        fresh.stackCount = bottles;
        SetWaterQuality(fresh, water);
        Thing? held = HeldBottles(tap, out _);
        if (held != null) held.TryAbsorbStack(fresh, respectStackLimit: true);
        else GenSpawn.Spawn(fresh, tap.parent.Position, tap.parent.Map);
    }

    /// <summary>Changes how many bottles the tap keeps ready, and says so.</summary>
    public static void ChangeTarget(CompBottleTap tap, int change)
    {
        tap.Target = TapRule.ClampTarget(tap.Target + change, tap.StackLimit);
        Messages.Message("PhobosWaterTap_TargetChanged".Translate(tap.Target.Named("COUNT")), tap.parent,
            MessageTypeDefOf.SilentInput, historical: false);
    }

    public static ContaminationLevel WaterQualityOf(Thing bottle) =>
        bottle.TryGetComp<CompContamination>()?.level ?? ContaminationLevel.Treated;

    public static void SetWaterQuality(Thing bottle, ContaminationLevel level)
    {
        var water = bottle.TryGetComp<CompContamination>();
        if (water != null) water.level = level;
    }

    /// <summary>
    /// The worst water on the net. PullWater reports only the towers it happened to draw from, in a random order and
    /// stopping once it has enough, so its answer changes from bottle to bottle on a net with mixed towers. Water in
    /// connected pipes is one supply, so every bottle gets the worst of it.
    /// </summary>
    private static ContaminationLevel WorstWater(PlumbingNet net)
    {
        var worst = ContaminationLevel.Treated;
        foreach (CompWaterStorage tower in net.WaterTowers)
        {
            if (tower.WaterQuality > worst) worst = tower.WaterQuality;
        }
        return worst;
    }

    /// <summary>The bottles on the tap's cell, if any. Any other item there blocks the tap.</summary>
    private static Thing? HeldBottles(CompBottleTap tap, out bool blocked)
    {
        blocked = false;
        foreach (Thing thing in tap.parent.Position.GetThingList(tap.parent.Map))
        {
            if (thing.def == tap.Props.bottle) return thing;
            if (thing.def.category == ThingCategory.Item) blocked = true;
        }
        return null;
    }
}
