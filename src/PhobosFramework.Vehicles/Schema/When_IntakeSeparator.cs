namespace Phobos.VehicleSystems;

/// <summary>
/// While the aircraft has an intake separator (Framework 0.373.0, for the systems schematic's aircraft page; docs/development/vehicle-inlet-separator.md):
/// a turboshaft aircraft followed in full, with the "Intake separators" setting on and no inlet barrier fitted. Reads what the separator last
/// worked out, never works it out itself.
/// </summary>
public class When_IntakeSeparator : VehicleSystemCondition
{
    protected override bool HoldsRaw(SystemContext context) => context.Comp.IntakeSeparator.Fitted;
}
