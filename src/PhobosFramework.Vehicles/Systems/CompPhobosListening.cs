using System.Collections.Generic;
using Phobos.Framework.Vehicles.Rules;
using RimWorld;
using UnityEngine;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

[DefOf]
public static class ListeningDefOf
{
    public static VehicleStatDef Phobos_ListeningRange = null!;

    static ListeningDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(ListeningDefOf));
}

/// <summary>A listening array a vehicle carries in the water. Every figure is authored for play (docs/development/vehicle-seabed-crawler.md, "The listening array as built").</summary>
public class CompProperties_PhobosListening : VehicleCompProperties
{
    /// <summary>The share of its range the array keeps while the vehicle moves: its own machinery and the water rushing past drown out the rest.</summary>
    public float movingFactor = 0.5f;
    /// <summary>The share of its range at which it hears things moving on land, whose sound carries poorly into the water.</summary>
    public float landShare = 0.5f;
    /// <summary>The share of its range at which it hears a boat that is under and keeping still, compared with one on the move.</summary>
    public float stillBoatShare = 0.5f;
    /// <summary>How many quiet looks in a row, about four seconds each, before new movement is reported again. Keeps one contact stopping and starting from repeating the news.</summary>
    public int quietLooksToReport = 3;

    public CompProperties_PhobosListening() => compClass = typeof(CompPhobosListening);
}

/// <summary>
/// A passive listening array: it hears hostile movement around a vehicle in the water and makes no sound itself. A boat that is under and heard is spotted, which is how one boat finds
/// another. Nothing is saved: what was heard is rebuilt at every look, and a load only means the news may be given once more.
/// </summary>
public class CompPhobosListening : VehicleComp
{
    readonly List<IntVec3> heardAt = new();
    float range;
    string? summary;
    bool reported;
    int quietLooks;

    public CompProperties_PhobosListening Props => (CompProperties_PhobosListening)props;

    float FittedRange => Vehicle.GetStatValue(ListeningDefOf.Phobos_ListeningRange);

    /// <summary>The best hearing of anyone aboard who is awake and up: the game's own hearing capacity, so deafness, ear injuries and bionic ears all count.</summary>
    float BestHearing()
    {
        float best = 0f;
        foreach (Pawn pawn in Vehicle.AllPawnsAboard)
        {
            if (pawn.Dead || pawn.Downed || !pawn.Awake() || pawn.health?.capacities == null) continue;
            best = Mathf.Max(best, pawn.health.capacities.GetLevel(PawnCapacityDefOf.Hearing));
        }
        return best;
    }

    bool InWater => Vehicle.Position.GetTerrain(Vehicle.Map)?.IsWater ?? false;

    /// <summary>One look: who is moving within hearing, and the line that says so. The one place what the array hears is decided.</summary>
    public void Listen()
    {
        heardAt.Clear();
        range = 0f;
        if (!Vehicle.Spawned || FittedRange <= 0f)
        {
            summary = null;
            return;
        }
        if (!InWater)
        {
            summary = "PhobosListening_OutOfWater".Translate().Resolve();
            return;
        }
        float hearing = BestHearing();
        if (hearing <= 0f)
        {
            summary = "PhobosListening_NobodyListening".Translate().Resolve();
            return;
        }
        range = ListeningRule.Range(FittedRange, hearing, VehicleMotion.Moving(Vehicle), Props.movingFactor);

        Map map = Vehicle.Map;
        IntVec3 at = Vehicle.Position;
        float nearest = -1f;
        IntVec3 nearestCell = at;
        int boats = 0;
        foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
        {
            if (pawn == Vehicle || pawn.Dead || !pawn.HostileTo(Vehicle)) continue;
            float distance = pawn.Position.DistanceTo(at);
            if (distance >= range) continue;
            CompPhobosSubmersible? boat = pawn is VehiclePawn vehicle && Submersibles.IsHidden(vehicle) ? Submersibles.CompOf(vehicle) : null;
            bool moving = pawn is VehiclePawn mover ? VehicleMotion.Moving(mover) : pawn.pather?.Moving ?? false;
            bool inWater = pawn.Position.GetTerrain(map)?.IsWater ?? false;
            ContactKind kind = boat != null ? ContactKind.HiddenBoat : inWater ? ContactKind.InWater : ContactKind.OnLand;
            float noise = boat == null ? 1f : DiveRule.Noise(boat.Props.noise, boat.Settled, boat.Props.settledNoiseFactor);
            if (!ListeningRule.Hears(kind, distance, range, moving, Props.landShare, noise, Props.stillBoatShare)) continue;
            heardAt.Add(pawn.Position);
            if (boat != null)
            {
                boats++;
                // Heard is found: the boat's position is known, as when it is seen.
                boat.Spot();
            }
            if (nearest < 0f || distance < nearest)
            {
                nearest = distance;
                nearestCell = pawn.Position;
            }
        }
        summary = Line(nearest, nearestCell, boats);
        Report(nearest, nearestCell);
    }

