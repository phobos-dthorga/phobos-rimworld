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

    /// <summary>A colonist never fetches a sidearm their Combat Extended loadout would only drop again (Patches 0.29.1; the owner's choice of
    /// 21 Nov 2026, "the loadout wins"). On by default, agent default.</summary>
    public bool sidearmLoopGuard = true;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref keepGuns, "PhobosCombatExtendedPatches_KeepGuns", true);
        Scribe_Values.Look(ref fetchSameKind, "PhobosCombatExtendedPatches_FetchSameKind", true);
        Scribe_Values.Look(ref fixSidearms, "PhobosCombatExtendedPatches_FixSidearms", true);
        Scribe_Values.Look(ref sidearmLoopGuard, "PhobosCombatExtendedPatches_SidearmLoopGuard", true);
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
public class CombatExtendedPatchesMod : SettingsBookMod
{
    /// <summary>The Harmony ID is the packageId, shared by every part; never change it once the mod is published.</summary>
    public const string PackageId = "PhobosDthorga.Patches";
    public const string ModVersion = "0.34.0";
    public const string LogPrefix = "[D'thorga Patches]";
    public static readonly Phobos.Logging.PhobosLog Logger = Phobos.Framework.Logging.Logs.For("D'thorga Patches", "Combat Extended");
    const string AmmoFeature = "temporary guard: ammo set entries naming ammo that does not exist are dropped before Combat Extended's ammo setup";
    const string KeepGunsFeature = "soldiers go back to their own guns instead of fighting with grenades";
    const string SidearmsFeature = "Simple Sidearms keeps a colonist's gun, not a grenade, as the weapon it hands back";
    const string SidearmLoopFeature = "Simple Sidearms fetches no copy of a weapon that the loadout would only drop again";

    public static CombatExtendedPatchesSettings Settings { get; private set; } = new();

    public CombatExtendedPatchesMod(ModContentPack content) : base(content)
    {
        Settings = GetSettings<CombatExtendedPatchesSettings>();
        AddSettingsPages();
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
            if (!Settings.sidearmLoopGuard) report.Off(SidearmLoopFeature, "its setting is off, as the player chose");
            else if (KeepGuns.Patch_SimpleSidearms_RetrieveWeapon.Prepare()) report.On(SidearmLoopFeature);
            else report.Off(SidearmLoopFeature, "Simple Sidearms' or Combat Extended's code has changed");
        }
        report.Log();
    }


    static readonly SettingsBook SettingsPages = SettingsBook.For("PhobosDthorga.Patches", () => "PhobosPatches_SettingsBook".Translate());
    static readonly CombatExtendedPatchesSettings Defaults = new();
    protected override SettingsBook Book => SettingsPages;

    /// <summary>One D'thorga Patches entry with a page per patched mod (owner's choice of 20 Nov 2026, docs/development/settings-pages.md).</summary>
    void AddSettingsPages() =>
        SettingsPages.Join(this).Add(new SettingsPage("combat-extended", 10f, () => "PhobosCombatExtendedPatches_Page".Translate(), DrawSettings));

    static void DrawSettings(SettingsList list)
    {
        CombatExtendedPatchesSettings s = Settings, d = Defaults;
        list.Section("PhobosCombatExtendedPatches_SectionKeepGuns".Translate());
        list.Note("PhobosCombatExtendedPatches_KeepGunsWhy".Translate());
        list.Check("PhobosCombatExtendedPatches_SettingKeepGuns".Translate(), ref s.keepGuns, d.keepGuns, "PhobosCombatExtendedPatches_SettingKeepGunsDesc".Translate());
        list.Warning(s.keepGuns ? null : "PhobosCombatExtendedPatches_KeepGunsOffWarning".Translate().ToString());
        if (s.keepGuns)
        {
            list.Indent();
            list.Check("PhobosCombatExtendedPatches_SettingFetchSameKind".Translate(), ref s.fetchSameKind, d.fetchSameKind,
                "PhobosCombatExtendedPatches_SettingFetchSameKindDesc".Translate());
            list.Outdent();
        }
        list.Check("PhobosCombatExtendedPatches_SettingFixSidearms".Translate(), ref s.fixSidearms, d.fixSidearms,
            "PhobosCombatExtendedPatches_SettingFixSidearmsDesc".Translate());
        list.Check("PhobosCombatExtendedPatches_SettingSidearmLoopGuard".Translate(), ref s.sidearmLoopGuard, d.sidearmLoopGuard,
            "PhobosCombatExtendedPatches_SettingSidearmLoopGuardDesc".Translate());
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
