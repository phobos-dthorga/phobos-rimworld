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

    public static PhobosRadiationRulesDef Current => Current<PhobosRadiationRulesDef>();
}
