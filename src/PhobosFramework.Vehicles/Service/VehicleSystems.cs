using System.Collections.Generic;
using System.Linq;
using Phobos.Framework;
using Phobos.Framework.Rules;
using Phobos.Framework.Vehicles.Rules;
using RimWorld;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>What happened to one vehicle design while Defs loaded, for the settings window and the load line.</summary>
public sealed class DesignRecord
{
    public VehicleDef def = null!;
    public string packageId = "";
    public string modName = "";
    public PhobosVehicleLoadoutDef? loadout;
    public string? skippedBecause;
    public int partsAdded;
    public bool tied;
}

/// <summary>
/// The service: picks each vehicle design's loadout, appends its parts and its comp before Vehicle Framework resolves the design
/// (so Vehicle Framework builds the hitboxes and sees the comp), and sweeps the parts out of a running game before removal.
/// Presentation-free: the settings window and dev actions only call it.
/// </summary>
public static class VehicleSystems
{
    static readonly HashSet<VehicleDef> processed = new();
    static bool statPartsAdded;

    public static readonly List<DesignRecord> Designs = new();
    public static readonly List<string> Problems = new();

    /// <summary>Set by the uninstall sweep: the service stops for the rest of the session.</summary>
    public static bool Swept { get; private set; }

    /// <summary>Called from the prefix on VehicleDef.ResolveReferences, once per design.</summary>
    public static void Inject(VehicleDef def)
    {
        if (!processed.Add(def)) return;
        AddStatParts();
        var record = new DesignRecord
        {
            def = def,
            packageId = def.modContentPack?.PackageId ?? "",
            modName = def.modContentPack?.Name ?? "",
        };
        Designs.Add(record);
        try
        {
            PhobosVehicleLoadoutDef? loadout = Choose(def, out bool tied);
            record.loadout = loadout;
            record.tied = tied;
            if (loadout == null) { record.skippedBecause = "no loadout matches"; return; }
            if (loadout.systems.Count == 0) { record.skippedBecause = "its loadout has no systems"; return; }
            VehicleSystemsSettings settings = VehicleSystemsMod.Settings;
            if (settings.scope == VehicleSystemsSettings.Scope.PhobosOnly && !record.packageId.StartsWith("phobosdthorga.", System.StringComparison.OrdinalIgnoreCase))
            {
                record.skippedBecause = "setting: D'thorga vehicles only";
                return;
            }
            if (settings.disabledPackages.Contains(record.packageId.ToLowerInvariant())) { record.skippedBecause = "setting: this mod's vehicles off"; return; }
            if (settings.disabledKinds.Contains(loadout.kind)) { record.skippedBecause = "setting: " + loadout.kind + " off"; return; }
            record.partsAdded = Apply(def, loadout);
        }
        catch (System.Exception error)
        {
            record.skippedBecause = "error";
            Problems.Add(def.defName + ": " + error.Message);
            Log.Error(VehicleSystemsMod.LogPrefix + " Could not give " + def.defName + " its systems; it runs without them. " + error);
        }
    }

    /// <summary>The loadout a design gets: the highest priority match, the last loaded among equals.</summary>
    public static PhobosVehicleLoadoutDef? Choose(VehicleDef def, out bool tied)
    {
        var matches = new List<PhobosVehicleLoadoutDef>();
        foreach (PhobosVehicleLoadoutDef loadout in DefDatabase<PhobosVehicleLoadoutDef>.AllDefsListForReading)
        {
            if (loadout.selector.Matches(def)) matches.Add(loadout);
        }
        int pick = LoadoutChoice.Pick(matches.Select(m => m.priority).ToList(), matches.Select(m => m.rank).ToList(), out tied);
        return pick < 0 ? null : matches[pick];
    }

