using Phobos.Framework.Defs;
using RimWorld;
using Verse;

namespace Phobos.Medical.Defs;

/// <summary>
/// The starting values to tune in play. One Def; the first loaded wins. Every figure is authored. Player preferences are mod
/// settings, and each machine's own figures sit on its comp in XML, so other mods can add machines.
/// </summary>
public class PhobosMedicalRulesDef : PhobosRulesDef
{
    /// <summary>Breathing below this counts as needing oxygen, whatever caused it. Authored: mild trouble is left alone.</summary>
    public float breathingNeedThreshold = 0.8f;

    /// <summary>How long the benefit lasts after the last supply: long enough to survive a machine's ticking, short enough to end soon after the patient leaves.</summary>
    public int treatmentLingerTicks = 600;
}

/// <summary>Marks an injury Def as a burn that a burn unit bed eases. On the game's burns and Combat Extended's; other mods add it in XML.</summary>
public class PhobosBurnCareExtension : DefModExtension
{
}

[DefOf]
public static class MedicalDefOf
{
    public static HediffDef PhobosMedical_OnOxygen = null!;

    static MedicalDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(MedicalDefOf));
}
