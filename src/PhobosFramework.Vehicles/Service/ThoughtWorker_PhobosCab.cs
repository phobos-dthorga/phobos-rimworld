using Phobos.Framework.Rules;
using RimWorld;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// How a person aboard a closed cab feels about it (owner's follow-up, 9 Nov 2026): glad of a warm cab in the cold or a cool one in the
/// heat, unhappy in a cab too cold or too hot for them, by their own comfort range (the game's ComfyTemperatureMin and Max stats, so
/// apparel, genes and traits count). The rule is the Framework core's ShelterComfort; the cab temperature is the cache the Cabin
/// system writes, and the outside is the comp's cached ambient, so this reads and never computes beyond the game's own stats. Open
/// cabs give no thought. A player setting turns it off.
/// </summary>
public class ThoughtWorker_PhobosCab : ThoughtWorker
{
    protected override ThoughtState CurrentStateInternal(Pawn p)
    {
        if (!VehicleSystemsMod.Settings.cabMoods || p.Spawned || p.needs?.mood == null) return ThoughtState.Inactive;
        VehiclePawn? vehicle = p.GetVehicle();
        if (vehicle == null || !CabClimate.TryGet(vehicle, out float cab)) return ThoughtState.Inactive;
        float outside = vehicle.GetComp<CompPhobosVehicleSystems>()?.Ambient ?? cab;
        ShelterStage stage = ShelterComfort.Stage(cab, outside, p.GetStatValue(StatDefOf.ComfyTemperatureMin), p.GetStatValue(StatDefOf.ComfyTemperatureMax));
        return stage == ShelterStage.None ? ThoughtState.Inactive : ThoughtState.ActiveAtStage((int)stage);
    }
}