    static int Apply(VehicleDef def, PhobosVehicleLoadoutDef loadout)
    {
        def.components ??= new List<VehicleComponentProperties>();
        var props = new CompProperties_PhobosVehicleSystems
        {
            loadout = loadout,
            originalPartCount = def.components.Count,
            drivetrain = loadout.drivetrain,
            flightProfile = loadout.flightProfile,
        };
        foreach (LoadoutSystem entry in loadout.systems)
        {
            if (entry.def == null) continue;
            var unknown = new List<string>();
            Dictionary<string, double> parameters = ParameterRule.Resolve(entry.def.parameters.AsDoubles(),
                entry.set?.AsDoubles() ?? Enumerable.Empty<KeyValuePair<string, double>>(), unknown);
            foreach (string name in unknown) Problems.Add(loadout.defName + ": " + entry.def.defName + " has no parameter " + name);
            props.systems.Add(new ResolvedSystem(entry.def, parameters));
        }
        Tuning.Fill(props, def);
        int added = 0;
        // Bind every role to the design's own parts first, so an appended part's "cells of" can name any found role.
        var pending = new List<PartRoleSpec>();
        bool slingStandsDown = loadout.sling?.StandsDown == true;
        foreach (PartRoleSpec role in loadout.partRoles)
        {
            // Sling loads stand down for a mod with its own airlift on this aircraft: no hook mount to fit a hook to.
            if (slingStandsDown && role.role == SlingRoles.HookMount) continue;
            // The first finder that matches anything decides: its first match, or every match for a role marked "all".
            var keys = new List<string>();
            foreach (PartFinder finder in role.find)
            {
                foreach (VehicleComponentProperties part in def.components.Take(props.originalPartCount))
                {
                    if (!finder.Matches(part) || keys.Contains(part.key)) continue;
                    keys.Add(part.key);
                    if (!role.all) break;
                }
                if (keys.Count > 0) break;
            }
            if (keys.Count > 0) props.roleKeys[role.role] = keys;
            else if (role.add != null) pending.Add(role);
        }
        foreach (PartRoleSpec role in pending)
        {
            PartToAdd add = role.add!;
            if (def.components.Any(p => p.key == add.key))
            {
                Problems.Add(def.defName + ": already has a part called " + add.key);
                continue;
            }
            VehicleComponentProperties part = MakePart(def, add, props);
            def.components.Add(part);
            props.roleKeys[role.role] = new List<string> { add.key };
            props.addedKeys.Add(add.key);
            added++;
        }
        // Every bound part reports its hits to the systems (a holed radiator loses coolant).
        foreach (KeyValuePair<string, List<string>> pair in props.roleKeys)
        {
            foreach (string key in pair.Value)
            {
                VehicleComponentProperties? part = def.components.FirstOrDefault(p => p.key == key);
                if (part == null) continue;
                part.reactors ??= new List<Reactor>();
                if (!part.reactors.Any(r => r is Reactor_PhobosVehicleSystems reactor && reactor.role == pair.Key))
                {
                    part.reactors.Add(new Reactor_PhobosVehicleSystems { role = pair.Key });
                }
            }
        }
        def.comps ??= new List<CompProperties>();
        def.comps.Add(props);
        // Sling loads (Framework 0.92.0): a loadout that rates a cargo hook gives the helicopter its sling, used once a hook is fitted.
        if (loadout.sling != null && !slingStandsDown && !def.comps.Any(c => c is CompProperties_PhobosSling)) def.comps.Add(new CompProperties_PhobosSling());
        // The Dashboard and Systems tabs, before ThingDef.ResolveReferences resolves the tab list (this runs in its prefix; re-check the
        // order when Vehicle Framework releases 1.7). Only fitted designs get them.
        def.inspectorTabs ??= new List<System.Type>();
        // Our Health tab takes Vehicle Framework's slot (docs/development/vehicle-health-design.md), unless the player keeps theirs.
        if (!VehicleSystemsMod.Settings.keepVehicleFrameworkHealthTab && !def.inspectorTabs.Contains(typeof(ITab_PhobosVehicleHealth)))
        {
            int slot = def.inspectorTabs.IndexOf(typeof(ITab_Vehicle_Health));
            if (slot >= 0) def.inspectorTabs[slot] = typeof(ITab_PhobosVehicleHealth);
            else def.inspectorTabs.Add(typeof(ITab_PhobosVehicleHealth));
        }
        if (!def.inspectorTabs.Contains(typeof(ITab_VehicleDashboard))) def.inspectorTabs.Add(typeof(ITab_VehicleDashboard));
        if (!def.inspectorTabs.Contains(typeof(ITab_VehicleSystems))) def.inspectorTabs.Add(typeof(ITab_VehicleSystems));
        return added;
    }

