using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Phobos.Framework.Defs;
using Phobos.Framework.Vehicles.Rules;
using RimWorld;
using SmashTools;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// Ties each weapon to its mount at start-up (Framework 0.115.0; owner, 16 Nov 2026; docs/development/vehicle-knocked-out-systems.md, T1,
/// T9, T10): the weapon stops while the mount's health is at or under a tenth of full, and starts again when it is repaired.
///
/// How: Vehicle Framework's own part requirement (`VehicleTurret.component`, a `ComponentRequirement`) is set on each weapon's template,
/// on the design and on any upgrade that adds weapons. Every vehicle copies it from the template when it loads (`VehicleTurret.Init`) and
/// checks it on each health change, so this costs nothing between hits, needs no Harmony and works on every faction's vehicles. It is not
/// saved: without this mod, the templates carry no requirement and the weapons work as their authors made them.
///
/// The mount is the vehicle's own part where it has one named in the rules (an author's turret part, our Harrier pylons); otherwise a turret
/// ring (a ground vehicle) or weapon pylons (an aircraft) are appended, inside, at the gunner's seat. An author's own requirement always
/// stands, a weapon mounted on another stops with its parent, and a fixed launcher on the hull is left alone.
/// </summary>
public static class WeaponMounts
{
    public const string PartKey = "PhobosWeaponMount";
    // ComponentRequirement's fields are private and set only by XML; these names are checked against the assembly by check-targets.py.
    const string RequirementKeyField = "key";
    const string RequirementShareField = "healthPercent";
    static readonly FieldInfo? keyField = typeof(ComponentRequirement).GetField(RequirementKeyField, BindingFlags.NonPublic | BindingFlags.Instance);
    static readonly FieldInfo? shareField = typeof(ComponentRequirement).GetField(RequirementShareField, BindingFlags.NonPublic | BindingFlags.Instance);

    /// <summary>Ties this design's free weapons to a mount, appending the mount where the design has none. Returns how many weapons it tied.</summary>
    public static int Apply(VehicleDef def, CompProperties_PhobosVehicleSystems props)
    {
        PhobosKnockOutRulesDef rules = PhobosRulesDef.Current<PhobosKnockOutRulesDef>();
        if (!rules.weaponMounts) return 0;
        List<VehicleTurret> free = Templates(def)
            .Where(t => WeaponMountRule.NeedsMount(t.component != null, !string.IsNullOrEmpty(t.parentKey), t.def?.turretType == TurretType.Static))
            .ToList();
        if (free.Count == 0) return 0;
        if (keyField == null || shareField == null)
        {
            VehicleSystems.Problems.Add(def.defName + ": Vehicle Framework's part requirement has changed, so its weapons are not tied to a mount");
            return 0;
        }
        string? mount = MountFor(def, props, rules, free);
        if (mount == null) return 0;
        foreach (VehicleTurret turret in free)
        {
            var requirement = new ComponentRequirement();
            keyField.SetValue(requirement, mount);
            shareField.SetValue(requirement, rules.stopsAtOrBelow);
            turret.component = requirement;
        }
        return free.Count;
    }

    /// <summary>Every weapon template the design can carry: its own, and those its upgrades add.</summary>
    static IEnumerable<VehicleTurret> Templates(VehicleDef def)
    {
        var own = def.GetCompProperties<CompProperties_VehicleTurrets>();
        if (own?.turrets != null)
        {
            foreach (VehicleTurret turret in own.turrets) yield return turret;
        }
        var tree = def.GetCompProperties<CompProperties_UpgradeTree>()?.def;
        if (tree?.nodes == null) yield break;
        foreach (UpgradeNode node in tree.nodes)
        {
            if (node?.upgrades == null) continue;
            foreach (Upgrade upgrade in node.upgrades)
            {
                if (upgrade is not TurretUpgrade added || added.turrets == null) continue;
                foreach (VehicleTurret turret in added.turrets) yield return turret;
            }
        }
    }

    static string? MountFor(VehicleDef def, CompProperties_PhobosVehicleSystems props, PhobosKnockOutRulesDef rules, List<VehicleTurret> free)
    {
        List<string> keys = def.components.Select(p => p.key).ToList();
        int own = WeaponMountRule.AuthorMount(keys, rules.authorMountKeys);
        if (own >= 0) return keys[own];
        if (keys.Contains(PartKey)) return PartKey;
        bool aircraft = def.GetCompProperties<CompProperties_VehicleLauncher>() != null;
        var add = new PartToAdd
        {
            key = PartKey,
            label = aircraft ? rules.pylonLabel : rules.turretRingLabel,
            health = aircraft ? rules.pylonHealth : rules.turretRingHealth,
            depth = VehicleComponent.VehiclePartDepth.Internal,
            cells = GunnerCells(def, free),
        };
        def.components.Add(VehicleSystems.MakePart(def, add, props));
        props.addedKeys.Add(PartKey);
        return PartKey;
    }

    /// <summary>The cells of the seat that works the first free weapon, so hits there reach the ring; else the vehicle's centre.</summary>
    static List<IntVec2> GunnerCells(VehicleDef def, List<VehicleTurret> free)
    {
        var turretKeys = new HashSet<string>(free.SelectMany(t => new[] { t.key, t.groupKey }).Where(k => !string.IsNullOrEmpty(k)));
        VehicleRole? gunner = def.properties?.roles?.FirstOrDefault(r => r.turretIds != null && r.turretIds.Any(turretKeys.Contains));
        List<IntVec2>? cells = gunner?.hitbox?.cells;
        return cells != null && cells.Count > 0 ? new List<IntVec2>(cells) : new List<IntVec2> { IntVec2.Zero };
    }
}
