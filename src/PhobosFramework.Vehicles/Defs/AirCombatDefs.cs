using Phobos.Framework.Defs;
using RimWorld;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// Air combat (Framework 0.119.0; owner, 13 and 17 Nov 2026; docs/development/air-defence-tie-ins.md, AD2, AD3 and AD7, and
/// docs/development/vehicle-avionics.md, A12, A14 and A16): what another mod's air defence does to our aircraft, and what their warning
/// receivers, countermeasures and oxygen do about it. The first loaded instance wins; another mod may patch it in XML. Every figure is authored
/// unless it names the owner.
/// </summary>
public class PhobosAirCombatRulesDef : PhobosRulesDef
{
    // ---- A hit in flight (AD3) ----

    /// <summary>The damage one interceptor's hit does to an aircraft in flight, shared out over this many hits where its own hit areas put them.</summary>
    public float interceptDamage = 60f;
    public int interceptHits = 4;
    public float interceptArmourPenetration = 0.5f;
    public DamageDef? interceptDamageDef;
    /// <summary>An aircraft whose body integrity falls below this share has an airframe that has failed: it comes down.</summary>
    public float airframeFailsBelow = 0.2f;

    // ---- Tracks: how an aircraft in flight looks (AD3) ----

    /// <summary>An aircraft climbs to its cruise over this many tiles after take-off and comes down over as many before it lands.</summary>
    public float climbTiles = 1.5f;

    // ---- The warning receiver (AD2) ----

    /// <summary>A warning of one kind about one aircraft is not repeated within this many hours.</summary>
    public float warningCooldownHours = 1f;

    // ---- Countermeasures (A16) ----

    /// <summary>The cartridges the dispensers fire; loaded into their magazine (Framework 0.180.0; cargo before).</summary>
    public ThingDef? cartridgeDef;
    /// <summary>The cartridges one salvo uses: a chaff and a flare.</summary>
    public int cartridgesPerSalvo = 2;
    /// <summary>How many cartridges a magazine must be short before a hauler walks out to top it up (Framework 0.180.0). An order by hand
    /// ignores it.</summary>
    public int magazineLeastLoad = 10;
    /// <summary>The share of its magazine another faction's aircraft arrives with, picked once per aircraft between these (Framework
    /// 0.180.0; owner, 20 Nov 2026: "partly full"). Rounded down to whole salvos.</summary>
    public float otherFactionsFillMin = 0.25f;
    public float otherFactionsFillMax = 0.75f;
    /// <summary>On the world map: the factor on an air defence's chance to hit, fired on the receiver's cue and fired late, by eye, without it.</summary>
    public float hitChanceFactorCued = 0.6f;
    public float hitChanceFactorLate = 0.85f;
    /// <summary>On the world map: the factor on the time a fire-control radar needs to lock on, with chaff fired on the receiver's cue.</summary>
    public float lockTimeFactorCued = 1.5f;
    /// <summary>On a map (the Harrier's button): the dodge chance a salvo adds, for this many ticks.</summary>
    public float mapSalvoEvasionBonus = 0.25f;
    public int mapSalvoTicks = 600;

    // ---- Evasion (A12) ----

    /// <summary>The factor on the Harrier's dodge chance with its warning receiver out: the crew sees the shot late.</summary>
    public float noWarningEvasionFactor = 0.75f;
    /// <summary>No dodge chance goes above this, however many salvos are fired.</summary>
    public float evasionCap = 0.85f;

    // ---- Hypoxia (A14; the player's setting, off by default) ----

    /// <summary>The condition a crew gets when its oxygen fails at height, and its severity.</summary>
    public HediffDef? hypoxiaHediff;
    public float hypoxiaSeverity = 0.5f;
}
