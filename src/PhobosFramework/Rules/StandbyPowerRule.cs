using System;

namespace Phobos.Framework.Rules;

/// <summary>
/// The kinds of building standby power looks after (Framework 0.222.0; docs/development/standby-power.md). Each is found by what the
/// building is and does, never by its name, so other mods' buildings of the same kind join in.
/// </summary>
public enum StandbyKind
{
    /// <summary>A powered light that does nothing else: dark when no one awake is in its room.</summary>
    Light,
    /// <summary>A workbench or a research bench: full power while someone works at it.</summary>
    Machine,
    /// <summary>Something people watch for recreation, a television: full power while someone watches it.</summary>
    Watch,
    /// <summary>A powered turret: full power while it has a target.</summary>
    Turret,
    /// <summary>A powered plant grower, a hydroponics basin: full power while anything grows in it.</summary>
    Grower,
    /// <summary>A powered facility linked to beds or benches, a vitals monitor: full power while what it serves is in use.</summary>
    Facility,
    /// <summary>A powered door: full power while it stands open.</summary>
    Door,
}

/// <summary>
/// Standby power's rule (Framework 0.222.0; the owner's choices of 22 Nov 2026, docs/development/standby-power.md): a building draws its
/// listed power while in use and a share of it, the standby draw, while idle; a light that is off draws nothing. Game-free.
/// </summary>
public static class StandbyPowerRule
{
    /// <summary>How often a map looks at who is using what, ticks: once a second of game time. Agent default [A]: the owner wanted no work
    /// every tick, and a light or a bench a second late is not seen in play.</summary>
    public const int SweepTicks = 60;
    /// <summary>The standby draw, percent of the listed power (the owner's choice of 22 Nov 2026: 10%, realistic, a setting).</summary>
    public const int DefaultStandbyPercent = 10;
    /// <summary>The standby draws the setting offers, percent: round values as buttons (the owner's preference for presets).</summary>
    public static readonly int[] StandbyPercents = { 0, 5, 10, 20, 30 };
    /// <summary>The largest standby draw the rule accepts, percent: past half, standby saves too little to be worth the name.</summary>
    public const int MaxStandbyPercent = 50;
    /// <summary>A switched-off light's draw, percent of its listed power. Agent default [A]: a light that is off draws nothing.</summary>
    public const float LightOffPercent = 0f;
    /// <summary>Watts within which two draws count as the same: the game stores them as floats.</summary>
    public const float SameWatts = 0.01f;

    /// <summary>The standby percent kept within the rule's range.</summary>
    public static int ClampPercent(int percent) => Math.Max(0, Math.Min(MaxStandbyPercent, percent));

    /// <summary>
    /// The watts a building draws now: its listed power in use (the owner's choice: no more than listed), its standby share idle, or
    /// nothing for a light that is off. Never negative.
    /// </summary>
    public static float Draw(float listedWatts, bool inUse, StandbyKind kind, int standbyPercent)
    {
        float listed = Math.Max(0f, listedWatts);
        if (inUse) return listed;
        float percent = kind == StandbyKind.Light ? LightOffPercent : ClampPercent(standbyPercent);
        return listed * percent / 100f;
    }

    /// <summary>
    /// Whether a building's draw as found is one standby power or the game set: the draw it last wrote, the listed power (the game sets
    /// it again when the building joins a grid or a map loads), or nothing. Anything else means another mod sets this building's draw
    /// itself, and standby power leaves it alone. Draws are positive watts.
    /// </summary>
    public static bool DrawIsOursOrTheGames(float foundWatts, float lastWrittenWatts, float listedWatts)
    {
        return Same(foundWatts, lastWrittenWatts) || Same(foundWatts, listedWatts) || Same(foundWatts, 0f);
    }

    /// <summary>Whether two draws are the same within <see cref="SameWatts"/>.</summary>
    public static bool Same(float a, float b) => Math.Abs(a - b) <= SameWatts;

    /// <summary>
    /// Whether a light should be lit: always outdoors (left to the game), indoors only while someone awake is in its room. With the kind
    /// switched off it is always lit.
    /// </summary>
    public static bool LightWanted(bool kindOn, bool indoors, bool someoneAwakeInRoom) => !kindOn || !indoors || someoneAwakeInRoom;

    /// <summary>Whether a map looks this tick: once every <see cref="SweepTicks"/>, each map at its own offset so two maps share the load.</summary>
    public static bool SweepDue(int tick, int mapIndex) => Mod(tick + mapIndex * 7, SweepTicks) == 0;

    static int Mod(int value, int by) => ((value % by) + by) % by;
}
