using RimWorld;
using UnityEngine;
using Verse;

namespace Phobos.Framework.Protection;

/// <summary>
/// A kind of protective-gear line: what protects (a stat worn gear raises), how much is enough, and the colour its painted area shows
/// in. Content mods define one per hazard in XML (D'thorga Medical's radiation line first); control points and painted areas name it.
/// The label reads in sentences such as "radiation controlled area".
/// </summary>
public class ProtectionZoneDef : Def
{
    /// <summary>The stat worn gear must raise, read on the pawn, so genes, implants and every worn item count.</summary>
    public StatDef stat = null!;

    /// <summary>The stat's value a pawn needs to cross the line without stopping.</summary>
    public float required = 0.75f;

    /// <summary>The painted area's colour.</summary>
    public Color color = new(1f, 0.85f, 0.2f);

    /// <summary>
    /// Conditions a colonist should be under before going in (Framework 0.109.0; soft-dependency-crosslinks.md, SD21): one who has none
    /// takes a dose of any drug that gives one, if one is in reach outside the zone, before crossing. Optional; none in reach stops nobody.
    /// Named by the condition the drug gives, never the drug, so any mod's drug that gives it counts.
    /// </summary>
    public System.Collections.Generic.List<HediffDef> preventiveHediffs = new();

    System.Collections.Generic.List<ThingDef>? preventiveDrugs;

    /// <summary>Every item whose ingestion gives one of the preventive conditions, found once.</summary>
    public System.Collections.Generic.List<ThingDef> PreventiveDrugs
    {
        get
        {
            if (preventiveDrugs != null) return preventiveDrugs;
            preventiveDrugs = new System.Collections.Generic.List<ThingDef>();
            if (preventiveHediffs.Count == 0) return preventiveDrugs;
            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                if (def.ingestible?.outcomeDoers == null || def.category != ThingCategory.Item) continue;
                foreach (IngestionOutcomeDoer doer in def.ingestible.outcomeDoers)
                {
                    if (doer is IngestionOutcomeDoer_GiveHediff give && give.hediffDef != null && preventiveHediffs.Contains(give.hediffDef))
                    {
                        preventiveDrugs.Add(def);
                        break;
                    }
                }
            }
            return preventiveDrugs;
        }
    }

    public override System.Collections.Generic.IEnumerable<string> ConfigErrors()
    {
        foreach (string error in base.ConfigErrors()) yield return error;
        if (stat == null) yield return "names no stat for worn gear to raise";
        if (required <= 0f) yield return "requires nothing, so it would never stop anyone";
    }
}
