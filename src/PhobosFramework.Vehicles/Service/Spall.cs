using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using HarmonyLib;
using Phobos.Framework;
using Phobos.Framework.Defs;
using Phobos.Framework.Vehicles.Rules;
using RimWorld;
using UnityEngine;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// Spall inside vehicles (Framework 0.207.0; docs/development/vehicle-spall.md; owner, 21 Nov 2026). When a round beats a vehicle's outer
/// plate, debris from what the plate took sprays into the compartment behind it and may wound the crew sitting there or hole the inner parts
/// on those cells; a heavy hit the face stops can knock a little off the inside. A spall liner, a part of its own, catches a share and wears
/// as it does. Combat Extended only (owner: without it Vehicle Framework's armour is a chance, not a thickness), and only while its "Use New
/// Vehicle Armor" is on (Framework 0.209.0): the walk below is that setting's.
///
/// No Harmony: Combat Extended's armour step calls Vehicle Framework's reactors on every part it damages, so a reactor on each outer part is
/// told of every hit. What the plate took is worked out with Combat Extended's own armour function, called by name on copies of the hit's
/// figures; fragments on inner parts go through Combat Extended's own part step, so a part's armour counts and its reactors fire. Fragments on
/// people carry Combat Extended's own small fragment as their source, as its fragments do, so armour stops them by its own rule. Nothing per
/// tick; nothing saved but the liner's health, which is a part's health.
/// </summary>
public static class Spall
{
    /// <summary>The liner's part key, appended by our vehicles' loadouts in D'thorga Vehicles' Combat Extended folder.</summary>
    public const string LinerKey = "PhobosSpallLiner";

    const string DamageArmorMethod = "CombatExtended.Compatibility.VehiclesCompat.VehicleArmorPatch:DamageArmor";
    const string HitComponentMethod = "CombatExtended.Compatibility.VehiclesCompat.VehicleArmorPatch:HitComponent";
    const string CombatSettingsField = "CombatExtended.Controller:settings";
    const string NewVehicleArmourField = "CombatExtended.Settings:patchArmorDamage";
    const string FragmentProjectile = "Fragment_Small";

    delegate bool DamageArmorFn(bool isSharp, float armorAmount, ref float penAmount, ref float dmgAmount, out float armorDamage);
    delegate bool HitComponentFn(VehicleComponent component, ref DamageInfo dinfo, StringBuilder? report);

    static DamageArmorFn? damageArmor;
    static HitComponentFn? hitComponent;
    static FieldInfo? combatSettings;
    static FieldInfo? newVehicleArmour;
    static DamageDef? fragment;
    static ThingDef? fragmentSource;
    static bool looked;
    static bool ready;

    // A beaten part is reported twice in one step (its armour, then its bulk): the second report is the same hit.
    static VehicleComponent? pendingBulk;
    static int pendingBulkTick = -1;
    // One hit throws spall once: Combat Extended's walk goes on to meet the far plate from inside, which reports like an entry (Framework
    // 0.209.0, fault-read of 21 Nov 2026). A walk's reports share the vehicle, the tick, the shooter and the angle.
    static VehiclePawn? lastWalkVehicle;
    static int lastWalkTick = -1;
    static Thing? lastWalkInstigator;
    static float lastWalkAngle = float.NaN;
    // A blast reaches a vehicle as several hits, one per edge cell: shock spall counts once per part per tick.
    static readonly ConditionalWeakTable<VehicleComponent, StrongBox<int>> lastShock = new();
    // Our own fragments are hits too: the reactor stands aside while they land.
    static bool applying;

    static readonly List<IntVec2> reach = new();
    static readonly List<Pawn> crew = new();
    static readonly List<VehicleComponent> inner = new();

    static PhobosSpallRulesDef Rules => PhobosRulesDef.Current<PhobosSpallRulesDef>();
    static VehicleSystemsSettings Settings => VehicleSystemsMod.Settings;

