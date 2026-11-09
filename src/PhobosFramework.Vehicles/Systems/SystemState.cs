using System.Collections.Generic;
using Phobos.Framework.Vehicles.Rules;
using Vehicles;

namespace Phobos.VehicleSystems;

/// <summary>A system as one vehicle design runs it: the Def and its parameters after the loadout's overrides.</summary>
public sealed class ResolvedSystem
{
    public readonly PhobosVehicleSystemDef def;
    public readonly Dictionary<string, double> parameters;

    public ResolvedSystem(PhobosVehicleSystemDef def, Dictionary<string, double> parameters)
    {
        this.def = def;
        this.parameters = parameters;
    }
}

/// <summary>One vehicle's running state of one system. The values are saved; the stages are worked out again on load.</summary>
public sealed class SystemState
{
    public readonly ResolvedSystem system;
    public readonly double[] values;
    public readonly double[] min;
    public readonly double[] max;
    public readonly int[] stage;
    public readonly List<FlowSample> samples = new();

    public SystemState(ResolvedSystem system)
    {
        this.system = system;
        int count = system.def.quantities.Count;
        values = new double[count];
        min = new double[count];
        max = new double[count];
        stage = new int[count];
        for (int i = 0; i < count; i++)
        {
            QuantitySpec quantity = system.def.quantities[i];
            min[i] = quantity.min;
            max[i] = quantity.max;
            stage[i] = -1;
        }
    }

    public PhobosVehicleSystemDef Def => system.def;

    public StageSpec? CurrentStage(int quantity)
    {
        int index = stage[quantity];
        List<StageSpec> stages = system.def.quantities[quantity].stages;
        return index >= 0 && index < stages.Count ? stages[index] : null;
    }
}

/// <summary>What an input, condition or effect may read: the vehicle's comp, the system being run and the stage being entered.</summary>
public sealed class SystemContext
{
    public readonly CompPhobosVehicleSystems Comp;
    public readonly SystemState State;
    /// <summary>A stable name for the stage being entered or applied, for message cooldowns.</summary>
    public string StageKey = "";

    public SystemContext(CompPhobosVehicleSystems comp, SystemState state)
    {
        Comp = comp;
        State = state;
    }

    public VehiclePawn Vehicle => Comp.Vehicle;

    /// <summary>A parameter after the loadout's overrides, the player's tuning in Vehicle Framework's window, and the vehicle's fittings and tunes.</summary>
    public float Parameter(string name)
    {
        if (!State.system.parameters.TryGetValue(name, out double value)) return 0f;
        return (float)Tuning.Value(Vehicle?.VehicleDef, State.Def.defName, name, value) * Comp.Fittings.ParameterFactor(name);
    }
}
