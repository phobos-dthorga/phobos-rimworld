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
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// Spall inside vehicles (Framework 0.207.0; docs/development/vehicle-spall.md; owner, 21 Nov 2026). When a round beats a vehicle's outer
/// plate, debris from what the plate took sprays into the compartment behind it and may wound the crew sitting there or hole the inner parts
/// on those cells; a heavy hit the face stops can knock a little off the inside. A spall liner, a part of its own, catches a share and wears
/// as it does. Combat Extended only (owner: without it Vehicle Framework's armour is a chance, not a thickness).
///
/// No Harmony: Combat Extended's armour step calls Vehicle Framework's reactors on every part it damages, so a reactor on each outer part is
/// told of every hit. What the plate took is worked out with Combat Extended's own armour function, called by name on copies of the hit's
/// figures, so ours never drifts from its rule; fragments on inner parts go through Combat Extended's own part step, so a part's armour
/// counts and its reactors fire (a holed fuel tank leaks through Vehicle Framework's). Nothing per tick; nothing saved but the liner's
/// health, which is a part's health.
/// </summary>
public static class Spall
{
    /// <summary>The liner's part key, appended by our trucks' loadouts in D'thorga Vehicles' Combat Extended folder.</summary>
    public const string LinerKey = "PhobosSpallLiner";

    const string DamageArmorMethod = "CombatExtended.Compatibility.VehiclesCompat.VehicleArmorPatch:DamageArmor";
    const string HitComponentMethod = "CombatExtended.Compatibility.VehiclesCompat.VehicleArmorPatch:HitComponent";

    delegate bool DamageArmorFn(bool isSharp, float armorAmount, ref float penAmount, ref float dmgAmount, out float armorDamage);
    delegate bool HitComponentFn(VehicleComponent component, ref DamageInfo dinfo, StringBuilder? report);

    static DamageArmorFn? damageArmor;
    static HitComponentFn? hitComponent;
    static DamageDef? fragment;
    static bool looked;
    static bool ready;

    // A beaten part is reported twice in one step (its armour, then its bulk): the second report is the same hit.
    static VehicleComponent? pendingBulk;
    static int pendingBulkTick = -1;
    // A blast reaches a vehicle as several hits, one per edge cell: shock spall counts once per part per tick.
    static readonly ConditionalWeakTable<VehicleComponent, StrongBox<int>> lastShock = new();
    // Our own fragments are hits too: the reactor stands aside while they land.
    static bool applying;

    static readonly List<IntVec2> reach = new();
    static readonly List<Pawn> crew = new();
    static readonly List<VehicleComponent> inner = new();

    static PhobosSpallRulesDef Rules => PhobosRulesDef.Current<PhobosSpallRulesDef>();
    static VehicleSystemsSettings Settings => VehicleSystemsMod.Settings;

    /// <summary>Whether Combat Extended is active and its two functions were found: the load line and a dev check read it.</summary>
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
                fragment = DefDatabase<DamageDef>.GetNamedSilentFail("Fragment");
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

