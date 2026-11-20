using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Phobos.Framework.Interface;

/// <summary>
/// Marks an Architect tab as a sub-tab of another (Framework 0.205.0; docs/development/architect-tabs.md): it leaves the bar and is
/// opened from inside its parent. Put it on a DesignationCategoryDef. Each of our sub-tabs also carries Vanilla Gravship Expanded's own
/// marker (with MayRequire), so that while that mod is active it lists them down its side, as it does its gravship tabs; ours draws them
/// only when nothing else does (<see cref="ArchitectSubTabs"/>).
/// </summary>
public class PhobosNestedCategoryExtension : DefModExtension
{
    /// <summary>The tab this one opens from.</summary>
    public DesignationCategoryDef? parentCategory;
    /// <summary>The picture on the button that opens it.</summary>
    public string? iconTexPath;

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (string error in base.ConfigErrors()) yield return error;
        if (parentCategory == null) yield return "a nested Architect tab names no parentCategory";
    }
}

/// <summary>
/// Sub-tabs in the Architect (Framework 0.205.0; the owner's choice of 21 Nov 2026: one D'thorga tab on the bar, with Motor pool, Farming,
/// Medical and Radio inside it). <b>Who draws them</b>: Vanilla Gravship Expanded while it is active, from its own marker on the same
/// tabs; nobody where another mod replaces the Architect (Better Architect; Dubs Mint Menus, which lists every tab in its own way); and
/// this class otherwise, with the game's own buttons: the parent tab shows one button per sub-tab, a sub-tab shows a button back, and a
/// search in the parent finds what its sub-tabs hold. Its two patches stand down unless it is the one drawing, so two mods never redraw
/// the same panel. UI only: it changes nothing in the game itself.
/// </summary>
public static class ArchitectSubTabs
{
    const string GravshipPackageId = "vanillaexpanded.gravship";
    const string BetterArchitectPackageId = "ferny.BetterArchitect";
    const string MintMenusPackageId = "dubwise.dubsmintmenus";

    /// <summary>The game's own left arrow (UI/Widgets, read in its code), for the button back.</summary>
    const string BackIconPath = "UI/Widgets/ArrowLeft";

    /// <summary>Who lists sub-tabs inside their parents in this game.</summary>
    public enum Drawer
    {
        /// <summary>This class, with the game's own Architect.</summary>
        Ours,
        /// <summary>Vanilla Gravship Expanded, from its own marker on the same tabs.</summary>
        Gravship,
        /// <summary>Nobody: another mod draws the Architect, and each sub-tab is a tab of its own there.</summary>
        Nobody,
    }

    static Drawer? drawer;

    /// <summary>Who lists sub-tabs, worked out once from the active mods.</summary>
    public static Drawer WhoDraws => drawer ??= Decide();

    static Drawer Decide()
    {
        if (ModLister.AnyModActiveNoSuffix(new List<string> { BetterArchitectPackageId })) return Drawer.Nobody;
        bool gravship = ModLister.AnyModActiveNoSuffix(new List<string> { GravshipPackageId });
        if (gravship) return Drawer.Gravship;
        if (ModLister.AnyModActiveNoSuffix(new List<string> { MintMenusPackageId })) return Drawer.Nobody;
        return Drawer.Ours;
    }

    /// <summary>For the load line: who lists the sub-tabs, in words.</summary>
    public static string Describe() => WhoDraws switch
    {
        Drawer.Gravship => "Architect sub-tabs: listed by Vanilla Gravship Expanded",
        Drawer.Nobody => "Architect sub-tabs: shown as tabs of their own (another mod draws the Architect)",
        _ => "Architect sub-tabs: listed by D'thorga Framework",
    };

    // ---- Which tab holds which ----

    static Dictionary<DesignationCategoryDef, DesignationCategoryDef>? parents;
    static Dictionary<DesignationCategoryDef, List<DesignationCategoryDef>>? children;

    static void Index()
    {
        if (parents != null) return;
        parents = new Dictionary<DesignationCategoryDef, DesignationCategoryDef>();
        children = new Dictionary<DesignationCategoryDef, List<DesignationCategoryDef>>();
        foreach (DesignationCategoryDef def in DefDatabase<DesignationCategoryDef>.AllDefsListForReading)
        {
            DesignationCategoryDef? parent = def.GetModExtension<PhobosNestedCategoryExtension>()?.parentCategory;
            // One level only: a tab under a sub-tab, or under itself, stays on the bar.
            if (parent == null || parent == def || parent.GetModExtension<PhobosNestedCategoryExtension>() != null) continue;
            parents[def] = parent;
            if (!children.TryGetValue(parent, out List<DesignationCategoryDef>? list)) children[parent] = list = new List<DesignationCategoryDef>();
            list.Add(def);
        }
        foreach (List<DesignationCategoryDef> list in children.Values) list.Sort((a, b) => b.order != a.order ? b.order.CompareTo(a.order) : string.CompareOrdinal(a.defName, b.defName));
    }

