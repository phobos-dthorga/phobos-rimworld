using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace Phobos.Factory;

public class FactoryMod : Mod
{
    /// <summary>The Harmony ID is the packageId; never change it once the mod is published.</summary>
    public const string HarmonyId = "PhobosDthorga.FactoryAdditions";

    /// <summary>The only mod these settings concern so far; without it the mod shows no options page.</summary>
    private const string HygienePackageId = "Dubwise.DubsBadHygiene";

    public static FactorySettings Settings { get; private set; } = null!;

    public FactoryMod(ModContentPack content) : base(content)
    {
        Settings = GetSettings<FactorySettings>();
        // Mods are constructed before the game loads Defs, so the patches are in place for DefGeneratorPatch.
        ApplyPatches(new Harmony(HarmonyId), typeof(FactoryMod).Assembly);
    }

    /// <summary>
    /// Applies an assembly's patch classes one at a time. The output guard patches a private Pipe System method,
    /// and if an update renames it, the bills and the menu filter should still work. Tie-in assemblies use this too.
    /// </summary>
    public static void ApplyPatches(Harmony harmony, Assembly assembly)
    {
        foreach (var type in AccessTools.GetTypesFromAssembly(assembly))
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

    public override string SettingsCategory() =>
        ModsConfig.IsActive(HygienePackageId) ? "PhobosFactory_SettingsCategory".Translate().Resolve() : "";

    public override void DoSettingsWindowContents(Rect inRect)
    {
        var listing = new Listing_Standard();
        listing.Begin(inRect);
        listing.CheckboxLabeled("PhobosFactory_SettingWaterRequired".Translate(), ref Settings.waterRequired);
        listing.Label("PhobosFactory_SettingWaterRequiredDesc".Translate().Resolve());
        listing.Gap();
        if (Settings.waterRequired)
        {
            listing.CheckboxLabeled("PhobosFactory_SettingDirtyWaterPoisons".Translate(), ref Settings.dirtyWaterPoisons);
            listing.Label("PhobosFactory_SettingDirtyWaterPoisonsDesc".Translate().Resolve());
        }
        listing.End();
    }
}
