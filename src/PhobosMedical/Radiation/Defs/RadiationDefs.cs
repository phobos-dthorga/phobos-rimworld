using Phobos.Framework.Defs;
using RimWorld;
using Verse;

namespace Phobos.Radiation.Defs;

[DefOf]
public static class RadiationDefOf
{
    public static HediffDef PhobosRadiation_Dose = null!;
    public static HediffDef PhobosRadiation_EarlySickness = null!;
    public static HediffDef PhobosRadiation_Illness = null!;
    public static HediffDef PhobosRadiation_Leukaemia = null!;
    public static HediffDef PhobosRadiation_TemporarySterility = null!;
    public static HediffDef PhobosRadiation_PrenatalMalformation = null!;
    public static HediffDef PhobosRadiation_PrenatalBrainInjury = null!;
    public static HediffDef Carcinoma = null!;
    public static HediffDef Cataract = null!;
    public static HediffDef Sterilized = null!;
    [MayRequire("Dubwise.Rimatomics")] public static HediffDef? PotassiumIodide;

    static RadiationDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(RadiationDefOf));
}

/// <summary>
/// The radiation model's figures that are not in its game-free rule (docs/development/radiation-model.md). The first loaded instance
/// wins; another mod may patch it in XML. Every figure is authored.
/// </summary>
public class PhobosRadiationRulesDef : PhobosRulesDef
{
    /// <summary>How often a dosed person's course is worked out, in ticks (a multiple of the givers' 60).</summary>
    public int intervalTicks = 300;
    /// <summary>How fast the illness recedes after its height, a share of its severity a day.</summary>
    public float recoveryPerDay = 0.05f;
    /// <summary>A course ends, and the dose condition fades, once the effective dose is below this and no illness is left.</summary>
    public float courseEndsBelow = 0.2f;
    /// <summary>Care, 0 to 1, as the sum of: tended (times tend quality), in bed, in a clean room, on fluids, on a medicine that speeds
    /// immunity.</summary>
    public float careTended = 0.35f;
    public float careInBed = 0.2f;
    public float careClean = 0.15f;
    public float careFluids = 0.15f;
    public float careImmunity = 0.15f;
    /// <summary>A room counts as clean at this cleanliness or better (a hospital's sterile floor reaches it).</summary>
    public float cleanRoomFrom = 0.6f;
    /// <summary>Doses below this are added to the record but start nothing, and are not shown.</summary>
    public float smallestShown = 0.05f;
    /// <summary>Where a solid cancer from radiation forms: the first of these the person has, from a place fixed by who they are.</summary>
    public System.Collections.Generic.List<BodyPartDef> solidCancerParts = new();
    /// <summary>A leukaemia or carcinoma from radiation begins at this severity.</summary>
    public float cancerStartSeverity = 0.05f;

    // ---- Contamination (Medical 0.24.0, RM6), all authored ----

    /// <summary>Dust gathered per gray of fallout's dose taken outdoors, spread over the clothes and bare skin.</summary>
    public float dustPerGray = 1f;
    /// <summary>The dose a day, in grays, from each unit of dust carried on the skin or in worn clothes.</summary>
    public float dustDosePerDay = 0.02f;
    /// <summary>Dust at or above this on a garment or the skin wants washing off.</summary>
    public float dustToWash = 0.1f;
    /// <summary>The dose from inside, in grays, taken in per gray of fallout's dose while breathing unmasked.</summary>
    public float breathedPerGray = 0.03f;
    /// <summary>The dose from inside taken in per unit of dust on the skin per day, swallowed from the hands (a mask does not stop it).</summary>
    public float swallowedPerDustDay = 0.01f;
    /// <summary>The dose to the thyroid per gray of fallout's dose while breathing unmasked, without potassium iodide.</summary>
    public float thyroidPerGray = 2f;
    /// <summary>How long caesium stays in the body, a half-life in days (Radiogardase label: about 110).</summary>
    public float insideHalfLifeDays = 110f;

    public static PhobosRadiationRulesDef Current => Current<PhobosRadiationRulesDef>();
}
