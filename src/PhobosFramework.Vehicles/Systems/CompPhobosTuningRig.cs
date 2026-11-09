using Verse;
using RimWorld;

namespace Phobos.VehicleSystems;

public sealed class CompProperties_PhobosTuningRig : CompProperties
{
    /// <summary>How many cells from the rig's own edge a vehicle may stand. Authored.</summary>
    public int reach = 3;

    public CompProperties_PhobosTuningRig() => compClass = typeof(CompPhobosTuningRig);
}

/// <summary>What a tuning rig knows: whether it is running and whether a vehicle is close enough. The rule that tunes need one is in <see cref="VehicleFittings"/>.</summary>
public sealed class CompPhobosTuningRig : ThingComp
{
    CompPowerTrader? power;

    public CompProperties_PhobosTuningRig Props => (CompProperties_PhobosTuningRig)props;

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        power = parent.GetComp<CompPowerTrader>();
    }

    public bool Working => power == null || power.PowerOn;

    public bool Reaches(CellRect area) => area.ExpandedBy(Props.reach).Overlaps(parent.OccupiedRect());

    public override string CompInspectStringExtra() => (Working
        ? "PhobosVehicleSystems_RigInspectReady".Translate(Props.reach.Named("CELLS"))
        : "PhobosVehicleSystems_RigInspectOff".Translate()).Resolve();
}
