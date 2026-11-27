using System.Collections.Generic;
using System.Linq;
using Phobos.Framework;
using Phobos.Framework.Defs;
using Phobos.Framework.Vehicles.Rules;
using RimWorld;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// An aircraft's turbine engines lit one after another before it may take off (Framework 0.250.0, per-engine realism round two, step 9; the
/// owner's card of 23 Nov 2026: game-scaled). Timed from Vehicle Framework's own sign that the engine is on (drafted, or flying), so no flight
/// phase is needed to begin it. An aircraft whose flight framework has its own spooling up (a "SpoolingUp" phase in its profile) keeps that
/// instead: its engines start only after the take-off is ordered, so a refusal here would never clear. Derived, never saved: a game loaded
/// mid-start finds its engines lit.
/// </summary>
public sealed class StartSequence
{
    readonly CompPhobosVehicleSystems comp;
    int startedTick = -1;
    bool wasOn;
    /// <summary>Whether it has had its first look. Derived, never saved.</summary>
    bool noted;

    public StartSequence(CompPhobosVehicleSystems comp) => this.comp = comp;

    static PhobosVehicleRulesDef Rules => PhobosRulesDef.Current<PhobosVehicleRulesDef>();

    bool Applies => VehicleSystemsMod.Settings.enginesStartInTurn && comp.Powertrain is { IsAircraft: true, Turbine: not null } train
                    && train.Profile?.phases.Any(p => p.phase == "SpoolingUp") != true;

    /// <summary>Called at each update with Vehicle Framework's sign that the engine is on.</summary>
    public void Note(bool on)
    {
        // The first look after a load, or after the systems are built again, finds the engines as they were: running ones stay lit, with no
        // new start (fault-read 25 Nov 2026, E4: a drafted aircraft lit its engines again after every load).
        if (!noted)
        {
            noted = true;
            wasOn = on;
            return;
        }
        if (on && !wasOn) startedTick = Find.TickManager.TicksGame;
        if (!on) startedTick = -1;
        wasOn = on;
    }

    /// <summary>How many engines are lit and how many there are; all of them where the sequence does not apply or has not begun.</summary>
    public (int lit, int engines) Lit()
    {
        int engines = System.Math.Max(1, comp.Powertrain?.EngineRoles.Count ?? 1);
        if (!Applies || startedTick < 0) return (engines, engines);
        double hours = (Find.TickManager.TicksGame - startedTick) / (double)GenDate.TicksPerHour;
        return (StartSequenceRule.Lit(hours, Rules.engineStartHours, engines), engines);
    }

    /// <summary>Why it may not take off yet, or null. A take-off asked for before the update saw the engine on starts the sequence now.</summary>
    public string? Refusal()
    {
        if (!Applies) return null;
        if (startedTick < 0)
        {
            if (!comp.Vehicle.Spawned || !comp.Vehicle.Drafted) return null;
            startedTick = Find.TickManager.TicksGame;
            wasOn = true;
        }
        (int lit, int engines) = Lit();
        if (lit >= engines)
        {
            if (Seen.Wants("aircraft.engines-started-in-turn", comp.Vehicle)) Seen.Note("aircraft.engines-started-in-turn", comp.Vehicle, engines.ToString());
            return null;
        }
        return "PhobosVehicleSystems_EnginesStarting".Translate(comp.Vehicle.LabelShort.Named("VEHICLE"), lit.Named("LIT"), engines.Named("ENGINES")).Resolve();
    }
}

/// <summary>
/// Stones and grit thrown into engine intakes on rough ground (Framework 0.250.0, step 10; the owner's card: jets and helicopters). In a
/// flight phase that throws debris (the landing-surface rules' list: take-off and landing rolls, taxiing, a helicopter lifting off or
/// settling), an aircraft whose loadout gives its intakes an exposure may take a strike that costs an engine some health, more often the
/// more of the ground under it is rough (not a floor, not water). An aircraft on our own flight is judged at each lift-off and touch-down
/// instead (LandingSurface). Never below the dust rule's floor, so it grounds nothing by itself. Works on any faction's aircraft.
/// </summary>
public static class StripDebris
{
    static PhobosLandingSurfaceRulesDef Rules => PhobosRulesDef.Current<PhobosLandingSurfaceRulesDef>();

