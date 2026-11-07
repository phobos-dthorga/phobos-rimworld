using Phobos.BurnsAndSmoke.Defs;
using RimWorld;
using Verse;

namespace Phobos.BurnsAndSmoke.Services;

/// <summary>
/// Fluids for burn shock. Tending the shock already stands in for giving fluids, and its tend quality is the fluids term of the
/// shock's treatment; a measured drip is the same care at a fixed quality, through the game's own tending, so the shock's tooltip and
/// its clock read it like any other tend.
/// </summary>
public static class FluidsService
{
    /// <summary>Counts as tending the patient's burn shock at <paramref name="quality"/>. False when they have no burn shock.</summary>
    public static bool Give(Pawn pawn, float quality)
    {
        Hediff? shock = pawn.health.hediffSet.GetFirstHediffOfDef(BurnsDefOf.PhobosBurns_BurnShock);
        if (shock == null) return false;
        shock.Tended(quality, 1f);
        return true;
    }

    public static bool NeedsFluids(Pawn pawn) => pawn.health.hediffSet.HasHediff(BurnsDefOf.PhobosBurns_BurnShock);

    /// <summary>The quality of the fluids the burn shock has now: its tend quality while the tend lasts, 0 when untended or none.</summary>
    public static float CurrentQuality(Pawn pawn)
    {
        Hediff? shock = pawn.health.hediffSet.GetFirstHediffOfDef(BurnsDefOf.PhobosBurns_BurnShock);
        HediffComp_TendDuration? tend = shock?.TryGetComp<HediffComp_TendDuration>();
        return tend != null && tend.IsTended ? tend.tendQuality : 0f;
    }
}
