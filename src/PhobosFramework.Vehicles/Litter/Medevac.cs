using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using SmashTools.Targeting;
using Vehicles;
using Vehicles.World;
using Verse;

namespace Phobos.VehicleSystems;

// The flying doctor flies: a medevac (Framework 0.345.0; docs/development/radio-friends-school-medevac.md, the owner's card of 27 Nov 2026:
// "A convoy button"). A convoy whose radio reaches home calls an aircraft with stretchers from the colony it reaches: it flies out, touches
// down at the convoy with no map, takes the downed aboard its litters, the worst bleeding first, and flies home to the spot it left, where
// they are set down for the colony's doctors. It reuses the airdrop's way home (ReturnFlight) and the litter's holder (CompPhobosLitter).

/// <summary>
/// The medevac: whether a convoy can call one, which aircraft answers, and the patients just brought home.
/// <para>Cost: nothing each tick. The button's answer is kept a tenth of a game hour for a convoy; a flight costs what any flight costs.</para>
/// </summary>
public static class Medevac
{
    /// <summary>How long a patient set down at home is left alone by "take casualties aboard", in ticks (agent default: an hour).</summary>
    const int LeftAloneTicks = GenDate.TicksPerHour;
    /// <summary>How long the button's answer is kept for a convoy, in ticks.</summary>
    const int KeepTicks = 250;

    static readonly Dictionary<int, int> broughtHome = new();
    static readonly Dictionary<Caravan, (int tick, VehiclePawn? aircraft, string? why)> answers = new();

    /// <summary>A patient set down at home by a medevac: the litters leave them for the beds a while.</summary>
    public static void BroughtHome(Pawn patient) => broughtHome[patient.thingIDNumber] = Find.TickManager.TicksGame + LeftAloneTicks;

    public static bool JustBrought(Pawn patient)
    {
        if (broughtHome.Count == 0 || !broughtHome.TryGetValue(patient.thingIDNumber, out int until)) return false;
        if (Find.TickManager.TicksGame < until) return true;
        broughtHome.Remove(patient.thingIDNumber);
        return false;
    }

    /// <summary>The convoy's downed people and animals of the player's, the worst bleeding first.</summary>
    public static List<Pawn> Downed(Caravan convoy) =>
        convoy.PawnsListForReading.Where(p => p is not VehiclePawn && p.Downed && !p.Dead && (p.Faction == Faction.OfPlayer || p.IsPrisonerOfColony))
            .OrderByDescending(p => p.health.hediffSet.BleedRateTotal).ToList();

    /// <summary>The colony the convoy's radio reaches, or null.</summary>
    static Map? HomeOf(Caravan convoy) =>
        ConvoyRadio.Of(convoy) is { InTouch: true, HomeTile: >= 0 } reach ? Find.Maps.FirstOrDefault(m => m.Tile == reach.HomeTile) : null;

    /// <summary>The aircraft that would answer, or null with the reason (kept a tenth of a game hour).</summary>
    public static VehiclePawn? Answering(Caravan convoy, out string? why)
    {
        int now = Find.TickManager.TicksGame;
        if (answers.TryGetValue(convoy, out var held) && now - held.tick >= 0 && now - held.tick < KeepTicks)
        {
            why = held.why;
            return held.aircraft;
        }
        if (answers.Count > 64) answers.Clear();
        VehiclePawn? aircraft = Search(convoy, out why);
        answers[convoy] = (now, aircraft, why);
        return aircraft;
    }

    static VehiclePawn? Search(Caravan convoy, out string? why)
    {
        why = null;
        if (!ConvoyRadio.Available) return Refuse("PhobosMedevac_NoRadio", convoy, out why);
        if (Downed(convoy).Count == 0) return Refuse("PhobosMedevac_NobodyDowned", convoy, out why);
        if (HomeOf(convoy) is not { } home)
        {
            why = "PhobosMedevac_OutOfTouch".Translate(convoy.LabelCap.Named("CONVOY"), (ConvoyRadio.Of(convoy)?.Line ?? "").Named("WHY")).Resolve();
            return null;
        }
        VehiclePawn? best = null;
        int bestFree = 0;
        string? closest = null;
        foreach (Pawn pawn in home.mapPawns.AllPawnsSpawned)
        {
            if (pawn is not VehiclePawn vehicle || vehicle.Faction != Faction.OfPlayer || vehicle.CompVehicleLauncher is not { } launcher) continue;
            if (vehicle.GetComp<CompPhobosLitter>() is not { KitFitted: true } litter) continue;
            string? refusal = litter.Free <= 0 ? "PhobosMedevac_Full".Translate(vehicle.LabelShort.Named("VEHICLE")).Resolve()
                : !RemoteControl.CrewDrives(vehicle) ? "PhobosMedevac_NoCrew".Translate(vehicle.LabelShort.Named("VEHICLE")).Resolve()
                : !launcher.CanLaunchWithCargoCapacity(out string reason) ? reason
                : !ReturnFlight.EnoughFuel(vehicle, home.Tile, convoy.Tile, legs: 2) ? "PhobosMedevac_NoFuel".Translate(vehicle.LabelShort.Named("VEHICLE")).Resolve()
                : null;
            if (refusal != null)
            {
                closest ??= refusal;
                continue;
            }
            if (litter.Free <= bestFree) continue;
            best = vehicle;
            bestFree = litter.Free;
        }
        if (best == null)
            why = closest ?? "PhobosMedevac_NoAircraft".Translate(home.Parent.LabelCap.Named("HOME")).Resolve();
        return best;
    }