    /// <summary>Whether Combat Extended is active and its functions were found: the load line and a dev check read it.</summary>
    public static bool Ready
    {
        get
        {
            if (looked) return ready;
            looked = true;
            if (!OptionalMods.IsActive(OptionalMods.CombatExtended)) return ready = false;
            try
            {
                MethodInfo? armour = AccessTools.Method(DamageArmorMethod);
                MethodInfo? hit = AccessTools.Method(HitComponentMethod);
                combatSettings = AccessTools.Field(CombatSettingsField);
                newVehicleArmour = AccessTools.Field(NewVehicleArmourField);
                fragment = DefDatabase<DamageDef>.GetNamedSilentFail("Fragment");
                fragmentSource = DefDatabase<ThingDef>.GetNamedSilentFail(FragmentProjectile);
                if (armour == null || hit == null || fragment == null)
                {
                    Log.Warning(VehicleSystemsMod.LogPrefix + " Spall inside vehicles stays off: Combat Extended's vehicle armour functions or its fragment damage were not found.");
                    return ready = false;
                }
                damageArmor = (DamageArmorFn)armour.CreateDelegate(typeof(DamageArmorFn));
                hitComponent = (HitComponentFn)hit.CreateDelegate(typeof(HitComponentFn));
                return ready = true;
            }
            catch (Exception error)
            {
                Log.Warning(VehicleSystemsMod.LogPrefix + " Spall inside vehicles stays off: Combat Extended's vehicle armour functions have changed. " + error.Message);
                return ready = false;
            }
        }
    }

    /// <summary>Combat Extended's "Use New Vehicle Armor": off, Vehicle Framework's own armour step runs, which this cannot read. Read at each
    /// hit, so the player's change counts at once; on when the setting cannot be found, as Combat Extended's default is.</summary>
    static bool NewVehicleArmourOn()
    {
        if (combatSettings == null || newVehicleArmour == null) return true;
        object? settings = combatSettings.GetValue(null);
        return settings == null || newVehicleArmour.GetValue(settings) is not bool on || on;
    }

    /// <summary>
    /// Called once per vehicle design as it resolves, after our loadout appended its parts: every outer part gets the spall reactor. Only
    /// with Combat Extended; the reactor itself asks the player's settings at each hit, so they change without a restart.
    /// </summary>
    public static void AddReactors(VehicleDef def)
    {
        if (def.components == null || !OptionalMods.IsActive(OptionalMods.CombatExtended)) return;
        foreach (VehicleComponentProperties part in def.components)
        {
            if (part.depth != VehicleComponent.VehiclePartDepth.External) continue;
            part.reactors ??= new List<Reactor>();
            if (!part.reactors.Exists(r => r is Reactor_PhobosSpall)) part.reactors.Add(new Reactor_PhobosSpall());
        }
    }

    // ---- At a hit ----

    internal static void Hit(VehiclePawn vehicle, VehicleComponent plate, DamageInfo dinfo, VehicleComponent.Penetration penetration)
    {
        if (applying || !Settings.spallInsideVehicles || vehicle?.statHandler == null || Current.Game == null) return;
        int now = Find.TickManager.TicksGame;
        if (pendingBulk == plate && pendingBulkTick == now)
        {
            pendingBulk = null;
            return;
        }
        bool beaten = penetration == VehicleComponent.Penetration.Penetrated;
        pendingBulk = beaten ? plate : null;
        pendingBulkTick = now;
        if (penetration == VehicleComponent.Penetration.Electrified || !Ready || !NewVehicleArmourOn()) return;
        if (!Settings.spallOnOtherVehicles && !Ours(vehicle)) return;
        if (SameWalk(vehicle, now, dinfo)) return;
        DamageArmorCategoryDef? category = dinfo.Def?.armorCategory;
        if (category == null) return;
        PhobosSpallRulesDef rules = Rules;
        SpallFigures f = rules.Figures;
        float sharpArmour = plate.ArmorRating(DamageArmorCategoryDefOf.Sharp, out _);
        if (sharpArmour < rules.MinimumMillimetres) return;
        bool sharp = category.armorRatingStat == StatDefOf.ArmorRating_Sharp;
        bool explosive = dinfo.Def!.isExplosive;
        // Read after Combat Extended took this hit's armour damage off, so the plate's efficiency may be a little lower than the one it used.
        float armour = plate.Efficiency * plate.ArmorRating(category, out _);
        float penetrationIn = dinfo.ArmorPenetrationInt;
        float damage = dinfo.Amount;
        SpallKind kind = SpallRule.KindOf(beaten, sharp, explosive, damage, penetrationIn, armour, f);
        if (kind == SpallKind.None) return;
        if (kind == SpallKind.Shock)
        {
            if (!Settings.shockSpall) return;
            StrongBox<int> last = lastShock.GetValue(plate, _ => new StrongBox<int>(-1));
            if (last.Value == now) return;
            last.Value = now;
        }
        // A blast counts against a plate as a blunt blow Combat Extended lets it shrug off, so its shock comes from the blast's own damage.
        float source = kind == SpallKind.Shock && explosive ? damage : PlateTook(sharp, armour, penetrationIn, damage, plate, beaten);
        double pool = SpallRule.Pool(kind, source, f);
        if (!(pool >= 0.5)) return;    // also refuses a NaN from a plate at no armour and a hit at no penetration
        RememberWalk(vehicle, now, dinfo);
        Throw(vehicle, plate, dinfo, kind, pool, sharpArmour, f);
    }

