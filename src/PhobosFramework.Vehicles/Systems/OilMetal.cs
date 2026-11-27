using System.Collections.Generic;
using System.Linq;
using Phobos.Framework.Rules;
using Phobos.Framework.Vehicles.Rules;
using RimWorld;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// The chip detectors (Framework 0.365.0; docs/development/vehicle-chip-detector.md; the owner's answers of 28 Nov 2026: report only, and
/// helicopter gearboxes too). Each engine and each rotor gearbox keeps the metal its wear has shed into its oil, saved here by part; the
/// worst engine's is the lubrication system's <see cref="EngineQuantity"/> and the worst gearbox's the rotor drive's
/// <see cref="GearboxQuantity"/>, so their stages light the lamps. Metal comes only from wear the model already does: health lost to wear
/// (low oil pressure, overheating, over-torque; never a hit, a fire or grit, which <see cref="CompPhobosVehicleSystems.WearRole"/> is told), and
/// hours run past the service or with a worn hot section. It harms nothing. The engine service changes the oil and clears it, but an engine
/// still wearing brings it back.
/// </summary>
public sealed class OilMetal
{
    public const string EngineQuantity = "oilMetal";
    public const string GearboxQuantity = "gearboxMetal";
    const string GearboxRole = "RotorDrive";
    const string ServiceQuantity = "engineHours";
    const string HotSectionQuantity = "hotSectionLife";
    /// <summary>Running hours after which a service is overdue (the service system's "overdue" stage).</summary>
    const float OverdueHours = 180f;
    const float Epsilon = 1e-4f;

    readonly CompPhobosVehicleSystems comp;
    /// <summary>Each part's metal, by part key; null until any is gained. Saved by the comp.</summary>
    internal Dictionary<string, float>? metal;
    /// <summary>What was last written into each figure, to see a service lower it. Derived, never saved.</summary>
    readonly Dictionary<string, float> written = new();
    readonly HashSet<string> told = new();
    /// <summary>A lamp already lit when the save was loaded is taken as told, so a load never repeats its message.</summary>
    bool settled;

    public OilMetal(CompPhobosVehicleSystems comp) => this.comp = comp;

    bool Keeps(string quantity) => comp.Tier == DetailTier.Full && comp.TryGetQuantity(quantity, out _);

    static bool IsEngineRole(string role) => role == "Engine" || (role.StartsWith("Engine") && role.Length > 6 && char.IsDigit(role[6]));

    List<VehicleComponent> Engines() =>
        Powertrain.EngineRolesOf(comp).Select(role => comp.PartsOf(role).FirstOrDefault()).Where(p => p != null).Select(p => p!).Distinct().ToList();

    List<VehicleComponent> Gearboxes() => comp.PartsOf(GearboxRole).ToList();

    /// <summary>The metal in one part's oil (Framework 0.373.0, the schematic's engine tabs); false while it has gained none.</summary>
    public bool TryOf(VehicleComponent part, out float figure)
    {
        figure = 0f;
        return metal != null && metal.TryGetValue(part.props.key, out figure);
    }

    /// <summary>Called by the comp when wear (not a hit, a fire or grit) takes <paramref name="shareLost"/> of a part's health.</summary>
    public void NoteWear(VehicleComponent part, string role, float shareLost)
    {
        string? quantity = IsEngineRole(role) ? EngineQuantity : role == GearboxRole ? GearboxQuantity : null;
        if (quantity == null || shareLost <= 0f || !Keeps(quantity)) return;
        metal ??= new Dictionary<string, float>();
        metal.TryGetValue(part.props.key, out float now);
        metal[part.props.key] = (float)ChipDetectorRule.AfterWear(now, shareLost);
    }

    /// <summary>At each update: running engines worn past their service or with a worn hot section gain metal, a service's oil change is
    /// taken up, the figures are written, and a lamp newly lit names its part.</summary>
    public void Update(float hours)
    {
        if (Keeps(EngineQuantity)) Settle(EngineQuantity, Engines(), hours, running: true);
        if (Keeps(GearboxQuantity)) Settle(GearboxQuantity, Gearboxes(), hours, running: false);
        settled = true;
    }

