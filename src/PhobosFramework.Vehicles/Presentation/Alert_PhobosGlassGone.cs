using System.Collections.Generic;
using RimWorld;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// The broken-glass alert (Framework 0.238.0; docs/development/vehicle-glass.md, VG11): names the player's vehicles on a map whose glass is
/// gone, so their cabs are open to the weather, before anyone drives them through cold or bad air. Presentation only: the glass's state is
/// the glass service's answer.
/// </summary>
public class Alert_PhobosGlassGone : Alert
{
    readonly List<Thing> culprits = new();
    readonly List<string> lines = new();

    public Alert_PhobosGlassGone()
    {
        defaultLabel = "PhobosVehicleSystems_GlassGoneAlert".Translate();
        defaultPriority = AlertPriority.Medium;
    }

    void Refresh()
    {
        culprits.Clear();
        lines.Clear();
        if (!VehicleSystemsMod.Settings.vehicleGlass) return;
        foreach (Map map in Find.Maps)
        {
            foreach (Pawn pawn in map.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer))
            {
                if (pawn is not VehiclePawn vehicle || !Glass.Gone(vehicle)) continue;
                culprits.Add(vehicle);
                Seen.Note("vehicles.glass-alert", vehicle);
                lines.Add("PhobosVehicleSystems_GlassGoneLine".Translate(vehicle.LabelShort.Named("VEHICLE")).Resolve());
            }
        }
    }

    public override AlertReport GetReport()
    {
        Refresh();
        return culprits.Count == 0 ? AlertReport.Inactive : AlertReport.CulpritsAre(culprits);
    }

    public override TaggedString GetExplanation() =>
        "PhobosVehicleSystems_GlassGoneIntro".Translate() + "\n\n" + string.Join("\n", lines);
}