    /// <summary>The tab this one opens from, or null for a tab on the bar.</summary>
    public static DesignationCategoryDef? ParentOf(DesignationCategoryDef def)
    {
        Index();
        return parents!.TryGetValue(def, out DesignationCategoryDef? parent) ? parent : null;
    }

    /// <summary>The sub-tabs of this tab, in their order; empty for none.</summary>
    public static IReadOnlyList<DesignationCategoryDef> SubTabsOf(DesignationCategoryDef def)
    {
        Index();
        return children!.TryGetValue(def, out List<DesignationCategoryDef>? list) ? list : (IReadOnlyList<DesignationCategoryDef>)System.Array.Empty<DesignationCategoryDef>();
    }

    // ---- The Architect window ----

    static readonly AccessTools.FieldRef<MainTabWindow_Architect, List<ArchitectCategoryTab>> DesPanels =
        AccessTools.FieldRefAccess<MainTabWindow_Architect, List<ArchitectCategoryTab>>("desPanelsCached");
    static readonly AccessTools.FieldRef<MainTabWindow_Architect, QuickSearchWidget> SearchWidget =
        AccessTools.FieldRefAccess<MainTabWindow_Architect, QuickSearchWidget>("quickSearchWidget");

    static MainTabWindow_Architect? Window => MainButtonDefOf.Architect.TabWindow as MainTabWindow_Architect;

    /// <summary>Whether the player is searching the Architect now: a parent then shows what its sub-tabs hold, so a search finds it.</summary>
    static bool Searching => Window is { } window && SearchWidget(window) is { } widget && widget.filter.Active;

    // The panels of sub-tabs, which are not on the bar, made once for each window.
    static MainTabWindow_Architect? panelsFor;
    static readonly Dictionary<DesignationCategoryDef, ArchitectCategoryTab> panels = new();

    /// <summary>Takes the sub-tabs off the bar. Called when the window lists its tabs.</summary>
    internal static void HideSubTabs(MainTabWindow_Architect window)
    {
        Index();
        DesPanels(window).RemoveAll(tab => parents!.ContainsKey(tab.def));
        panels.Clear();
        panelsFor = window;
    }

    /// <summary>Opens a tab: one on the bar, or a sub-tab, whose panel this class keeps. Says why not when nothing in it is available yet.</summary>
    public static void Open(DesignationCategoryDef def)
    {
        if (Window is not { } window) return;
        if (!def.Visible)
        {
            Messages.Message("NothingAvailableInCategory".Translate() + ": " + def.LabelCap, MessageTypeDefOf.RejectInput, historical: false);
            return;
        }
        ArchitectCategoryTab? tab = DesPanels(window).FirstOrDefault(t => t.def == def);
        if (tab == null)
        {
            if (panelsFor != window)
            {
                panels.Clear();
                panelsFor = window;
            }
            if (!panels.TryGetValue(def, out tab)) panels[def] = tab = new ArchitectCategoryTab(def, SearchWidget(window).filter);
        }
        window.selectedDesPanel = tab;
        SoundDefOf.ArchitectCategorySelect.PlayOneShotOnCamera();
        if (ParentOf(def) != null && Seen.Wants("framework.architect.sub-tab")) Seen.Note("framework.architect.sub-tab", null, def.defName);
    }

    // ---- The buttons a tab shows ----

    static readonly Dictionary<DesignationCategoryDef, Designator> openButtons = new();
    static readonly Dictionary<DesignationCategoryDef, Designator> backButtons = new();

    /// <summary>What a tab shows: a parent its sub-tabs' buttons first (or, while searching, what they hold); a sub-tab a button back first.</summary>
    internal static IEnumerable<Designator> WithSubTabs(DesignationCategoryDef def, IEnumerable<Designator> own)
    {
        Index();
        if (parents!.TryGetValue(def, out DesignationCategoryDef? parent)) return BackThen(def, parent, own);
        if (children!.TryGetValue(def, out List<DesignationCategoryDef>? subs)) return Searching ? OwnAndHeld(own, subs) : OpenersThen(subs, own);
        return own;
    }

    static IEnumerable<Designator> BackThen(DesignationCategoryDef def, DesignationCategoryDef parent, IEnumerable<Designator> own)
    {
        if (!backButtons.TryGetValue(def, out Designator? back)) backButtons[def] = back = new Designator_PhobosSubTabBack(parent);
        yield return back;
        foreach (Designator designator in own) yield return designator;
    }

    static IEnumerable<Designator> OpenersThen(List<DesignationCategoryDef> subs, IEnumerable<Designator> own)
    {
        foreach (DesignationCategoryDef sub in subs)
        {
            if (!openButtons.TryGetValue(sub, out Designator? open)) openButtons[sub] = open = new Designator_PhobosSubTab(sub);
            yield return open;
        }
        foreach (Designator designator in own) yield return designator;
    }

