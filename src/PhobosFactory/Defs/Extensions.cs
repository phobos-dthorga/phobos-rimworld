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
