using PipeSystem;
using Verse;

namespace Phobos.Factory;

/// <summary>Whether a bill may be offered in a machine's Add process menu now.</summary>
public static class BillAvailability
{
    /// <summary>
    /// A generated bill follows its bench recipe, which covers research and Ideology memes, faction tags and
    /// other mods' own unlocks (Dubs Rimatomics patches the same check for its research). Every other bill is
    /// left to Pipe System.
    /// </summary>
    public static bool IsAvailable(ProcessDef process)
    {
        var source = process.GetModExtension<GeneratedBillExtension>()?.sourceRecipe;
        return source == null || source.AvailableNow;
    }
}
