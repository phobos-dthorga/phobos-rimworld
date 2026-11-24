using System.Collections.Generic;
using System.Linq;
using Phobos.Framework.Rules;
using RimWorld;
using SmashTools;
using UnityEngine;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>A vehicle's hold for other vehicles (Framework 0.288.0): how big, how heavy, how many, and how near its ramp one must be.</summary>
public sealed class CompProperties_PhobosHold : CompProperties
{
    /// <summary>The largest footprint it takes, cells, either way round.</summary>
    public IntVec2 maxSize = new(2, 3);
    /// <summary>The most it carries in all, kilograms.</summary>
    public float maxKg = 2500f;
    /// <summary>How many vehicles at once.</summary>
    public int slots = 1;
    /// <summary>How far behind its ramp a vehicle may be taken aboard from, cells (authored).</summary>
    public float rampReachCells = 6f;

    public CompProperties_PhobosHold() => compClass = typeof(CompPhobosHold);

    public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
    {
        foreach (string error in base.ConfigErrors(parentDef)) yield return error;
        if (maxSize.x < 1 || maxSize.z < 1 || maxKg <= 0f || slots < 1) yield return "CompProperties_PhobosHold: maxSize, maxKg and slots must be positive";
    }
}

/// <summary>
/// A hold for vehicles (D'thorga Framework 0.288.0; docs/development/airdrop.md, "The hold"; MV5, the read of 18 Nov 2026): vehicles are
/// driven aboard over the ramp and held off the map in a holder of its own, saved, found by the game through our holders postfix, and weighed
/// as carried load. Their crew rides in the carrier's seats. Only the player's vehicles, nothing nested, no aircraft, within its size and
/// weight. They leave by the ramp, or by parachute (Airdrop). Destroyed, it puts them out beside it.
/// </summary>
public sealed class CompPhobosHold : VehicleComp, IThingHolder, IVehicleCarriedLoad, IPhobosVehicleHolder
{
    ThingOwner<VehiclePawn> held;

    public CompPhobosHold() => held = new ThingOwner<VehiclePawn>(this);

    public CompProperties_PhobosHold Props => (CompProperties_PhobosHold)props;

    /// <summary>The vehicles in the hold now.</summary>
    public IReadOnlyList<VehiclePawn> Held => held.InnerListForReading;

    public IEnumerable<VehiclePawn> HeldVehicles => held.InnerListForReading;

    public ThingOwner GetDirectlyHeldThings() => held;

