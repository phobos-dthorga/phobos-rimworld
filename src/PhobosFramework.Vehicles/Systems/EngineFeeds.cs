using System.Collections.Generic;
using System.Linq;
using Phobos.Framework.Vehicles.Rules;
using RimWorld;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// Each engine's own fuel feed and oil (Framework 0.360.0; docs/development/vehicle-standard.md, batch 3; the owner's cards of 27 Nov 2026:
/// each engine its own pumps and oil; real consequences in the air, behind a setting that starts on; jet fuel freezing at its real point).
/// Built like the side tanks (<see cref="SideTanks"/>): an engine this starves stops, and the powertrain counts it out
/// (<see cref="Powertrain.Engines"/>, <see cref="Powertrain.EngineOrders"/>), so everything that follows an engine lost follows it too: the
/// engine-out ratings, lopsided thrust, the hover check and a fall, the cruising height's turn back, being shot down, and on the world map
/// <see cref="DeadStick"/>. Engine i is paired with the pumps numbered as it is (<see cref="EnginePumps"/>).
/// <list type="bullet">
/// <item>Fuel: a pump below a tenth of its work starves its engine; below half it gives three quarters (the land thresholds). Fuel frozen
/// past half way (the fuel system's fuelFrozen, from the fuel's temperature) starves every engine.</item>
/// <item>Oil: each engine has its own sump, saved here; it burns with that engine's output, more when worn, and loses oil when that engine
/// or its oil pump is hit. Pressure is the pump's work times what the level allows. With no pressure a running engine seizes in about five
/// hours; low, it wears (the land figures, the oil system's parameters). The system's oilLevel reads the lowest sump, so the dashboard, the
/// low-oil service call and the service's top-up work as on land: a top-up raises every sump to it.</item>
/// </list>
/// With the player's setting off, or figures only, the readings are kept and shown but no engine is stopped or harmed.
/// </summary>
public sealed class EngineFeeds
{
    public const string FuelPumpRole = "FuelPump";
    public const string OilPumpRole = "OilPump";
    /// <summary>The oil system's quantity that reads the lowest sump, which the service tops up.</summary>
    public const string OilLevel = "oilLevel";
    public const string FuelFeed = "fuelFeed";
    public const string OilPressure = "oilPressure";
    /// <summary>The fuel system's quantity: how far the fuel in the tanks has frozen.</summary>
    public const string FuelFrozen = "fuelFrozen";

    const float Epsilon = 1e-4f;

    readonly CompPhobosVehicleSystems comp;
    /// <summary>Each engine's sump, 0 to 1, in the engines' order; null until first looked at. Saved by the comp.</summary>
    internal List<float>? oil;
    /// <summary>The oil level last written into the system's figure; derived, never saved.</summary>
    float lastOil = float.NaN;
    ResolvedSystem? fuelSystem, oilSystem;
    bool looked;
    readonly bool[] starvedTold = new bool[Powertrain.MaxEngines];
    readonly bool[] oilGoneTold = new bool[Powertrain.MaxEngines];
    bool frozenTold;

    public EngineFeeds(CompPhobosVehicleSystems comp) => this.comp = comp;

    void Look()
    {
        if (looked) return;
        looked = true;
        foreach (ResolvedSystem system in comp.Props.systems)
        {
            string? role = system.def.partPerEngine?.role;
            if (role == FuelPumpRole) fuelSystem = system;
            else if (role == OilPumpRole) oilSystem = system;
        }
    }

    static bool Enabled(ResolvedSystem? system) => system != null && !VehicleSystemsMod.Settings.disabledSystems.Contains(system.def.defName);

    /// <summary>Whether this vehicle keeps a fuel feed for each engine.</summary>
    public bool HasFuel
    {
        get
        {
            Look();
            return Enabled(fuelSystem);
        }
    }

    /// <summary>Whether this vehicle keeps a sump for each engine.</summary>
    public bool HasOil
    {
        get
        {
            Look();
            return Enabled(oilSystem);
        }
    }

    /// <summary>Whether a fault may stop or harm an engine: the player's setting, and not figures only.</summary>
    static bool Consequences => VehicleSystemsMod.Settings.aircraftEngineFaults && !VehicleSystemsMod.Settings.showOnly;

    IReadOnlyList<string> Engines => comp.Powertrain?.EngineRoles ?? System.Array.Empty<string>();

    bool IsEngine(int engine) => engine >= 0 && engine < Engines.Count;

    /// <summary>Engine <paramref name="engine"/>'s fuel pump's work, 0 to 1; whole where it has none.</summary>
    public float Feed(int engine) => IsEngine(engine) ? comp.PartEfficiency(EnginePumps.RoleFor(FuelPumpRole, Engines[engine]), 1f) : 1f;

