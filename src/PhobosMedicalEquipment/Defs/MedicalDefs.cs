using Phobos.Framework.Defs;
using Phobos.Medical.Rules;
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

    /// <summary>Breathing below this is weak enough for a doctor to push air in by hand. Authored.</summary>
    public float breathingAssistThreshold = 0.5f;
}

/// <summary>Marks an injury Def as a burn that a burn unit bed eases. On the game's burns and Combat Extended's; other mods add it in XML.</summary>
public class PhobosBurnCareExtension : DefModExtension
{
}

/// <summary>
/// Makes an item a device doctors use on a patient by themselves, as Doctor work: they fetch it, take it to the patient and use it.
/// Who it helps is the <see cref="need"/>, read from the patient's body, never from a condition's name. Other mods can add devices in XML.
/// </summary>
public class PhobosPatientDeviceExtension : DefModExtension
{
    public DeviceNeed need;
    /// <summary>The condition the patient gets. It lapses by itself unless the device keeps it going (its treatment comp's linger time).</summary>
    public HediffDef treatment = null!;
    /// <summary>Ticks to fit it; for a sustained device, the longest one turn of use lasts before the doctor checks their other work.</summary>
    public int workTicks = 300;
    /// <summary>Used up by one use (a kit), or kept by the doctor afterwards (a tool).</summary>
    public bool consumed = true;
    /// <summary>The doctor keeps working it, and the treatment lasts only while they do (a hand pump), refreshed every <see cref="refreshTicks"/>.</summary>
    public bool sustained;
    public int refreshTicks = 250;

    public override System.Collections.Generic.IEnumerable<string> ConfigErrors()
    {
        foreach (string error in base.ConfigErrors()) yield return error;
        if (treatment == null) yield return "PhobosPatientDeviceExtension needs a treatment HediffDef.";
        if (workTicks <= 0) yield return "PhobosPatientDeviceExtension needs workTicks above 0.";
        if (sustained && refreshTicks <= 0) yield return "A sustained PhobosPatientDeviceExtension needs refreshTicks above 0.";
    }
}

[DefOf]
public static class MedicalDefOf
{
    public static HediffDef PhobosMedical_OnOxygen = null!;
    public static JobDef PhobosMedical_UseDevice = null!;

    static MedicalDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(MedicalDefOf));
}
