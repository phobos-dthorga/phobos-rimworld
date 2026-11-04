using System.Collections.Generic;
using DubsBadHygiene;
using Phobos.WaterTap.Rules;
using RimWorld;
using UnityEngine;
using Verse;

namespace Phobos.WaterTap;

/// <summary>Sets up a bottle tap: what it fills and how fast. The values are in the tap's Def.</summary>
public class CompProperties_BottleTap : CompProperties
{
    /// <summary>The item the tap fills.</summary>
    public ThingDef? bottle;

    /// <summary>Water drawn per bottle, in Dubs Bad Hygiene's water units.</summary>
    public float waterPerBottle = 0.5f;

    /// <summary>Bottles made at each rare tick (250 game ticks) while the tap has room.</summary>
    public int bottlesPerFill = 1;

    /// <summary>How many bottles a newly built tap keeps ready.</summary>
    public int startingTarget = 10;

    public CompProperties_BottleTap()
    {
        compClass = typeof(CompBottleTap);
    }

    public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
    {
        foreach (string error in base.ConfigErrors(parentDef)) yield return error;
        if (bottle == null) yield return "needs a bottle";
        if (waterPerBottle <= 0f) yield return "waterPerBottle must be above 0";
        if (bottlesPerFill < 1) yield return "bottlesPerFill must be at least 1";
        if (parentDef.tickerType != TickerType.Rare) yield return "the building's tickerType must be Rare";
        if (!parentDef.HasComp(typeof(CompPipe))) yield return "the building needs a CompPipe";
    }
}

/// <summary>
/// A tap on the plumbing that keeps a stack of water bottles ready on its cell. The target is the only saved state;
/// <see cref="TapService"/> does the filling and <see cref="TapRule"/> decides.
/// </summary>
[StaticConstructorOnStartup]
public class CompBottleTap : ThingComp
{
    /// <summary>Saved key; never rename once saves can hold it.</summary>
    private const string TargetSaveKey = "PhobosWaterTap_Target";

    private static readonly Texture2D LessIcon = ContentFinder<Texture2D>.Get("UI/Buttons/Minus");
    private static readonly Texture2D MoreIcon = ContentFinder<Texture2D>.Get("UI/Buttons/Plus");

    private int target;

    public CompProperties_BottleTap Props => (CompProperties_BottleTap)props;

    public CompPipe? Pipe { get; private set; }

    /// <summary>The colony limit of the map this tap stands on.</summary>
    public MapComponent_WaterTapStock? Stock { get; private set; }

    public int StackLimit => Props.bottle!.stackLimit;

    public int Target
    {
        get => target;
        internal set => target = value;
    }

    public override void Initialize(CompProperties props)
    {
        base.Initialize(props);
        target = TapRule.ClampTarget(Props.startingTarget, StackLimit);
    }

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        Pipe = parent.GetComp<CompPipe>();
        Stock = parent.Map.GetComponent<MapComponent_WaterTapStock>();
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref target, TargetSaveKey, TapRule.ClampTarget(Props.startingTarget, StackLimit));
    }

    public override void CompTickRare()
    {
        base.CompTickRare();
        TapService.Fill(this);
    }

    public override string CompInspectStringExtra()
    {
        string keeps = "PhobosWaterTap_Keeps".Translate(target.Named("COUNT"), Props.waterPerBottle.Named("WATER"));
        TapReading reading = TapService.Read(this);
        string stock = "PhobosWaterTap_MapStock".Translate(reading.MapStock.Named("COUNT"), reading.StockLimit.Named("LIMIT"));
        string state = ("PhobosWaterTap_State" + TapRule.Decide(reading)).Translate();
        return keeps + "\n" + stock + "\n" + state;
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        foreach (Gizmo gizmo in base.CompGetGizmosExtra()) yield return gizmo;
        if (parent.Faction != Faction.OfPlayer) yield break;

        var less = new Command_Action
        {
            defaultLabel = "PhobosWaterTap_Less".Translate(),
            defaultDesc = "PhobosWaterTap_LessDesc".Translate(),
            icon = LessIcon,
            action = () => TapService.ChangeTarget(this, -1),
        };
        if (target <= 0) less.Disable("PhobosWaterTap_AlreadyOff".Translate());
        yield return less;

        var more = new Command_Action
        {
            defaultLabel = "PhobosWaterTap_More".Translate(),
            defaultDesc = "PhobosWaterTap_MoreDesc".Translate(StackLimit.Named("LIMIT")),
            icon = MoreIcon,
            action = () => TapService.ChangeTarget(this, 1),
        };
        if (target >= StackLimit) more.Disable("PhobosWaterTap_AlreadyFull".Translate(StackLimit.Named("LIMIT")));
        yield return more;

        if (Stock == null) yield break;
        int limit = Stock.StockLimit;

        // The limit is one number for the map, so with several taps selected a click must count once, not once per tap.
        var lowerLimit = new Command_Action
        {
            defaultLabel = "PhobosWaterTap_LowerLimit".Translate(TapRule.StockLimitStep.Named("STEP")),
            defaultDesc = "PhobosWaterTap_LimitDesc".Translate(limit.Named("LIMIT")),
            icon = LessIcon,
            alsoClickIfOtherInGroupClicked = false,
            action = () => TapService.ChangeStockLimit(this, -TapRule.StockLimitStep),
        };
        if (limit <= 0) lowerLimit.Disable("PhobosWaterTap_LimitAtLowest".Translate());
        yield return lowerLimit;

        var raiseLimit = new Command_Action
        {
            defaultLabel = "PhobosWaterTap_RaiseLimit".Translate(TapRule.StockLimitStep.Named("STEP")),
            defaultDesc = "PhobosWaterTap_LimitDesc".Translate(limit.Named("LIMIT")),
            icon = MoreIcon,
            alsoClickIfOtherInGroupClicked = false,
            action = () => TapService.ChangeStockLimit(this, TapRule.StockLimitStep),
        };
        if (limit >= TapRule.MaxStockLimit) raiseLimit.Disable("PhobosWaterTap_LimitAtHighest".Translate(TapRule.MaxStockLimit.Named("LIMIT")));
        yield return raiseLimit;
    }
}