    /// <summary>Whether the fuel in the tanks is frozen too far to flow.</summary>
    public bool Frozen => HasFuel && comp.TryGetQuantity(FuelFrozen, out float frozen) && frozen >= EngineFeedRule.FrozenStops;

    /// <summary>Engine <paramref name="engine"/>'s sump, 0 to 1.</summary>
    public float Oil(int engine) => oil != null && engine >= 0 && engine < oil.Count ? oil[engine] : 1f;

    /// <summary>Oil reaching engine <paramref name="engine"/>'s bearings, 0 to 1.</summary>
    public float Pressure(int engine) => IsEngine(engine)
        ? (float)EngineFeedRule.OilPressure(comp.PartEfficiency(EnginePumps.RoleFor(OilPumpRole, Engines[engine]), 1f), Oil(engine))
        : 1f;

    /// <summary>Whether engine <paramref name="engine"/> is starved of fuel, by its pump or by frozen fuel, and so stopped.</summary>
    public bool Starved(int engine) => Consequences && HasFuel && IsEngine(engine) && (Frozen || Feed(engine) < EngineFeedRule.FeedNone);

    /// <summary>What engine <paramref name="engine"/>'s fuel pump leaves of its working order: 0 starved, 0.75 fed poorly, else 1.</summary>
    public double OrderFactor(int engine) => Consequences && HasFuel && IsEngine(engine) ? EngineFeedRule.FeedOrder(Feed(engine)) : 1.0;

    /// <summary>The worst engine's reading (<see cref="FuelFeed"/>, <see cref="OilPressure"/> or <see cref="OilLevel"/>), for the systems'
    /// figure; 1 with no engines.</summary>
    public float Worst(string reading)
    {
        float worst = 1f;
        for (int i = 0; i < Engines.Count; i++) worst = System.Math.Min(worst, Reading(reading, i));
        return worst;
    }

    float Reading(string reading, int engine) => reading switch
    {
        FuelFeed => Feed(engine),
        OilPressure => Pressure(engine),
        _ => Oil(engine),
    };

    /// <summary>Each engine's reading of a quantity, by the engine's label, for the Systems tab; empty with fewer than two engines or for a
    /// quantity kept for the whole vehicle.</summary>
    public IEnumerable<(string part, float figure)> Shown(string quantity)
    {
        bool ours = quantity == FuelFeed ? HasFuel : (quantity == OilLevel || quantity == OilPressure) && HasOil;
        if (!ours || Engines.Count < 2) yield break;
        for (int i = 0; i < Engines.Count; i++) yield return (EngineLabel(i), Reading(quantity, i));
    }

    string EngineLabel(int engine) => comp.PartsOf(Engines[engine]).FirstOrDefault()?.props.label ?? Engines[engine];

    double Parameter(ResolvedSystem system, string name, double fallback) => system.parameters.TryGetValue(name, out double value) ? value : fallback;

    /// <summary>At each update, after the systems advance and the side tanks are shared out: each sump burns, a top-up reaches every sump,
    /// an engine without oil pressure is harmed, and the player is told of an engine starved, frozen or losing its oil.</summary>
    public void Update(float hours)
    {
        Look();
        if (Engines.Count == 0) return;
        if (Enabled(oilSystem)) UpdateOil(oilSystem!, hours);
        if (Enabled(fuelSystem)) TellFuel();
    }

    void UpdateOil(ResolvedSystem system, float hours)
    {
        int count = Engines.Count;
        float now = comp.TryGetQuantity(OilLevel, out float level) ? level : 1f;
        oil ??= new List<float>();
        while (oil.Count < count) oil.Add(now);
        if (!float.IsNaN(lastOil))
        {
            // Something set the figure since: the service's top-up (every sump up to it) or a dev action or repair lowering it.
            if (now > lastOil + Epsilon) for (int i = 0; i < count; i++) oil[i] = System.Math.Max(oil[i], now);
            else if (now < lastOil - Epsilon) for (int i = 0; i < count; i++) oil[i] = System.Math.Min(oil[i], now);
        }
        if (comp.EngineRunning && comp.Powertrain is { } train)
        {
            IReadOnlyList<double> outputs = train.EngineOutputs();
            double burn = Parameter(system, "oilBurnPerHour", 0.0015);
            for (int i = 0; i < count && i < outputs.Count; i++)
            {
                oil[i] = (float)EngineFeedRule.OilAfterBurn(oil[i], burn, outputs[i], comp.PartHealthShare(Engines[i], 1f), hours);
            }
            if (Consequences) Harm(system, train, hours);
        }
        float lowest = oil.Take(count).Min();
        comp.SetQuantity(OilLevel, lowest);
        lastOil = comp.TryGetQuantity(OilLevel, out float written) ? written : lowest;
        TellOil(count);
    }