    static bool SameWalk(VehiclePawn vehicle, int now, DamageInfo dinfo) =>
        lastWalkVehicle == vehicle && lastWalkTick == now && lastWalkInstigator == dinfo.Instigator && lastWalkAngle == dinfo.Angle;

    static void RememberWalk(VehiclePawn vehicle, int now, DamageInfo dinfo)
    {
        lastWalkVehicle = vehicle;
        lastWalkTick = now;
        lastWalkInstigator = dinfo.Instigator;
        lastWalkAngle = dinfo.Angle;
    }

    /// <summary>What the plate took from the hit, by Combat Extended's own rule: its armour, then, for a beaten face, its bulk. Combat
    /// Extended skips the armour step for a part with no armour, and so does this.</summary>
    static float PlateTook(bool sharp, float armour, float penetration, float damage, VehicleComponent plate, bool beaten)
    {
        float pen = penetration, dmg = damage, took = 0f;
        if (armour > 0f) damageArmor!(sharp, armour, ref pen, ref dmg, out took);
        if (beaten && dmg > 0f && plate.Health > 0f)
        {
            damageArmor!(sharp, plate.Health / 50f, ref pen, ref dmg, out float bulk);
            took += bulk;
        }
        return took;
    }

    static void Throw(VehiclePawn vehicle, VehicleComponent plate, DamageInfo dinfo, SpallKind kind, double pool, float sharpArmour, in SpallFigures f)
    {
        // The liner catches spall only off a plate whose cells it lines, and only once fitted (a liner mended by a dev action is not fitted).
        VehicleComponent? liner = LinerBehind(vehicle, plate);
        double linerShare = liner != null ? liner.Health / liner.MaxHealth : 0;
        double stopped = pool * SpallRule.LinerStops(linerShare, f);
        bool narrow = SpallRule.Narrow(linerShare, f);
        if (stopped > 0 && liner != null)
        {
            double wear = SpallRule.LinerWear(stopped, f);
            liner.SetHealth(Mathf.Max(0f, liner.Health - (float)wear));    // raises Vehicle Framework's HealthChanged itself
            CompPhobosVehicleSystems.Of(vehicle)?.RememberAllPartHealths();
            // The logbook and the alert (Framework 0.209.0): the moment it wears through.
            if (linerShare > f.linerSpentAt && liner.Health / liner.MaxHealth <= f.linerSpentAt) VehicleStory.LinerSpent(vehicle);
            if (Seen.Wants("vehicles.spall-liner-caught", vehicle))
            {
                Seen.Note("vehicles.spall-liner-caught", vehicle, plate.props.label, values: new[]
                {
                    new KeyValuePair<string, double>("stopped", stopped), new KeyValuePair<string, double>("wear", wear),
                    new KeyValuePair<string, double>("linerShareBefore", linerShare),
                });
            }
        }
        double left = pool - stopped;
        int fragments = SpallRule.Fragments(kind, narrow, f);
        if (VehicleSystemsMod.SpallLog.DebugEnabled)
        {
            VehicleSystemsMod.SpallLog.Debug(vehicle.LabelShort + " (" + vehicle.ThingID + ") " + kind + " spall off " + plate.props.label + ": pool " + pool.ToString("0.0")
                + ", the liner (" + ((float)linerShare).ToStringPercent() + ") stopped " + stopped.ToString("0.0") + ", " + fragments + " fragment(s) of " + (left / Math.Max(1, fragments)).ToString("0.0")
                + (narrow ? ", narrow" : ", wide"));
        }
        if (!(left >= 0.5) || fragments <= 0) return;
        float each = (float)(left / fragments);
        if (each < 0.5f) return;
        float penetration = (float)SpallRule.FragmentPenetration(sharpArmour, f);
        int landed = 0;
        bool partHit = false;
        applying = true;
        try
        {
            GatherTargets(vehicle, plate, narrow);
            if (crew.Count + inner.Count == 0) return;
            for (int i = 0; i < fragments; i++)
            {
                if (vehicle.Destroyed) break;
                if (Rand.Chance((float)f.missChance)) continue;
                int pick = Rand.Range(0, crew.Count + inner.Count);
                // Combat Extended's own small fragment as the source, as its fragments carry: armour then stops it by its own rule.
                var hit = new DamageInfo(fragment, each, penetration, dinfo.Angle, dinfo.Instigator, weapon: fragmentSource);
                if (pick < crew.Count)
                {
                    Pawn pawn = crew[pick];
                    if (pawn.Dead || pawn.Destroyed) continue;
                    pawn.TakeDamage(hit);
                    landed++;
                    VehicleStory.SpallWounded(vehicle, pawn);
                    if (Seen.Wants("vehicles.spall-hit", vehicle, "crew")) Seen.Note("vehicles.spall-hit", vehicle, pawn.LabelShort, "crew");
                }
                else
                {
                    VehicleComponent part = inner[pick - crew.Count];
                    if (part.Health <= 0f) continue;
                    hitComponent!(part, ref hit, null);
                    landed++;
                    partHit = true;
                    if (Seen.Wants("vehicles.spall-hit", vehicle, "part")) Seen.Note("vehicles.spall-hit", vehicle, part.props.label, "part");
                }
            }
            // Combat Extended's part step raises only DamageTaken, and Vehicle Framework checks a holed tank for leaks on HealthChanged.
            if (partHit && !vehicle.Destroyed) vehicle.EventRegistry[VehicleEventDefOf.HealthChanged].ExecuteEvents();
        }
        catch (Exception error)
        {
            Log.ErrorOnce(VehicleSystemsMod.LogPrefix + " Spall inside " + vehicle.LabelShort + " could not land: " + error, 0x5BA11 ^ vehicle.thingIDNumber);
        }
        finally
        {
            applying = false;
            reach.Clear();
            crew.Clear();
            inner.Clear();
        }
        if (landed > 0 && Seen.Wants("vehicles.spall", vehicle, kind == SpallKind.Shock ? "shock" : "beaten"))
        {
            Seen.Note("vehicles.spall", vehicle, plate.props.label, kind == SpallKind.Shock ? "shock" : "beaten", new[]
            {
                new KeyValuePair<string, double>("pool", pool), new KeyValuePair<string, double>("fragments", landed),
                new KeyValuePair<string, double>("lined", stopped > 0 ? 1 : 0),
            });
        }
    }