    /// <summary>The share of the ground under the vehicle that is rough: neither a floor nor water.</summary>
    public static double RoughShare(VehiclePawn vehicle)
    {
        if (!vehicle.Spawned || vehicle.Map is not { } map) return 0;
        int cells = 0, rough = 0;
        foreach (IntVec3 cell in vehicle.OccupiedRect())
        {
            if (!cell.InBounds(map)) continue;
            cells++;
            TerrainDef terrain = cell.GetTerrain(map);
            if (!terrain.IsFloor && !terrain.IsWater) rough++;
        }
        return cells == 0 ? 0 : (double)rough / cells;
    }

    static float Exposure(CompPhobosVehicleSystems comp) => comp.Props.loadout?.intakeDebris ?? 0f;

    static double Barrier(VehiclePawn vehicle) => vehicle.GetStatValue(AircraftUpgradeDefOf.Phobos_InletBarrierLoss) > 0f ? Rules.barrierStops : 0;

    /// <summary>
    /// Whether the aircraft's flight profile has a phase of its own for leaving or reaching the ground (a take-off roll, a lift-off, a
    /// landing: a debris phase that needs no movement). One that has none takes off and lands through Vehicle Framework's own launch, where
    /// it is off the map for the whole of it (the Black Hawk, the C-17, Vanilla Vehicles Expanded's), so it is judged once at its launch
    /// and once when it is back on the ground instead (Framework 0.254.0: before, such an aircraft was never judged at either).
    /// </summary>
    static bool ModelsItsOwnTakeOff(CompPhobosVehicleSystems comp, PhobosLandingSurfaceRulesDef rules) =>
        comp.Powertrain?.Profile is { } profile && profile.phases.Any(p => rules.debrisPhases.Any(d => !d.movingOnly && d.phase == p.phase));

    static bool OnOurOwnFlight(VehiclePawn vehicle) => vehicle.GetComp<CompPhobosFlight>() is { Active: true };

    /// <summary>Vehicle Framework's launch event, while the aircraft still stands on the ground: one judgement for the take-off, for an
    /// aircraft with no take-off phase of its own that is not on our own flight (which judges its own, LandingSurface).</summary>
    public static void OnLaunch(CompPhobosVehicleSystems comp)
    {
        VehiclePawn vehicle = comp.Vehicle;
        if (!vehicle.Spawned || Exposure(comp) <= 0f || OnOurOwnFlight(vehicle) || ModelsItsOwnTakeOff(comp, Rules)) return;
        OnEvent(vehicle, RoughShare(vehicle), "take-off", "stock");
    }

    /// <summary>At each update: an aircraft in a phase that throws debris may take a strike; one back on the ground from Vehicle
    /// Framework's own flight is judged once for its touch-down.</summary>
    public static void Update(CompPhobosVehicleSystems comp, float hours)
    {
        VehiclePawn vehicle = comp.Vehicle;
        bool flying = vehicle.CompVehicleLauncher?.inFlight ?? false;
        bool landed = comp.debrisWasFlying && !flying && vehicle.Spawned;
        comp.debrisWasFlying = flying;
        bool moved = vehicle.Spawned && vehicle.Position != comp.debrisLastCell;
        if (vehicle.Spawned) comp.debrisLastCell = vehicle.Position;
        if (VehicleSystemsMod.Settings.showOnly || Exposure(comp) <= 0f || comp.Powertrain is not { IsAircraft: true } train) return;
        if (!vehicle.Spawned) return;
        PhobosLandingSurfaceRulesDef rules = Rules;
        if (landed && !OnOurOwnFlight(vehicle) && !ModelsItsOwnTakeOff(comp, rules)) OnEvent(vehicle, RoughShare(vehicle), "touch-down", "stock");
        if (train.Phase is not { } phase) return;
        DebrisPhase? throwing = rules.debrisPhases.FirstOrDefault(p => p.phase == phase);
        // A phase that throws grit only on the move (taxiing): an aircraft standing with its engines on throws none.
        if (throwing == null || (throwing.movingOnly && !moved)) return;
        double chance = StripDebrisRule.Chance(rules.debrisPerHour, Exposure(comp), throwing.weight, RoughShare(vehicle), hours) * (1 - Barrier(vehicle))
            * comp.IntakeSeparator.Reaching;    // Framework 0.365.0: an intake separator on keeps half the grit out
        if (chance > 0 && Rand.Chance((float)chance)) Strike(comp, rules, phase, "phase");
    }