    public void GetChildHolders(List<IThingHolder> outChildren) => ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, held);

    public float CarriedKilograms
    {
        get
        {
            float kg = 0f;
            foreach (VehiclePawn vehicle in held.InnerListForReading) kg += Airdrop.VehicleKg(vehicle);
            return kg;
        }
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Deep.Look(ref held, "PhobosHold_Held", this);
        if (Scribe.mode == LoadSaveMode.PostLoadInit) held ??= new ThingOwner<VehiclePawn>(this);
    }

    /// <summary>The ground behind its ramp: the cell past its back along its facing.</summary>
    IntVec3 RampCell()
    {
        VehiclePawn v = Vehicle;
        IntVec3 back = v.Rotation.Opposite.FacingCell;
        int reach = v.VehicleDef.Size.z / 2 + 1;
        return v.Position + new IntVec3(back.x * reach, 0, back.z * reach);
    }

    // ---- Refusals ----

    /// <summary>Why this vehicle may not be taken aboard now, or null.</summary>
    public string? LoadRefusal(VehiclePawn other)
    {
        VehiclePawn v = Vehicle;
        NamedArgument who = v.LabelShort.Named("VEHICLE"), load = other.LabelShort.Named("LOAD");
        if (!v.Spawned || FlightRegistry.InAir(v)) return "PhobosHold_RefuseNotOnGround".Translate(who).Resolve();
        if (other == v || !other.Spawned || other.Map != v.Map || other.Faction != Faction.OfPlayer) return "PhobosHold_RefuseNotOurs".Translate(load).Resolve();
        if (other.CompVehicleLauncher != null) return "PhobosHold_RefuseAircraft".Translate(load).Resolve();
        if (other.GetComp<CompPhobosSling>() is { Carried: not null } || other.GetComp<CompPhobosHold>() is { Held.Count: > 0 }
            || other.GetComp<CompPhobosHitch>() is { HoldsTowed: true })
            return "PhobosHold_RefuseNested".Translate(load).Resolve();
        IntVec2 size = other.VehicleDef.Size;
        if (!HoldRule.Fits(size.x, size.z, Props.maxSize.x, Props.maxSize.z))
            return "PhobosHold_RefuseTooBig".Translate(load, who, Props.maxSize.x.Named("WIDTH"), Props.maxSize.z.Named("LENGTH")).Resolve();
        if (held.Count >= Props.slots) return "PhobosHold_RefuseFull".Translate(who).Resolve();
        float kg = Airdrop.VehicleKg(other);
        if (!HoldRule.Takes(kg, CarriedKilograms, Props.maxKg))
            return "PhobosHold_RefuseTooHeavy".Translate(load, kg.ToString("0").Named("KILOGRAMS"), (Props.maxKg - CarriedKilograms).ToString("0").Named("ROOM")).Resolve();
        if (!other.OccupiedRect().ExpandedBy(Mathf.CeilToInt(Props.rampReachCells)).Contains(RampCell()))
            return "PhobosHold_RefuseNotAtRamp".Translate(load, who).Resolve();
        int crew = other.AllPawnsAboard.Count;
        int seats = v.handlers.Where(h => h.role.HandlingTypes == 0).Sum(h => h.role.Slots - h.thingOwner.Count);
        if (crew > seats) return "PhobosHold_RefuseNoSeats".Translate(load, crew.Named("COUNT"), who).Resolve();
        return null;
    }

    /// <summary>Why the hold cannot be emptied now, or null.</summary>
    public string? UnloadRefusal()
    {
        VehiclePawn v = Vehicle;
        NamedArgument who = v.LabelShort.Named("VEHICLE");
        if (held.Count == 0) return "PhobosHold_RefuseEmpty".Translate(who).Resolve();
        if (!v.Spawned || FlightRegistry.InAir(v)) return "PhobosHold_RefuseNotOnGround".Translate(who).Resolve();
        return null;
    }

    // ---- Orders ----

    /// <summary>Takes a vehicle aboard over the ramp: its crew into the carrier's seats, the vehicle into the hold.</summary>
    public void Load(VehiclePawn other)
    {
        VehiclePawn v = Vehicle;
        if (LoadRefusal(other) is { } refused)
        {
            Messages.Message(refused, v, MessageTypeDefOf.RejectInput, historical: false);
            return;
        }
        foreach (Pawn pawn in other.AllPawnsAboard.ToList())
        {
            other.RemovePawn(pawn);
            VehicleRoleHandler? seat = v.handlers.FirstOrDefault(h => h.role.HandlingTypes == 0 && h.AreSlotsAvailable);
            if (seat == null || !v.TryAddPawn(pawn, seat)) GenPlace.TryPlaceThing(pawn, other.Position, other.Map, ThingPlaceMode.Near);
        }
        Map map = other.Map;
        other.DeSpawn(DestroyMode.Vanish);
        map.GetDetachedMapComponent<VehiclePositionManager>()?.ReleaseClaimed(other);
        held.TryAdd(other, canMergeWithExistingStacks: false);
        CarriedLoads.Changed(v);
        Seen.Note("vehicle.hold", v, variant: "loaded");
        Messages.Message("PhobosHold_MsgLoaded".Translate(other.LabelShort.Named("LOAD"), v.LabelShort.Named("VEHICLE")).Resolve(), v,
            MessageTypeDefOf.SilentInput, historical: false);
    }

    /// <summary>Puts every vehicle out behind the ramp, facing away; any that finds no room stays aboard, and it says so.</summary>
    public void Unload()
    {
        VehiclePawn v = Vehicle;
        if (UnloadRefusal() is { } refused)
        {
            Messages.Message(refused, v, MessageTypeDefOf.RejectInput, historical: false);
            return;
        }
        int stayed = 0;
        foreach (VehiclePawn vehicle in held.InnerListForReading.ToList())
        {
            if (!PutOut(vehicle, v.Map, RampCell(), v.Rotation.Opposite, v.VehicleRect())) stayed++;
        }
        CarriedLoads.Changed(v);
        if (stayed > 0) Messages.Message("PhobosHold_MsgNoRoom".Translate(stayed.Named("COUNT"), v.LabelShort.Named("VEHICLE")).Resolve(), v, MessageTypeDefOf.RejectInput, historical: false);
    }

    bool PutOut(VehiclePawn vehicle, Map map, IntVec3 near, Rot4 rot, CellRect avoid)
    {
        if (!SlingPlacement.TryFindClear(vehicle.VehicleDef, rot, map, near, 20f, avoid, out IntVec3 spot)) return false;
        held.Remove(vehicle);
        GenSpawn.Spawn(vehicle, spot, map, rot, WipeMode.Vanish);
        Seen.Note("vehicle.hold", Vehicle, variant: "unloaded");
        return true;
    }

    public override void PostDestroy(DestroyMode mode, Map? previousMap)
    {
        base.PostDestroy(mode, previousMap);
        if (held.Count == 0 || previousMap == null) return;
        // Destroyed with vehicles aboard: they are put out beside it, wherever there is room; one with no room anywhere is lost with it.
        foreach (VehiclePawn vehicle in held.InnerListForReading.ToList())
        {
            if (!SlingPlacement.TryFindClear(vehicle.VehicleDef, Rot4.North, previousMap, parent.Position, GenRadial.MaxRadialPatternRadius - 1f, CellRect.Empty, out IntVec3 spot))
                continue;
            held.Remove(vehicle);
            GenSpawn.Spawn(vehicle, spot, previousMap, Rot4.North, WipeMode.Vanish);
        }
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        foreach (Gizmo gizmo in base.CompGetGizmosExtra()) yield return gizmo;
        VehiclePawn v = Vehicle;
        if (v.Faction != Faction.OfPlayer || !v.Spawned) yield break;
        NamedArgument who = v.LabelShort.Named("VEHICLE");
        var load = new Command_Action
        {
            defaultLabel = "PhobosHold_Load".Translate(),
            defaultDesc = "PhobosHold_LoadDesc".Translate(who, Props.maxSize.x.Named("WIDTH"), Props.maxSize.z.Named("LENGTH"), Props.maxKg.ToString("0").Named("KILOGRAMS")),
            icon = SystemIcons.For("Phobos/VehicleSystems/Airdrop/LoadHold"),
            action = () => Find.Targeter.BeginTargeting(new TargetingParameters
                {
                    canTargetPawns = true, canTargetBuildings = false, canTargetLocations = false,
                    validator = t => t.Thing is VehiclePawn other && LoadRefusal(other) == null,
                }, target => { if (target.Thing is VehiclePawn other) Load(other); }),
        };
        if (held.Count >= Props.slots) load.Disable("PhobosHold_RefuseFull".Translate(who));
        else if (!v.Spawned || FlightRegistry.InAir(v)) load.Disable("PhobosHold_RefuseNotOnGround".Translate(who));
        yield return load;
        var unload = new Command_Action
        {
            defaultLabel = "PhobosHold_Unload".Translate(),
            defaultDesc = "PhobosHold_UnloadDesc".Translate(who),
            icon = SystemIcons.For("Phobos/VehicleSystems/Airdrop/UnloadHold"),
            action = Unload,
        };
        if (UnloadRefusal() is { } refused) unload.Disable(refused);
        yield return unload;
    }

    public override string CompInspectStringExtra()
    {
        if (held.Count == 0) return "PhobosHold_InspectEmpty".Translate(Props.maxKg.ToString("0").Named("KILOGRAMS")).Resolve();
        return "PhobosHold_Inspect".Translate(held.InnerListForReading.Select(h => h.LabelShort).ToCommaList().Named("VEHICLES"),
            CarriedKilograms.ToString("0").Named("KILOGRAMS"), Props.maxKg.ToString("0").Named("LIMIT")).Resolve();
    }
}
