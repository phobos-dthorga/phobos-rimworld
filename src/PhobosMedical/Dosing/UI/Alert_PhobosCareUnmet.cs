using System.Collections.Generic;
using Phobos.Dosing.Services;
using RimWorld;
using Verse;

namespace Phobos.Dosing.UI;

/// <summary>
/// The alert for a care need nothing in stock answers (Medical 0.28.0; the owner's card of 26 Nov 2026: "an alert while it lasts").
/// It names each person and what they lack, for as long as the need lasts and the stock holds nothing for it, and only for something
/// the colony knows how to make. Presentation only: who is listed is <see cref="CareStock"/>'s answer, read from its per-map list.
/// The game finds every alert class by itself, so this needs no Def and no patch.
/// </summary>
public class Alert_PhobosCareUnmet : Alert
{
    readonly List<Thing> culprits = new();
    readonly List<string> lines = new();

    public Alert_PhobosCareUnmet()
    {
        defaultLabel = "PhobosDosing_UnmetAlert".Translate();
        defaultPriority = AlertPriority.Medium;
    }

    void Refresh()
    {
        culprits.Clear();
        lines.Clear();
        if (!DosingMod.Settings.enabled || !DosingMod.Settings.unmetAlert) return;
        List<Map> maps = Find.Maps;
        for (int i = 0; i < maps.Count; i++)
        {
            if (!maps[i].IsPlayerHome && maps[i].mapPawns.FreeColonistsSpawnedCount == 0) continue;
            IReadOnlyList<UnmetCare> unmet = CareStock.Unmet(maps[i]);
            for (int j = 0; j < unmet.Count; j++)
            {
                if (unmet[j].Pawn.DestroyedOrNull() || !unmet[j].Pawn.Spawned) continue;
                if (!culprits.Contains(unmet[j].Pawn)) culprits.Add(unmet[j].Pawn);
                lines.Add("PhobosDosing_UnmetLine".Translate(unmet[j].Pawn.LabelShort.Named("PAWN"), unmet[j].What.Named("WHAT")).Resolve());
            }
        }
    }

    public override AlertReport GetReport()
    {
        Refresh();
        return culprits.Count == 0 ? AlertReport.Inactive : AlertReport.CulpritsAre(culprits);
    }

    public override TaggedString GetExplanation() =>
        "PhobosDosing_UnmetIntro".Translate() + "\n\n" + string.Join("\n", lines);
}