    /// <summary>At a lift-off or touch-down: of an aircraft on our own flight, or (<paramref name="how"/> "stock") on Vehicle Framework's.</summary>
    public static void OnEvent(VehiclePawn vehicle, double roughShare, string when, string how = "own-flight")
    {
        if (VehicleSystemsMod.Settings.showOnly || CompPhobosVehicleSystems.Of(vehicle) is not { Usable: true } comp || Exposure(comp) <= 0f) return;
        PhobosLandingSurfaceRulesDef rules = Rules;
        double chance = StripDebrisRule.EventChance(rules.debrisPerEvent, Exposure(comp), roughShare, Barrier(vehicle)) * comp.IntakeSeparator.ReachingNow();
        if (chance > 0 && Rand.Chance((float)chance)) Strike(comp, rules, when, how);
    }

    static void Strike(CompPhobosVehicleSystems comp, PhobosLandingSurfaceRulesDef rules, string when, string how)
    {
        VehiclePawn vehicle = comp.Vehicle;
        List<string> roles = Powertrain.EngineRolesOf(comp).Where(role => comp.PartsOf(role).Any(p => p.HealthPercent > rules.engineWearFloor)).ToList();
        if (roles.Count == 0) return;
        string role = roles.RandomElement();
        VehicleComponent? part = comp.PartsOf(role).FirstOrDefault();
        comp.WearRole(role, 0f, rules.debrisDamage, rules.engineWearFloor, shedsMetal: false);
        // The case says which flight it was (Framework 0.301.0): our own ("own-flight"), Vehicle Framework's ("stock"), or a phase's grit.
        if (Seen.Wants("aircraft.strip-debris", vehicle, how)) Seen.Note("aircraft.strip-debris", vehicle, when, how);
        if (vehicle.Faction != Faction.OfPlayer) return;
        Messages.Message("PhobosVehicleSystems_StripDebris".Translate(vehicle.LabelShort.Named("VEHICLE"), (part?.props.label ?? role).Named("PART")).Resolve(),
            vehicle, MessageTypeDefOf.NegativeEvent);
    }
}

/// <summary>How a twin's fuel cross-feed is set (Framework 0.250.0, step 11; the owner's card: an automatic and a manual mode).</summary>
public enum CrossFeedMode
{
    /// <summary>Shut, until a tank runs dry while the other holds fuel: then the crew opens it.</summary>
    Automatic,
    Open,
    Shut,
}

/// <summary>
/// A twin's two fuel tanks, one each side, each feeding its own engine, with a cross-feed between them (Framework 0.250.0, step 11; the
/// owner's card of 23 Nov 2026: an automatic and a manual mode). The vehicle keeps its one fuel figure; this keeps how it is split, as an
/// imbalance (SideTankRule). Engines burn their own side with the cross-feed shut; a holed tank leaks from one side (drawn at random when
/// the leak begins: the tank part does not say which side was hit), and once that side is dry it stops leaking and its engine starves.
/// With the cross-feed open both engines draw on both tanks, so neither starves, but the leak drains them both. Side i is engine i's
/// (Engine1, Engine2), named in what the player reads by its engine. Saved by the comp.
/// </summary>
public sealed class SideTanks
{
    readonly CompPhobosVehicleSystems comp;
    internal float imbalance;
    internal int leakSide = -1;
    internal CrossFeedMode mode = CrossFeedMode.Automatic;
    internal bool crewOpened;
    /// <summary>Derived, never saved: the fuel at the last update, what leaked since, and whether a leak was seen since.</summary>
    float lastTotal = float.NaN;
    float leakedSince;
    bool leakSeen;
    readonly bool[] starvedTold = new bool[2];

    public SideTanks(CompPhobosVehicleSystems comp) => this.comp = comp;

    static PhobosVehicleRulesDef Rules => PhobosRulesDef.Current<PhobosVehicleRulesDef>();

    /// <summary>Which side each engine draws on (Framework 0.254.0: any number of engines, by where each sits; a twin's by its order).</summary>
    int[]? EngineSides => comp.Powertrain?.EngineSides();

