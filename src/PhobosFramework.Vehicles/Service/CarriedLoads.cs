using System.Collections.Generic;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// A comp holding something a vehicle carries that is not cargo (Framework 0.182.0; docs/development/farming-batch-one.md, "Where each
/// piece goes"): a tank's fluid, a grain tank's grain, whatever a later mod adds. Its weight loads the engine and the tow as the same
/// weight of cargo would, through <see cref="CarriedLoads"/>. Before this, the two places that weigh a vehicle each knew only tanks.
/// </summary>
public interface IVehicleCarriedLoad
{
    /// <summary>What it holds now, in kilograms.</summary>
    float CarriedKilograms { get; }
}

/// <summary>
/// Every carried load a vehicle has, summed (Framework 0.182.0). Read by the powertrain's mass and by a vehicle's weight as a load (a
/// trailer towed, a vehicle slung). A comp that changes what it holds calls <see cref="Changed"/>, so the engine feels it at once.
/// </summary>
public static class CarriedLoads
{
    /// <summary>The kilograms every carried load of this vehicle holds.</summary>
    public static float Kilograms(VehiclePawn vehicle)
    {
        float total = 0f;
        List<ThingComp> comps = vehicle.AllComps;
        for (int i = 0; i < comps.Count; i++)
        {
            if (comps[i] is IVehicleCarriedLoad load) total += load.CarriedKilograms;
        }
        return total;
    }

    /// <summary>Tells the vehicle's powertrain that a carried load changed, so its mass is worked out afresh.</summary>
    public static void Changed(VehiclePawn vehicle)
    {
        vehicle.GetComp<CompPhobosVehicleSystems>()?.Powertrain?.MarkMassDirty();
        // Towed, its weight is the towing vehicle's to pull: every hitch in front counts again (the fault-read of 21 Nov 2026, B13).
        CompPhobosHitch.WeightChanged(vehicle);
    }
}

/// <summary>
/// A comp on an implement that drags through the ground while it works (Framework 0.182.0; docs/development/farming-machines.md, "Field
/// work"): a plough, a cultivator, a drill's coulters. Its pull loads whatever moves it, through <see cref="Draughts"/>.
/// </summary>
public interface IVehicleDraught
{
    /// <summary>The pull it needs now, in newtons: nothing while it is lifted or not working.</summary>
    double DraughtNewtons { get; }
    /// <summary>The pull it needs while it works, in newtons, whether or not it works now: for the forecast before a tow.</summary>
    double WorkingDraughtNewtons { get; }
}

/// <summary>
/// What a moving vehicle drags (Framework 0.182.0): its own working implements and those of every vehicle behind its hitch (a chain, since
/// 0.193.0). Read by the powertrain each sample, so a plough in the ground costs fuel and speed as real draught does.
/// </summary>
public static class Draughts
{
    /// <summary>The newtons this vehicle and everything it tows drag through the ground now.</summary>
    public static double Newtons(VehiclePawn vehicle)
    {
        double total = Of(vehicle);
        foreach (VehiclePawn behind in CompPhobosHitch.Behind(vehicle)) total += Of(behind);
        return total;
    }

    static double Of(VehiclePawn vehicle)
    {
        double total = 0;
        List<ThingComp> comps = vehicle.AllComps;
        for (int i = 0; i < comps.Count; i++)
        {
            if (comps[i] is IVehicleDraught draught) total += System.Math.Max(0, draught.DraughtNewtons);
        }
        return total;
    }

    /// <summary>The newtons this vehicle and everything behind it would drag while working, lowered or not now: what a forecast asks.</summary>
    public static double Working(VehiclePawn vehicle)
    {
        double total = WorkingOf(vehicle);
        foreach (VehiclePawn behind in CompPhobosHitch.Behind(vehicle)) total += WorkingOf(behind);
        return total;
    }

    static double WorkingOf(VehiclePawn vehicle)
    {
        double total = 0;
        List<ThingComp> comps = vehicle.AllComps;
        for (int i = 0; i < comps.Count; i++)
        {
            if (comps[i] is IVehicleDraught draught) total += System.Math.Max(0, draught.WorkingDraughtNewtons);
        }
        return total;
    }
}
