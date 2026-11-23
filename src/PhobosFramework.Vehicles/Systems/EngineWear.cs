using System.Collections.Generic;
using System.Linq;
using Phobos.Framework.Vehicles.Rules;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// Each engine of a multi-engined turbine vehicle keeps its own hot-section life (Framework 0.247.0, per-engine realism step 4; the owner's
/// card of 23 Nov 2026), so one engine can be due while the other is fresh, and each is overhauled on its own. The turbine system still
/// works out one wear figure; after each update the wear it added is shared among the engines by the work each did
/// (<see cref="EngineWearRule.Shares"/>), and the vehicle's figure is set to the most worn engine's, so the dashboard, the worn stage and
/// its breakdowns read the engine that needs it. A single-engined vehicle is untouched. The engines' figures are saved by the comp.
/// </summary>
public sealed class EngineWear
{
    /// <summary>The turbine system's quantity: hot-section life used, 1 when due.</summary>
    public const string Quantity = "hotSectionLife";

    readonly CompPhobosVehicleSystems comp;
    /// <summary>What was last written into the vehicle's figure; NaN until the first look this session. Derived, never saved.</summary>
    float mirrored = float.NaN;

    public EngineWear(CompPhobosVehicleSystems comp) => this.comp = comp;

    IReadOnlyList<string> Roles => comp.Powertrain?.EngineRoles ?? System.Array.Empty<string>();

    Dictionary<string, float>? Lives => comp.engineHotSection;

    /// <summary>Whether this vehicle keeps a figure for each engine: two or more engines and a turbine's hot section.</summary>
    public bool Applies => Roles.Count >= 2 && comp.TryGetQuantity(Quantity, out _);

    /// <summary>Shares the wear the vehicle's figure gained since the last look among the engines, and sets the figure to the worst. Called
    /// after the systems advance, before their stages are judged.</summary>
    public void Sync()
    {
        IReadOnlyList<string> roles = Roles;
        if (roles.Count < 2 || !comp.TryGetQuantity(Quantity, out float now)) return;
        Dictionary<string, float> lives = comp.engineHotSection ??= new Dictionary<string, float>();
        // A vehicle seen for the first time, or an older save: every engine starts where the vehicle's one figure stood.
        foreach (string role in roles)
        {
            if (!lives.ContainsKey(role)) lives[role] = now;
        }
        if (float.IsNaN(mirrored)) mirrored = now;
        float delta = now - mirrored;
        if (delta > 0f)
        {
            IReadOnlyList<double> outputs = comp.Powertrain!.EngineOutputs();
            double[] shares = EngineWearRule.Shares(outputs);
            for (int i = 0; i < roles.Count && i < shares.Length; i++) lives[roles[i]] += delta * (float)shares[i];
        }
        else if (delta < 0f)
        {
            // Something else lowered the figure (a dev action): no engine stays above it.
            foreach (string role in roles) lives[role] = System.Math.Min(lives[role], now);
        }
        Mirror(roles, lives);
    }

    void Mirror(IReadOnlyList<string> roles, Dictionary<string, float> lives)
    {
        float worst = roles.Max(role => lives[role]);
        if (!comp.TryGetQuantity(Quantity, out float now) || System.Math.Abs(worst - now) > 1e-5f) comp.SetQuantity(Quantity, worst);
        mirrored = comp.TryGetQuantity(Quantity, out float written) ? written : worst;
        // What the figure clamps to (its stated maximum) holds for each engine too.
        foreach (string role in roles) lives[role] = System.Math.Min(lives[role], mirrored);
    }

    int MostWornIndex(IReadOnlyList<string> roles, Dictionary<string, float> lives) =>
        EngineWearRule.MostWorn(roles.Select(role => (double)(lives.TryGetValue(role, out float life) ? life : 0f)).ToList());

    /// <summary>A hot-section overhaul on the most worn engine only, to <paramref name="value"/>; the vehicle's figure then follows the next
    /// worst. Returns the engine's part label, or null when the vehicle keeps one figure (the caller restores it as before).</summary>
    public string? OverhaulMostWorn(float value)
    {
        IReadOnlyList<string> roles = Roles;
        Dictionary<string, float>? lives = Lives;
        if (roles.Count < 2 || lives == null || roles.Any(role => !lives.ContainsKey(role))) return null;
        int index = MostWornIndex(roles, lives);
        if (index < 0) return null;
        lives[roles[index]] = value;
        Mirror(roles, lives);
        VehicleComponent? part = comp.PartsOf(roles[index]).FirstOrDefault();
        return part?.props.label ?? roles[index];
    }

    /// <summary>A worn hot section's breakdown, on the most worn engine that still stands; null when the vehicle keeps one figure.</summary>
    public VehicleComponent? BreakMostWorn(float fraction)
    {
        IReadOnlyList<string> roles = Roles;
        Dictionary<string, float>? lives = Lives;
        if (roles.Count < 2 || lives == null) return null;
        int index = MostWornIndex(roles, lives);
        if (index < 0) return null;
        VehicleComponent? part = comp.PartsOf(roles[index]).Where(p => p.Health > 0f).RandomElementWithFallback();
        if (part == null) return null;
        comp.BreakPart(part, fraction);
        return part;
    }

    /// <summary>Each engine's hot-section life used, by its part's label, for what the player reads; empty when the vehicle keeps one figure.</summary>
    public IEnumerable<(string engine, float life)> Shown()
    {
        IReadOnlyList<string> roles = Roles;
        Dictionary<string, float>? lives = Lives;
        if (roles.Count < 2 || lives == null) yield break;
        foreach (string role in roles)
        {
            if (!lives.TryGetValue(role, out float life)) continue;
            yield return (comp.PartsOf(role).FirstOrDefault()?.props.label ?? role, life);
        }
    }
}