    /// <summary>The crew behind the plate (seats inside the vehicle whose hit areas share its cells) and the inner parts on those cells.</summary>
    static void GatherTargets(VehiclePawn vehicle, VehicleComponent plate, bool narrow)
    {
        reach.Clear();
        crew.Clear();
        inner.Clear();
        List<IntVec2>? cells = plate.props.hitbox?.Hitbox;
        if (cells == null || cells.Count == 0) return;
        if (narrow) reach.Add(cells.RandomElement());
        else reach.AddRange(cells);
        if (vehicle.handlers != null)
        {
            foreach (VehicleRoleHandler handler in vehicle.handlers)
            {
                if (handler?.role?.Hitbox == null || handler.role.Exposed || handler.thingOwner == null || handler.thingOwner.Count == 0) continue;
                if (!Covers(handler.role.Hitbox)) continue;
                crew.AddRange(handler.thingOwner.InnerListForReading);
            }
        }
        foreach (VehicleComponent part in vehicle.statHandler.components)
        {
            if (part.Depth != VehicleComponent.VehiclePartDepth.Internal || part.Health <= 0f || part.props.key == LinerKey) continue;
            if (part.props.hitbox != null && Covers(part.props.hitbox)) inner.Add(part);
        }
    }

    static bool Covers(ComponentHitbox hitbox)
    {
        foreach (IntVec2 cell in reach)
        {
            if (hitbox.Contains(cell)) return true;
        }
        return false;
    }

    static bool Ours(VehiclePawn vehicle) =>
        vehicle.def?.modContentPack?.PackageId?.StartsWith("phobosdthorga.", StringComparison.OrdinalIgnoreCase) == true;

