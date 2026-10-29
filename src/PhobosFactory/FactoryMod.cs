using HarmonyLib;
using Verse;

namespace Phobos.Factory;

public class FactoryMod : Mod
{
    /// <summary>The Harmony ID is the packageId; never change it once the mod is published.</summary>
    public const string HarmonyId = "PhobosDthorga.FactoryAdditions";

    public FactoryMod(ModContentPack content) : base(content)
    {
        // Mods are constructed before the game loads Defs, so the patches are in place for DefGeneratorPatch.
        new Harmony(HarmonyId).PatchAll();
    }
}
