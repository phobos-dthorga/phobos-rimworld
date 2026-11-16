namespace Phobos.Framework.Vehicles.Rules;

/// <summary>What a powered fitting is short of, the first that applies.</summary>
public enum FeedShortfall
{
    None,
    /// <summary>It needs the engine running, and it is off.</summary>
    EngineOff,
    /// <summary>Its electrical load cannot run: shed, or the battery flat.</summary>
    NoPower,
    /// <summary>What it draws on (hydraulic fluid, air) is too low.</summary>
    Short,
}

/// <summary>
/// One rule for whatever feeds a fitting (owner, 13 Nov 2026: "an accessory uses up something real"; docs/development/vehicle-knocked-out-systems.md,
/// W4). Game-free: the service reads the vehicle, this decides what to say.
/// </summary>
public static class FeedRule
{
    /// <summary>The shortfall to name, engine first (nothing else runs without it), then power, then what it draws on.</summary>
    public static FeedShortfall Check(bool needsEngine, bool engineRunning, bool needsPower, bool powerCanRun, bool needsQuantity, double quantity, double atLeast)
    {
        if (needsEngine && !engineRunning) return FeedShortfall.EngineOff;
        if (needsPower && !powerCanRun) return FeedShortfall.NoPower;
        if (needsQuantity && quantity < atLeast) return FeedShortfall.Short;
        return FeedShortfall.None;
    }
}
