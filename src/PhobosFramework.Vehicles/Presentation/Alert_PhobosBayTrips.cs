using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// Names the player's vehicles that should go to a dyno bay and cannot, each with its reason (Framework 0.201.0;
/// docs/development/vehicle-bay-trips.md): no bay, a bay without power or room, nobody free to drive, a part nobody has made, a vehicle
/// that will not drive. Presentation only: it reads each vehicle's last answer (<see cref="CompPhobosBayTrip.Blocked"/>) and works
/// nothing out.
/// </summary>
public class Alert_PhobosBayTrips : Alert
{
    /// <summary>How many vehicles the alert names before it says how many more there are (authored).</summary>
    const int MostLines = 12;

    readonly List<Thing> culprits = new();
    readonly List<string> lines = new();
    int more;

    public Alert_PhobosBayTrips()
    {
        defaultLabel = "PhobosBayTrip_Alert".Translate();
        defaultPriority = AlertPriority.Medium;
    }

    void Refresh()
    {
        culprits.Clear();
        lines.Clear();
        more = 0;
        if (!BayTrips.On) return;
        foreach (Map map in Find.Maps)
        {
            if (!map.IsPlayerHome) continue;
            IReadOnlyList<CompPhobosBayTrip> trips = BayTrips.Of(map);
            for (int i = 0; i < trips.Count; i++)
            {
                if (trips[i].Blocked is not { } why || trips[i].Vehicle.Faction != Faction.OfPlayer) continue;
                culprits.Add(trips[i].Vehicle);
                if (lines.Count < MostLines) lines.Add("PhobosBayTrip_AlertLine".Translate(trips[i].Vehicle.LabelShort.Named("VEHICLE"), why.Named("REASON")).Resolve());
                else more++;
            }
        }
        if (culprits.Count > 0) Seen.Note("vehicles.bay-trip.alert", culprits[0]);
    }

    public override AlertReport GetReport()
    {
        Refresh();
        return culprits.Count == 0 ? AlertReport.Inactive : AlertReport.CulpritsAre(culprits);
    }

    public override TaggedString GetExplanation() =>
        "PhobosBayTrip_AlertIntro".Translate() + "\n\n" + string.Join("\n", lines)
        + (more > 0 ? "\n" + "PhobosBayTrip_AlertMore".Translate(more.Named("COUNT")).Resolve() : "");
}
