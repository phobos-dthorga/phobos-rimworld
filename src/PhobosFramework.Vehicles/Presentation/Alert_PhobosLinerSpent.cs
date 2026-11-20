using System.Collections.Generic;
using Phobos.Framework.Vehicles.Rules;
using RimWorld;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// The spent-liner alert (Framework 0.209.0; docs/development/vehicle-spall.md, the follow-ups the owner chose on 21 Nov 2026): names the
/// player's vehicles on a map whose fitted spall liner is spent and so catches nothing, so the crew is not sent out behind it unknowing.
/// Presentation only: the liner's state is the spall service's answer, read for the player's spawned vehicles that have a liner.
/// </summary>
public class Alert_PhobosLinerSpent : Alert
{
    readonly List<Thing> culprits = new();
    readonly List<string> lines = new();

    public Alert_PhobosLinerSpent()
    {
        defaultLabel = "PhobosVehicleSystems_LinerSpentAlert".Translate();
        defaultPriority = AlertPriority.Medium;
    }

    void Refresh()
    {
        culprits.Clear();
        lines.Clear();
        foreach (Map map in Find.Maps)
        {
            foreach (Pawn pawn in map.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer))
            {
                if (pawn is not VehiclePawn vehicle || vehicle.statHandler?.GetComponent(Spall.LinerKey) is not { } liner) continue;
                if (Spall.StateOf(vehicle, liner) != LinerState.Spent) continue;
                culprits.Add(vehicle);
                Seen.Note("vehicles.spall-liner-alert", vehicle);
                lines.Add("PhobosVehicleSystems_LinerSpentLine".Translate(vehicle.LabelShort.Named("VEHICLE")).Resolve());
            }
        }
    }

    public override AlertReport GetReport()
    {
        Refresh();
        return culprits.Count == 0 ? AlertReport.Inactive : AlertReport.CulpritsAre(culprits);
    }

    public override TaggedString GetExplanation() =>
        "PhobosVehicleSystems_LinerSpentIntro".Translate() + "\n\n" + string.Join("\n", lines);
}
