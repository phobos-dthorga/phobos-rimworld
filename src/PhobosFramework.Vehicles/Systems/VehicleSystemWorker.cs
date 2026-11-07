using System.Collections.Generic;
using Phobos.Framework.Vehicles.Rules;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// Runs one system for one vehicle: the generic stock-and-flow model every shipped system uses. A mod whose system needs maths the
/// XML cannot say subclasses this and names it in the system's workerClass; the stages, effects and saving stay the service's.
/// </summary>
public class VehicleSystemWorker
{
    /// <summary>Where each quantity starts on a vehicle that has never run the system.</summary>
    public virtual void Initialise(SystemContext context)
    {
        SystemState state = context.State;
        List<QuantitySpec> quantities = state.Def.quantities;
        for (int i = 0; i < quantities.Count; i++)
        {
            QuantitySpec quantity = quantities[i];
            double value = quantity.startsAt switch
            {
                QuantityStart.Ambient => context.Comp.Ambient,
                QuantityStart.Full => quantity.max,
                QuantityStart.Empty => quantity.min,
                _ => quantity.startValue,
            };
            state.values[i] = SystemIntegrator.Clamp(value, quantity.min, quantity.max);
        }
        UpdateDerived(context);
    }

    /// <summary>Moves the quantities on by the hours that passed.</summary>
    public virtual void Advance(SystemContext context, double hours)
    {
        SystemState state = context.State;
        UpdateDerived(context);
        state.samples.Clear();
        foreach (FlowSpec flow in state.Def.flows)
        {
            if (flow.when != null && !flow.when.Holds(context)) continue;
            int quantity = state.Def.QuantityIndex(flow.quantity);
            if (quantity < 0) continue;
            double rate = flow.factor;
            if (flow.factorParameter != null) rate *= context.Parameter(flow.factorParameter);
            if (flow.divideByParameter != null)
            {
                double divisor = context.Parameter(flow.divideByParameter);
                if (divisor == 0) continue;
                rate /= divisor;
            }
            foreach (VehicleSystemInput input in flow.inputs)
            {
                rate *= input.Evaluate(context);
                if (rate == 0) break;
            }
            if (rate <= 0) continue;
            state.samples.Add(new FlowSample
            {
                Quantity = quantity,
                Kind = flow.kind,
                Rate = rate,
                Target = flow.target?.Evaluate(context) ?? 0f,
                OpenAbove = flow.openAboveParameter != null ? context.Parameter(flow.openAboveParameter) : flow.openAbove,
                OpenBelow = flow.openBelowParameter != null ? context.Parameter(flow.openBelowParameter) : flow.openBelow,
                Band = flow.band,
            });
        }
        double step = PhobosVehicleRulesDef.Current<PhobosVehicleRulesDef>().stepHours;
        SystemIntegrator.Advance(state.values, state.min, state.max, state.samples, hours, step);
        UpdateDerived(context);
    }

    /// <summary>Sets every derived quantity to the product of its inputs.</summary>
    public virtual void UpdateDerived(SystemContext context)
    {
        SystemState state = context.State;
        List<QuantitySpec> quantities = state.Def.quantities;
        for (int i = 0; i < quantities.Count; i++)
        {
            List<VehicleSystemInput>? inputs = quantities[i].derivedFrom;
            if (inputs == null) continue;
            double value = 1;
            foreach (VehicleSystemInput input in inputs) value *= input.Evaluate(context);
            state.values[i] = SystemIntegrator.Clamp(value, state.min[i], state.max[i]);
        }
    }
}
