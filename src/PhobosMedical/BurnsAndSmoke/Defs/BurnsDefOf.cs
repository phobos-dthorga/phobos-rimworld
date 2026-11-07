using RimWorld;
using Verse;

namespace Phobos.BurnsAndSmoke.Defs;

[DefOf]
public static class BurnsDefOf
{
    public static HediffDef PhobosBurns_BurnLoad = null!;
    public static HediffDef PhobosBurns_BurnShock = null!;
    public static HediffDef PhobosBurns_AirwayBurn = null!;
    public static HediffDef PhobosBurns_CarbonMonoxide = null!;
    public static HediffDef PhobosBurns_LungInjury = null!;
    public static HediffDef PhobosBurns_SmokeInhalation = null!;
    public static JobDef PhobosBurns_MoveAwayFromHeat = null!;

    static BurnsDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(BurnsDefOf));
}
