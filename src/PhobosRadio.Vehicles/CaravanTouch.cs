using Phobos.Radio.Physics;
using Phobos.Radio.Services;
using Phobos.VehicleSystems;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Phobos.Radio.Vehicles;

/// <summary>
/// The in-touch mood on the road (Radio 0.44.0; the owner's card of 27 Nov 2026, for any caravan of the player's, on foot or in vehicles):
/// a caravan's reach home as D'thorga Framework last kept it, as an outcome. In touch is workable or better (by Morse too); short of that,
/// the direct link's margin, which may be faint. The mood only reads it.
/// </summary>
public static class CaravanTouch
{
    public static LinkOutcome? KeptOutcome(Caravan caravan)
    {
        if (ConvoyRadio.Kept(caravan) is not { } reach) return null;
        LinkOutcome outcome = RadioRulesDef.Current.MarginBands.Classify(reach.MarginDb);
        if (reach.InTouch && outcome < LinkOutcome.Workable) return LinkOutcome.Workable;
        if (!reach.InTouch && outcome >= LinkOutcome.Workable) return LinkOutcome.Marginal;
        return outcome;
    }
}

/// <summary>
/// Keeps each of the player's caravans' reach home fresh while the in-touch mood is on (Radio 0.44.0), so the mood has an answer to read.
/// <para>Cost: with the mood setting on, once a tenth of a game hour, one reach check for each of the player's caravans with people in it:
/// a look for its radio, then for one with a radio the relay search over a handful of nodes. It renews the places a caravan in reach holds
/// at relays, as a convoy's own checks do. Nothing with the setting off. Saves nothing.</para>
/// </summary>
public sealed class WorldComponent_PhobosCaravanTouch : WorldComponent
{
    /// <summary>Where in the tenth of an hour the look falls, apart from the road work's own (authored).</summary>
    const int Offset = 97;

    public WorldComponent_PhobosCaravanTouch(World world) : base(world) { }

    public override void WorldComponentTick()
    {
        if (!RadioMod.Settings.inTouchThought || Find.TickManager.TicksGame % RoadWork.Interval != Offset) return;
        foreach (Caravan caravan in Find.WorldObjects.Caravans)
        {
            if (caravan.Faction != Faction.OfPlayer || !caravan.PawnsListForReading.Exists(p => p.RaceProps.Humanlike)) continue;
            _ = ConvoyRadio.Of(caravan);
        }
    }
}
