using System.Runtime.CompilerServices;
using Phobos.Framework.Environment;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// Camp lights on a vehicle's battery (Framework 0.231.0; docs/development/vehicle-accessories.md, "Built: camp lights"). A 12-volt camp light
/// on a pole is camp gear: set out with the camp, it is fed by the vehicle whose camp it belongs to, as one more load on that vehicle's
/// electrics (the rules Def's camp loads). It lights while that load runs: switched on, not shed, the battery above flat; since Framework
/// 0.276.3 on the house battery when one is fitted, and otherwise out before the starting battery is too low to start the engine. Set down by hand, or
/// left when the camp is packed up, it belongs to no camp and stays dark. The lantern burns its own chemfuel and needs none of this.
/// </summary>
[StaticConstructorOnStartup]
public static class CampLights
{
    /// <summary>The load's name in the rules Def's camp loads.</summary>
    public const string LoadName = "campLights";

    sealed class Owner
    {
        public CompPhobosCamp? camp;
        public int checkedAt = -99999;
    }

    // Which camp each light belongs to, found by a look at the map's camps and kept a while. Weak: a light gone is forgotten.
    static readonly ConditionalWeakTable<Thing, Owner> owners = new();
    const int KeepTicks = 2500;

    static CampLights()
    {
        CompPhobosNightLight.Supply = Powered;
        CompPhobosNightLight.DarkWhy = DarkWhy;
    }

    /// <summary>The camp a light belongs to now, or null.</summary>
    public static CompPhobosCamp? CampOf(Thing light)
    {
        if (!light.Spawned) return null;
        Owner owner = owners.GetOrCreateValue(light);
        int now = Find.TickManager.TicksGame;
        if (now - owner.checkedAt < KeepTicks && now >= owner.checkedAt && owner.camp is { } kept && kept.Placed.Contains(light)) return kept;
        owner.checkedAt = now;
        owner.camp = null;
        foreach (Pawn pawn in light.Map.mapPawns.AllPawnsSpawned)
        {
            if (pawn is not VehiclePawn vehicle || vehicle.GetComp<CompPhobosCamp>() is not { } camp || !camp.Placed.Contains(light)) continue;
            owner.camp = camp;
            break;
        }
        return owner.camp;
    }

    /// <summary>Whether this light has power now: its camp's vehicle runs the camp lights load.</summary>
    static bool Powered(Thing light)
    {
        if (CampOf(light) is not { } camp || CompPhobosVehicleSystems.Of(camp.Vehicle) is not { } systems) return false;
        bool on = systems.LoadRunning(LoadName);
        if (on && Seen.Wants("vehicles.camp-pole-light", camp.Vehicle)) Seen.Note("vehicles.camp-pole-light", camp.Vehicle, light.LabelShort);
        return on;
    }

    /// <summary>Why this light is dark, when its vehicle keeps the camp lights off to save its start (Framework 0.276.3), or null.</summary>
    static string? DarkWhy(Thing light) =>
        CampOf(light) is { } camp && CompPhobosVehicleSystems.Of(camp.Vehicle) is { } systems ? systems.KeptForStartWhy(LoadName) : null;

    /// <summary>The watts this vehicle's camp lights ask now: each one that wants to be lit, its own watts; with <paramref name="onlyInUse"/>
    /// false, every one set out, lit or not.</summary>
    public static float Watts(VehiclePawn vehicle, bool onlyInUse)
    {
        if (vehicle.GetComp<CompPhobosCamp>() is not { } camp) return 0f;
        float watts = 0f;
        foreach (Thing piece in camp.Placed)
        {
            if (piece is not ThingWithComps { Spawned: true } lamp || lamp.GetComp<CompPhobosNightLight>() is not { Props.supplied: true } light) continue;
            if (onlyInUse && !light.Wants) continue;
            watts += light.Props.watts;
        }
        return watts;
    }
}

/// <summary>This vehicle's camp has camp lights on poles set out; with <c>inUse</c>, one that wants to be lit now (it draws).</summary>
public class When_CampLights : VehicleSystemCondition
{
    public bool inUse;

    protected override bool HoldsRaw(SystemContext context) => CampLights.Watts(context.Vehicle, inUse) > 0f;
}

/// <summary>What the camp lights draw now, in amps at this vehicle's voltage: their own watts, from their Defs.</summary>
public class Input_CampLightAmps : VehicleSystemInput
{
    protected override float Raw(SystemContext context)
    {
        float volts = context.Comp.ElectricalVolts();
        return volts > 0f ? CampLights.Watts(context.Vehicle, onlyInUse: true) / volts : 0f;
    }
}