    static IEnumerable<Designator> OwnAndHeld(IEnumerable<Designator> own, List<DesignationCategoryDef> subs)
    {
        foreach (Designator designator in own) yield return designator;
        foreach (DesignationCategoryDef sub in subs)
        {
            foreach (Designator designator in sub.ResolvedAllowedDesignators)
            {
                // Its own button back, and the shared orders every tab has (cancel, deconstruct), are not shown twice.
                if (designator is Designator_PhobosSubTabBack || designator.isOrder) continue;
                yield return designator;
            }
        }
    }

    static readonly Dictionary<string, Texture2D> icons = new();

    /// <summary>A picture by its path, else a plain white square: never the game's red X.</summary>
    internal static Texture2D Icon(string? path)
    {
        if (path.NullOrEmpty()) return BaseContent.WhiteTex;
        if (!icons.TryGetValue(path!, out Texture2D? icon)) icons[path!] = icon = ContentFinder<Texture2D>.Get(path, reportFailure: false) ?? BaseContent.WhiteTex;
        return icon;
    }

    internal static Texture2D BackIcon => Icon(BackIconPath);
}

/// <summary>The button inside a parent tab that opens one of its sub-tabs (presentation: it changes which panel the Architect shows).</summary>
public class Designator_PhobosSubTab : Designator
{
    readonly DesignationCategoryDef sub;

    public Designator_PhobosSubTab(DesignationCategoryDef sub)
    {
        this.sub = sub;
        defaultLabel = sub.LabelCap;
        defaultDesc = "PhobosArchitect_OpenDesc".Translate(sub.LabelCap.Named("TAB"), (sub.description ?? "").Named("WHAT")).Resolve();
        icon = ArchitectSubTabs.Icon(sub.GetModExtension<PhobosNestedCategoryExtension>()?.iconTexPath);
        // Before every build button, which the game orders by their own uiOrder (2999 and up by default).
        Order = -100f;
    }

    public override AcceptanceReport CanDesignateCell(IntVec3 c) => false;

    public override void ProcessInput(Event ev) => ArchitectSubTabs.Open(sub);
}

/// <summary>The button inside a sub-tab that goes back to its parent tab.</summary>
public class Designator_PhobosSubTabBack : Designator
{
    readonly DesignationCategoryDef parent;

    public Designator_PhobosSubTabBack(DesignationCategoryDef parent)
    {
        this.parent = parent;
        defaultLabel = "PhobosArchitect_Back".Translate(parent.LabelCap.Named("TAB")).Resolve();
        defaultDesc = "PhobosArchitect_BackDesc".Translate(parent.LabelCap.Named("TAB")).Resolve();
        icon = ArchitectSubTabs.BackIcon;
        Order = -101f;
    }

    public override AcceptanceReport CanDesignateCell(IntVec3 c) => false;

    public override void ProcessInput(Event ev) => ArchitectSubTabs.Open(parent);
}

/// <summary>
/// Sub-tabs, patch one: the Architect lists its tabs without the sub-tabs. Reason for the patch: the window's list of tabs has no
/// extension point. A postfix; the game's method always runs. Applied only when this mod draws the sub-tabs (Vanilla Gravship
/// Expanded patches the same method for its own).
/// </summary>
[HarmonyPatch(typeof(MainTabWindow_Architect), "CacheDesPanels")]
public static class Patch_MainTabWindowArchitect_CacheDesPanels
{
    public static bool Prepare() => ArchitectSubTabs.WhoDraws == ArchitectSubTabs.Drawer.Ours;

    public static void Postfix(MainTabWindow_Architect __instance) => ArchitectSubTabs.HideSubTabs(__instance);
}

/// <summary>
/// Sub-tabs, patch two: the buttons a tab shows, through the one property the game's panel, its search and every Architect mod read.
/// A parent gains its sub-tabs' buttons, or while the player searches what its sub-tabs hold; a sub-tab gains a button back. Reason for
/// the patch: a tab's buttons have no extension point but their Defs' categories. A postfix; tabs with no sub-tabs are passed through.
/// Applied only when this mod draws the sub-tabs.
/// </summary>
[HarmonyPatch(typeof(DesignationCategoryDef), nameof(DesignationCategoryDef.ResolvedAllowedDesignators), MethodType.Getter)]
public static class Patch_DesignationCategoryDef_ResolvedAllowedDesignators
{
    public static bool Prepare() => ArchitectSubTabs.WhoDraws == ArchitectSubTabs.Drawer.Ours;

    public static void Postfix(DesignationCategoryDef __instance, ref IEnumerable<Designator> __result) =>
        __result = ArchitectSubTabs.WithSubTabs(__instance, __result);
}
