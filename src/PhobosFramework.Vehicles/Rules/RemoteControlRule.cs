namespace Phobos.Framework.Vehicles.Rules;

/// <summary>What a vehicle under remote control does when its link is lost: the player's choice per vehicle (owner, 24 Nov 2026: "hold by default").</summary>
public enum LostLinkChoice
{
    /// <summary>Stop where it is and wait for the link.</summary>
    Hold,
    /// <summary>Drive back toward the station it last heard, until the link returns.</summary>
    ComeBack,
    /// <summary>Finish the drive it was given, then wait.</summary>
    CarryOn,
}

/// <summary>Where a vehicle under remote control stands with its link.</summary>
public enum RemoteState
{
    /// <summary>Not asked to be driven from a station: its own crew, or nobody, drives it.</summary>
    Off,
    /// <summary>Asked, but no station has had it yet: it waits, and nothing is lost.</summary>
    Unlinked,
    /// <summary>A station has it: it drives as if crewed.</summary>
    Linked,
    /// <summary>The link has just gone: it carries on its last order while the delay runs, in case the link comes back.</summary>
    Waiting,
    /// <summary>The delay ran out and it holds where it is.</summary>
    Holding,
    /// <summary>The delay ran out and it drives back toward its station.</summary>
    ComingBack,
    /// <summary>The delay ran out and it finishes its last drive.</summary>
    CarryingOn,
}

/// <summary>
/// The remote control of vehicles (Framework 0.278.0; docs/development/autonomous-vehicles.md, step 3): how a vehicle's state follows its
/// link, whoever provides the link (the radio today; a route or a mechanitor later). Game-free and tested; the delay is the rules Def's
/// (authored: ten seconds at normal speed, as real procedures wait before acting on a lost link).
/// </summary>
public static class RemoteControlRule
{
    /// <summary>
    /// The state after this look. <paramref name="lostFor"/> is how long the link has been gone, in ticks (0 when it is not);
    /// <paramref name="moving"/> whether the vehicle is driving now. A link that returns at any point takes it back; one never had
    /// is waited for, not acted on; acting on a loss starts only once the delay has run.
    /// </summary>
    public static RemoteState Next(RemoteState state, bool wanted, bool linked, int lostFor, int delayTicks, LostLinkChoice choice, bool moving)
    {
        if (!wanted) return RemoteState.Off;
        if (linked) return RemoteState.Linked;
        switch (state)
        {
            case RemoteState.Off:
            case RemoteState.Unlinked:
                return RemoteState.Unlinked;
            case RemoteState.Linked:
                return delayTicks <= 0 ? Act(choice, moving) : RemoteState.Waiting;
            case RemoteState.Waiting:
                return lostFor >= delayTicks ? Act(choice, moving) : RemoteState.Waiting;
            case RemoteState.ComingBack:
            case RemoteState.CarryingOn:
                // Arrived, or stuck: either way it holds now.
                return moving ? state : RemoteState.Holding;
            default:
                return RemoteState.Holding;
        }
    }

    static RemoteState Act(LostLinkChoice choice, bool moving) => choice switch
    {
        LostLinkChoice.ComeBack => RemoteState.ComingBack,
        LostLinkChoice.CarryOn when moving => RemoteState.CarryingOn,
        _ => RemoteState.Holding,
    };

    /// <summary>Whether the vehicle may drive in this state, with nobody at its wheel.</summary>
    public static bool Drives(RemoteState state) =>
        state is RemoteState.Linked or RemoteState.Waiting or RemoteState.ComingBack or RemoteState.CarryingOn;

    /// <summary>Whether this state acts on a lost link (the player is told when it starts).</summary>
    public static bool Acting(RemoteState state) =>
        state is RemoteState.Holding or RemoteState.ComingBack or RemoteState.CarryingOn;

    /// <summary>Whether the player is told the link is lost: the vehicle has just started acting on it.</summary>
    public static bool TellsLost(RemoteState before, RemoteState after) => !Acting(before) && Acting(after);

    /// <summary>Whether the player is told the link is back: a vehicle that was acting on a loss has its station again.</summary>
    public static bool TellsFound(RemoteState before, RemoteState after) => Acting(before) && after == RemoteState.Linked;
}
