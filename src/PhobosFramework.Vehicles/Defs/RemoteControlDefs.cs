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
    /// <summary>Keeping to radio cover (Framework 0.279.0): the extra cost of a cell where a vehicle driven from afar would lose its link.
    /// Vehicle Framework averages it over the vehicle's footprint and weighs it ten to one against a step of ten, so 8 makes each step
    /// wholly out of cover worth about eight cells of detour.</summary>
    public int uncoveredCellCost = 8;

    // The route level (Framework 0.281.0; autonomous-vehicles.md, step 7a). All authored, agent defaults for the owner to revise.
    /// <summary>The most followers a leader may have (the owner, 18 Nov 2026: four, as the US Army's leader-follower trucks were fielded).</summary>
    public int maxFollowers = 4;
    /// <summary>Clear cells a follower keeps between itself and the vehicle ahead, beyond both their half lengths.</summary>
    public int followGapCells = 2;
    /// <summary>A follower this near its aim gives no new order; one whose drive ends this far off the aim gives a fresh one.</summary>
    public float followNearCells = 2f;
    public float followSlackCells = 4f;
    /// <summary>How far round its aim a vehicle looks for a place to stand, in rings of cells.</summary>
    public int standRingCells = 4;
    /// <summary>A loop's point counts as reached within this many cells.</summary>
    public float loopArriveCells = 3f;
    /// <summary>How long a vehicle on a loop waits at each stop for its loading or unloading, in ticks (2,500: an hour of the game's).</summary>
    public int loopDwellTicks = 2500;
    /// <summary>The mechanitor level (Framework 0.282.0, step 7b): a land vehicle of this empty mass or less costs 1 bandwidth, a heavier one 2,
    /// a heavy or tracked one 3 (the owner's card: by size). Light and medium share one class of loadout, so their line is this mass [A].</summary>
    public float mechLightMaxKg = 4000f;

    public static PhobosRemoteControlRulesDef Current => Current<PhobosRemoteControlRulesDef>();
}
