using Phobos.BurnsAndSmoke.Defs;
using RimWorld;
using UnityEngine;
using Verse;

namespace Phobos.BurnsAndSmoke.Comps;

/// <summary>
/// Caustic residue in a garment, for the strip-and-rinse setting (owner, 8 Nov 2026: off by default; docs/development/caustic-gas.md).
/// Clothes worn in tox gas take it up; while worn they keep dosing the skin; it fades over a few days as it off-gasses, and a wash
/// (Dubs Bad Hygiene's washing machine) clears it. Saved with the garment: the level and the tick it was last set, so it fades without
/// ticking. Put on every apparel Def at startup by <see cref="ContaminationInjector"/>; idle while the setting is off.
/// </summary>
public class CompProperties_PhobosContamination : CompProperties
{
    public CompProperties_PhobosContamination() => compClass = typeof(CompPhobosContamination);
}

public class CompPhobosContamination : ThingComp
{
    float level;
    int setTick;

    /// <summary>The residue now, 0 to 1, faded since it was last set.</summary>
    public float Level
    {
        get
        {
            if (level <= 0f || Find.TickManager == null) return 0f;
            float days = (Find.TickManager.TicksGame - setTick) / 60000f;
            float now = level - PhobosBurnRulesDef.Current.causticClothesFadePerDay * days;
            return now > 0f ? now : 0f;
        }
    }

    public bool Contaminated => Level >= PhobosBurnRulesDef.Current.causticClothesThreshold;

    public void Add(float amount)
    {
        if (amount <= 0f) return;
        level = Mathf.Min(1f, Level + amount);
        setTick = Find.TickManager.TicksGame;
    }

    public void Clean()
    {
        level = 0f;
        setTick = 0;
        dust = 0f;
        dustTick = 0;
    }

    // ---- Radioactive fallout dust (D'thorga Medical 0.24.0; docs/development/radiation-model.md, RM6) ----

    float dust;
    int dustTick;

    /// <summary>Fallout dust in the garment now, faded by the 7:10 rule from when it last gathered (Glasstone and Dolan, 9.16).</summary>
    public float Dust => dust <= 0f || Find.TickManager == null ? 0f
        : dust * (float)Phobos.Radiation.Rules.ContaminationRule.DustLeft((Find.TickManager.TicksGame - dustTick) / 60000.0);

    // Only while the radiation model is on (fault-read 25 Nov 2026, F9): with it off, nothing doses, so no garment is shown or filtered dusty.
    public bool Dusty => Phobos.Radiation.RadiationPart.On && Phobos.Radiation.RadiationPart.Settings.contamination
        && Dust >= Phobos.Radiation.Defs.PhobosRadiationRulesDef.Current.dustToWash;

    public void AddDust(float amount)
    {
        if (amount <= 0f) return;
        dust = Dust + amount;
        dustTick = Find.TickManager.TicksGame;
    }

    public override string TransformLabel(string label)
    {
        if (BurnsAndSmokeMod.Settings.causticStripAndRinse && Contaminated) return "PhobosBurns_ContaminatedLabel".Translate(label.Named("LABEL")).Resolve();
        return Dusty ? "PhobosRadiation_DustyLabel".Translate(label.Named("LABEL")).Resolve() : label;
    }

    public override string? CompInspectStringExtra()
    {
        if (BurnsAndSmokeMod.Settings.causticStripAndRinse && Contaminated) return "PhobosBurns_ContaminatedInspect".Translate();
        return Dusty ? "PhobosRadiation_DustyInspect".Translate() : null;
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref level, "PhobosBurns_ContaminationLevel", 0f);
        Scribe_Values.Look(ref setTick, "PhobosBurns_ContaminationTick", 0);
        Scribe_Values.Look(ref dust, "PhobosRadiation_Dust", 0f);
        Scribe_Values.Look(ref dustTick, "PhobosRadiation_DustTick", 0);
    }
}

/// <summary>Puts the contamination comp on every apparel Def once Defs are loaded, before any garment is made or loaded.</summary>
[StaticConstructorOnStartup]
public static class ContaminationInjector
{
    static ContaminationInjector()
    {
        foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading)
        {
            if (!def.IsApparel || def.HasComp(typeof(CompPhobosContamination))) continue;
            def.comps ??= new System.Collections.Generic.List<CompProperties>();
            def.comps.Add(new CompProperties_PhobosContamination());
        }
    }
}

/// <summary>Matches contaminated garments, for apparel policies and stockpiles (disallowed in new ones by default).</summary>
public class SpecialThingFilterWorker_PhobosContaminated : SpecialThingFilterWorker
{
    public override bool Matches(Thing t) => t.TryGetComp<CompPhobosContamination>() is { } comp && (comp.Contaminated || comp.Dusty);

    public override bool CanEverMatch(ThingDef def) => def.IsApparel;
}
