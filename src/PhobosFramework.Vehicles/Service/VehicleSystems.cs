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
            // A trailer (Framework 0.101.0) has no engine, seats or crew. Since Framework 0.133.0 it takes a loadout of its own (the selector's
            // trailer switch keeps every other loadout off it): its running gear wears as it is towed, so it has our Health tab and repairs.
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
        finally
        {
            // Spall inside vehicles (Framework 0.207.0): every design's outer plates, loadout or not (owner, 21 Nov 2026: every vehicle). Last,
            // so the outer parts our loadout appended get it too (Framework 0.209.0, fault-read of 21 Nov 2026).
            try
            {
                Spall.AddReactors(def);
            }
            catch (System.Exception error)
            {
                Problems.Add(def.defName + ": spall: " + error.Message);
            }
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
            trailer = LoadoutSelector.IsTrailer(def),
        };
        // Framework 0.359.0: a design that dives takes nothing marked for other boats only (submarines are on the back burner).
        bool submersible = def.GetCompProperties<CompProperties_PhobosSubmersible>() != null;
        foreach (LoadoutSystem entry in loadout.systems)
        {
            if (entry.def == null || (submersible && entry.def.notOnSubmersibles)) continue;
            var unknown = new List<string>();
            Dictionary<string, double> parameters = ParameterRule.Resolve(entry.def.parameters.AsDoubles(),
                entry.set?.AsDoubles() ?? Enumerable.Empty<KeyValuePair<string, double>>(), unknown);
            foreach (string name in unknown) Problems.Add(loadout.defName + ": " + entry.def.defName + " has no parameter " + name);
            // The avionics system reads what the aircraft carries from the loadout's fit (Framework 0.110.0).
            if (entry.def.takesAvionicsFit && loadout.avionics != null)
            {
                foreach ((string name, double value) in AvionicsRule.Parameters(loadout.avionics.Values)) parameters[name] = value;
            }
            props.systems.Add(new ResolvedSystem(entry.def, parameters));
        }
        // Framework 0.300.0: a system brings those it always comes with (the gas turbine its compressor wash), once, after the loadout's own,
        // so every loadout that names it has them, whether it inherits its list or writes its own.
        for (int i = 0; i < props.systems.Count; i++)
        {
            foreach (PhobosVehicleSystemDef with in props.systems[i].def.comesWith)
            {
                if (with == null || props.systems.Any(s => s.def == with) || (submersible && with.notOnSubmersibles)) continue;
                props.systems.Add(new ResolvedSystem(with, ParameterRule.Resolve(with.parameters.AsDoubles(),
                    Enumerable.Empty<KeyValuePair<string, double>>(), new List<string>())));
            }
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
            if (submersible && role.notOnSubmersibles) continue;
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
        // Parts added one for each part of another role (Framework 0.250.0, brakes at each wheel): the first goes here, the rest last.
        var waiting = new List<(string role, PartToAdd add, List<string> targets)>();
        // Framework 0.258.0: parts added to designs that saves already hold, appended after every other part we add.
        var late = new List<PartRoleSpec>();
        // Framework 0.366.0: parts appended at the very end, after the code's own (the turbocharger, each engine's pumps).
        var last = new List<PartRoleSpec>();
        foreach (PartRoleSpec role in pending)
        {
            PartToAdd add = role.add!;
            if (add.last)
            {
                last.Add(role);
                continue;
            }
            if (role.onlyWithRole != null) Problems.Add(loadout.defName + ": " + role.role + " names onlyWithRole but is not added last, so it is added everywhere");
            if (add.late)
            {
                late.Add(role);
                continue;
            }
            if (def.components.Any(p => p.key == add.key))
            {
                Problems.Add(def.defName + ": already has a part called " + add.key);
                continue;
            }
            List<string>? targets = EachPart.Targets(add, props);
            if (targets != null) waiting.Add((role.role, add, targets));
            VehicleComponentProperties part = targets != null ? MakePart(def, add, props, targets[0], EachPart.Label(def, add, targets[0])) : MakePart(def, add, props);
            def.components.Add(part);
            props.roleKeys[role.role] = new List<string> { add.key };
            props.addedKeys.Add(add.key);
            if (!add.countsTowardDestruction) props.uncountedKeys.Add(add.key);
            added++;
        }
        // Weapons stop when their mount is shot to pieces (Framework 0.115.0, T1): before the reactors, so an appended mount is a part like any other.
        props.weaponsTied = WeaponMounts.Apply(def, props);
        // A thrown track stops a tracked vehicle (M2).
        Mobility.Apply(def, props);
        // A tank's ammunition rack can blow its turret off (T8; after the mounts, which the reaction needs).
        TurretPopping.Apply(def, props);
        if (props.addedKeys.Contains(WeaponMounts.PartKey)) added++;
        // A closed cab's glass (Framework 0.238.0, VG1): appended last, so the saved order of every earlier part stays as it was.
        Glass.Apply(def, props);
        if (props.addedKeys.Contains(Glass.Key)) added++;
        // The extinguisher's bracket and the suppression systems (Framework 0.241.0, FR10): appended after the glass, for the same reason.
        FireFighting.Apply(def, props);
        // A design another author's patch marks as lined gets the spall liner, born whole (Framework 0.244.0, the shared extension).
        Spall.ApplyMarked(def, props);
        if (props.addedKeys.Contains(Spall.LinerKey)) added++;
        if (props.addedKeys.Contains(FireFighting.BracketKey)) added++;
        if (props.addedKeys.Contains(FireFighting.EngineSystemKey)) added++;
        if (props.addedKeys.Contains(FireFighting.CrewSystemKey)) added++;
        // The rest of the parts made one for each part of a role (Framework 0.250.0), after every other part we add, so saved indexes hold.
        added += EachPart.AppendWaiting(def, props, waiting);
        added += AppendWhole(def, props, late);
        // Framework 0.320.0: the turbocharger, on every design with a piston engine, after every other part we add so saved indexes hold.
        Turbochargers.Apply(def, props);
        if (props.addedKeys.Contains(Turbochargers.Key)) added++;
        // Framework 0.360.0 (the vehicle standard, batch 3): a part for each engine where a system asks for one (an aircraft's fuel and oil
        // pumps), after every other part we add so saved indexes hold, and before the numbered roles roll up.
        added += EnginePumps.Apply(def, props);
        // Framework 0.366.0 (the vehicle standard, batch 4): parts marked last, at the very end, so a saved vehicle keeps every earlier part
        // where it was. Every role is bound by now, so a part that asks for another role (onlyWithRole) can be judged.
        added += AppendWhole(def, props, last.Where(r => r.onlyWithRole == null || props.roleKeys.ContainsKey(r.onlyWithRole)));
        // Framework 0.368.0 (the vehicle standard's batch 5, VS11; the owner's cards of 28 Nov 2026): a military vehicle goes without the
        // civilian bays. They were made in their places above, so every stage ran as before, and are taken out now with their places recorded
        // for saves (PartsOnLoad).
        if (VehicleFittings.CountsAsMilitary(def, loadout)) added -= TakeOut(def, props, loadout.partRoles.Where(r => r.notOnMilitary));
        // Framework 0.256.0: numbered roles roll up into their base role (Engine1 to EngineN are "the engines"), before the reactors are set.
        SystemParts.RollUpFamilies(props);
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
        // EMP (Framework 0.109.0): the electrical parts get Vehicle Framework's EMP rating by the vehicle's hardening.
        VehicleEmp.Rate(def, loadout, props);
        def.comps ??= new List<CompProperties>();
        def.comps.Add(props);
        // The work store (Framework 0.272.0): supplies for its services, repairs and fittings are laid out beside it, never in its cargo.
        if (!def.comps.Any(c => c is CompProperties_PhobosWorkStore)) def.comps.Add(new CompProperties_PhobosWorkStore());
        // Sling loads (Framework 0.92.0): a loadout that rates a cargo hook gives the helicopter its sling, used once a hook is fitted.
        if (loadout.sling != null && !slingStandsDown && !def.comps.Any(c => c is CompProperties_PhobosSling)) def.comps.Add(new CompProperties_PhobosSling());
        // Countermeasures (Framework 0.180.0): an aircraft whose avionics carry chaff and flare dispensers gets their magazine, which haulers
        // top up, instead of firing cartridges from its cargo.
        if (loadout.avionics is { countermeasures: true } fit && !def.comps.Any(c => c is CompProperties_PhobosDispensers))
        {
            def.comps.Add(new CompProperties_PhobosDispensers { capacity = fit.countermeasureCartridges });
        }
        // The litter kit (Framework 0.101.0): a loadout that names a cabin gives the vehicle its litters, used once the kit is fitted there.
        if (props.roleKeys.ContainsKey(LitterRoles.Cabin) && !def.comps.Any(c => c is CompProperties_PhobosLitter)) def.comps.Add(new CompProperties_PhobosLitter());
        // Working without a crew (Framework 0.281.0; autonomous-vehicles.md, step 7a): a design with the route kit's sensor mount gets the
        // remote control comp and the route comp, off until a kit is fitted, so the route level needs no other mod.
        // The mechanitor level (Framework 0.282.0, step 7b): its link goes in the control radio bay.
        if (props.roleKeys.ContainsKey("RemoteKitMount"))
        {
            if (!def.comps.Any(c => c is CompProperties_PhobosRemoteControl)) def.comps.Add(new CompProperties_PhobosRemoteControl());
            if (!def.comps.Any(c => c is CompProperties_PhobosMechLink)) def.comps.Add(new CompProperties_PhobosMechLink());
        }
        if (props.roleKeys.ContainsKey("RouteKitMount"))
        {
            if (!def.comps.Any(c => c is CompProperties_PhobosRemoteControl)) def.comps.Add(new CompProperties_PhobosRemoteControl());
            if (!def.comps.Any(c => c is CompProperties_PhobosRoute)) def.comps.Add(new CompProperties_PhobosRoute());
            // Supply runs on the world map (Framework 0.329.0): land vehicles that can take the route kit.
            if (def.type == VehicleType.Land && !def.comps.Any(c => c is CompProperties_PhobosSupplyRun)) def.comps.Add(new CompProperties_PhobosSupplyRun());
        }
        // Rounds between places (Framework 0.307.0; autonomous-vehicles.md, step 9): every aircraft on our flight that flies on the map.
        if (def.comps.Any(c => c is CompProperties_PhobosFlight { onlyInDrops: false }) && !def.comps.Any(c => c is CompProperties_PhobosRound))
            def.comps.Add(new CompProperties_PhobosRound());
        // Smoke dischargers (Framework 0.319.0): a vehicle whose own turrets or upgrade tree can carry ours pops smoke when hit under threat.
        if (SmokeDischargers.Carries(def) && !def.comps.Any(c => c is CompProperties_PhobosSmoke)) def.comps.Add(new CompProperties_PhobosSmoke());
        // The hitch (Framework 0.101.0): every land vehicle with a loadout can tow a trailer, unless its loadout says not; whether it may tow
        // now (our drivetrain, or the player's setting for others) is asked when the player orders it.
        // A trailer never gets one, whatever its loadout says: nothing tows a trailer behind a trailer.
        if (def.type == VehicleType.Land && !loadout.noHitch && !props.trailer && !def.comps.Any(c => c is CompProperties_PhobosHitch)) def.comps.Add(new CompProperties_PhobosHitch());
        // Bay trips (Framework 0.201.0; docs/development/vehicle-bay-trips.md): every land vehicle with a loadout, trailers apart, can be taken
        // to a dyno bay for repair without being told. Whether it goes is the player's setting and its own switch, asked at each look.
        if (def.type == VehicleType.Land && !props.trailer && !def.comps.Any(c => c is CompProperties_PhobosBayTrip)) def.comps.Add(new CompProperties_PhobosBayTrip());
        // The camp order (Framework 0.212.0; docs/development/vehicle-accessories.md, Y3): every land vehicle with a loadout, trailers apart, can set
        // out a camp of its fitted awning and roof tent and the swags and bedrolls in its cargo. It offers its buttons only when it has any.
        // A trailer whose deck or side takes camp gear has it too (Framework 0.216.0, the camper trailer; 0.217.0, tank trailers and the
        // kitchen): its camp is pitched once it stands unhitched.
        if (def.type == VehicleType.Land && (!props.trailer || LoadoutSelector.TrailerCamps(def)) && !def.comps.Any(c => c is CompProperties_PhobosCamp)) def.comps.Add(new CompProperties_PhobosCamp());
        // A flat deck takes a deck tent (Framework 0.217.0): a walk-in room on the game's pocket maps, there only while the tent is fitted.
        // A vehicle with a cabin of its own keeps that one.
        if (def.GetModExtension<PhobosFlatDeckExtension>() is { } deck && !def.comps.Any(c => c is CompProperties_PhobosVehicleCabin))
        {
            var tent = new CompProperties_PhobosVehicleCabin { deckTent = true };
            tent.layouts.Add(new CabinLayoutSpec { whenGranted = { DeckTents.Ability }, rows = deck.rows });
            def.comps.Add(tent);
        }
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
        // A trailer has no driver, engine or gauges, so no Dashboard (Framework 0.133.0); its Systems tab lists its tyres' wear and service.
        if (!props.trailer && !def.inspectorTabs.Contains(typeof(ITab_VehicleDashboard))) def.inspectorTabs.Add(typeof(ITab_VehicleDashboard));
        if (!def.inspectorTabs.Contains(typeof(ITab_VehicleSystems))) def.inspectorTabs.Add(typeof(ITab_VehicleSystems));
        // The logbook (Framework 0.181.0, EM23): the core's tab, shown for the player's vehicles.
        if (!def.inspectorTabs.Contains(typeof(Phobos.Framework.Interface.ITab_PhobosLogbook))) def.inspectorTabs.Add(typeof(Phobos.Framework.Interface.ITab_PhobosLogbook));
        return added;
    }

    /// <summary>
    /// Takes out the parts we added for these roles (never a design's own part), recording where each stood in the full list and the layout that
    /// took it out (<see cref="PartLayouts.Current"/>), and unbinds the roles. Returns how many went.
    /// </summary>
    static int TakeOut(VehicleDef def, CompProperties_PhobosVehicleSystems props, IEnumerable<PartRoleSpec> roles)
    {
        var keys = new HashSet<string>();
        var unbind = new List<string>();
        foreach (PartRoleSpec role in roles)
        {
            if (role.add == null || !props.addedKeys.Contains(role.add.key)) continue;
            keys.Add(role.add.key);
            unbind.Add(role.role);
        }
        if (keys.Count == 0) return 0;
        List<int> places = Phobos.Framework.Vehicles.Rules.PartLayoutRule.PlacesOf(def.components.Select(p => p.key).ToList(), keys);
        foreach (int place in places) props.removedParts.Add(new RemovedPart { index = place, key = def.components[place].key, layout = PartLayouts.Current });
        for (int i = places.Count - 1; i >= 0; i--) def.components.RemoveAt(places[i]);
        foreach (string role in unbind) props.roleKeys.Remove(role);
        props.addedKeys.RemoveAll(keys.Contains);
        props.uncountedKeys.ExceptWith(keys);
        return places.Count;
    }

    /// <summary>Appends one whole part for each of these roles, in their order (the late and the last stages). Returns how many were added.</summary>
    static int AppendWhole(VehicleDef def, CompProperties_PhobosVehicleSystems props, IEnumerable<PartRoleSpec> roles)
    {
        int added = 0;
        foreach (PartRoleSpec role in roles)
        {
            PartToAdd add = role.add!;
            if (def.components.Any(p => p.key == add.key))
            {
                Problems.Add(def.defName + ": already has a part called " + add.key);
                continue;
            }
            def.components.Add(MakePart(def, add, props));
            props.roleKeys[role.role] = new List<string> { add.key };
            props.addedKeys.Add(add.key);
            if (!add.countsTowardDestruction) props.uncountedKeys.Add(add.key);
            added++;
        }
        return added;
    }

    /// <param name="referenceKey">The part whose cells (and armour) to take, in place of the first part of the add's cellsOf role (eachOf).</param>
    /// <param name="label">The part's label, in place of the add's (eachOf).</param>
    /// <param name="key">The part's key, in place of the add's (eachOf).</param>
    internal static VehicleComponentProperties MakePart(VehicleDef def, PartToAdd add, CompProperties_PhobosVehicleSystems props, string? referenceKey = null,
        string? label = null, string? key = null)
    {
        var hitbox = new ComponentHitbox();
        VehicleComponentProperties? reference = null;
        // The design may move parts placed by one role's cells to another's (Framework 0.166.0, the loadout's cellsOfRemap).
        string? cellsOf = add.cellsOf;
        if (cellsOf != null && props.loadout?.cellsOfRemap is { Count: > 0 } remaps)
        {
            foreach (CellsOfRemap remap in remaps)
            {
                if (remap.from == cellsOf) { cellsOf = remap.to; break; }
            }
        }
        if (referenceKey != null)
        {
            reference = def.components.FirstOrDefault(p => p.key == referenceKey);
        }
        else if (cellsOf != null && props.roleKeys.TryGetValue(cellsOf, out List<string>? keys) && keys.Count > 0)
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
            key = key ?? add.key,
            label = label ?? add.label,
            health = add.health,
            // A design armoured by class keeps one outer part to a cell (Framework 0.154.0; vehicle-armour.md): what we append goes inside it,
            // or Combat Extended would make a shot beat our bracket's armour on top of the design's own.
            depth = def.GetModExtension<PhobosArmourExtension>() is { appendedPartsInside: true } ? VehicleComponent.VehiclePartDepth.Internal : add.depth,
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
