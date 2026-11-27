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
    /// <summary>
    /// How fast the waste tank empties by gravity, in Dubs' sewage units an hour (Framework 0.283.0, authored): into the colony's sewers
    /// through its plumbing, or onto the ground through its dump valve. The full 1,000 in a little over three hours.
    /// </summary>
    public float drainPerHour = 300f;
    /// <summary>How far from its waste tank the dump valve's waste may run, in cells (authored).</summary>
    public float dumpRadius = 3.9f;

    public CompProperties_PhobosTrailerPlumbing() => compClass = typeof(CompPhobosTrailerPlumbing);
}

/// <summary>
/// The shower trailer's side of Dubs Bad Hygiene (Framework 0.121.0; docs/development/vehicle-accessories.md, "How Dubs Bad Hygiene sees water").
/// Dubs' fixtures draw only from towers on their pipe net, so while the trailer is set up a hidden stand-in tower lies beneath it, topped up from
/// the trailer's own tank on the rare tick: every litre a shower or flush uses comes off the tank, and none is made. Its toilets drain into a hidden
/// stand-in septic tank beneath it, which is the trailer's waste tank: Dubs' own colonists empty it into sludge drums when it is half full, and our
/// tanker pumps it out from beside the trailer's rear. Packing up gives the tower's water back to the tank and keeps the waste in the trailer, and
/// hidden plumbing is laid under everything the trailer sets out, so its showers, toilets and water heater join its pipes. No Harmony.
/// <para>
/// Framework 0.283.0 (the owner's cards of 25 Nov 2026): the waste tank stays beneath the trailer whenever it is parked, set up or not, so
/// Dubs' colonists and a sludge tanker empty it without setting up; it ranks below every sewage handler of Dubs' (its Def's priority), so the
/// colony's toilets never drain into it, and while its plumbing joins a colony septic tank, treatment plant or outlet it drains into them by
/// gravity (the sewer hookup). The player sets how full it gets before Dubs' colonists empty it, and may open its dump valve onto the ground.
/// </para>
/// </summary>
public sealed class CompPhobosTrailerPlumbing : VehicleComp, ISetUpPart
{
    const float HoursPerRareTick = GenTicks.TickRareInterval / (float)GenDate.TicksPerHour;
    /// <summary>The fullness Dubs' colonists empty it at when the player has not chosen: Dubs' own default.</summary>
    public const float DefaultEmptyAt = 0.5f;

    Thing? tower;
    Thing? septic;
    float waste;
    float ownWater;
    float ownWaterAtLay;
    PipeUnderlay underlay = new();
    /// <summary>How full the waste tank gets before Dubs' colonists empty it; 1 or more, never (they leave it).</summary>
    float emptyAt = DefaultEmptyAt;
    /// <summary>Whether its dump valve is open, letting the waste run onto the ground.</summary>
    bool dumping;
    /// <summary>Why the dump valve last let nothing go, for the inspect line. Derived, never saved.</summary>
    string? dumpStatus;

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

    /// <summary>Whether this is a trailer's own water (a field kitchen's tank), not plumbing fed from a tank of the bowsers' kind.</summary>
    public bool OwnsWater => Props.ownWater;

    /// <summary>
    /// Takes up to this much of the trailer's own water, from the tower while set up or from what it keeps while packed (Framework 0.216.0:
    /// a caravan's kettle and its people on the road). Returns what came.
    /// </summary>
    public float TakeOwnWater(float most)
    {
        if (!Props.ownWater || most <= 0f) return 0f;
        if (Tower is { } water && tower is { Spawned: true })
        {
            float fromTower = Mathf.Min(most, water.WaterStorage);
            water.WaterStorage -= fromTower;
            return fromTower;
        }
        float got = Mathf.Min(most, ownWater);
        ownWater -= got;
        return got;
    }

    IntVec3 Cell(IntVec2 local) => Vehicle.Position + new IntVec3(local.x, 0, local.z).RotatedBy(Vehicle.Rotation);

