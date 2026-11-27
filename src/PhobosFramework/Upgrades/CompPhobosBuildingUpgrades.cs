using System.Collections.Generic;
using System.Linq;
using System.Text;
using Phobos.Framework.Environment;
using Phobos.Framework.Rules;
using RimWorld;
using UnityEngine;
using Verse;

namespace Phobos.Framework.Upgrades;

/// <summary>Lets a placed building take upgrades (<see cref="PhobosBuildingUpgradeDef"/>s that list it in their <c>buildings</c>): the
/// player orders one, and a colonist brings its materials and fits it, or takes it off again. Goes on the building's ThingDef with no
/// fields; the building's own comps then read the abilities and figures the fitted upgrades give (the depot bay's faster pump, the fuel
/// pump's charging cable).</summary>
public class CompProperties_PhobosBuildingUpgrades : CompProperties
{
    public CompProperties_PhobosBuildingUpgrades() => compClass = typeof(CompPhobosBuildingUpgrades);
}

/// <summary>
/// Which upgrades a building has fitted, and the one piece of work under way on it: an upgrade being fitted, with the materials brought so
/// far, or one being taken off (D'thorga Framework 0.210.0; <see cref="PhobosBuildingUpgradeDef"/>). Its own comps ask "is this fitted" and
/// "what does this figure read" from a kept list, so nothing is worked out each tick. Buttons and the window read this and call
/// <see cref="BuildingUpgrades"/>; nothing here changes the building's state but through it.
/// </summary>
public class CompPhobosBuildingUpgrades : ThingComp
{
    List<PhobosBuildingUpgradeDef> fitted = new();
    PhobosBuildingUpgradeDef? ordered;
    bool removing;
    float workDone;
    /// <summary>The materials brought for the order, by kind: held as counts, the things themselves used up as they arrive.</summary>
    Dictionary<ThingDef, int> delivered = new();

    /// <summary>The abilities the fitted upgrades give. Derived, never saved.</summary>
    readonly HashSet<string> grants = new();

    /// <summary>The figures the fitted upgrades set, each at the largest any of them sets (0.275.0). Derived, never saved.</summary>
    readonly Dictionary<string, float> figures = new();

    /// <summary>
    /// Sent to every comp on the building, through the game's own <c>ReceiveCompSignal</c>, when an upgrade is fitted or taken off
    /// (0.275.0), so a comp that works figures out from the upgrades knows to work them out again.
    /// </summary>
    public const string ChangedSignal = "PhobosBuildingUpgradesChanged";

    public IReadOnlyList<PhobosBuildingUpgradeDef> Fitted => fitted;
    public PhobosBuildingUpgradeDef? Ordered => ordered;
    public bool Removing => removing;
    public float WorkDone => workDone;
    public List<PhobosBuildingUpgradeDef> Available => PhobosBuildingUpgradeDef.For(parent.def);

    public bool Has(PhobosBuildingUpgradeDef def) => fitted.Contains(def);

    /// <summary>Whether a fitted upgrade gives this ability.</summary>
    public bool Grants(string ability) => grants.Contains(ability);

    /// <summary>
    /// What a figure reads on this building: set by a fitted upgrade, else the building's own. Read from a kept table, so it costs a
    /// lookup and allocates nothing (0.275.0; it walked every fitted upgrade before).
    /// </summary>
    public float Figure(string name, float fallback) => figures.TryGetValue(name, out float value) ? value : fallback;

    public int Delivered(ThingDef what) => delivered.TryGetValue(what, out int count) ? count : 0;

    /// <summary>What is still to be brought for the order; nothing for a removal or no order.</summary>
    public List<(ThingDef what, int count)> Missing() =>
        ordered == null || removing ? new List<(ThingDef, int)>() : BuildingUpgradeRule.Missing(ordered.Cost, Delivered);

    /// <summary>The work the order takes in all.</summary>
    public float TotalWork => ordered == null ? 0f : removing ? BuildingUpgradeRule.RemoveWork(ordered.work, ordered.removeWorkShare) : ordered.work;

    public float Progress => BuildingUpgradeRule.Progress(workDone, TotalWork);

    /// <summary>An order with everything brought, ready for a colonist to work.</summary>
    public bool ReadyForWork => ordered != null && Missing().Count == 0;

    // ---- Changed only through BuildingUpgrades ----