    public bool Applies => comp.Props.loadout is { sideTanks: true } && comp.Vehicle.CompFueledTravel != null
                           && EngineSides is { } sides && sides.Contains(0) && sides.Contains(1);

    public bool Open => mode == CrossFeedMode.Open || (mode == CrossFeedMode.Automatic && crewOpened);

    float Total => comp.Vehicle.CompFueledTravel?.Fuel ?? 0f;

    /// <summary>Each side's fuel now, side 0 then side 1.</summary>
    public (double first, double second) Sides() => SideTankRule.Sides(Total, imbalance);

    /// <summary>Called by the central fuel burn for every leak (VehicleFuel.Burn), with what it drained.</summary>
    public void NoteLeak(float amount)
    {
        if (!Applies) return;
        leakSeen = true;
        if (leakSide < 0) leakSide = Rand.ValueSeeded(comp.Vehicle.thingIDNumber ^ Find.TickManager.TicksGame) < 0.5f ? 0 : 1;
        if (amount <= 0f) return;
        imbalance = (float)SideTankRule.AfterLeak(imbalance, amount, leakSide, Open);
        leakedSince += amount;
    }

    /// <summary>Whether a leak has nothing left to drain: its side is dry and the cross-feed is shut, so the tank stops leaking.</summary>
    public bool LeakSideDry => Applies && !Open && leakSide >= 0 && SideTankRule.SideDry(Total, imbalance, leakSide, Rules.sideTankDry);

    /// <summary>Whether engine <paramref name="engine"/> (0 or 1) is starved: its side dry, the cross-feed shut, fuel still in the other.</summary>
    public bool Starved(int engine) => Applies && !Open && Total > Rules.sideTankDry && EngineSides is { } sides && engine >= 0 && engine < sides.Length
                                       && sides[engine] >= 0 && SideTankRule.SideDry(Total, imbalance, sides[engine], Rules.sideTankDry);

    /// <summary>At each update, after the systems advance: shares what the engines burned since between the sides, fills the emptier side
    /// first with what was put in, and lets a crew open the cross-feed in the automatic mode.</summary>
    public void Update()
    {
        if (!Applies) return;
        float total = Total;
        if (float.IsNaN(lastTotal)) lastTotal = total;
        float change = total - lastTotal;
        if (change > 0f) imbalance = (float)SideTankRule.AfterRefuel(imbalance, change);
        else
        {
            float burned = -change - leakedSince;
            if (burned > 0f && comp.Powertrain is { } train)
            {
                // Each side burns by the work of the engines it feeds; an engine on the centreline draws on both alike.
                IReadOnlyList<double> outputs = train.EngineOutputs();
                int[] sides = EngineSides ?? System.Array.Empty<int>();
                double first = 0, second = 0;
                for (int i = 0; i < outputs.Count && i < sides.Length; i++)
                {
                    if (sides[i] == 0) first += outputs[i];
                    else if (sides[i] == 1) second += outputs[i];
                    else { first += outputs[i] / 2; second += outputs[i] / 2; }
                }
                double share = first + second <= 0 ? 0.5 : first / (first + second);
                imbalance = (float)SideTankRule.AfterBurn(imbalance, burned * share, burned * (1 - share), Open);
            }
        }
        imbalance = System.Math.Max(-total, System.Math.Min(total, imbalance));
        lastTotal = total;
        leakedSince = 0f;
        if (!leakSeen) leakSide = -1;
        leakSeen = false;
        AutomaticCrew(total);
        TellStarved();
    }

    void AutomaticCrew(float total)
    {
        float dry = Rules.sideTankDry;
        if (mode != CrossFeedMode.Automatic) return;
        bool sideDry = SideTankRule.SideDry(total, imbalance, 0, dry) || SideTankRule.SideDry(total, imbalance, 1, dry);
        if (crewOpened)
        {
            // Shut again once the leak is gone and the tanks are level.
            if (leakSide < 0 && System.Math.Abs(imbalance) <= dry) crewOpened = false;
            return;
        }
        bool crew = comp.Vehicle.Faction != Faction.OfPlayer || comp.Vehicle.AllPawnsAboard.Count > 0;
        if (!sideDry || total <= dry || !crew) return;
        crewOpened = true;
        if (Seen.Wants("vehicles.crossfeed-opened", comp.Vehicle)) Seen.Note("vehicles.crossfeed-opened", comp.Vehicle, "automatic");
        if (comp.Vehicle.Faction == Faction.OfPlayer)
        {
            Messages.Message("PhobosVehicleSystems_CrossFeedCrewOpened".Translate(comp.Vehicle.LabelShort.Named("VEHICLE")).Resolve(), comp.Vehicle,
                MessageTypeDefOf.CautionInput);
        }
    }

