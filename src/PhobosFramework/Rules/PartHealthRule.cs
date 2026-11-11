using System;

namespace Phobos.Framework.Rules;

/// <summary>
/// The health a part we append to a vehicle has after a load, game-free (Framework 0.83.1). Vehicle Framework restores part health by a
/// part's place in the list, so an appended part new since the save, or one whose place moved, can arrive with another part's health, even
/// above its own maximum (the owner's L-ATV, 11 Nov 2026: a drive belt at 300% and a fuel pump at 150%, both holding 30). Our own saved
/// value wins; a part a save of ours never held was fitted new; and no part ever holds more than its maximum or less than nothing.
/// </summary>
public static class PartHealthRule
{
    /// <summary>
    /// The part's health after a load. <paramref name="saved"/> is the value our save held for it, if any; <paramref name="saveHeldParts"/>
    /// says whether our save held any part healths at all (a fresh vehicle or a save from before they were kept holds none, and then the
    /// current health is trusted, only capped).
    /// </summary>
    public static double Restored(double? saved, bool saveHeldParts, double current, double max)
    {
        double health = saved ?? (saveHeldParts ? max : current);
        return Math.Max(0, Math.Min(health, max));
    }
}
