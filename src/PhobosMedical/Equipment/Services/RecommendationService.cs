using Phobos.Framework;
using RimWorld;
using Verse;

namespace Phobos.Medical.Services;

/// <summary>
/// The one recommendation this mod makes in play: a letter, once per install, when a colony first starts or loads without Vanilla
/// Gravship Expanded, which five of its pieces need. The owner approved naming a mod here as the one exception to the in-world text
/// rule (7 Nov 2026). Remembered in the mod settings, so it never repeats, whichever colony is loaded.
/// </summary>
public static class RecommendationService
{
    public const string VanillaGravshipExpanded = "vanillaexpanded.gravship";

    public static bool GravshipActive => OptionalMods.IsActive(VanillaGravshipExpanded);

    public static void ShowOnceIfDue()
    {
        MedicalEquipmentSettings settings = MedicalEquipmentMod.Settings;
        if (GravshipActive || settings.gravshipLetterShown || Find.LetterStack == null) return;
        Find.LetterStack.ReceiveLetter("PhobosMedical_GravshipLetterLabel".Translate(), "PhobosMedical_GravshipLetterText".Translate(),
            LetterDefOf.NeutralEvent);
        settings.gravshipLetterShown = true;
        MedicalMod.WriteAll();
    }
}