    void Settle(string quantity, List<VehicleComponent> parts, float hours, bool running)
    {
        if (parts.Count == 0 || !comp.TryGetQuantity(quantity, out float figure)) return;
        metal ??= new Dictionary<string, float>();
        // A service set the figure lower since the last look: the oil is changed, so no part keeps more than it.
        if (written.TryGetValue(quantity, out float last) && figure < last - Epsilon)
        {
            foreach (VehicleComponent part in parts)
            {
                if (metal.TryGetValue(part.props.key, out float held)) metal[part.props.key] = System.Math.Min(held, figure);
            }
        }
        if (running && comp.EngineRunning)
        {
            bool overdue = comp.TryGetQuantity(ServiceQuantity, out float served) && served >= OverdueHours;
            IReadOnlyList<double> orders = comp.Powertrain?.EngineOrders() ?? System.Array.Empty<double>();
            for (int i = 0; i < parts.Count; i++)
            {
                if (i < orders.Count && orders[i] <= 0) continue;
                int worn = (overdue ? 1 : 0) + (HotSectionWorn(parts[i]) ? 1 : 0);
                if (worn == 0) continue;
                metal.TryGetValue(parts[i].props.key, out float now);
                metal[parts[i].props.key] = (float)ChipDetectorRule.AfterRunning(now, hours, worn);
            }
        }
        float worst = parts.Max(p => metal.TryGetValue(p.props.key, out float m) ? m : 0f);
        comp.SetQuantity(quantity, worst);
        written[quantity] = comp.TryGetQuantity(quantity, out float read) ? read : worst;
        Tell(parts);
    }

    /// <summary>
    /// Every engine and gearbox whose lamp is lit, with its metal and its likely cause as the part stands now (Framework 0.374.0; the record's
    /// CD5): for the diagnosis report, which names them. Reads only.
    /// </summary>
    public IEnumerable<(VehicleComponent part, float metal, ChipCause cause)> Lit()
    {
        if (metal == null) yield break;
        bool overdue = comp.TryGetQuantity(ServiceQuantity, out float served) && served >= OverdueHours;
        if (Keeps(EngineQuantity))
        {
            List<VehicleComponent> engines = Engines();
            for (int i = 0; i < engines.Count; i++)
            {
                if (!metal.TryGetValue(engines[i].props.key, out float m) || !ChipDetectorRule.Lit(m)) continue;
                yield return (engines[i], m, ChipDetectorRule.Cause(false, HotSectionWorn(engines[i]), overdue, LowOil(i)));
            }
        }
        if (Keeps(GearboxQuantity))
        {
            foreach (VehicleComponent gearbox in Gearboxes())
            {
                if (metal.TryGetValue(gearbox.props.key, out float m) && ChipDetectorRule.Lit(m)) yield return (gearbox, m, ChipCause.Gearbox);
            }
        }
    }

    /// <summary>Whether engine <paramref name="engine"/> is short of oil pressure now: its own figure where each engine keeps one, else the
    /// vehicle's.</summary>
    bool LowOil(int engine) =>
        (EngineFigures.TryRead(comp, EngineFeeds.OilPressure, engine, out float pressure) || comp.TryGetQuantity(EngineFeeds.OilPressure, out pressure))
        && pressure < EngineFeedRule.PressureLow;

    bool HotSectionWorn(VehicleComponent engine)
    {
        if (comp.partFigures != null && comp.partFigures.TryGetValue(HotSectionQuantity + "|" + engine.props.key, out float life)) return life >= 1f;
        return comp.TryGetQuantity(HotSectionQuantity, out float shared) && shared >= 1f;
    }

    void Tell(List<VehicleComponent> parts)
    {
        foreach (VehicleComponent part in parts)
        {
            bool lit = metal!.TryGetValue(part.props.key, out float m) && ChipDetectorRule.Lit(m);
            string key = part.props.key;
            if (lit && !settled)
            {
                told.Add(key);
                continue;
            }
            if (lit && told.Add(key))
            {
                if (Seen.Wants("vehicles.chip-light", comp.Vehicle)) Seen.Note("vehicles.chip-light", comp.Vehicle, part.props.label ?? key);
                if (comp.Vehicle.Faction == Faction.OfPlayer)
                {
                    Messages.Message("PhobosVehicleSystems_MsgChipLight".Translate(comp.Vehicle.LabelShort.Named("VEHICLE"), (part.props.label ?? key).Named("PART")).Resolve(),
                        comp.Vehicle, MessageTypeDefOf.CautionInput);
                }
            }
            else if (!lit) told.Remove(key);
        }
    }
}
