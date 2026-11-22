using System.Collections.Generic;
using Phobos.Framework.Defs;
using Phobos.Framework.Vehicles.Rules;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>One part role and the zone of the vehicle a fire it lights starts in.</summary>
public class PhobosFireZoneRole
{
    public string role = "";
    public FireZones zone = FireZones.EngineBay;
}

/// <summary>
/// Fighting fires on vehicles (Framework 0.241.0; docs/development/vehicle-fire.md, FR10; the owner's answers of 23 Nov 2026): where a fire
/// starts and spreads, the hand extinguisher and its refill, the two suppression systems and their recharge, and which other authors'
/// vehicles count as armoured. The first loaded instance wins; another mod may patch it in XML. Every figure is authored.
/// </summary>
public class PhobosFireFightingRulesDef : PhobosRulesDef
{
    // ---- Zones ----
    /// <summary>The zone a fire starts in when the part hit just before it plays this role.</summary>
    public List<PhobosFireZoneRole> roleZones = new();
    /// <summary>A hit this many hours before a fire caught is taken as its cause.</summary>
    public float hitLitHours = 0.05f;
    /// <summary>Where a fire with no known cause starts.</summary>
    public FireZones fallbackZone = FireZones.EngineBay;
    /// <summary>Hours a zone burns before it sets the next one alight.</summary>
    public float gearToEngineHours = 0.4f;
    public float engineToCrewHours = 0.25f;

    // ---- The hand extinguisher ----
    [MustTranslate] public string bracketLabel = "extinguisher bracket";
    public int bracketHealth = 20;
    /// <summary>Uses in a full extinguisher.</summary>
    public int extinguisherCharges = 3;
    /// <summary>How much of a fire one use knocks down, in the game's fire sizes (0.1 to 1.75).</summary>
    public float knockDown = 0.75f;
    /// <summary>Ticks a colonist outside spends using it.</summary>
    public int useTicks = 90;
    public ThingDef? refill;
    public float refillWork = 300f;

    // ---- Automatic suppression ----
    [MustTranslate] public string engineSystemLabel = "engine fire suppression";
    [MustTranslate] public string crewSystemLabel = "crew fire suppression";
    public int systemHealth = 30;
    public ThingDef? bottle;
    public float rechargeWork = 600f;

    // ---- Other authors' vehicles ----
    /// <summary>The outer plates' sharp armour from which another author's vehicle counts as armoured, without and with Combat Extended
    /// (whose figures are millimetres of steel).</summary>
    public float armouredFrom = 0.8f;
    public float armouredFromCombatExtended = 8f;

    public static PhobosFireFightingRulesDef Rules => Current<PhobosFireFightingRulesDef>();
}

/// <summary>
/// Listed on an automatic suppression upgrade node (Framework 0.241.0, FR10(b)): fitting the node fits a full system covering its zone;
/// removing it takes the system out. Its state (ready or spent) is the fire-fighting comp's, saved there, so nothing runs after a load.
/// </summary>
public class PhobosSuppressionUpgrade : Upgrade
{
    public FireZones zone = FireZones.EngineBay;

    public override bool UnlockOnLoad => false;

    public override void Unlock(VehiclePawn vehicle, bool unlockingPostLoad)
    {
        if (!unlockingPostLoad) FireFighting.SystemFitted(vehicle, zone, fitted: true);
    }

    public override void Refund(VehiclePawn vehicle) => FireFighting.SystemFitted(vehicle, zone, fitted: false);
}

/// <summary>
/// Equipment fitted to another author's vehicle by its Def (Framework 0.241.0; FR10's "fitted equipment" extension, which the spall liner
/// and the glass are to share): whether it counts as armoured, where its armour figure alone would say otherwise. Added by an XML patch.
/// </summary>
public class PhobosFittedEquipmentExtension : DefModExtension
{
    /// <summary>True or false marks the vehicle armoured or not; unset leaves it to its armour.</summary>
    public bool? armoured;
}