    static VehiclePawn? Refuse(string key, Caravan convoy, out string? why)
    {
        why = key.Translate(convoy.LabelCap.Named("CONVOY")).Resolve();
        return null;
    }

    /// <summary>The player called a medevac: the answering aircraft takes off for the convoy. Says what it did, or why not.</summary>
    public static void Call(Caravan convoy)
    {
        answers.Remove(convoy);
        if (Answering(convoy, out string? why) is not { } aircraft)
        {
            Messages.Message(why ?? "", convoy, MessageTypeDefOf.RejectInput, historical: false);
            return;
        }
        var data = new TargetData<GlobalTargetInfo>();
        data.targets.Add(new GlobalTargetInfo(convoy.Tile));
        aircraft.CompVehicleLauncher.Launch(data, new ArrivalAction_PhobosMedevac(aircraft, convoy));
        answers.Remove(convoy);
        Messages.Message("PhobosMedevac_Called".Translate(aircraft.LabelShort.Named("VEHICLE"), convoy.LabelCap.Named("CONVOY")).Resolve(), convoy,
            MessageTypeDefOf.PositiveEvent, historical: false);
        if (Seen.Wants("vehicles.medevac-called", aircraft)) Seen.Note("vehicles.medevac-called", aircraft, convoy.LabelCap);
    }

    /// <summary>The convoy's button, on its first vehicle. Refused, with the reason, when no aircraft can come.</summary>
    public static IEnumerable<Gizmo> Gizmos(Caravan convoy)
    {
        if (convoy.Faction != Faction.OfPlayer || !ConvoyRadio.Available) yield break;
        var call = new Command_Action
        {
            defaultLabel = "PhobosMedevac_Gizmo".Translate(),
            defaultDesc = "PhobosMedevac_GizmoDesc".Translate(),
            icon = SystemIcons.For("Phobos/VehicleSystems/Litter/Collect"),
            action = () => Call(convoy),
        };
        if (Answering(convoy, out string? why) == null) call.Disable(why);
        yield return call;
    }
}

/// <summary>
/// A medevac aircraft's arrival (Framework 0.345.0): at the convoy's tile it touches down with no map, takes the downed aboard its litters,
/// the worst bleeding first, and flies home to the spot it left (<see cref="ReturnFlight"/>), where they are set down at the first tick. A
/// convoy that has moved on is missed, and the aircraft flies home empty.
/// </summary>
public class ArrivalAction_PhobosMedevac : VehicleArrivalAction
{
    Caravan? convoy;
    MapParent? originParent;
    IntVec3 originCell = IntVec3.Invalid;
    Rot4 originRot = Rot4.North;

    public ArrivalAction_PhobosMedevac() { }

    public ArrivalAction_PhobosMedevac(VehiclePawn vehicle, Caravan convoy) : base(vehicle)
    {
        this.convoy = convoy;
        if (vehicle.Spawned)
        {
            originParent = vehicle.Map.Parent;
            originCell = vehicle.Position;
            originRot = vehicle.Rotation;
        }
    }

    public override bool DestroyOnArrival => false;

    public override void Arrived(GlobalTargetInfo globalTarget)
    {
        AerialVehicleInFlight? flight = AerialVehicle;
        PlanetTile tile = flight?.Tile ?? globalTarget.Tile;
        int taken = 0;
        if (convoy is { Destroyed: false } here && here.Tile == tile && vehicle.GetComp<CompPhobosLitter>() is { } litter)
        {
            foreach (Pawn patient in Medevac.Downed(here))
            {
                if (litter.Free <= 0) break;
                if (litter.LoadFromCaravan(here, patient)) taken++;
            }
            if (taken > 0) litter.SetDownAtHome();
            if (here is VehicleCaravan vehicles) vehicles.RecacheVehicles();
            Messages.Message("PhobosMedevac_Taken".Translate(vehicle.LabelShort.Named("VEHICLE"), taken.Named("COUNT"), here.LabelCap.Named("CONVOY")).Resolve(),
                here, MessageTypeDefOf.PositiveEvent, historical: false);
            if (Seen.Wants("vehicles.medevac-taken", vehicle)) Seen.Note("vehicles.medevac-taken", vehicle, taken + " patients");
        }
        else
        {
            Messages.Message("PhobosMedevac_Missed".Translate(vehicle.LabelShort.Named("VEHICLE")).Resolve(), MessageTypeDefOf.NegativeEvent, historical: false);
            if (Seen.Wants("vehicles.medevac-missed", vehicle)) Seen.Note("vehicles.medevac-missed", vehicle);
        }
        ReturnFlight.FlyHome(vehicle, flight, tile, originParent, originCell, originRot);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref convoy, "PhobosMedevac_Convoy");
        Scribe_References.Look(ref originParent, "PhobosMedevac_OriginParent");
        Scribe_Values.Look(ref originCell, "PhobosMedevac_OriginCell", IntVec3.Invalid);
        Scribe_Values.Look(ref originRot, "PhobosMedevac_OriginRot", Rot4.North);
    }
}
