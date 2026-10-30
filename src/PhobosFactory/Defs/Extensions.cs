using Verse;

namespace Phobos.Factory;

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

/// <summary>
/// On a storage building's Def that VFE Factory's belts take from (Pipe System's <c>FactoryHopperExtension</c>
/// with <c>isfactoryHopper</c>): belts take only items its storage filter allows, and leave the rest for haulers.
/// VFE Factory's belts otherwise take the first item they find, so one stray item jams a line: in the owner's
/// game a steel slag chunk rode out of the corpse fridge and blocked the corpse belts at the mincer's hopper
/// (30 Oct 2026). Set in XML; the [sbz] corpse fridge carries it. Storage switched on with "Belts may take from
/// here" obeys its filter without it.
/// </summary>
public class BeltFilterExtension : DefModExtension
{
}
