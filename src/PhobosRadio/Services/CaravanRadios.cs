using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Phobos.Radio.Services;

/// <summary>
/// The hand-helds a caravan of the player's can call home with (Radio 0.44.0; the owner's cards of 27 Nov 2026: "Make it real", hand-helds
/// "worn or packed"; docs/development/radio-world-coverage.md). A hand-held worn by anyone in the caravan, or packed in its goods, counts
/// while someone in the caravan can use it: a person of the player's, not downed, able to talk. The best is the most powerful, then the
/// one held highest. A vehicle's own radio is not a hand-held: the vehicles tie-in prefers it where one is fitted.
/// <para>Cost: asked only when a caravan's reach home is worked out (at most once a tenth of a game hour for each caravan): one look over
/// its people's worn clothes and its goods.</para>
/// </summary>
public static class CaravanRadios
{
    /// <summary>The best hand-held this caravan can call with, or null when it has none, or nobody able to use one.</summary>
    public static CompRadio? BestHandheld(Caravan caravan)
    {
        if (caravan.Faction != Faction.OfPlayer) return null;
        CompRadio? best = null;
        bool anyAble = false;
        foreach (Pawn pawn in caravan.PawnsListForReading)
        {
            if (!Able(pawn)) continue;
            anyAble = true;
            if (pawn.apparel == null) continue;
            foreach (Apparel worn in pawn.apparel.WornApparel) Consider(worn.TryGetComp<CompRadio>(), ref best);
        }
        if (!anyAble) return null;
        // Packed in the caravan's goods: someone able takes it out to call (the owner's card: "Worn or packed").
        foreach (Thing item in CaravanInventoryUtility.AllInventoryItems(caravan)) Consider(item.TryGetComp<CompRadio>(), ref best);
        return best;
    }

    /// <summary>Whether this hand-held is worn (true) or packed in the goods (false): for the seen-in-play note.</summary>
    public static bool Worn(CompRadio radio) => radio.parent is Apparel { Wearer: not null };

    /// <summary>A person of the player's who can use a radio: not downed, and able to talk.</summary>
    static bool Able(Pawn pawn) =>
        pawn.Faction == Faction.OfPlayer && pawn.RaceProps.Humanlike && !pawn.Dead && !pawn.Downed
        && pawn.health.capacities.CapableOf(PawnCapacityDefOf.Talking);

    /// <summary>Keeps the better of the two: a hand-held (worn radio, never a station, never a pawn's built-in), the most power, then the highest.</summary>
    internal static void Consider(CompRadio? radio, ref CompRadio? best)
    {
        if (radio == null || radio.parent.def.race != null) return;
        CompProperties_Radio props = radio.Props;
        if (props.isStation || props.mode == null || props.transmitPowerWatts <= 0 || props.bands.Count == 0) return;
        if (best != null)
        {
            CompProperties_Radio held = best.Props;
            if (props.transmitPowerWatts < held.transmitPowerWatts) return;
            if (props.transmitPowerWatts == held.transmitPowerWatts && props.antennaHeightMetres <= held.antennaHeightMetres) return;
        }
        best = radio;
    }
}
