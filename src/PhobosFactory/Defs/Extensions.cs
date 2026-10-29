using Verse;

namespace Phobos.Factory;

/// <summary>
/// On a factory building: give every bill on it an output limit that never loses a finished batch
/// (<see cref="Rules.OutputCapRule"/>). Patched onto VFE Factory's automated machining bay in
/// Common/Patches; other machines opt in the same way.
/// </summary>
public class SafeOutputLimitsExtension : DefModExtension
{
}

/// <summary>
/// On a bill made by a <see cref="BenchBillTemplateDef"/>: the bench recipe it automates, so the bill is
/// offered only while that recipe is (research, Ideology memes, faction tags, and other mods' own unlocks).
/// Set in code, never in XML.
/// </summary>
public class GeneratedBillExtension : DefModExtension
{
    public RecipeDef? sourceRecipe;
    public BenchBillTemplateDef? template;
}