    void TellStarved()
    {
        for (int i = 0; i < 2; i++)
        {
            bool starved = SideStarved(i);
            if (starved && !starvedTold[i])
            {
                if (Seen.Wants("vehicles.engine-starved", comp.Vehicle)) Seen.Note("vehicles.engine-starved", comp.Vehicle, i.ToString());
                if (comp.Vehicle.Faction == Faction.OfPlayer)
                {
                    Messages.Message("PhobosVehicleSystems_EngineStarved".Translate(comp.Vehicle.LabelShort.Named("VEHICLE"), EngineLabel(i).Named("ENGINE")).Resolve(),
                        comp.Vehicle, MessageTypeDefOf.NegativeEvent);
                }
            }
            starvedTold[i] = starved;
        }
    }

    /// <summary>Whether the engines a side feeds are starved.</summary>
    bool SideStarved(int side) => Applies && !Open && Total > Rules.sideTankDry && SideTankRule.SideDry(Total, imbalance, side, Rules.sideTankDry);

    /// <summary>The engines a side feeds, by their parts' labels ("Engine 1, Engine 2"; a twin's one engine).</summary>
    string EngineLabel(int side)
    {
        IReadOnlyList<string> roles = comp.Powertrain?.EngineRoles ?? System.Array.Empty<string>();
        int[] sides = EngineSides ?? System.Array.Empty<int>();
        var labels = new List<string>();
        for (int i = 0; i < roles.Count && i < sides.Length; i++)
        {
            if (sides[i] == side) labels.Add(comp.PartsOf(roles[i]).FirstOrDefault()?.props.label ?? roles[i]);
        }
        return labels.Count == 0 ? side.ToString() : string.Join(", ", labels);
    }

    /// <summary>The next mode, as the player's button turns it: automatic, open, shut, automatic.</summary>
    public void Cycle()
    {
        mode = mode switch { CrossFeedMode.Automatic => CrossFeedMode.Open, CrossFeedMode.Open => CrossFeedMode.Shut, _ => CrossFeedMode.Automatic };
        crewOpened = false;
        if (mode != CrossFeedMode.Automatic && Seen.Wants("vehicles.crossfeed-opened", comp.Vehicle, mode.ToString()))
            Seen.Note("vehicles.crossfeed-opened", comp.Vehicle, null, mode.ToString().ToLowerInvariant());
    }

    /// <summary>The button for the player's twin: presentation only; the click turns the mode through <see cref="Cycle"/>.</summary>
    public Gizmo Button()
    {
        (double first, double second) = Sides();
        float capacity = System.Math.Max(0.01f, comp.Vehicle.CompFueledTravel?.FuelCapacity ?? 1f);
        string sides = "PhobosVehicleSystems_CrossFeedSides".Translate(EngineLabel(0).Named("FIRST"), ((float)(first * 2 / capacity)).ToStringPercent("0").Named("FIRSTFILL"),
            EngineLabel(1).Named("SECOND"), ((float)(second * 2 / capacity)).ToStringPercent("0").Named("SECONDFILL")).Resolve();
        string label = mode switch
        {
            CrossFeedMode.Open => "PhobosVehicleSystems_CrossFeedOpen",
            CrossFeedMode.Shut => "PhobosVehicleSystems_CrossFeedShut",
            _ => crewOpened ? "PhobosVehicleSystems_CrossFeedAutoOpened" : "PhobosVehicleSystems_CrossFeedAuto",
        };
        return new Command_Action
        {
            defaultLabel = label.Translate(),
            defaultDesc = "PhobosVehicleSystems_CrossFeedDesc".Translate(sides.Named("SIDES")).Resolve(),
            icon = SystemIcons.For("Phobos/VehicleSystems/Dash/Fuel"),
            action = Cycle,
        };
    }
}
