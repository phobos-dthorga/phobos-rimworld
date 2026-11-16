using System.Collections.Generic;
using System.Linq;
using Phobos.Framework.Defs;
using Phobos.Framework.Vehicles.Rules;
using RimWorld;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// The player's vehicles whose crew is in danger: burning, or a dead hull that can neither move nor fire (Framework 0.115.0, C2; owner,
/// 16 Nov 2026: the player's crews are never ordered out, the player is told). Rebuilt at most every few seconds; presentation only.
/// </summary>
public class Alert_PhobosCrewInDanger : Alert
{
    const int RecheckTicks = 250;

    readonly List<VehiclePawn> inDanger = new();
    readonly Dictionary<VehiclePawn, CrewDanger> why = new();
    int checkedTick = -RecheckTicks;

    public Alert_PhobosCrewInDanger()
    {
        defaultLabel = "PhobosVehicleSystems_AlertCrewInDanger".Translate();
        defaultPriority = AlertPriority.High;
    }

    public override AlertReport GetReport()
    {
        Recheck();
        return inDanger.Count == 0 ? AlertReport.Inactive : AlertReport.CulpritsAre(inDanger.Cast<Pawn>().ToList());
    }

    public override TaggedString GetExplanation()
    {
        Recheck();
        string lines = string.Join("\n", inDanger.Select(v => "  - " + (why[v] == CrewDanger.Burning
            ? "PhobosVehicleSystems_AlertCrewBurning".Translate(v.LabelShortCap.Named("VEHICLE"))
            : "PhobosVehicleSystems_AlertCrewKnockedOut".Translate(v.LabelShortCap.Named("VEHICLE"))).Resolve()));
        return "PhobosVehicleSystems_AlertCrewInDangerDesc".Translate(lines.Named("VEHICLES"));
    }

    void Recheck()
    {
        int now = Find.TickManager.TicksGame;
        if (now - checkedTick < RecheckTicks) return;
        checkedTick = now;
        inDanger.Clear();
        why.Clear();
        if (!PhobosRulesDef.Current<PhobosKnockOutRulesDef>().crewsBailOut) return;
        foreach (Map map in Find.Maps)
        {
            foreach (Pawn pawn in map.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer))
            {
                if (pawn is not VehiclePawn vehicle) continue;
                CompPhobosVehicleSystems? comp = vehicle.GetComp<CompPhobosVehicleSystems>();
                if (comp == null || !comp.Usable) continue;
                CrewDanger danger = CrewBailOut.Danger(comp);
                if (danger == CrewDanger.None) continue;
                inDanger.Add(vehicle);
                why[vehicle] = danger;
                Seen.Note("vehicles.crew-in-danger-alert", vehicle, danger.ToString());
            }
        }
    }
}
