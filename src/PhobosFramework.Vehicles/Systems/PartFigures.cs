using System.Collections.Generic;
using System.Linq;
using Phobos.Framework.Vehicles.Rules;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// Quantities kept per part (Framework 0.248.0, per-engine realism round two; the owner's card of 23 Nov 2026: "any system figure can be
/// marked per part"). Grew out of Framework 0.247.0's EngineWear, which kept each engine's hot-section life. A quantity whose spec carries
/// <see cref="PerPartSpec"/> still has one figure worked out by its system; after each update the wear it gained is shared among the parts
/// (engines by the work each did, wheel parts by axle, <see cref="PartWearRule"/>), and the figure is set to the most worn part's, so the
/// dashboard, the stages and their breakdowns read the part that needs it. A vehicle with fewer than two such parts is untouched. The
/// parts' figures are saved by the comp, keyed by quantity and part key.
/// </summary>
public sealed class PartFigures
{
    readonly CompPhobosVehicleSystems comp;
    /// <summary>What was last written into each quantity's figure; missing until the first look this session. Derived, never saved.</summary>
    readonly Dictionary<string, float> mirrored = new();

    public PartFigures(CompPhobosVehicleSystems comp) => this.comp = comp;

    static string Key(string quantity, VehicleComponent part) => quantity + "|" + part.props.key;

    /// <summary>Every quantity of this vehicle kept per part.</summary>
    IEnumerable<QuantitySpec> PerPartQuantities()
    {
        foreach (SystemState state in comp.States)
        {
            foreach (QuantitySpec quantity in state.Def.quantities)
            {
                if (quantity.perPart != null) yield return quantity;
            }
        }
    }

    QuantitySpec? Spec(string quantity) => PerPartQuantities().FirstOrDefault(q => q.name == quantity);

    /// <summary>The parts that keep their own figure of a quantity, in a fixed order; fewer than two means the vehicle keeps one figure.</summary>
    public List<VehicleComponent> Parts(PerPartSpec spec)
    {
        if (spec.engines)
        {
            IReadOnlyList<string> roles = comp.Powertrain?.EngineRoles ?? System.Array.Empty<string>();
            var engines = new List<VehicleComponent>();
            foreach (string role in roles)
            {
                if (comp.PartsOf(role).FirstOrDefault() is { } part) engines.Add(part);
            }
            // An engine role with no part would put the work shares out of step with the parts: keep one figure then.
            return engines.Count == roles.Count ? engines : new List<VehicleComponent>();
        }
        return spec.role.NullOrEmpty() ? new List<VehicleComponent>() : comp.PartsOf(spec.role!).ToList();
    }

    /// <summary>Where each part sits along the vehicle (larger further forward), or null when any has no hit area to say.</summary>
    static List<float>? Along(List<VehicleComponent> parts)
    {
        var along = new List<float>();
        foreach (VehicleComponent part in parts)
        {
            if (part.props.hitbox?.Hitbox is not { Count: > 0 } cells) return null;
            along.Add((float)cells.Average(cell => cell.z));
        }
        return along;
    }

    double[] Shares(PerPartSpec spec, List<VehicleComponent> parts)
    {
        if (spec.engines && comp.Powertrain is { } train)
        {
            double[] work = PartWearRule.Shares(train.EngineOutputs());
            if (work.Length == parts.Count) return work;
        }
        if (spec.frontAxleWeight != 1f && Along(parts) is { } along) return PartWearRule.AxleShares(along, spec.frontAxleWeight, VehicleFittings.AxleTolerance);
        return Enumerable.Repeat(1.0, parts.Count).ToArray();
    }

    /// <summary>Shares the wear each per-part quantity gained since the last look among its parts, and sets its figure to the worst. Called
    /// after the systems advance, before their stages are judged.</summary>
    public void Sync()
    {
        foreach (QuantitySpec quantity in PerPartQuantities())
        {
            List<VehicleComponent> parts = Parts(quantity.perPart!);
            if (parts.Count < 2 || !comp.TryGetQuantity(quantity.name, out float now)) continue;
            Dictionary<string, float> figures = comp.partFigures ??= new Dictionary<string, float>();
            Migrate(quantity, parts, figures);
            // A vehicle seen for the first time, or an older save: every part starts where the vehicle's one figure stood.
            foreach (VehicleComponent part in parts)
            {
                if (!figures.ContainsKey(Key(quantity.name, part))) figures[Key(quantity.name, part)] = now;
            }
            float last = mirrored.TryGetValue(quantity.name, out float seen) ? seen : now;
            float delta = now - last;
            if (delta > 0f)
            {
                double[] shares = Shares(quantity.perPart!, parts);
                for (int i = 0; i < parts.Count && i < shares.Length; i++) figures[Key(quantity.name, parts[i])] += delta * (float)shares[i];
            }
            else if (delta < 0f)
            {
                // Something else lowered the figure (a dev action, a whole-vehicle repair): no part stays above it.
                foreach (VehicleComponent part in parts) figures[Key(quantity.name, part)] = System.Math.Min(figures[Key(quantity.name, part)], now);
            }
            Mirror(quantity.name, parts, figures);
        }
    }