    string BearingText(IntVec3 cell) => CompassText.From(Vehicle.Position, cell);

    string Line(float nearest, IntVec3 nearestCell, int boats)
    {
        string rangeText = range.ToString("0");
        if (heardAt.Count == 0) return "PhobosListening_Quiet".Translate(rangeText.Named("RANGE")).Resolve();
        string line = "PhobosListening_Heard".Translate(heardAt.Count.Named("COUNT"), rangeText.Named("RANGE"), nearest.ToString("0").Named("DISTANCE"), BearingText(nearestCell).Named("BEARING")).Resolve();
        if (boats > 0) line += "\n" + "PhobosListening_BoatsHeard".Translate(boats.Named("COUNT")).Resolve();
        // A boat that is under learns it is in danger before it is seen: the nearest contact is within the distance a watcher could spot it from.
        CompPhobosSubmersible? own = Submersibles.CompOf(Vehicle);
        if (own != null && own.Hidden && ListeningRule.WithinSpotting(nearest, own.Props.spotRange)) line += "\n" + "PhobosListening_CloseEnoughToSpot".Translate().Resolve();
        return line;
    }

    /// <summary>Tells the player once when movement is first heard, and again only after a quiet spell.</summary>
    void Report(float nearest, IntVec3 nearestCell)
    {
        if (heardAt.Count == 0)
        {
            if (++quietLooks >= Props.quietLooksToReport) reported = false;
            return;
        }
        quietLooks = 0;
        if (reported) return;
        reported = true;
        if (Vehicle.Faction != Faction.OfPlayer) return;
        VehicleWorkSounds.Play(Vehicle, "Contact");
        string text = "PhobosListening_Report".Translate(Vehicle.LabelShort.Named("VEHICLE"), heardAt.Count.Named("COUNT"), nearest.ToString("0").Named("DISTANCE"), BearingText(nearestCell).Named("BEARING")).Resolve();
        Messages.Message(text, Vehicle, MessageTypeDefOf.ThreatSmall, historical: false);
    }

    /// <summary>While selected, the ring the array hears within and a line to each place movement was heard at the last look. A line into the fog gives a bearing and lifts no fog.</summary>
    public override void PostDrawExtraSelectionOverlays()
    {
        base.PostDrawExtraSelectionOverlays();
        if (!Vehicle.Spawned || Vehicle.Faction != Faction.OfPlayer || range <= 0f) return;
        GenDraw.DrawRadiusRing(Vehicle.Position, Mathf.Min(range, GenRadial.MaxRadialPatternRadius));
        // Through Thing: Vehicle Framework's 1.6 build declares its own DrawPos against a Unity module the 1.7 build does not reference.
        Vector3 from = ((Thing)Vehicle).DrawPos;
        foreach (IntVec3 cell in heardAt) GenDraw.DrawLineBetween(from, cell.ToVector3Shifted(), SimpleColor.Red);
    }

    public override void CompTickRare()
    {
        base.CompTickRare();
        Listen();
    }

    public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
    {
        heardAt.Clear();
        range = 0f;
        summary = null;
        base.PostDeSpawn(map, mode);
    }

    public override string? CompInspectStringExtra() => summary;
}
