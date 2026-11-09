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
    public static DamageDef PhobosBurns_CausticScorch = null!;
    public static HediffDef PhobosBurns_SmokeInhalation = null!;
    public static JobDef PhobosBurns_MoveAwayFromHeat = null!;
    public static HediffDef PhobosBurns_Eschar = null!;
    public static HediffDef PhobosBurns_EscharotomyWound = null!;
    public static HediffDef PhobosBurns_EscharReleased = null!;
    public static DamageDef PhobosBurns_Ischaemia = null!;
    public static HediffDef PhobosBurns_ChemicalOnSkin = null!;
    public static HediffDef PhobosBurns_Rinsed = null!;
    public static HediffDef PhobosBurns_CausticExposure = null!;
    public static HediffDef PhobosBurns_CausticEye = null!;
    public static HediffDef PhobosBurns_CausticWeakness = null!;
    public static HediffDef PhobosBurns_IrritableAirways = null!;
    public static HediffDef PhobosBurns_IrritableAirwaysLasting = null!;
    public static DamageDef PhobosBurns_CausticSkin = null!;

    static BurnsDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(BurnsDefOf));
}
