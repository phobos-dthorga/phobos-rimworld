using System.Collections.Generic;
using Phobos.Framework.Defs;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// Knocked out, not destroyed (Framework 0.115.0; owner, 16 and 17 Nov 2026; docs/development/vehicle-knocked-out-systems.md): what a
/// vehicle loses before it is a wreck. The first loaded instance wins; another mod may patch it in XML. Every figure is authored unless it
/// names the owner.
/// </summary>
public class PhobosKnockOutRulesDef : PhobosRulesDef
{
    // ---- Weapons and their mounts (step 1: T1, T9, T10) ----

    /// <summary>Off: no weapon is tied to a mount by us (an author's own requirement still stands).</summary>
    public bool weaponMounts = true;
    /// <summary>The share of the mount's full health at or below which its weapons stop (owner, 16 Nov 2026: under 10 %).</summary>
    public float stopsAtOrBelow = 0.1f;
    /// <summary>A vehicle's own parts to use as the mount, first found wins (by key; an author's turret part, our Harrier pylons).</summary>
    public List<string> authorMountKeys = new();
    /// <summary>The part we add where a vehicle has none: health for a ground vehicle's turret ring and an aircraft's weapon pylons.</summary>
    public int turretRingHealth = 60;
    public int pylonHealth = 40;
    [MustTranslate] public string turretRingLabel = "turret ring";
    [MustTranslate] public string pylonLabel = "weapon pylons";

    // ---- Mobility (step 2: M1, M2) ----

    /// <summary>Off: a thrown track only slows a tracked vehicle, as Vehicle Framework averages it.</summary>
    public bool thrownTracks = true;
    /// <summary>A part is a track when its key ends with one of these and it feeds the vehicle's speed (a hint by name, as the tracked
    /// loadout's selector uses: nothing else marks a track).</summary>
    public List<string> trackKeySuffixes = new();
    /// <summary>The share of a track's health at or below which it is thrown, and the vehicle cannot move at all.</summary>
    public float trackThrownAtOrBelow = 0f;

    // ---- A weapon's pace (step 4: T2, C4) ----

    /// <summary>Off: a damaged mount does not slow its weapons before they stop.</summary>
    public bool damageSlowsWeapons = true;
    /// <summary>The pace of a weapon whose mount is at the line where it stops: traverse at this share, aiming and reloading this much slower.</summary>
    public float damagedMountPace = 0.55f;
    /// <summary>Off: the gunner's sight and hands do not change the weapon's pace.</summary>
    public bool gunnerConditionSlowsWeapons = true;
    /// <summary>The slowest a hurt gunner makes a weapon: their sight paces the aim, their hands the traverse and the reload.</summary>
    public float hurtGunnerPaceFloor = 0.5f;

    // ---- Turret power (step 5: T3, T4) ----

    /// <summary>Off: a turret never loses power, and no turret drive draws on the battery.</summary>
    public bool turretPower = true;
    /// <summary>The electrical load that drives the turrets (in the loadouts' loads).</summary>
    public string turretDriveLoad = "turretDrive";
    /// <summary>Below this share of charge the battery is flat, and gives the turret drive nothing.</summary>
    public float batteryFlatBelow = 0.05f;
    /// <summary>A turret with no power is cranked by hand (owner, 16 Nov 2026: never dead): it turns at this share of its powered speed,
    /// and lays its gun this much slower.</summary>
    public float handCrankTraversePace = 0.2f;
    public float handCrankAimPace = 0.5f;
    /// <summary>Weapons swung by hand anyway, by a part of their key: pintle and ring-mounted machine guns, which need no power.</summary>
    public List<string> handSwungTurretKeys = new();

    // ---- The crew (step 3: C2) ----

    /// <summary>Off: no crew leaves by itself and the player gets no alert; Vehicle Framework's own rules stand.</summary>
    public bool crewsBailOut = true;
}