    /// <summary>
    /// Called once per vehicle design as it resolves: every outer part gets the spall reactor. Only with Combat Extended; the reactor itself
    /// asks the player's settings at each hit, so they change without a restart.
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
        if (penetration == VehicleComponent.Penetration.Electrified || !Ready) return;
        if (!Settings.spallOnOtherVehicles && !Ours(vehicle)) return;
        DamageArmorCategoryDef? category = dinfo.Def?.armorCategory;
        if (category == null) return;
        PhobosSpallRulesDef rules = Rules;
        SpallFigures f = rules.Figures;
        float sharpArmour = plate.ArmorRating(DamageArmorCategoryDefOf.Sharp, out _);
        if (sharpArmour < rules.MinimumMillimetres) return;
        bool sharp = category.armorRatingStat == StatDefOf.ArmorRating_Sharp;
        bool explosive = dinfo.Def!.isExplosive;
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
        if (pool < 0.5) return;
        Throw(vehicle, plate, dinfo, kind, pool, sharpArmour, f);
    }

    /// <summary>What the plate took from the hit, by Combat Extended's own rule: its armour, then, for a beaten face, its bulk.</summary>
    static float PlateTook(bool sharp, float armour, float penetration, float damage, VehicleComponent plate, bool beaten)
    {
        float pen = penetration, dmg = damage;
        damageArmor!(sharp, armour, ref pen, ref dmg, out float took);
        if (beaten && dmg > 0f)
        {
            damageArmor(sharp, plate.Health / 50f, ref pen, ref dmg, out float bulk);
            took += bulk;
        }
        return took;
    }

    static void Throw(VehiclePawn vehicle, VehicleComponent plate, DamageInfo dinfo, SpallKind kind, double pool, float sharpArmour, in SpallFigures f)
    {
        VehicleComponent? liner = vehicle.statHandler.GetComponent(LinerKey);
        double linerShare = liner != null && liner.MaxHealth > 0f ? liner.Health / liner.MaxHealth : 0;
        double stopped = pool * SpallRule.LinerStops(linerShare, f);
        bool narrow = SpallRule.Narrow(linerShare, f);
        if (stopped > 0 && liner != null)
        {
            double wear = SpallRule.LinerWear(stopped, f);
            liner.SetHealth(UnityEngine.Mathf.Max(0f, liner.Health - (float)wear));
            vehicle.EventRegistry[VehicleEventDefOf.HealthChanged].ExecuteEvents();
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
        if (Seen.Wants("vehicles.spall", vehicle, kind == SpallKind.Shock ? "shock" : "beaten"))
        {
            Seen.Note("vehicles.spall", vehicle, plate.props.label, kind == SpallKind.Shock ? "shock" : "beaten", new[]
            {
                new KeyValuePair<string, double>("pool", pool), new KeyValuePair<string, double>("fragments", fragments),
                new KeyValuePair<string, double>("lined", stopped > 0 ? 1 : 0),
            });
        }
        if (left < 0.5 || fragments <= 0) return;
        GatherTargets(vehicle, plate, narrow);
        float each = (float)(left / fragments);
        if (each < 0.5f || crew.Count + inner.Count == 0) return;
        float penetration = (float)SpallRule.FragmentPenetration(sharpArmour, f);
        applying = true;
        try
        {
            for (int i = 0; i < fragments; i++)
            {
                if (Rand.Chance((float)f.missChance)) continue;
                int pick = Rand.Range(0, crew.Count + inner.Count);
                var hit = new DamageInfo(fragment, each, penetration, dinfo.Angle, dinfo.Instigator);
                if (pick < crew.Count)
                {
                    Pawn pawn = crew[pick];
                    if (pawn.Dead) continue;
                    pawn.TakeDamage(hit);
                    if (Seen.Wants("vehicles.spall-hit", vehicle, "crew")) Seen.Note("vehicles.spall-hit", vehicle, pawn.LabelShort, "crew");
                }
                else
                {
                    VehicleComponent part = inner[pick - crew.Count];
                    if (part.Health <= 0f) continue;
                    hitComponent!(part, ref hit, null);
                    if (Seen.Wants("vehicles.spall-hit", vehicle, "part")) Seen.Note("vehicles.spall-hit", vehicle, part.props.label, "part");
                }
            }
        }
        catch (Exception error)
        {
            Log.ErrorOnce(VehicleSystemsMod.LogPrefix + " Spall inside " + vehicle.LabelShort + " could not land: " + error, 0x5BA11 ^ vehicle.thingIDNumber);
        }
        finally
        {
            applying = false;
            crew.Clear();
            inner.Clear();
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

    /// <summary>Whether the vehicle's liner upgrade is fitted: a node of its tree carrying <see cref="PhobosSpallLinerUpgrade"/> is unlocked.</summary>
    public static bool LinerFitted(VehiclePawn vehicle)
    {
        CompUpgradeTree? tree = vehicle?.CompUpgradeTree;
        List<UpgradeNode>? nodes = tree?.Props?.def?.nodes;
        if (nodes == null) return false;
        foreach (UpgradeNode node in nodes)
        {
            if (node.upgrades == null || !node.upgrades.Exists(u => u is PhobosSpallLinerUpgrade)) continue;
            if (tree!.NodeUnlocked(node)) return true;
        }
        return false;
    }

    /// <summary>The liner's state for the Health tab.</summary>
    public static LinerState StateOf(VehiclePawn vehicle, VehicleComponent liner) =>
        SpallRule.StateOf(LinerFitted(vehicle), liner.MaxHealth > 0f ? liner.Health / liner.MaxHealth : 0, Rules.Figures);

    /// <summary>The liner's health when a vehicle is first looked at (see <see cref="SpallRule.StartingHealth"/>).</summary>
    internal static float StartingHealth(VehiclePawn vehicle, VehicleComponent liner, float? saved) =>
        (float)SpallRule.StartingHealth(saved, LinerFitted(vehicle), liner.MaxHealth);

    /// <summary>Puts a whole liner in, or takes it out: the liner upgrade's fitting and removal.</summary>
    internal static void SetLiner(VehiclePawn vehicle, bool whole, string how)
    {
        VehicleComponent? liner = vehicle?.statHandler?.GetComponent(LinerKey);
        if (liner == null) return;
        liner.SetHealth(whole ? liner.MaxHealth : 0f);
        vehicle!.EventRegistry[VehicleEventDefOf.HealthChanged].ExecuteEvents();
        CompPhobosVehicleSystems.Of(vehicle)?.Repairs.Invalidate();
        if (Seen.Wants("vehicles.spall-liner-set", vehicle, how)) Seen.Note("vehicles.spall-liner-set", vehicle, liner.props.label, how);
    }

    /// <summary>The liner panels a re-line of this vehicle's liner takes now: none when it is whole, not fitted, or the panel is not loaded.</summary>
    internal static int PanelsToReline(VehiclePawn vehicle, VehicleComponent liner)
    {
        if (Rules.Panel == null || !LinerFitted(vehicle) || liner.MaxHealth <= 0f) return 0;
        return SpallRule.PanelsToReline(liner.Health / liner.MaxHealth, Rules.Figures);
    }

    internal static ThingDef? Panel => Rules.Panel;

    internal static float RelineWork => Rules.relineWork;
}

/// <summary>On every outer part, with Combat Extended (Framework 0.207.0): hands each hit to <see cref="Spall"/>. Named in XML by its full name
/// should another mod wish to add it to a part of its own.</summary>
public class Reactor_PhobosSpall : Reactor
{
    public override void Hit(VehiclePawn vehicle, VehicleComponent component, ref DamageInfo dinfo, VehicleComponent.Penetration penetration) =>
        Spall.Hit(vehicle, component, dinfo, penetration);
}
