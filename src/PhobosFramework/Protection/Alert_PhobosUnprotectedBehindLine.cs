using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace Phobos.Framework.Protection;

/// <summary>
/// Someone of the colony's is behind a protective-gear line without enough protection (owner, 10 Nov 2026). Colonists suit up at the
/// line by themselves, but the line stops only free colonists on foot: animals, prisoners, slaves, guests and drafted colonists cross
/// it as they are, and someone can be caught inside when an area is painted round them. This lists them so the player can move them
/// out or equip them. Living flesh only: a machine is not harmed by what the line keeps out. Read at most every few seconds; a map
/// without a line costs a field read. Presentation only: it changes nothing.
/// </summary>
public class Alert_PhobosUnprotectedBehindLine : Alert
{
    /// <summary>How often the list is rebuilt: the game asks an alert far more often than people move.</summary>
    const int RecheckTicks = 250;

    readonly List<Pawn> unprotected = new();
    readonly Dictionary<Pawn, ProtectionZoneDef> zoneOf = new();
    int checkedTick = -RecheckTicks;

    public Alert_PhobosUnprotectedBehindLine()
    {
        defaultLabel = "PhobosProtection_AlertUnprotected".Translate();
        defaultPriority = AlertPriority.High;
    }

    public override AlertReport GetReport()
    {
        Recheck();
        return unprotected.Count == 0 ? AlertReport.Inactive : AlertReport.CulpritsAre(unprotected);
    }

    public override TaggedString GetExplanation()
    {
        Recheck();
        string names = string.Join("\n", unprotected.Select(p => "  - " + p.LabelShortCap + " (" + zoneOf[p].label + ")"));
        return "PhobosProtection_AlertUnprotectedDesc".Translate(names.Named("PAWNS"));
    }

    void Recheck()
    {
        int now = Find.TickManager.TicksGame;
        if (now - checkedTick < RecheckTicks) return;
        checkedTick = now;
        unprotected.Clear();
        zoneOf.Clear();
        foreach (Map map in Find.Maps)
        {
            ProtectionZones? zones = ProtectionZones.For(map);
            if (zones == null || !zones.Active) continue;
            foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
            {
                if (!Ours(pawn)) continue;
                ProtectionZoneDef? zone = zones.ZoneAt(pawn.Position);
                if (zone == null || ProtectionLine.Protected(pawn, zone)) continue;
                unprotected.Add(pawn);
                zoneOf[pawn] = zone;
            }
        }
    }

    /// <summary>The colony's own living flesh: its people and animals, its prisoners and slaves, and its guests.</summary>
    static bool Ours(Pawn pawn) =>
        !pawn.Dead && pawn.RaceProps.IsFlesh
        && (pawn.Faction == Faction.OfPlayer || pawn.IsPrisonerOfColony || pawn.IsSlaveOfColony || pawn.HostFaction == Faction.OfPlayer);
}