    /// <summary>A running engine without oil pressure seizes in about five hours (a fifth of its health an hour); one on low pressure wears
    /// (2 health an hour). An engine already stopped is not harmed.</summary>
    void Harm(ResolvedSystem system, Powertrain train, float hours)
    {
        IReadOnlyList<double> orders = train.EngineOrders();
        float seize = (float)Parameter(system, "seizeSharePerHour", 0.2);
        float wear = (float)Parameter(system, "lowPressureWearPerHour", 2);
        for (int i = 0; i < Engines.Count && i < orders.Count; i++)
        {
            if (orders[i] <= 0) continue;
            float pressure = Pressure(i);
            if (pressure < EngineFeedRule.PressureGone) comp.WearRole(Engines[i], 0f, seize * hours);
            else if (pressure < EngineFeedRule.PressureLow) comp.WearRole(Engines[i], wear * hours);
        }
    }

    /// <summary>Called by the comp for every hit on a bound part: an engine hit, or its oil pump hit, loses oil from that engine's sump.</summary>
    public void NoteHit(VehicleComponent part, float healthLost)
    {
        Look();
        if (!Enabled(oilSystem) || oil == null || part.MaxHealth <= 0f) return;
        float share = healthLost / part.MaxHealth;
        for (int i = 0; i < Engines.Count && i < oil.Count; i++)
        {
            double perShare = comp.PartPlays(part, Engines[i]) ? Parameter(oilSystem!, "engineHitOilPerShare", 0.6)
                : comp.PartPlays(part, EnginePumps.RoleFor(OilPumpRole, Engines[i])) ? Parameter(oilSystem!, "pumpHitOilPerShare", 0.25)
                : 0;
            if (perShare > 0) oil[i] = (float)EngineFeedRule.OilAfterHit(oil[i], share, perShare);
        }
    }

    void TellFuel()
    {
        bool frozen = Consequences && Frozen;
        if (frozen && !frozenTold)
        {
            if (Seen.Wants("vehicles.fuel-frozen", comp.Vehicle))
            {
                Seen.Note("vehicles.fuel-frozen", comp.Vehicle, comp.TryGetQuantity("fuelTemperature", out float celsius) ? celsius.ToString("0") + " C" : null);
            }
            if (comp.Vehicle.Faction == Faction.OfPlayer)
            {
                Messages.Message("PhobosVehicleSystems_MsgFuelFrozen".Translate(comp.Vehicle.LabelShort.Named("VEHICLE")).Resolve(), comp.Vehicle,
                    MessageTypeDefOf.NegativeEvent);
            }
        }
        frozenTold = frozen;
        for (int i = 0; i < Engines.Count; i++)
        {
            bool starved = !frozen && Starved(i);
            if (starved && !starvedTold[i])
            {
                if (Seen.Wants("vehicles.engine-pump-starved", comp.Vehicle)) Seen.Note("vehicles.engine-pump-starved", comp.Vehicle, EngineLabel(i));
                if (comp.Vehicle.Faction == Faction.OfPlayer)
                {
                    Messages.Message("PhobosVehicleSystems_MsgEnginePumpStarved".Translate(comp.Vehicle.LabelShort.Named("VEHICLE"), EngineLabel(i).Named("ENGINE")).Resolve(),
                        comp.Vehicle, MessageTypeDefOf.NegativeEvent);
                }
            }
            starvedTold[i] = starved;
        }
    }

    void TellOil(int count)
    {
        IReadOnlyList<double> orders = comp.Powertrain?.EngineOrders() ?? System.Array.Empty<double>();
        for (int i = 0; i < count && i < orders.Count; i++)
        {
            bool gone = Consequences && comp.EngineRunning && orders[i] > 0 && Pressure(i) < EngineFeedRule.PressureGone;
            if (gone && !oilGoneTold[i])
            {
                if (Seen.Wants("vehicles.engine-oil-gone", comp.Vehicle)) Seen.Note("vehicles.engine-oil-gone", comp.Vehicle, EngineLabel(i));
                if (comp.Vehicle.Faction == Faction.OfPlayer)
                {
                    Messages.Message("PhobosVehicleSystems_MsgEngineOilGone".Translate(comp.Vehicle.LabelShort.Named("VEHICLE"), EngineLabel(i).Named("ENGINE")).Resolve(),
                        comp.Vehicle, MessageTypeDefOf.ThreatSmall);
                }
            }
            oilGoneTold[i] = gone;
        }
    }
}