    static VehicleComponentProperties MakePart(VehicleDef def, PartToAdd add, CompProperties_PhobosVehicleSystems props)
    {
        var hitbox = new ComponentHitbox();
        VehicleComponentProperties? reference = null;
        if (add.cellsOf != null && props.roleKeys.TryGetValue(add.cellsOf, out List<string>? keys) && keys.Count > 0)
        {
            reference = def.components.FirstOrDefault(p => p.key == keys[0]);
        }
        if (add.cells != null)
        {
            hitbox.cells = new List<IntVec2>(add.cells);
        }
        else if (reference?.hitbox != null)
        {
            hitbox.cells = new List<IntVec2>(reference.hitbox.cells ?? new List<IntVec2>());
            hitbox.side = reference.hitbox.side;
            hitbox.from = reference.hitbox.from;
            hitbox.to = reference.hitbox.to;
        }
        return new VehicleComponentProperties
        {
            key = add.key,
            label = add.label,
            health = add.health,
            depth = add.depth,
            // Combat Extended sorts parts the other way round (a smaller weight is hit more): keep the intent under both.
            hitWeight = (float)CombatExtendedRules.HitWeight(add.hitWeight, add.hitWeightCE, OptionalMods.IsActive(OptionalMods.CombatExtended)),
            efficiencyWeight = add.efficiencyWeight,
            hitbox = hitbox,
            categories = add.categories != null ? new List<VehicleStatDef>(add.categories) : new List<VehicleStatDef>(),
            armor = add.armor != null ? new List<StatModifier>(add.armor) : reference?.armor != null ? new List<StatModifier>(reference.armor) : null,
            tags = add.tags != null ? new List<string>(add.tags) : null,
        };
    }

    /// <summary>Puts the service's stat part on every Vehicle Framework stat, once, so a stage can slow a vehicle or the like.</summary>
    static void AddStatParts()
    {
        if (statPartsAdded) return;
        statPartsAdded = true;
        foreach (VehicleStatDef stat in DefDatabase<VehicleStatDef>.AllDefsListForReading)
        {
            stat.parts ??= new List<VehicleStatPart>();
            if (!stat.parts.Any(p => p is VehicleStatPart_PhobosSystems)) stat.parts.Add(new VehicleStatPart_PhobosSystems { statDef = stat });
        }
    }

    // ---- In a running game ----

    /// <summary>Every vehicle with the service's comp, anywhere: on maps, in caravans, among world pawns.</summary>
    public static IEnumerable<VehiclePawn> AllVehicles()
    {
        var seen = new HashSet<Pawn>();
        foreach (Pawn pawn in PawnsFinder.All_AliveOrDead)
        {
            if (seen.Add(pawn) && pawn is VehiclePawn vehicle && vehicle.GetComp<CompPhobosVehicleSystems>() != null) yield return vehicle;
        }
        if (Find.WorldPawns == null) yield break;
        foreach (Pawn pawn in Find.WorldPawns.AllPawnsAliveOrDead)
        {
            if (seen.Add(pawn) && pawn is VehiclePawn vehicle && vehicle.GetComp<CompPhobosVehicleSystems>() != null) yield return vehicle;
        }
    }

    /// <summary>
    /// Before removing the service: cuts every vehicle's parts back to its design's own count, forgets the systems' state and stops
    /// the service for the session. The player saves straight after. Returns how many vehicles changed.
    /// </summary>
    public static int Sweep()
    {
        int changed = 0;
        foreach (VehiclePawn vehicle in AllVehicles().ToList())
        {
            CompPhobosVehicleSystems comp = vehicle.GetComp<CompPhobosVehicleSystems>();
            comp.ClearForRemoval();
            if (PartsOnLoad.TrimTo(vehicle.statHandler, comp.Props.originalPartCount) > 0) changed++;
        }
        Swept = true;
        return changed;
    }

    /// <summary>The settings window's answer to "which mods' vehicles are affected", grouped.</summary>
    public static IEnumerable<IGrouping<string, DesignRecord>> DesignsByMod() =>
        Designs.Where(d => d.loadout != null && d.loadout.systems.Count > 0).GroupBy(d => d.packageId.ToLowerInvariant());
}
