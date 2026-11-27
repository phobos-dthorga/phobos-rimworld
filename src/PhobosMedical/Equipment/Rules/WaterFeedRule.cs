namespace Phobos.Medical.Rules;

/// <summary>What a water-fed producer (the oxygen electrolyser with Dubs Bad Hygiene) does with its plumbing at a check.</summary>
public enum WaterFeedStep
{
    /// <summary>Nothing: switched off, unpowered, or idle with its oxygen storage full.</summary>
    Nothing,
    /// <summary>Draws the water for the coming interval: it is producing.</summary>
    Draw,
    /// <summary>Looks whether water has come back, without drawing any: it stopped for want of water.</summary>
    Look,
}

/// <summary>
/// When a water-fed producer takes its water (the owner's card of 28 Nov 2026, "check both, fix if real": it used to draw whenever it
/// was switched on and powered, idle or not). Game-free.
/// </summary>
public static class WaterFeedRule
{
    /// <param name="running">Switched on and powered.</param>
    /// <param name="producing">Actually making oxygen: its pipe-network trader is on and not idling.</param>
    /// <param name="shutForWater">Its last draw found no water, so it stopped to wait for some.</param>
    public static WaterFeedStep Step(bool running, bool producing, bool shutForWater)
    {
        if (!running) return WaterFeedStep.Nothing;
        if (shutForWater) return WaterFeedStep.Look;
        return producing ? WaterFeedStep.Draw : WaterFeedStep.Nothing;
    }
}
