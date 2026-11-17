using System.Linq;
using DubsBadHygiene;
using Phobos.Framework.Hygiene;
using Phobos.Framework.Interface;
using Phobos.Framework.Persistence;
using RimWorld;
using UnityEngine;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems.Hygiene;

/// <summary>What the shower trailer's plumbing is (Framework 0.121.0; docs/development/vehicle-trailers.md, Q7). Cells in the trailer's own frame, facing north.</summary>
public class CompProperties_PhobosTrailerPlumbing : VehicleCompProperties
{
    /// <summary>The hidden stand-in water tower laid beneath the trailer, and the hidden stand-in septic tank that is its waste tank.</summary>
    public ThingDef? towerDef;
    public ThingDef? septicDef;
    public PhobosTankContentsDef? water;
    /// <summary>What the stand-in tower is kept topped up to from the trailer's tank, in litres (authored): a working amount, never the whole tank.</summary>
    public float workingLitres = 150f;
    public IntVec2 towerCell = new(-1, -3);
    /// <summary>
    /// Own water (Framework 0.122.0, Billy): the trailer has no tank comp of its own, and its water lives in the stand-in tower while it is set up and
    /// in the trailer while it is packed. Bowsers and plumbing fill it through the tower, as they fill any tower. The tower is the bigger one while
    /// <see cref="towerNode"/> is fitted.
    /// </summary>
    public bool ownWater;
    public ThingDef? towerDefWithNode;
    public string? towerNode;
    public IntVec2 septicCell = new(1, -3);

    public CompProperties_PhobosTrailerPlumbing() => compClass = typeof(CompPhobosTrailerPlumbing);
}

/// <summary>
/// The shower trailer's side of Dubs Bad Hygiene (Framework 0.121.0; docs/development/vehicle-accessories.md, "How Dubs Bad Hygiene sees water").
/// Dubs' fixtures draw only from towers on their pipe net, so while the trailer is set up a hidden stand-in tower lies beneath it, topped up from
/// the trailer's own tank on the rare tick: every litre a shower or flush uses comes off the tank, and none is made. Its toilets drain into a hidden
/// stand-in septic tank beneath it, which is the trailer's waste tank: Dubs' own colonists empty it into sludge drums when it is half full, and our
/// tanker pumps it out from beside the trailer's rear. Packing up gives the tower's water back to the tank and keeps the waste in the trailer, and
/// hidden plumbing is laid under everything the trailer sets out, so its showers, toilets and water heater join its pipes. No Harmony.
/// </summary>
public sealed class CompPhobosTrailerPlumbing : VehicleComp, ISetUpPart
{
    Thing? tower;
    Thing? septic;
    float waste;
    float ownWater;
    float ownWaterAtLay;
    PipeUnderlay underlay = new();

    public CompProperties_PhobosTrailerPlumbing Props => (CompProperties_PhobosTrailerPlumbing)props;

    CompPhobosTank? Tank => Vehicle.GetComp<CompPhobosTank>();

    CompWaterStorage? Tower => tower?.TryGetComp<CompWaterStorage>();

    CompSepticTank? Septic => septic?.TryGetComp<CompSepticTank>();

    /// <summary>The waste held now, in Dubs' sewage units, whether in the stand-in or kept while packed.</summary>
    public float Waste => Septic?.sewageBuffer ?? waste;

    /// <summary>What the waste tank holds when full: the stand-in septic's Def.</summary>
    public float WasteCapacity => Props.septicDef?.GetCompProperties<CompProperties_SepticTank>()?.capacity ?? 0f;

    /// <summary>Dubs' own "unlimited pumping": every tower is kept full by the player's choice, so the stand-in must never give water back.</summary>
    static bool UnlimitedPumping => ModOption.WaterPumpCapacity.Val == 0f;

    ThingDef? TowerDef => Props.towerNode is { Length: > 0 } node && Props.towerDefWithNode != null && VehicleUpgrades.Unlocked(Vehicle, node) ? Props.towerDefWithNode : Props.towerDef;

    /// <summary>What the trailer's own water holds when full, in litres: its stand-in tower's size.</summary>
    public float OwnWaterCapacity => TowerDef?.GetCompProperties<CompProperties_WaterStorage>()?.WaterStorageCap ?? 0f;

    /// <summary>The trailer's own water now, in litres, in the tower while set up or kept while packed.</summary>
    public float OwnWater => Tower is { } water && tower is { Spawned: true } ? water.WaterStorage : ownWater;

