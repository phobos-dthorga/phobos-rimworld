namespace Phobos.VehicleSystems;

/// <summary>
/// What a powered fitting needs to work (Framework 0.115.0; owner, 13 Nov 2026: "an accessory uses up something real";
/// docs/development/vehicle-knocked-out-systems.md, W4). One shape for the winch, the field kitchen's power lead, a fridge and anything
/// after: Feeds answers whether it can run and, if not, why. Any of the three may be left out.
/// </summary>
public class FeedSpec
{
    /// <summary>The electrical load (in the vehicle's loadout) that powers it: it must be fitted, not shed, and the battery above flat.</summary>
    public string? load;
    /// <summary>A quantity of the vehicle's systems it draws on (hydraulicSupply, airPressure), and the least it needs.</summary>
    public string? quantity;
    public float atLeast;
    /// <summary>It works only with the engine running (an engine-driven pump, a power take-off).</summary>
    public bool engineRunning;
}
