namespace Phobos.Medical.Rules;

/// <summary>What an alarm is sounding for. Flags, so smoke and carbon monoxide can sound together.</summary>
[System.Flags]
public enum AlarmGases
{
    None = 0,
    CarbonMonoxide = 1,
    Smoke = 2,
}

/// <summary>
/// When a carbon monoxide and smoke alarm sounds. Game-free. It starts at a gas's alarm level and stops only when the gas falls below a
/// share of it, so it doesn't flicker on and off at the edge (as real alarms hold until the air clears). The levels are authored: the
/// game's gases are fractions of a cell or a room, not parts per million.
/// </summary>
public static class GasAlarmRule
{
    public static AlarmGases Next(AlarmGases previous, float carbonMonoxide, float smoke, float carbonMonoxideLevel, float smokeLevel, float quietShare)
    {
        AlarmGases next = AlarmGases.None;
        if (Sounds(previous, AlarmGases.CarbonMonoxide, carbonMonoxide, carbonMonoxideLevel, quietShare)) next |= AlarmGases.CarbonMonoxide;
        if (Sounds(previous, AlarmGases.Smoke, smoke, smokeLevel, quietShare)) next |= AlarmGases.Smoke;
        return next;
    }

    /// <summary>A gas it wasn't sounding for has joined: time to warn and wake the room.</summary>
    public static bool NewWarning(AlarmGases previous, AlarmGases next) => (next & ~previous) != AlarmGases.None;

    static bool Sounds(AlarmGases previous, AlarmGases gas, float amount, float level, float quietShare)
    {
        if (level <= 0f) return false;
        if (amount >= level) return true;
        return (previous & gas) != AlarmGases.None && amount >= level * quietShare;
    }
}