    IntVec3 Cell(IntVec2 local) => Vehicle.Position + new IntVec3(local.x, 0, local.z).RotatedBy(Vehicle.Rotation);

    public string? SetUpPartRefusal() => null;

    public string? PackUpPartRefusal() => null;

    public void SetUpChanged(bool on)
    {
        if (PlumbingFeed.LiteMode || !Vehicle.Spawned) return;
        if (on) Lay();
        else TakeUp();
    }

    void Lay()
    {
        Map map = Vehicle.Map;
        if (tower is not { Spawned: true } && TowerDef != null)
        {
            tower = GenSpawn.Spawn(ThingMaker.MakeThing(TowerDef), Cell(Props.towerCell), map, WipeMode.Vanish);
            tower.SetFaction(Vehicle.Faction);
            if (Props.ownWater && Tower is { } own)
            {
                // The trailer's own water goes into its tower; what a smaller tower cannot hold, after the bigger tank was taken off, stays aboard.
                own.WaterStorage = UnityEngine.Mathf.Min(ownWater, own.Props.WaterStorageCap);
                ownWater -= own.WaterStorage;
                ownWaterAtLay = own.WaterStorage;
            }
        }
        if (septic is not { Spawned: true } && Props.septicDef != null)
        {
            septic = GenSpawn.Spawn(ThingMaker.MakeThing(Props.septicDef), Cell(Props.septicCell), map, WipeMode.Vanish);
            septic.SetFaction(Vehicle.Faction);
            if (Septic is { } tank)
            {
                tank.sewageBuffer = waste;
                tank.AllowDrain = true;
                waste = 0f;
            }
        }
        // Hidden plumbing under all the trailer set out and under itself, so the pieces' pipes join the trailer's.
        CellRect area = SetOutArea();
        int before = underlay.Changes;
        underlay.Fill(area, map, cell => DubsPipes.IsPiped(cell, map), DubsPipes.Make);
        if (underlay.Changes != before) DubsPipes.Redraw(map, area);
        TopUp();
        Seen.Note("trailers.shower-plumbing-laid", Vehicle, area.Area + " cells piped");
    }

    CellRect SetOutArea()
    {
        CellRect area = Vehicle.OccupiedRect();
        if (Vehicle.GetComp<CompPhobosSetOut>() is { } setOut)
        {
            foreach (Thing thing in setOut.Placed.Concat(setOut.Structure).Where(t => t.Spawned)) area = area.Encapsulate(thing.OccupiedRect());
        }
        return area;
    }

    void TakeUp()
    {
        Map map = Vehicle.Map;
        if (Tower is { } water && tower is { Spawned: true })
        {
            // The tower's working water goes back into the tank, unless Dubs' unlimited pumping filled it from nothing; own water stays aboard, and
            // under unlimited pumping only what it held when laid.
            if (Props.ownWater) ownWater += UnlimitedPumping ? ownWaterAtLay : water.WaterStorage;
            else if (!UnlimitedPumping && Props.water != null && water.WaterStorage > 0f) Tank?.PutIn(Props.water, water.WaterStorage, (int)water.WaterQuality);
            water.WaterStorage = 0f;
            tower.Destroy(DestroyMode.Vanish);
        }
        tower = null;
        if (Septic is { } tank && septic is { Spawned: true })
        {
            // The waste stays in the trailer; emptied first, so Dubs never spills it across the map as the stand-in goes.
            waste += tank.sewageBuffer;
            tank.sewageBuffer = 0f;
            septic.Destroy(DestroyMode.Vanish);
        }
        septic = null;
        CellRect area = SetOutArea();
        int before = underlay.Changes;
        underlay.Clear();
        if (underlay.Changes != before) DubsPipes.Redraw(map, area.ExpandedBy(8));
    }

    /// <summary>Keeps the stand-in tower at its working amount from the trailer's tank.</summary>
    void TopUp()
    {
        if (Props.ownWater) return;
        if (Tower is not { } water || Props.water == null || Tank is not { } tank) return;
        float wanted = Props.workingLitres - water.WaterStorage;
        if (wanted < 1f || UnlimitedPumping) return;
        float got = tank.TakeOut(Props.water, wanted, out int quality);
        if (got <= 0f) return;
        water.WaterStorage += got;
        if (quality > (int)water.WaterQuality) water.WaterQuality = (ContaminationLevel)quality;
    }

    public override void CompTickRare()
    {
        base.CompTickRare();
        if (tower is { Spawned: true }) TopUp();
    }

