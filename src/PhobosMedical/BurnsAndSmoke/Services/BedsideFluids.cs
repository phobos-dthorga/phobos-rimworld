using Phobos.BurnsAndSmoke.Defs;
using RimWorld;
using Verse;

namespace Phobos.BurnsAndSmoke.Services;

/// <summary>
/// A drip stand beside the bed keeps a burn-shock patient on fluids (Medical 0.16.0; soft-dependency-crosslinks.md, SD23; owner, 13 Nov
/// 2026). VFE Medical's intravenous drip stand says it delivers lost fluids to the patient of the hospital bed it serves; like its
/// immunity bonus, it needs no bags of its own (the game's abstraction, which we follow). While the patient lies in a bed with an
/// active listed stand linked, their burn shock counts as tended with fluids of the rules' drip quality, unless better fluids are
/// already running. Runs at the burn recompute, every few hundred ticks; nothing per tick.
/// </summary>
public static class BedsideFluids
{
    /// <summary>Gives the drip's fluids if a listed stand serves the patient's bed and theirs are poorer; true when it did.</summary>
    public static bool Give(Pawn pawn, PhobosBurnRulesDef rules)
    {
        if (rules.fluidsFacilities.Count == 0 || !HasDrip(pawn, rules)) return false;
        if (FluidsService.CurrentQuality(pawn) >= rules.dripFluidsQuality) return false;
        return FluidsService.Give(pawn, rules.dripFluidsQuality);
    }

    static bool HasDrip(Pawn pawn, PhobosBurnRulesDef rules)
    {
        CompAffectedByFacilities? facilities = pawn.CurrentBed()?.GetComp<CompAffectedByFacilities>();
        if (facilities == null) return false;
        foreach (Thing facility in facilities.LinkedFacilitiesListForReading)
        {
            if (rules.fluidsFacilities.Contains(facility.def) && facilities.IsFacilityActive(facility)) return true;
        }
        return false;
    }
}
