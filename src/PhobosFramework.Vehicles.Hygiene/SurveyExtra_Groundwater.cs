using System.Collections.Generic;
using DubsBadHygiene;
using RimWorld;
using Verse;

namespace Phobos.VehicleSystems.Hygiene;

/// <summary>The survey truck's groundwater sensor (docs/development/vehicle-survey-truck.md). It reveals Dubs Bad Hygiene's own water grids and marks the richest spot in reach: it never makes water, because wells only read those grids.</summary>
public class SurveyExtra_Groundwater : SurveyExtra
{
    public float radius = 15f;

    IntVec3 best = IntVec3.Invalid;
    int bestValue;
    int readTick = -99999;
    Thing? readFor;

    static bool Active(CompPhobosSurveyTruck truck) => truck.Deployed && !Settings.LiteMode && truck.Vehicle.GetStatValue(RigDefOf.Phobos_Hydrogeology) > 0f;

    void Read(CompPhobosSurveyTruck truck)
    {
        int now = Find.TickManager.TicksGame;
        if (readFor == truck.Vehicle && now - readTick < GenTicks.TickRareInterval) return;
        readFor = truck.Vehicle;
        readTick = now;
        best = IntVec3.Invalid;
        bestValue = 0;
        Map map = truck.Vehicle.Map;
        MapComponent_Hygiene water = map.Hygiene();
        foreach (IntVec3 cell in GenRadial.RadialCellsAround(truck.Vehicle.Position, radius, true))
        {
            if (!cell.InBounds(map) || !cell.Standable(map)) continue;
            int value = water.WaterGrid.ValueAt(cell) + water.DeepWaterGrid.ValueAt(cell);
            if (value <= bestValue) continue;
            bestValue = value;
            best = cell;
        }
    }

    public override void DrawSelected(CompPhobosSurveyTruck truck)
    {
        if (!Active(truck)) return;
        MapComponent_Hygiene water = truck.Vehicle.Map.Hygiene();
        water.WaterGrid.MarkForDraw();
        water.DeepWaterGrid.MarkForDraw();
        Read(truck);
        if (best.IsValid) GenDraw.DrawFieldEdges(new List<IntVec3> { best });
    }

    public override string? InspectLine(CompPhobosSurveyTruck truck)
    {
        if (!Active(truck)) return null;
        Read(truck);
        return (best.IsValid ? "PhobosVehicleSystems_GroundwaterLine".Translate(bestValue.Named("VALUE")) : "PhobosVehicleSystems_GroundwaterNone".Translate()).Resolve();
    }
}
