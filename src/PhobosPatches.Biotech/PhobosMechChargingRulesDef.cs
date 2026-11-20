using Phobos.Framework.Defs;

namespace Phobos.BiotechPatches;

/// <summary>
/// The figures behind mech rechargers that draw real power (D'thorga Patches 0.28.0; docs/development/mech-recharging-real-power.md).
/// The first loaded instance wins; another mod may patch it in XML. <b>Every figure is authored</b>, chosen so the common light mechs
/// (body size 0.7) draw the 200 W the game's own recharger lists: 0.7 x 480 Wd x half a battery a day / 0.84 = 200 W.
/// </summary>
public class PhobosMechChargingRulesDef : PhobosRulesDef
{
    /// <summary>Watt-days a mech's battery holds for each point of body size, at the game's standard 100 units of mech energy.</summary>
    public float wattDaysPerBodySize = 480f;
    /// <summary>The mech energy the game counts as a standard battery (RaceProperties.maxMechEnergy's default); a mech whose maximum
    /// another mod raises holds that much more, and takes that much longer to fill at the same power.</summary>
    public float standardMaxEnergy = 100f;
    /// <summary>The share of the wall's power that reaches the battery; the rest is lost as heat.</summary>
    public float efficiency = 0.84f;
    /// <summary>The charge level (0 to 1) from which charging eases off.</summary>
    public float easesFrom = 0.8f;
    /// <summary>The share of the full charging rate left when the battery is full.</summary>
    public float rateWhenFull = 0.25f;
    /// <summary>The share of a recharger's listed power it draws with no mech on it, where its Def gives no idle figure of its own.</summary>
    public float idleShare = 0.05f;
    /// <summary>Heat pushed into the room each second for each watt lost: the game's own heater's rate (21 heat a second for 175 W).</summary>
    public float heatPerSecondPerWatt = 21f / 175f;
    /// <summary>A recharger stops warming a room hotter than this. A gameplay limit, so a sealed bay cannot climb without end.</summary>
    public float heatUpToCelsius = 80f;
}
