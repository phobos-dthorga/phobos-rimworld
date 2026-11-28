using System.Collections.Generic;
using System.Linq;
using System.Text;
using Phobos.Radio.Services;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Phobos.Radio.UI;

/// <summary>
/// The countdown while a radar holds a raid back (Radio 0.74.0, row RD8; on the game's own "raids arriving" alert's pattern): "Inbound:
/// unknown contacts, 1 h 20 min", one line a raid, clicking through to its contact on the world map.
/// </summary>
public class Alert_PhobosRadarInbound : Alert_Critical
{
    private readonly List<GlobalTargetInfo> culprits = new();

    private static IEnumerable<PhobosInboundRaid> Due()
    {
        int now = Find.TickManager.TicksGame;
        return GameComponent_PhobosRadarWarnings.Current?.Inbound.Where(r => r.map != null && now < r.fireTick) ?? Enumerable.Empty<PhobosInboundRaid>();
    }

    public override string GetLabel()
    {
        PhobosInboundRaid? first = Due().OrderBy(r => r.fireTick).FirstOrDefault();
        if (first == null) return string.Empty;
        return "PhobosRadar_AlertInbound".Translate((first.fireTick - Find.TickManager.TicksGame).ToStringTicksToPeriod().Named("TIME"));
    }

    public override TaggedString GetExplanation()
    {
        var text = new StringBuilder("PhobosRadar_AlertInboundDesc".Translate());
        foreach (PhobosInboundRaid raid in Due().OrderBy(r => r.fireTick))
        {
            string who = raid.identified && raid.faction != null ? raid.faction.Name : "PhobosRadar_UnknownContacts".Translate().Resolve();
            text.AppendLine().Append("  - ").Append("PhobosRadar_AlertInboundLine".Translate(who.Named("WHO"), raid.CountText.Named("COUNT"),
                raid.map!.Parent.LabelCap.Named("COLONY"), RadarWarning.CompassOf(raid.bearing).Named("BEARING"),
                (raid.fireTick - Find.TickManager.TicksGame).ToStringTicksToPeriod().Named("TIME")).Resolve());
        }
        return text.ToString();
    }

    public override AlertReport GetReport()
    {
        culprits.Clear();
        foreach (PhobosInboundRaid raid in Due())
            culprits.Add(raid.marker is { Destroyed: false } ? new GlobalTargetInfo(raid.marker) : new GlobalTargetInfo(raid.map!.Parent));
        return culprits.Count == 0 ? AlertReport.Inactive : AlertReport.CulpritsAre(culprits);
    }
}

/// <summary>A radar station under a roof sees nothing (Radio 0.74.0, row RD3): the alert names it, as the game's "cannot be used roofed" does.</summary>
public class Alert_PhobosRadarRoofed : Alert
{
    private readonly List<Thing> culprits = new();

    public Alert_PhobosRadarRoofed()
    {
        defaultLabel = "PhobosRadar_AlertRoofed".Translate();
        defaultExplanation = "PhobosRadar_AlertRoofedDesc".Translate();
    }

    public override AlertReport GetReport()
    {
        culprits.Clear();
        foreach (Map map in Find.Maps)
        {
            if (!map.IsPlayerHome || map.GetComponent<MapComponent_PhobosRadar>() is not { } picture) continue;
            foreach (CompRadar radar in picture.Radars)
                if (radar.Roofed && radar.parent.Faction == Faction.OfPlayer) culprits.Add(radar.parent);
        }
        return culprits.Count == 0 ? AlertReport.Inactive : AlertReport.CulpritsAre(culprits);
    }
}
