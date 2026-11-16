using Phobos.Framework.Vehicles.Rules;
using Vehicles;

namespace Phobos.VehicleSystems;

/// <summary>
/// Vehicle Framework's body integrity, the figure it destroys a vehicle by when it falls under 1 %, with our empty mounting points left
/// out (owner, 16 Nov 2026; docs/development/vehicle-health-design.md). Set on the BodyIntegrity stat by an XML patch, in place of
/// Vehicle Framework's own worker, so Vehicle Framework's damage and Combat Extended's both read it with no Harmony. Every other part,
/// the author's and ours that do a job, counts exactly as before. A vehicle without our systems is summed exactly as Vehicle Framework
/// sums it. The figure's stat parts and offsets still apply, through the base class.
/// </summary>
public class VehicleStatWorker_PhobosBodyIntegrity : VehicleStatWorker
{
    public override float TransformValue(VehiclePawn vehicle, float value)
    {
        var uncounted = vehicle.GetComp<CompPhobosVehicleSystems>()?.Props.uncountedKeys;
        var rule = new IntegrityRule();
        foreach (VehicleComponent component in vehicle.statHandler.components)
        {
            bool counts = uncounted == null || uncounted.Count == 0 || !uncounted.Contains(component.props.key);
            rule.Add(component.Health, component.MaxHealth, counts);
        }
        return base.TransformValue(vehicle, (float)rule.Fraction());
    }
}