    // ---- The liner ----

    /// <summary>The fitted liner lining this plate, or null: one not fitted, at nothing, or on other cells (a cab's liner does not line the
    /// tipping body).</summary>
    static VehicleComponent? LinerBehind(VehiclePawn vehicle, VehicleComponent plate)
    {
        VehicleComponent? liner = vehicle.statHandler.GetComponent(LinerKey);
        if (liner == null || liner.Health <= 0f || liner.MaxHealth <= 0f || liner.props.hitbox == null) return null;
        List<IntVec2>? cells = plate.props.hitbox?.Hitbox;
        if (cells == null) return null;
        bool shares = false;
        foreach (IntVec2 cell in cells)
        {
            if (liner.props.hitbox.Contains(cell)) { shares = true; break; }
        }
        return shares && LinerFitted(vehicle) ? liner : null;
    }

    /// <summary>Whether the vehicle's liner upgrade is fitted: a node of its tree carrying <see cref="PhobosSpallLinerUpgrade"/> is unlocked.</summary>
    public static bool LinerFitted(VehiclePawn vehicle) => FittedLinerNode(vehicle) != null || MarkedLined(vehicle.VehicleDef);

    /// <summary>Another author's design marked as carrying a liner (Framework 0.244.0, the shared extension).</summary>
    internal static bool MarkedLined(VehicleDef def) => def.GetModExtension<PhobosFittedEquipmentExtension>()?.lined == true;

    /// <summary>
    /// Appends the liner part to a design marked as lined, with Combat Extended, at start-up (Framework 0.244.0; vehicle-spall.md: "other
    /// authors' vehicles marked as lined get the same part ..., born at full health, so their liners wear too"). On the cab's cells, inside,
    /// not counted toward destruction, as our trucks' is; ours come from the loadout patch instead, so a design that already has it is left.
    /// </summary>
    internal static void ApplyMarked(VehicleDef def, CompProperties_PhobosVehicleSystems props)
    {
        if (!MarkedLined(def) || !OptionalMods.IsActive(OptionalMods.CombatExtended) || def.components == null || def.components.Any(p => p.key == LinerKey)) return;
        var add = new PartToAdd
        {
            key = LinerKey, label = Rules.linerLabel, health = (int)Rules.linerHealth, depth = VehicleComponent.VehiclePartDepth.Internal, hitWeight = 0.05f,
            cellsOf = props.roleKeys.ContainsKey("Cabin") ? "Cabin" : "Engine", armor = new List<StatModifier>(), countsTowardDestruction = false,
        };
        def.components.Add(VehicleSystems.MakePart(def, add, props));
        props.roleKeys["SpallLiner"] = new List<string> { LinerKey };
        props.addedKeys.Add(LinerKey);
        props.uncountedKeys.Add(LinerKey);
    }

    /// <summary>The unlocked node carrying the liner upgrade, or null.</summary>
    static UpgradeNode? FittedLinerNode(VehiclePawn vehicle)
    {
        CompUpgradeTree? tree = vehicle?.CompUpgradeTree;
        List<UpgradeNode>? nodes = tree?.Props?.def?.nodes;
        if (nodes == null) return null;
        foreach (UpgradeNode node in nodes)
        {
            if (IsLinerNode(node) && tree!.NodeUnlocked(node)) return node;
        }
        return null;
    }

    internal static bool IsLinerNode(UpgradeNode? node) => node?.upgrades != null && node.upgrades.Exists(u => u is PhobosSpallLinerUpgrade);

    /// <summary>Whether this part is a liner not fitted: shown by its state, never as destroyed, and left out of the vehicle's worst part.</summary>
    public static bool Unfitted(VehiclePawn vehicle, VehicleComponent part) => part.props.key == LinerKey && !LinerFitted(vehicle);

    /// <summary>The liner's state for the Health tab.</summary>
    public static LinerState StateOf(VehiclePawn vehicle, VehicleComponent liner) =>
        SpallRule.StateOf(LinerFitted(vehicle), liner.MaxHealth > 0f ? liner.Health / liner.MaxHealth : 0, Rules.Figures);

    /// <summary>The liner's state in words, for the Health tab's row and the diagnosis.</summary>
    public static string StateWord(LinerState state) => state switch
    {
        LinerState.Sound => "PhobosVehicleSystems_HealthLinerSound".Translate(),
        LinerState.Worn => "PhobosVehicleSystems_HealthLinerWorn".Translate(),
        LinerState.Spent => "PhobosVehicleSystems_HealthLinerSpent".Translate(),
        _ => "PhobosVehicleSystems_HealthLinerNotFitted".Translate(),
    };