    public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
    {
        // Gone while set up: the stand-ins go with it. Destroyed, its waste spills as Dubs spills any broken septic tank; otherwise it is kept.
        if (septic is { Spawned: true } && Septic is { } tank)
        {
            if (mode != DestroyMode.KillFinalize)
            {
                waste += tank.sewageBuffer;
                tank.sewageBuffer = 0f;
            }
            septic.Destroy(mode == DestroyMode.KillFinalize ? DestroyMode.KillFinalize : DestroyMode.Vanish);
        }
        if (tower is { Spawned: true })
        {
            if (Props.ownWater && mode != DestroyMode.KillFinalize && Tower is { } own) ownWater += own.WaterStorage;
            tower.Destroy(DestroyMode.Vanish);
        }
        tower = null;
        septic = null;
        underlay.Clear();
        base.PostDeSpawn(map, mode);
    }

    public override string? CompInspectStringExtra()
    {
        if (UiGuard.Resting(GuardKey)) return UiGuard.FallbackText;
        try
        {
            if (PlumbingFeed.LiteMode) return null;
            if (Props.ownWater) return "PhobosTrailerPlumbing_InspectOwnWater".Translate(OwnWater.ToString("0").Named("LITRES"), OwnWaterCapacity.ToString("0").Named("CAPACITY")).Resolve();
            if (WasteCapacity <= 0f) return null;
            return "PhobosTrailerPlumbing_InspectWaste".Translate(Mathf.Clamp01(Waste / WasteCapacity).ToStringPercent().Named("PERCENT")).Resolve();
        }
        catch (System.Exception e) { UiGuard.Report(GuardKey, e); return null; }
    }

    const string GuardKey = "The shower trailer's plumbing line";

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_References.Look(ref tower, "PhobosTrailerPlumbing_Tower");
        Scribe_References.Look(ref septic, "PhobosTrailerPlumbing_Septic");
        Scribe_Values.Look(ref waste, "PhobosTrailerPlumbing_Waste");
        Scribe_Values.Look(ref ownWater, "PhobosTrailerPlumbing_OwnWater");
        Scribe_Values.Look(ref ownWaterAtLay, "PhobosTrailerPlumbing_OwnWaterAtLay");
        Scribe_Deep.Look(ref underlay, "PhobosTrailerPlumbing_Underlay");
        underlay ??= new PipeUnderlay();
    }
}

/// <summary>
/// The shower trailer's water heater (Framework 0.121.0): Dubs' own boiler and hot-water store on chemfuel, so showers are hot. While the trailer
/// is plugged in, its electric element carries half the load, so it burns half the chemfuel (the owner's "power a bonus", 17 Nov 2026, in the one
/// form Dubs' boiler allows: it cannot heat faster than its store fills). Half of what it burned on the last rare tick is put back.
/// </summary>
public class CompProperties_PhobosHeaterPowerSaving : CompProperties
{
    public float savedShare = 0.5f;
    /// <summary>Its burner, heard while it heats.</summary>
    public SoundDef? workingSound;

    public CompProperties_PhobosHeaterPowerSaving() => compClass = typeof(CompPhobosHeaterPowerSaving);
}

public class CompPhobosHeaterPowerSaving : ThingComp, ISetOutLoad
{
    float lastFuel = -1f;
    readonly Phobos.Framework.Sounds.SustainedSound burner = new();

    public CompProperties_PhobosHeaterPowerSaving Props => (CompProperties_PhobosHeaterPowerSaving)props;

    public float WattsNow => parent.def.GetModExtension<PhobosSetOutPowerExtension>()?.wattsWorking ?? 0f;

    public override void CompTickRare()
    {
        base.CompTickRare();
        // Heard while Dubs' boiler is burning: working, and not idling on its thermostat.
        burner.Set(Props.workingSound, parent.TryGetComp<CompBoiler>() is { } boiler && boiler.Capacity > 0f, parent);
        if (parent.TryGetComp<CompRefuelable>() is not { } fuel) return;
        float now = fuel.Fuel;
        if (lastFuel >= 0f && now < lastFuel && SetOuts.Powered(parent))
        {
            float back = (lastFuel - now) * Props.savedShare;
            fuel.Refuel(back);
            now += back;
            Seen.Note("trailers.heater-power-saving", parent);
        }
        lastFuel = now;
    }

    public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
    {
        burner.End();
        base.PostDeSpawn(map, mode);
    }
}