    /// <summary>Framework 0.247.0 kept each engine's hot-section life by its role under its own save key; taken over once, then dropped.</summary>
    void Migrate(QuantitySpec quantity, List<VehicleComponent> parts, Dictionary<string, float> figures)
    {
        if (comp.engineHotSection is not { Count: > 0 } old || !quantity.perPart!.engines || quantity.name != "hotSectionLife") return;
        IReadOnlyList<string> roles = comp.Powertrain?.EngineRoles ?? System.Array.Empty<string>();
        for (int i = 0; i < roles.Count && i < parts.Count; i++)
        {
            if (old.TryGetValue(roles[i], out float life)) figures[Key(quantity.name, parts[i])] = life;
        }
        comp.engineHotSection = null;
    }

    void Mirror(string quantity, List<VehicleComponent> parts, Dictionary<string, float> figures)
    {
        float worst = parts.Max(part => figures[Key(quantity, part)]);
        if (!comp.TryGetQuantity(quantity, out float now) || System.Math.Abs(worst - now) > 1e-5f) comp.SetQuantity(quantity, worst);
        float written = comp.TryGetQuantity(quantity, out float read) ? read : worst;
        mirrored[quantity] = written;
        // What the figure clamps to (its stated maximum) holds for each part too.
        foreach (VehicleComponent part in parts) figures[Key(quantity, part)] = System.Math.Min(figures[Key(quantity, part)], written);
    }

    List<double> FiguresOf(string quantity, List<VehicleComponent> parts, Dictionary<string, float> figures) =>
        parts.Select(part => (double)(figures.TryGetValue(Key(quantity, part), out float figure) ? figure : 0f)).ToList();

    /// <summary>A restore of a per-part quantity, to <paramref name="value"/>: the most worn part only, or its whole axle where the spec says,
    /// and the figure then follows the next worst. Returns the parts' labels, or null when the vehicle keeps one figure (the caller restores
    /// it as before).</summary>
    public string? RestoreMostWorn(string quantity, float value)
    {
        if (Spec(quantity) is not { perPart: { } spec } || comp.partFigures is not { } figures) return null;
        List<VehicleComponent> parts = Parts(spec);
        if (parts.Count < 2 || parts.Any(part => !figures.ContainsKey(Key(quantity, part)))) return null;
        List<int> done = PartWearRule.RestoreSet(FiguresOf(quantity, parts, figures), spec.restoreByAxle ? Along(parts) : null, VehicleFittings.AxleTolerance);
        if (done.Count == 0) return null;
        foreach (int i in done) figures[Key(quantity, parts[i])] = value;
        Mirror(quantity, parts, figures);
        return string.Join(", ", done.Select(i => parts[i].props.label ?? parts[i].props.key));
    }

    /// <summary>A worn stage's breakdown on the most worn part of a per-part quantity that still stands; null when the vehicle keeps one figure.</summary>
    public VehicleComponent? BreakMostWorn(string quantity, float fraction)
    {
        if (Spec(quantity) is not { perPart: { } spec } || comp.partFigures is not { } figures) return null;
        List<VehicleComponent> parts = Parts(spec);
        if (parts.Count < 2) return null;
        int index = PartWearRule.MostWorn(FiguresOf(quantity, parts, figures));
        if (index < 0 || parts[index].Health <= 0f) return null;
        comp.BreakPart(parts[index], fraction);
        return parts[index];
    }

    /// <summary>Each part's figure of a quantity, by the part's label, for what the player reads; empty when the vehicle keeps one figure.</summary>
    public IEnumerable<(string part, float figure)> Shown(QuantitySpec quantity)
    {
        // Framework 0.360.0: an aircraft's readings kept for each engine by its own fuel and oil (EngineFeeds), shown the same way.
        if (quantity.perPart == null)
        {
            foreach ((string part, float figure) each in comp.EngineFeeds.Shown(quantity.name)) yield return each;
            yield break;
        }
        if (comp.partFigures is not { } figures) yield break;
        List<VehicleComponent> parts = Parts(quantity.perPart);
        if (parts.Count < 2) yield break;
        foreach (VehicleComponent part in parts)
        {
            if (figures.TryGetValue(Key(quantity.name, part), out float figure)) yield return (part.props.label ?? part.props.key, figure);
        }
    }
}
