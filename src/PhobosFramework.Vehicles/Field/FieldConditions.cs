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
    // The vehicle's own implement (a combine's front and drum), never one it carries or pulls behind (the fault-read of 21 Nov 2026, D3:
    // any vehicle able to do a field run counted, so every tractor gave 40 % of its engine to a threshing drum while it ploughed). What an
    // implement behind takes is its draught.
    protected override bool HoldsRaw(SystemContext context)
    {
        if (context.Vehicle.GetComp<CompPhobosFieldRun>() == null) return false;
        System.Collections.Generic.List<Verse.ThingComp> comps = context.Vehicle.AllComps;
        for (int i = 0; i < comps.Count; i++)
        {
            if (comps[i] is IVehicleFieldWorker) return true;
        }
        return false;
    }
}

/// <summary>While the vehicle has a bulk hold with an auger (Framework 0.182.0).</summary>
public class When_BulkHoldFitted : VehicleSystemCondition
{
    protected override bool HoldsRaw(SystemContext context) => context.Vehicle.GetComp<CompPhobosBulkHold>() != null;
}
