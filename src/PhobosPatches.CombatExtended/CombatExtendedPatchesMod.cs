using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Phobos.Framework;
using Verse;

namespace Phobos.CombatExtendedPatches;

/// <summary>
/// D'thorga Patches' Combat Extended part. <b>Temporary</b> (owner, 10 Nov 2026): it holds one guard against an ammo set entry naming ammo that does not exist, which
/// stops Combat Extended's whole ammo setup at startup. The Dead Man's Switch - Synthriada's Combat Extended patch adds such entries. With no empty entries the guard
/// does nothing, so it stands down by itself when that is fixed. It finds Combat Extended by name and references no assembly of it.
/// How to take it out: docs/development/temporary-patches.md. No settings, no save data.
/// </summary>
public class CombatExtendedPatchesMod : Mod
{
    /// <summary>The Harmony ID is the packageId, shared by every part; never change it once the mod is published.</summary>
    public const string PackageId = "PhobosDthorga.Patches";
    public const string ModVersion = "0.18.0";
    public const string LogPrefix = "[D'thorga Patches]";
    public static readonly Phobos.Logging.PhobosLog Logger = Phobos.Framework.Logging.Logs.For("D'thorga Patches", "Combat Extended");
    const string Feature = "temporary guard: ammo set entries naming ammo that does not exist are dropped before Combat Extended's ammo setup";

    public CombatExtendedPatchesMod(ModContentPack content) : base(content)
    {
        EmptyAmmoLinksGuard.Apply(new Harmony(PackageId));
        LongEventHandler.ExecuteWhenFinished(LogLoaded);
    }

    static void LogLoaded()
    {
        var report = new LoadReport(LogPrefix, ModVersion, "Combat Extended");
        if (EmptyAmmoLinksGuard.Active) report.On(Feature);
        else report.Off(Feature, "Combat Extended's code has changed");
        report.Log();
    }
}

/// <summary>
/// An ammo set entry whose ammo or projectile did not resolve loads with an empty field, and Combat Extended's vehicle compatibility passes the empty ammo to
/// ThingFilter.SetAllow during AmmoInjector.Inject, which throws and ends the whole injection, so no ammo is set up and mods building on it (Munitions Industries'
/// factory recipes) never run. This prefix takes such entries out first. It never skips the original.
/// </summary>
public static class EmptyAmmoLinksGuard
{
    const string InjectorType = "CombatExtended.AmmoInjector";
    const string InjectMethod = "Inject";
    const string SetDefType = "CombatExtended.AmmoSetDef";
    const string LinkType = "CombatExtended.AmmoLink";

    static Type? setDef;
    static FieldInfo? ammoTypes, ammo, projectile;

    public static bool Active { get; private set; }

    public static void Apply(Harmony harmony)
    {
        setDef = AccessTools.TypeByName(SetDefType);
        Type? link = AccessTools.TypeByName(LinkType);
        MethodInfo? inject = AccessTools.Method(AccessTools.TypeByName(InjectorType), InjectMethod);
        if (setDef == null || link == null || inject == null) return;
        ammoTypes = AccessTools.Field(setDef, "ammoTypes");
        ammo = AccessTools.Field(link, "ammo");
        projectile = AccessTools.Field(link, "projectile");
        if (ammoTypes == null || ammo == null || projectile == null) return;
        harmony.Patch(inject, prefix: new HarmonyMethod(typeof(EmptyAmmoLinksGuard), nameof(Prefix)));
        Active = true;
    }

    public static void Prefix()
    {
        var sets = (IEnumerable)typeof(DefDatabase<>).MakeGenericType(setDef!).GetProperty("AllDefsListForReading")!.GetValue(null)!;
        var touched = new List<string>();
        int removed = 0;
        foreach (object set in sets)
        {
            if (ammoTypes!.GetValue(set) is not IList links) continue;
            int before = removed;
            for (int i = links.Count - 1; i >= 0; i--)
            {
                object? entry = links[i];
                if (entry != null && ammo!.GetValue(entry) != null && projectile!.GetValue(entry) != null) continue;
                links.RemoveAt(i);
                removed++;
            }
            if (removed > before) touched.Add(((Def)set).defName);
        }
        if (removed > 0) CombatExtendedPatchesMod.Logger.Info("Dropped " + removed + " ammo set entries naming ammo that does not exist, from: " + string.Join(", ", touched) + ".");
    }
}
