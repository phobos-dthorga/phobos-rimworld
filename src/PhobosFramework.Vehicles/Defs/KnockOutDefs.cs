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
    /// <summary>An autonomous weapon (no seat works it) with no power stops while nobody is aboard to crank it (Framework 0.120.0; owner,
    /// 17 Nov 2026). Off: it is slowed like a cranked one.</summary>
    public bool autonomousStopsWithoutPower = true;
    /// <summary>Weapons standing alone that are swung by hand anyway, by a part of their key or turret Def's name: pintle and ring-mounted
    /// machine guns and grenade launchers, which need no power.</summary>
    public List<string> handSwungTurretKeys = new();
    /// <summary>Weapons riding on a turret that go where it goes, by a part of their key or turret Def's name (Framework 0.117.2).</summary>
    public List<string> coaxialTurretKeys = new();
    /// <summary>Weapons riding on a turret that stand on their own ring or pintle on top of it, by a part of their key (Framework 0.117.2).
    /// A riding weapon worked by a different crew member from its turret's counts as one too.</summary>
    public List<string> ownMountTurretKeys = new();

    // ---- A cranked turret's aim (0.119.0: T5, T6) ----

    /// <summary>Off: a turret cranked by hand aims as well as a powered one.</summary>
    public bool crankedTurretsAimWorse = true;
    /// <summary>A cranked turret has no stabiliser: its accuracy while the vehicle moves (T5).</summary>
    public float noStabiliserAccuracy = 0.6f;
    /// <summary>A cranked turret's powered sights are dark: its accuracy at night and in poor weather (T6).</summary>
    public float noSightsAccuracy = 0.7f;
    /// <summary>Under Combat Extended the aim sways more instead: never more than this many times the weapon's own sway.</summary>
    public float combatExtendedSwayCap = 3f;
    /// <summary>The sway a cranked turret starts from under Combat Extended when its own is less (0.120.5, fault-read A8). Authored.</summary>
    public float combatExtendedLeastSway = 1f;

    // ---- Field re-fitting a track or wheel (0.116.0: M3) ----

    /// <summary>A destroyed track or wheel re-fitted in the field comes back to this share of its health (owner, 17 Nov 2026: a quarter).</summary>
    public float fieldRefitHealth = 0.25f;
    /// <summary>Re-fitting takes this many times a field patch's work: a track is heavy to drag back on.</summary>
    public float fieldRefitWorkFactor = 2f;

    // ---- The turret blown off (0.116.0: T8) ----

    /// <summary>Off: no ammunition rack blows a turret off (an author's own reaction still stands).</summary>
    public bool turretPopping = true;
    /// <summary>A part holding the ammunition, by key (case aside): RimThunder's tanks call it AmmoRack.</summary>
    public List<string> ammoRackKeys = new();
    /// <summary>The chance a hit sets the rack off once it is at or under this share of its health.</summary>
    public float popChanceOnHit = 0.25f;
    public float popAtOrBelowRackHealth = 0.25f;
    /// <summary>The chance a cook-off blast in the vehicle sets the rack off.</summary>
    public float popChanceOnCookOff = 0.3f;
    /// <summary>The blast's radius, in cells.</summary>
    public int popRadius = 2;

    // ---- The crew (step 3: C2) ----

    /// <summary>Off: no crew leaves by itself and the player gets no alert; Vehicle Framework's own rules stand.</summary>
    public bool crewsBailOut = true;
}