    internal void SetOrder(PhobosBuildingUpgradeDef def, bool remove)
    {
        ordered = def;
        removing = remove;
        workDone = 0f;
    }

    /// <summary>Ends the order; returns what had been brought, for the service to put down.</summary>
    internal Dictionary<ThingDef, int> ClearOrder()
    {
        Dictionary<ThingDef, int> brought = delivered;
        delivered = new Dictionary<ThingDef, int>();
        ordered = null;
        removing = false;
        workDone = 0f;
        return brought;
    }

    internal void AddDelivered(ThingDef what, int count) => delivered[what] = Delivered(what) + count;

    internal void AddWork(float amount) => workDone += amount;

    internal void SetFitted(PhobosBuildingUpgradeDef def, bool on)
    {
        if (on && !fitted.Contains(def)) fitted.Add(def);
        if (!on) fitted.Remove(def);
        RebuildGrants();
        parent.BroadcastCompSignal(ChangedSignal);
    }

    void RebuildGrants()
    {
        grants.Clear();
        figures.Clear();
        foreach (PhobosBuildingUpgradeDef def in fitted)
        {
            foreach (string ability in def.grants) grants.Add(ability);
        }
        // The rule decides what several upgrades setting one figure come to; the table only keeps its answer.
        foreach (string name in fitted.SelectMany(def => def.figures).Select(f => f.name).Distinct())
        {
            figures[name] = BuildingUpgradeRule.Figure(fitted.SelectMany(def => def.figures).Where(f => f.name == name).Select(f => f.value), 0f);
        }
    }

    // ---- The map, the save and the screen ----

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        RebuildGrants();
        MapRegistry<CompPhobosBuildingUpgrades>.Add(parent.Map, this);
    }

    public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
    {
        base.PostDeSpawn(map, mode);
        MapRegistry<CompPhobosBuildingUpgrades>.Remove(map, this);
        // Taken down, moved or destroyed with work under way: what was brought for it is put down where the building stood. Fitted
        // upgrades stay with the building, so a building moved keeps them.
        if (ordered != null) BuildingUpgrades.PutDown(ClearOrder(), parent.Position, map);
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Collections.Look(ref fitted, "PhobosUpgradesFitted", LookMode.Def);
        Scribe_Defs.Look(ref ordered, "PhobosUpgradesOrdered");
        Scribe_Values.Look(ref removing, "PhobosUpgradesRemoving");
        Scribe_Values.Look(ref workDone, "PhobosUpgradesWorkDone");
        Scribe_Collections.Look(ref delivered, "PhobosUpgradesDelivered", LookMode.Def, LookMode.Value);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            // An upgrade whose Def has gone (its mod removed) is dropped quietly: there is nothing left to say what it did.
            fitted ??= new List<PhobosBuildingUpgradeDef>();
            fitted.RemoveAll(def => def == null);
            delivered ??= new Dictionary<ThingDef, int>();
            delivered.RemoveAll(pair => pair.Key == null);
            RebuildGrants();
        }
    }

    public override void PostDraw()
    {
        base.PostDraw();
        for (int i = 0; i < fitted.Count; i++)
        {
            if (fitted[i].OverlayGraphic is not { } graphic) continue;
            Vector3 at = parent.DrawPos;
            at.y += fitted[i].overlayAltitude;
            graphic.Draw(at, parent.Rotation, parent);
        }
    }

    public override string? CompInspectStringExtra()
    {
        if (Available.Count == 0) return null;
        var text = new StringBuilder();
        if (fitted.Count > 0) text.Append("PhobosUpgrade_InspectFitted".Translate(fitted.Select(d => d.label).ToCommaList(useAnd: true).Named("UPGRADES")).Resolve());
        if (ordered != null)
        {
            if (text.Length > 0) text.AppendLine();
            text.Append(BuildingUpgrades.OrderLine(this));
        }
        return text.Length == 0 ? null : text.ToString();
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        foreach (Gizmo gizmo in base.CompGetGizmosExtra()) yield return gizmo;
        if (parent.Faction != Faction.OfPlayer || Available.Count == 0) yield break;
        yield return new Command_Action
        {
            defaultLabel = "PhobosUpgrade_Button".Translate(),
            defaultDesc = "PhobosUpgrade_ButtonDesc".Translate(),
            icon = BuildingUpgrades.Icon,
            action = () => Find.WindowStack.Add(new Dialog_PhobosBuildingUpgrades(this)),
        };
    }
}