    /// <summary>
    /// A trailer with its own water keeps its hidden tank beneath it whenever it is parked, set up or not (Framework 0.219.0; the owner,
    /// 22 Nov 2026: the Billy was "exempt from the Dub's Hygiene plumbing placement"), so plumbing or a water bowser beside it fills it while it is
    /// packed. Hitched or gone, the tank's water goes back aboard. Called with each look at its pipes.
    /// </summary>
    public void SettleOwnWater(bool parked)
    {
        if (PlumbingFeed.LiteMode || !Vehicle.Spawned) return;
        bool setOut = Vehicle.GetComp<CompPhobosSetOut>() is { IsSetOut: true };
        if (Props.ownWater)
        {
            if (parked && tower is not { Spawned: true })
            {
                LayTower(Vehicle.Map);
                if (Seen.Wants("trailers.own-water-parked", Vehicle)) Seen.Note("trailers.own-water-parked", Vehicle, OwnWater.ToString("0") + " L");
            }
            else if (!parked && !setOut && tower is { Spawned: true }) TakeUpTower();
        }
        // Framework 0.283.0: the waste tank too, so it is emptied without setting up (the owner's card of 25 Nov 2026).
        if (Props.septicDef != null)
        {
            if (parked && septic is not { Spawned: true }) LaySeptic(Vehicle.Map);
            else if (!parked && !setOut && septic is { Spawned: true }) TakeUpSeptic();
        }
    }

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
        LayTower(map);
        LaySeptic(map);
        // Hidden plumbing under all the trailer set out and under itself, so the pieces' pipes join the trailer's.
        CellRect area = SetOutArea();
        int before = underlay.Changes;
        underlay.Fill(area, map, cell => DubsPipes.IsPiped(cell, map), DubsPipes.Make);
        if (underlay.Changes != before) DubsPipes.Redraw(map, area);
        TopUp();
        if (Seen.Wants("trailers.shower-plumbing-laid", Vehicle)) Seen.Note("trailers.shower-plumbing-laid", Vehicle, area.Area + " cells piped");
    }

    /// <summary>The hidden water tower beneath it, with the water it carries.</summary>
    void LayTower(Map map)
    {
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
    }

    /// <summary>The hidden waste tank beneath it, with the waste it carries, set to empty at the player's level.</summary>
    void LaySeptic(Map map)
    {
        if (septic is { Spawned: true } || Props.septicDef == null) return;
        septic = GenSpawn.Spawn(ThingMaker.MakeThing(Props.septicDef), Cell(Props.septicCell), map, WipeMode.Vanish);
        septic.SetFaction(Vehicle.Faction);
        if (Septic is { } tank)
        {
            tank.sewageBuffer = waste;
            waste = 0f;
            ApplyEmptyAt(tank);
        }
        if (Seen.Wants("trailers.waste-tank-parked", Vehicle)) Seen.Note("trailers.waste-tank-parked", Vehicle, Waste.ToString("0") + " units");
    }

    void ApplyEmptyAt(CompSepticTank tank)
    {
        // Dubs' colonists leave it alone while the valve is open, or when the player chose never.
        tank.SetAllow(emptyAt < 1f && !dumping);
        if (emptyAt < 1f) tank.SetPcntToEmpty(emptyAt);
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
        // A trailer with its own water keeps its tank while it stays parked (Framework 0.219.0): only what was laid under the set-out pieces goes.
        // Framework 0.283.0: its waste tank stays too, so it can still be emptied.
        bool parked = Vehicle.GetComp<CompPhobosTrailer>() is { ParkedForPipes: true };
        if (!(Props.ownWater && parked)) TakeUpTower();
        if (!parked) TakeUpSeptic();
        CellRect area = SetOutArea();
        int before = underlay.Changes;
        underlay.Clear();
        if (underlay.Changes != before) DubsPipes.Redraw(map, area.ExpandedBy(8));
    }

    /// <summary>Takes the hidden tower up, its water back aboard.</summary>
    void TakeUpTower()
    {
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
    }

    /// <summary>Takes the hidden waste tank up, its waste back aboard. The dump valve closes with it: nothing runs out on the road.</summary>
    void TakeUpSeptic()
    {
        dumping = false;
        dumpStatus = null;
        if (Septic is { } tank && septic is { Spawned: true })
        {
            // The waste stays in the trailer; emptied first, so Dubs never spills it across the map as the stand-in goes.
            waste += tank.sewageBuffer;
            tank.sewageBuffer = 0f;
            septic.Destroy(DestroyMode.Vanish);
        }
        septic = null;
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
        if (septic is { Spawned: true } && Septic is { } tank)
        {
            // The dump valve closes when its setting is turned off (fault-read 25 Nov 2026, B5): its button is gone with it.
            if (dumping && !VehicleSystemsMod.Settings.FlagOn("trailerWasteDump")) SetDumping(false);
            if (dumping) Dump(tank);
            else DrainToSewer(tank);
        }
    }

    // ---- Emptying the waste tank (Framework 0.283.0; the owner's cards of 25 Nov 2026) ----

    /// <summary>The plumbing the waste tank stands on, while it is laid.</summary>
    CompPipe? SepticPipe => septic is { Spawned: true } ? septic.TryGetComp<CompPipe>() : null;

    /// <summary>
    /// The sewer hookup: while the trailer's plumbing joins a colony septic tank, treatment plant or outlet that can take more, its waste runs
    /// into them by gravity, at its drain rate. They outrank it (its Def's priority), so Dubs' own plumbing sends this waste to them.
    /// </summary>
    void DrainToSewer(CompSepticTank tank)
    {
        if (tank.sewageBuffer < 1f || SepticPipe?.pipeNet is not { } net) return;
        if (!net.Sewers.Any(sewer => sewer != tank && !sewer.Blocked && sewer.Props.priority > tank.Props.priority)) return;
        float amount = Phobos.Framework.Rules.TankRule.Drained(tank.sewageBuffer, Props.drainPerHour, HoursPerRareTick);
        if (amount <= 0f || !net.PushSewage(amount)) return;
        tank.sewageBuffer -= amount;
        if (Seen.Wants("trailers.waste-to-sewer", Vehicle)) Seen.Note("trailers.waste-to-sewer", Vehicle, amount.ToString("0") + " units");
    }

    /// <summary>
    /// The dump valve: the waste runs onto the ground beside the waste tank as Dubs' own sewage, at its drain rate, filling the nearest cells
    /// that can take more (the limit Dubs' outlets keep to). The valve closes by itself once the tank is empty.
    /// </summary>
    void Dump(CompSepticTank tank)
    {
        dumpStatus = null;
        if (tank.sewageBuffer < 1f)
        {
            SetDumping(false);
            Messages.Message("PhobosTrailerPlumbing_Dumped".Translate(Vehicle.LabelShort.Named("VEHICLE")).Resolve(), Vehicle, MessageTypeDefOf.TaskCompletion, historical: false);
            return;
        }
        if (SepticPipe?.MapComp is not { } hygiene)
        {
            return;
        }
        IntVec3 from = septic!.Position;
        CellRect footprint = Vehicle.OccupiedRect();
        float limit = ModOption.SewageGridCapacity.Val * 3f;
        IntVec3 spot = IntVec3.Invalid;
        foreach (IntVec3 cell in GenRadial.RadialCellsAround(from, Props.dumpRadius, useCenter: false))
        {
            if (!cell.InBounds(Vehicle.Map) || footprint.Contains(cell) || !hygiene.CanHaveSewage(cell)) continue;
            if (hygiene.SewageGrid.ValueAt(cell) > limit) continue;
            spot = cell;
            break;
        }
        if (!spot.IsValid)
        {
            dumpStatus = "PhobosTrailerPlumbing_DumpNowhere".Translate(Vehicle.LabelShort.Named("VEHICLE")).Resolve();
            return;
        }
        float amount = Phobos.Framework.Rules.TankRule.Drained(tank.sewageBuffer, Props.drainPerHour, HoursPerRareTick);
        hygiene.SewageGrid.AddAt(spot, amount);
        tank.sewageBuffer -= amount;
        if (Seen.Wants("trailers.waste-dump", Vehicle)) Seen.Note("trailers.waste-dump", Vehicle, amount.ToString("0") + " units");
    }

    /// <summary>Whether the waste tank stands beneath it now, to be emptied.</summary>
    public bool WasteTankLaid => septic is { Spawned: true };

    /// <summary>How full the waste tank gets before Dubs' colonists empty it; 1 or more means never.</summary>
    public float EmptyAt => emptyAt;

    public bool Dumping => dumping;

    /// <summary>Sets how full the waste tank gets before Dubs' colonists empty it into sludge drums; 1 or more means they leave it.</summary>
    public void SetEmptyAt(float level)
    {
        emptyAt = Mathf.Max(0.1f, level);
        if (Septic is { } tank && septic is { Spawned: true }) ApplyEmptyAt(tank);
        Messages.Message((emptyAt >= 1f
            ? "PhobosTrailerPlumbing_EmptyNever".Translate(Vehicle.LabelShort.Named("VEHICLE"))
            : "PhobosTrailerPlumbing_EmptyAtSet".Translate(Vehicle.LabelShort.Named("VEHICLE"), emptyAt.ToStringPercent().Named("PERCENT"))).Resolve(),
            Vehicle, MessageTypeDefOf.SilentInput, historical: false);
    }

    /// <summary>Why the dump valve cannot be opened now, or null.</summary>
    public string? DumpRefusal()
    {
        if (!WasteTankLaid) return "PhobosTrailerPlumbing_DumpHitched".Translate(Vehicle.LabelShort.Named("VEHICLE")).Resolve();
        if (Waste < 1f) return "PhobosTrailerPlumbing_DumpEmpty".Translate(Vehicle.LabelShort.Named("VEHICLE")).Resolve();
        return null;
    }

    /// <summary>Opens or closes the dump valve. While it is open, Dubs' colonists leave the tank to it.</summary>
    public void SetDumping(bool open)
    {
        if (open && DumpRefusal() is { } refused)
        {
            Messages.Message(refused, Vehicle, MessageTypeDefOf.RejectInput, historical: false);
            return;
        }
        dumping = open;
        dumpStatus = null;
        if (Septic is { } tank && septic is { Spawned: true }) ApplyEmptyAt(tank);
        if (open)
        {
            Messages.Message("PhobosTrailerPlumbing_DumpOpened".Translate(Vehicle.LabelShort.Named("VEHICLE")).Resolve(), Vehicle, MessageTypeDefOf.CautionInput, historical: false);
        }
    }

    public override System.Collections.Generic.IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        foreach (Gizmo gizmo in base.CompGetGizmosExtra()) yield return gizmo;
        if (Vehicle.Faction != Faction.OfPlayer || Props.septicDef == null || PlumbingFeed.LiteMode) yield break;
        foreach (Gizmo gizmo in TrailerWasteGizmos.For(this)) yield return gizmo;
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
            // A tank-fed tower's working water goes back into the tank, as at packing up (fault-read 25 Nov 2026, B4); Dubs' unlimited
            // pumping filled it from nothing, so then it goes.
            else if (!Props.ownWater && mode != DestroyMode.KillFinalize && !UnlimitedPumping && Props.water != null && Tower is { WaterStorage: > 0f } fed)
            {
                Tank?.PutIn(Props.water, fed.WaterStorage, (int)fed.WaterQuality);
            }
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
            string line = "PhobosTrailerPlumbing_InspectWaste".Translate(Mathf.Clamp01(Waste / WasteCapacity).ToStringPercent().Named("PERCENT")).Resolve();
            // Framework 0.283.0: what empties it, and the valve's state.
            if (dumping) return line + "\n" + (dumpStatus ?? "PhobosTrailerPlumbing_InspectDumping".Translate().Resolve());
            return line + "\n" + (emptyAt >= 1f
                ? "PhobosTrailerPlumbing_InspectEmptyNever".Translate().Resolve()
                : "PhobosTrailerPlumbing_InspectEmptyAt".Translate(emptyAt.ToStringPercent().Named("PERCENT")).Resolve());
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
        Scribe_Values.Look(ref emptyAt, "PhobosTrailerPlumbing_EmptyAt", DefaultEmptyAt);
        Scribe_Values.Look(ref dumping, "PhobosTrailerPlumbing_Dumping");
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

    /// <summary>Whether Dubs' boiler is heating now: switched on, fuelled, sound and not idling on its thermostat (its Capacity, which is 0
    /// otherwise).</summary>
    bool Heating => parent.TryGetComp<CompBoiler>() is { } boiler && boiler.Capacity > 0f;

    /// <summary>The element's draw, only while the boiler heats (the owner's card of 28 Nov 2026, as a thermostat heater would: before, it drew
    /// its watts the whole time the trailer was set out, even switched off). Read on the trailer's rare tick.</summary>
    public float WattsNow => Heating ? parent.def.GetModExtension<PhobosSetOutPowerExtension>()?.wattsWorking ?? 0f : 0f;

    public override void CompTickRare()
    {
        base.CompTickRare();
        // Heard while Dubs' boiler is burning: working, and not idling on its thermostat.
        burner.Set(Props.workingSound, Heating, parent);
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
