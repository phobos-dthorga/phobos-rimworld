using System;
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
        // Each patch class is applied on its own: the output guard patches a private Pipe System method, and if
        // an update renames it, the bills and the menu filter should still work.
        var harmony = new Harmony(HarmonyId);
        foreach (var type in AccessTools.GetTypesFromAssembly(typeof(FactoryMod).Assembly))
        {
            if (!type.IsDefined(typeof(HarmonyPatch), inherit: false)) continue;
            try
            {
                harmony.CreateClassProcessor(type).Patch();
            }
            catch (Exception error)
            {
                Log.Error($"[D'thorga Factory Additions] Could not apply {type.Name}; that part is switched off. {error}");
            }
        }
    }
}
