namespace Phobos.Dosing.Rules;

/// <summary>
/// Whether a disease is winning: the game's own race between severity reaching the lethal mark and immunity reaching 1, from the
/// current figures and rates, with a margin in days so a doctor acts before the last moment. A disease that is not getting worse is
/// not winning, whatever its figures. Rates change with care and rest, so this is a reading of now, not a forecast. Game-free.
/// </summary>
public static class ImmunityRule
{
    public static bool Losing(float severity, float lethalSeverity, float severityPerDay, float immunity, float immunityPerDay, float marginDays)
    {
        if (lethalSeverity <= 0f || severityPerDay <= 0f || immunity >= 1f) return false;
        float daysToLethal = (lethalSeverity - severity) / severityPerDay;
        if (immunityPerDay <= 0f) return true;
        float daysToImmune = (1f - immunity) / immunityPerDay;
        return daysToImmune + marginDays >= daysToLethal;
    }
}
