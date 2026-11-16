using System.Collections.Generic;
using Phobos.Framework.Defs;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// Weapons tied to their mounts (Framework 0.115.0; owner, 16 Nov 2026; docs/development/vehicle-knocked-out-systems.md, T1, T9, T10).
/// A weapon stops when its mount's health falls to <see cref="stopsAtOrBelow"/> of full, through Vehicle Framework's own part requirement,
/// so it works on every faction's vehicles. The first loaded instance wins; another mod may patch it in XML.
/// </summary>
public class PhobosWeaponMountsDef : PhobosRulesDef
{
    /// <summary>Off: no weapon is tied to a mount by us (an author's own requirement still stands).</summary>
    public bool enabled = true;
    /// <summary>The share of the mount's full health at or below which its weapons stop (owner, 16 Nov 2026: under 10 %).</summary>
    public float stopsAtOrBelow = 0.1f;
    /// <summary>A vehicle's own parts to use as the mount, first found wins (by key; an author's turret part, our Harrier pylons).</summary>
    public List<string> authorMountKeys = new();
    /// <summary>The part we add where a vehicle has none: health for a ground vehicle's turret ring and an aircraft's weapon pylons (authored).</summary>
    public int turretRingHealth = 60;
    public int pylonHealth = 40;
    [MustTranslate] public string turretRingLabel = "turret ring";
    [MustTranslate] public string pylonLabel = "weapon pylons";
}
