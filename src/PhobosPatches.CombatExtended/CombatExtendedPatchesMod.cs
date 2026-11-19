using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Phobos.CombatExtendedPatches.KeepGuns;
using Phobos.Framework;
using Phobos.Shared;
using UnityEngine;
using Verse;

namespace Phobos.CombatExtendedPatches;

public class CombatExtendedPatchesSettings : ModSettings
{
    /// <summary>Colonists left holding a grenade, a satchel charge or nothing go back to their own gun. Off is Combat Extended's and the game's
    /// own behaviour.</summary>
    public bool keepGuns = true;

    /// <summary>When their own gun is gone, they take another of the same kind lying unforbidden on the map.</summary>
    public bool fetchSameKind = true;

    /// <summary>Simple Sidearms keeps a colonist's gun, not a grenade, as the weapon it puts back in their hands.</summary>
    public bool fixSidearms = true;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref keepGuns, "PhobosCombatExtendedPatches_KeepGuns", true);
        Scribe_Values.Look(ref fetchSameKind, "PhobosCombatExtendedPatches_FetchSameKind", true);
        Scribe_Values.Look(ref fixSidearms, "PhobosCombatExtendedPatches_FixSidearms", true);
    }
}

/// <summary>
/// D'thorga Patches' Combat Extended part, loaded only while Combat Extended is active. It holds:
/// <list type="bullet">
/// <item><b>Soldiers keep their guns</b> (0.26.0; docs/development/soldiers-keep-their-guns.md): colonists left holding a grenade or a satchel
/// charge, or nothing after being downed, go back to their own gun. Its own behaviour, with three settings; its memory lives in D'thorga
/// Framework's part store. It reaches Combat Extended and Simple Sidearms by name and references no assembly of either.</item>
/// <item>A <b>temporary</b> guard (owner, 10 Nov 2026) against an ammo set entry naming ammo that does not exist, which stops Combat Extended's
/// whole ammo setup at startup. The Dead Man's Switch - Synthriada's Combat Extended patch adds such entries. With no empty entries the guard
/// does nothing, so it stands down by itself when that is fixed. How to take it out: docs/development/temporary-patches.md.</item>
/// </list>
/// </summary>
public class CombatExtendedPatchesMod : Mod
{
    /// <summary>The Harmony ID is the packageId, shared by every part; never change it once the mod is published.</summary>
    public const string PackageId = "PhobosDthorga.Patches";
    public const string ModVersion = "0.26.0";
    public const string LogPrefix = "[D'thorga Patches]";
    public static readonly Phobos.Logging.PhobosLog Logger = Phobos.Framework.Logging.Logs.For("D'thorga Patches", "Combat Extended");
    const string AmmoFeature = "temporary guard: ammo set entries naming ammo that does not exist are dropped before Combat Extended's ammo setup";
    const string KeepGunsFeature = "soldiers go back to their own guns instead of fighting with grenades";
    const string SidearmsFeature = "Simple Sidearms keeps a colonist's gun, not a grenade, as the weapon it hands back";

    public static CombatExtendedPatchesSettings Settings { get; private set; } = new();

    public CombatExtendedPatchesMod(ModContentPack content) : base(content)
    {
        Settings = GetSettings<CombatExtendedPatchesSettings>();
        var harmony = new Harmony(PackageId);
        EmptyAmmoLinksGuard.Apply(harmony);
        HarmonyPatchApplier.ApplyEach(harmony, typeof(CombatExtendedPatchesMod).Assembly, LogPrefix);
        LongEventHandler.ExecuteWhenFinished(LogLoaded);
    }

    static void LogLoaded()
    {
        var report = new LoadReport(LogPrefix, ModVersion, "Combat Extended");
        if (EmptyAmmoLinksGuard.Active) report.On(AmmoFeature);
        else report.Off(AmmoFeature, "Combat Extended's code has changed");
        if (Settings.keepGuns) report.On(KeepGunsFeature);
        else report.Off(KeepGunsFeature, "its setting is off, as the player chose");
        if (OptionalMods.IsActive("petetimessix.simplesidearms"))
        {
            if (!Settings.fixSidearms) report.Off(SidearmsFeature, "its setting is off, as the player chose");
            else if (SidearmsMemory.Available) report.On(SidearmsFeature);
            else report.Off(SidearmsFeature, "Simple Sidearms' code has changed");
        }
        report.Log();
    }

    public override string SettingsCategory() => "PhobosCombatExtendedPatches_SettingsCategory".Translate();

    public override void DoSettingsWindowContents(Rect inRect)
    {
        var listing = new Listing_Standard();
        listing.Begin(inRect);
        SettingsUi.Section(listing, "PhobosCombatExtendedPatches_SectionKeepGuns".Translate());
        listing.CheckboxLabeled("PhobosCombatExtendedPatches_SettingKeepGuns".Translate(), ref Settings.keepGuns,
            "PhobosCombatExtendedPatches_SettingKeepGunsDesc".Translate());
        if (Settings.keepGuns)
        {
            listing.CheckboxLabeled("PhobosCombatExtendedPatches_SettingFetchSameKind".Translate(), ref Settings.fetchSameKind,
                "PhobosCombatExtendedPatches_SettingFetchSameKindDesc".Translate());
        }
        listing.CheckboxLabeled("PhobosCombatExtendedPatches_SettingFixSidearms".Translate(), ref Settings.fixSidearms,
            "PhobosCombatExtendedPatches_SettingFixSidearmsDesc".Translate());
        listing.Gap(6f);
        listing.Label("PhobosCombatExtendedPatches_KeepGunsWhy".Translate());
        if (!Settings.keepGuns) SettingsUi.Warning(listing, "PhobosCombatExtendedPatches_KeepGunsOffWarning".Translate());
        listing.End();
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