    /// <summary>The liner's health when a vehicle is first looked at (see <see cref="SpallRule.StartingHealth"/>).</summary>
    internal static float StartingHealth(VehiclePawn vehicle, VehicleComponent liner, float? saved) =>
        (float)SpallRule.StartingHealth(saved, LinerFitted(vehicle), liner.MaxHealth);

    /// <summary>Puts a whole liner in, or takes it out: the liner upgrade's fitting and removal.</summary>
    internal static void SetLiner(VehiclePawn vehicle, bool whole, string how)
    {
        VehicleComponent? liner = vehicle?.statHandler?.GetComponent(LinerKey);
        if (liner == null) return;
        liner.SetHealth(whole ? liner.MaxHealth : 0f);    // raises Vehicle Framework's HealthChanged itself
        CompPhobosVehicleSystems? comp = CompPhobosVehicleSystems.Of(vehicle!) ?? vehicle!.GetComp<CompPhobosVehicleSystems>();
        // Remembered, or the next look reads the fitting or removal as damage ("came under fire").
        comp?.RememberAllPartHealths();
        comp?.Repairs.Invalidate();
        if (Seen.Wants("vehicles.spall-liner-set", vehicle, how)) Seen.Note("vehicles.spall-liner-set", vehicle, liner.props.label, how);
    }

    /// <summary>
    /// What taking the liner off gives back: the panels paid, scaled by how much of the liner is left, rounded down, so a spent liner taken
    /// off and put on again costs what a re-line would (fault-read of 21 Nov 2026). Anything else paid comes back as paid.
    /// </summary>
    internal static List<ThingDefCountClass> RefundFor(VehiclePawn vehicle, List<ThingDefCountClass> paid)
    {
        VehicleComponent? liner = vehicle?.statHandler?.GetComponent(LinerKey);
        ThingDef? panel = Rules.Panel;
        if (liner == null || panel == null || liner.MaxHealth <= 0f) return paid;
        double share = liner.Health / liner.MaxHealth;
        var scaled = new List<ThingDefCountClass>(paid.Count);
        foreach (ThingDefCountClass line in paid)
        {
            int count = line.thingDef == panel ? SpallRule.PanelsBack(line.count, share) : line.count;
            if (count > 0) scaled.Add(new ThingDefCountClass(line.thingDef, count));
        }
        return scaled;
    }

    /// <summary>The liner panels a re-line of this vehicle's liner takes now: none when it is whole or little worn, not fitted, or the panel is
    /// not loaded.</summary>
    internal static int PanelsToReline(VehiclePawn vehicle, VehicleComponent liner)
    {
        if (Rules.Panel == null || liner.MaxHealth <= 0f || !LinerFitted(vehicle)) return 0;
        // Each liner's own count, from its upgrade (Framework 0.209.0: the Osprey's lines more than a truck's cab); a marked design's, the rules'.
        int perLiner = FittedLinerNode(vehicle)?.upgrades.Find(u => u is PhobosSpallLinerUpgrade) is PhobosSpallLinerUpgrade { panels: > 0 } upgrade
            ? upgrade.panels : Rules.panelsPerLiner;
        return SpallRule.PanelsToReline(liner.Health / liner.MaxHealth, perLiner);
    }

    internal static ThingDef? Panel => Rules.Panel;

    internal static float RelineWork => Rules.relineWork;
}

/// <summary>On every outer part, with Combat Extended (Framework 0.207.0): hands each hit to <see cref="Spall"/>. Named in XML by its full name
/// should another mod wish to add it to a part of its own.</summary>
public class Reactor_PhobosSpall : Reactor
{
    /// <summary>Vehicle Framework's own Health tab colours a part by its first reactor; ours is on every outer plate, so it colours nothing
    /// (Framework 0.209.0: the default orange lit every plate of every vehicle).</summary>
    public Reactor_PhobosSpall()
    {
        highlightColor = Color.white;
    }

    public override void Hit(VehiclePawn vehicle, VehicleComponent component, ref DamageInfo dinfo, VehicleComponent.Penetration penetration) =>
        Spall.Hit(vehicle, component, dinfo, penetration);
}
