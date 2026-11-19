namespace Phobos.VehicleSystems;

/// <summary>
/// While the vehicle drives a row of a field run, its implement working (Framework 0.182.0): the condition an implement's work load on
/// the engine ledger switches on by, such as a combine's threshing drum.
/// </summary>
public class When_FieldWorking : VehicleSystemCondition
{
    protected override bool HoldsRaw(SystemContext context) => context.Vehicle.GetComp<CompPhobosFieldRun>()?.WorkingRow == true;
}

/// <summary>While the vehicle's bulk hold pours through its auger (Framework 0.182.0): the auger's work load.</summary>
public class When_BulkPouring : VehicleSystemCondition
{
    protected override bool HoldsRaw(SystemContext context) => context.Vehicle.GetComp<CompPhobosBulkHold>()?.Pouring == true;
}

/// <summary>While the vehicle has an implement that works fields (Framework 0.182.0): which vehicles carry an implement's work load at all.</summary>
public class When_FieldWorkerFitted : VehicleSystemCondition
{
    protected override bool HoldsRaw(SystemContext context) => context.Vehicle.GetComp<CompPhobosFieldRun>() != null;
}

/// <summary>While the vehicle has a bulk hold with an auger (Framework 0.182.0).</summary>
public class When_BulkHoldFitted : VehicleSystemCondition
{
    protected override bool HoldsRaw(SystemContext context) => context.Vehicle.GetComp<CompPhobosBulkHold>() != null;
}
