using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// A tank trailer's electric pump on the towing vehicle's battery (Framework 0.283.0; the owner's card of 25 Nov 2026, "power point and tow
/// vehicle's battery"): a trailer that pumps while packed (the shower trailer), standing on a hitch whose vehicle has stopped, runs its pump
/// through its lead as one more load on that vehicle's electrics, drawing the pump's own watts while it pumps. Powered only while that load
/// runs: switched on, not shed, the battery above flat. Built as the cold box in tow is (ColdInTow).
/// </summary>
public static class TrailerPumpInTow
{
    /// <summary>The load's name in the rules Def's tow loads.</summary>
    public const string LoadName = "trailerPump";

    /// <summary>The watts the electric pumps of the trailers behind this vehicle ask now.</summary>
    public static float Watts(VehiclePawn tractor)
    {
        float watts = 0f;
        foreach (VehiclePawn towed in CompPhobosHitch.Behind(tractor))
        {
            if (towed.GetComp<CompPhobosTank>() is { } tank) watts += tank.PumpWattsWanted;
        }
        return watts;
    }

    /// <summary>Whether a trailer behind this vehicle has an electric pump that needs a lead (fitted on a trailer that pumps packed).</summary>
    public static bool Any(VehiclePawn tractor)
    {
        foreach (VehiclePawn towed in CompPhobosHitch.Behind(tractor))
        {
            if (towed.GetComp<CompPhobosTank>() is { Props: { needsPumpPower: true, pumpsPacked: true }, ElectricPump: true }) return true;
        }
        return false;
    }

    /// <summary>The towing vehicle's look found the load running: every trailer pump behind it that asks for power is told it has it.</summary>
    public static void Power(VehiclePawn tractor)
    {
        int now = Find.TickManager.TicksGame;
        foreach (VehiclePawn towed in CompPhobosHitch.Behind(tractor))
        {
            if (towed.GetComp<CompPhobosTank>() is not { PumpWantsPower: true } tank) continue;
            tank.PoweredInTow(now);
            if (Seen.Wants("trailers.electric-pump-in-tow", towed)) Seen.Note("trailers.electric-pump-in-tow", towed, tank.PumpWattsWanted.ToString("0") + " W");
        }
    }
}

/// <summary>A trailer with an electric pump on a lead rides behind this vehicle; with <c>inUse</c>, one whose pump is running (it draws).</summary>
public class When_TowedPump : VehicleSystemCondition
{
    public bool inUse;

    protected override bool HoldsRaw(SystemContext context) =>
        context.Comp.Hitch is { Trailer: not null } && (inUse ? TrailerPumpInTow.Watts(context.Vehicle) > 0f : TrailerPumpInTow.Any(context.Vehicle));
}

/// <summary>What the trailer pumps behind draw now, in amps at this vehicle's voltage: their own watts, from their Defs.</summary>
public class Input_TowedPumpAmps : VehicleSystemInput
{
    protected override float Raw(SystemContext context)
    {
        float volts = context.Comp.ElectricalVolts();
        return volts > 0f ? TrailerPumpInTow.Watts(context.Vehicle) / volts : 0f;
    }
}
