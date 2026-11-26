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

    /// <summary>
    /// Stats a medical drug may raise to count as a preventive here (Framework 0.327.0): any item whose condition, at its dose, adds to
    /// one of them is offered at the line, found by what it does rather than by name. Optional; recreational drugs never count this way.
    /// </summary>
    public System.Collections.Generic.List<StatDef> preventiveStats = new();

    System.Collections.Generic.List<ThingDef>? preventiveDrugs;
    System.Collections.Generic.List<HediffDef>? preventiveConditions;

    /// <summary>Every item whose ingestion gives one of the preventive conditions, or raises one of the preventive stats, found once.</summary>
    public System.Collections.Generic.List<ThingDef> PreventiveDrugs
    {
        get
        {
            if (preventiveDrugs == null) FindPreventives();
            return preventiveDrugs!;
        }
    }

    /// <summary>The conditions that mean a pawn has had its preventive: the named ones, and those the drugs found by stat give.</summary>
    public System.Collections.Generic.List<HediffDef> PreventiveConditions
    {
        get
        {
            if (preventiveConditions == null) FindPreventives();
            return preventiveConditions!;
        }
    }

    void FindPreventives()
    {
        var drugs = new System.Collections.Generic.List<ThingDef>();
        var conditions = new System.Collections.Generic.List<HediffDef>(preventiveHediffs);
        if (preventiveHediffs.Count > 0 || preventiveStats.Count > 0)
        {
            foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                if (def.ingestible?.outcomeDoers == null || def.category != ThingCategory.Item) continue;
                foreach (IngestionOutcomeDoer doer in def.ingestible.outcomeDoers)
                {
                    if (doer is not IngestionOutcomeDoer_GiveHediff give || give.hediffDef == null) continue;
                    bool named = preventiveHediffs.Contains(give.hediffDef);
                    if (!named && !(def.IsDrug && !def.IsNonMedicalDrug && RaisesPreventiveStat(give))) continue;
                    if (!drugs.Contains(def)) drugs.Add(def);
                    if (!conditions.Contains(give.hediffDef)) conditions.Add(give.hediffDef);
                }
            }
        }
        preventiveDrugs = drugs;
        preventiveConditions = conditions;
    }

    /// <summary>Whether the condition an outcome gives, at the stage of its dose, adds to one of the preventive stats.</summary>
    bool RaisesPreventiveStat(IngestionOutcomeDoer_GiveHediff give)
    {
        if (preventiveStats.Count == 0 || give.hediffDef.stages == null || give.hediffDef.stages.Count == 0) return false;
        float severity = give.severity > 0f ? give.severity : give.hediffDef.initialSeverity;
        HediffStage stage = give.hediffDef.stages[give.hediffDef.StageAtSeverity(severity)];
        if (stage.statOffsets == null) return false;
        foreach (StatDef stat in preventiveStats)
        {
            if (stat != null && stage.statOffsets.GetStatOffsetFromList(stat) > 0f) return true;
        }
        return false;
    }

    public override System.Collections.Generic.IEnumerable<string> ConfigErrors()
    {
        foreach (string error in base.ConfigErrors()) yield return error;
        if (stat == null) yield return "names no stat for worn gear to raise";
        if (required <= 0f) yield return "requires nothing, so it would never stop anyone";
    }
}
