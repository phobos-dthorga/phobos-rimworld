using System.Collections.Generic;
using RimWorld;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// The cold-start alert (Framework 0.187.0; docs/development/vehicle-emergent-play.md, EM27: the game's cold snap meets the vehicles). While
/// a cold snap holds a map, or it is bitterly cold there, it names the player's parked vehicles that would not start now and why, before
/// anyone needs one. Presentation only: whether a vehicle starts is its systems' own answer (CanDraft), asked only of vehicles not
/// already running, and only on a cold map.
/// </summary>
public class Alert_PhobosColdStart : Alert
{
    /// <summary>Outdoors this cold, the alert looks even without a cold snap (authored: where diesel gels and batteries sag).</summary>
    public const float BitterCelsius = -15f;

    readonly List<Thing> culprits = new();
    readonly List<string> lines = new();

    public Alert_PhobosColdStart()
    {
        defaultLabel = "PhobosVehicleSystems_ColdStartAlert".Translate();
        defaultPriority = AlertPriority.Medium;
    }

    void Refresh()
    {
        culprits.Clear();
        lines.Clear();
        foreach (Map map in Find.Maps)
        {
            if (!Cold(map)) continue;
            foreach (Pawn pawn in map.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer))
            {
                if (pawn is not VehiclePawn vehicle || vehicle.Drafted) continue;
                if (vehicle.GetComp<CompPhobosVehicleSystems>() is not { Usable: true } comp) continue;
                AcceptanceReport starts = comp.CanDraft();
                if (starts.Accepted || starts.Reason.NullOrEmpty()) continue;
                culprits.Add(vehicle);
                Seen.Note("vehicles.cold-start-alert", vehicle);
                lines.Add("PhobosVehicleSystems_ColdStartLine".Translate(vehicle.LabelShort.Named("VEHICLE"), starts.Reason.Named("REASON")).Resolve());
            }
        }
    }

    static bool Cold(Map map) =>
        map.gameConditionManager.ConditionIsActive(GameConditionDefOf.ColdSnap) || map.mapTemperature.OutdoorTemp <= BitterCelsius;

    public override AlertReport GetReport()
    {
        Refresh();
        return culprits.Count == 0 ? AlertReport.Inactive : AlertReport.CulpritsAre(culprits);
    }

    public override TaggedString GetExplanation() =>
        "PhobosVehicleSystems_ColdStartIntro".Translate() + "\n\n" + string.Join("\n", lines);
}
