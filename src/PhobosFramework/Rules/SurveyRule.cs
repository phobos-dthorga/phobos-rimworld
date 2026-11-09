using System;

namespace Phobos.Framework.Rules;

/// <summary>What the survey has to say about its power.</summary>
public enum SurveyNote
{
    None,
    NoPower,
    ArrayRecharging,
    ArrayNoPower,
}

/// <summary>How a survey is running this rare tick.</summary>
public readonly record struct SurveyPower(bool CanScan, bool Fast, bool EngineWanted, bool Recharging, SurveyNote Note);

/// <summary>The survey truck's rules (docs/development/vehicle-survey-truck.md): each find on a map slows the next, and where its power comes from.</summary>
public static class SurveyRule
{
    /// <summary>What a map's earlier finds multiply the time to the next by. The owner's curve: 1.5 a find, capped at 8.</summary>
    public static float TimeFactor(int finds, float step, float cap) => finds <= 0 ? 1f : Math.Min(cap, MathF.Pow(step, finds));

    /// <param name="plugged">On a grid with power to give.</param>
    /// <param name="bigBattery">The large battery is fitted.</param>
    /// <param name="working">Someone is at the console.</param>
    /// <param name="wasRecharging">The engine was recharging the battery last time: it goes on to the high mark.</param>
    public static SurveyPower Decide(bool plugged, bool bigBattery, bool hasFuel, bool arrayFitted, bool arrayOn, bool working, float charge, bool wasRecharging, float low = 0.3f, float high = 0.8f)
    {
        bool wantFast = arrayFitted && arrayOn;
        if (plugged) return new SurveyPower(true, wantFast, false, false, SurveyNote.None);
        if (!bigBattery)
        {
            SurveyNote array = wantFast ? SurveyNote.ArrayNoPower : SurveyNote.None;
            if (hasFuel) return new SurveyPower(true, false, working, false, array);
            return charge > low ? new SurveyPower(true, false, false, false, array) : new SurveyPower(false, false, false, false, SurveyNote.NoPower);
        }
        bool recharging = hasFuel && (charge <= low || (wasRecharging && charge < high));
        if (recharging) return new SurveyPower(!wantFast, false, true, true, wantFast ? SurveyNote.ArrayRecharging : SurveyNote.None);
        if (!hasFuel && charge <= low) return new SurveyPower(false, false, false, false, SurveyNote.NoPower);
        return wantFast ? new SurveyPower(true, true, hasFuel, false, SurveyNote.None) : new SurveyPower(true, false, false, false, SurveyNote.None);
    }
}
