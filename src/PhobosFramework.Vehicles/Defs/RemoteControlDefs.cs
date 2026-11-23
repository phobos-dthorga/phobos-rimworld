using Phobos.Framework.Defs;

namespace Phobos.VehicleSystems;

/// <summary>
/// The remote control of vehicles' figures (Framework 0.278.0; docs/development/autonomous-vehicles.md, step 3). The first loaded instance
/// wins; another mod may patch it in XML. Every figure is authored, an agent default for the owner to revise.
/// </summary>
public class PhobosRemoteControlRulesDef : PhobosRulesDef
{
    /// <summary>How long a lost link is waited out before the vehicle acts on it, in ticks (600: ten seconds at normal speed).</summary>
    public int lostLinkDelayTicks = 600;
    /// <summary>How far round its station a vehicle coming back looks for a place to stop, in cells.</summary>
    public int comeBackRing = 5;

    public static PhobosRemoteControlRulesDef Current => Current<PhobosRemoteControlRulesDef>();
}
