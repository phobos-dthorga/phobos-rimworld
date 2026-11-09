using System;
using System.Collections.Generic;
using System.Linq;
using LudeonTK;
using Phobos.Framework.Debug;
using Phobos.Framework.Rules;
using Phobos.Framework.Vehicles.Rules;
using RimWorld;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// The riskiest vehicle checks of the in-game test pass, run in one go. Part one reads every vehicle the player has, wherever it is, and
/// so catches a vehicle that loaded badly; it also runs by itself after a load in dev mode. Part two works on a vehicle made for the test
/// and thrown away unspawned, so nothing in the colony is touched: fittings, tunes, a destroyed part and its replacement, and grades,
/// through the same service calls a mechanic's job makes. A pass says these run and move the right figures; it is not a play test.
/// </summary>
public static class VehicleSelfTest
{
    /// <summary>How many designs to try before giving up on finding one to exercise.</summary>
    const int MaxDesignsTried = 12;

    [DebugAction(DebugCategory.Name, "Vehicle systems: run the self-test", allowedGameStates = AllowedGameStates.Playing)]
    static void RunFromMenu()
    {
        var test = new SelfTest();
        ReadEveryVehicle(test);
        ExerciseTestVehicle(test);
        string report = "Vehicle self-test: " + test.Report();
        // The window is the answer. The game's log puts a stack trace under every line, which reads like a fault to a player, so a pass stays out of it.
        if (test.Failed > 0) Log.Warning(VehicleSystemsMod.LogPrefix + " " + report);
        Find.WindowStack.Add(new Dialog_MessageBox(report));
    }

    /// <summary>After a load, in dev mode: silent if every vehicle reads cleanly, an error and a message if one does not.</summary>
    public static void AfterLoad()
    {
        if (!Prefs.DevMode) return;
        var test = new SelfTest();
        ReadEveryVehicle(test);
        if (test.Failed == 0) return;
        VehicleSystemsMod.Logger.Error("Self-test after load: " + test.Report());
        Messages.Message("Vehicle self-test after load: " + test.Failed + " failed. See the log.", MessageTypeDefOf.NegativeEvent, historical: false);
    }

    // ---- Part one: every vehicle the player has ----

    static void ReadEveryVehicle(SelfTest test)
    {
        foreach (VehiclePawn vehicle in VehicleSystems.AllVehicles().ToList())
        {
            CompPhobosVehicleSystems? comp = vehicle.GetComp<CompPhobosVehicleSystems>();
            if (comp == null) continue;
            string name = "Reading the " + vehicle.LabelShort;
            if (test.Step(name, () => ReadThrough(comp))) test.Expect(name, GradesInForce(comp), "a graded part's health does not match its grade");
        }
    }

    /// <summary>What the game and the tabs read from a vehicle, in one pass. Throws where a loaded vehicle would.</summary>
    static void ReadThrough(CompPhobosVehicleSystems comp)
    {
        _ = comp.States.Count;
        _ = comp.Powertrain?.Spec.RatedPowerKw;
        VehicleFittings fittings = comp.Fittings;
        _ = fittings.PowerFactor + fittings.FuelFactor + fittings.FlatFuelFactor + fittings.FinalDriveFactor;
        foreach (VehicleStatDef stat in DefDatabase<VehicleStatDef>.AllDefsListForReading) _ = comp.StatFactor(stat);
        _ = comp.Repairs.Pending.Count;
        if (comp.Vehicle.statHandler == null) return;
        foreach (VehicleComponent part in comp.Vehicle.statHandler.components)
        {
            _ = fittings.GradeOf(part.props.key);
            _ = comp.Repairs.CanBeGraded(part);
            _ = part.MaxHealth;
        }
        _ = fittings.ListedParts().Count();
    }

    static bool GradesInForce(CompPhobosVehicleSystems comp)
    {
        if (comp.Vehicle.statHandler == null) return true;
        foreach (VehicleComponent part in comp.Vehicle.statHandler.components)
        {
            if (!comp.Fittings.GradeOf(part.props.key).isDefault && !part.AddHealthModifiers.ContainsKey(VehicleFittings.GradeHealthKey)) return false;
        }
        return true;
    }

    // ---- Part two: a vehicle made for the test ----

    static void ExerciseTestVehicle(SelfTest test)
    {
        Pawn? mechanic = PawnsFinder.AllMaps_FreeColonistsSpawned.FirstOrDefault();
        if (mechanic == null)
        {
            test.Skip("The test vehicle", "no colonist on a map to stand in as the mechanic");
            return;
        }
        VehicleNotices.Quiet = true;
        VehiclePawn? vehicle = null;
        try
        {
            vehicle = MakeTestVehicle(test);
            if (vehicle == null) return;
            CompPhobosVehicleSystems comp = vehicle.GetComp<CompPhobosVehicleSystems>();
            test.Step("Fittings", () => Fittings(test, comp, mechanic));
            test.Step("Tunes", () => Tunes(test, comp, mechanic));
            test.Step("Grades", () => Grades(test, comp, mechanic));
            test.Step("Enemy sets", () => EnemySets(test, comp));
        }
        finally
        {
            VehicleNotices.Quiet = false;
            if (vehicle != null) test.Step("Throwing the test vehicle away", () => vehicle.Discard(silentlyRemoveReferences: true));
        }
    }

    /// <summary>A vehicle of a design with something to fit, a design the player owns if possible, on a road-going piston engine for preference.</summary>
    static VehiclePawn? MakeTestVehicle(SelfTest test)
    {
        IEnumerable<VehicleDef> designs = VehicleSystems.AllVehicles().Select(v => v.VehicleDef)
            .Concat(DefDatabase<VehicleDef>.AllDefsListForReading.Where(d => d.GetCompProperties<CompProperties_PhobosVehicleSystems>() != null))
            .Distinct().Take(MaxDesignsTried);
        VehiclePawn? fallback = null;
        foreach (VehicleDef design in designs)
        {
            VehiclePawn? candidate = null;
            if (!test.Step("Making a test " + design.defName, () => candidate = VehicleSpawner.GenerateVehicle(design, Faction.OfPlayer)) || candidate == null) continue;
            CompPhobosVehicleSystems? comp = candidate.GetComp<CompPhobosVehicleSystems>();
            if (comp != null && comp.Usable && comp.Fittings.ListedParts().Any())
            {
                if (comp.Fittings.CanTune(VehicleTuneAxis.Engine))
                {
                    if (fallback != null) fallback.Discard(silentlyRemoveReferences: true);
                    return candidate;
                }
                if (fallback == null)
                {
                    fallback = candidate;
                    continue;
                }
            }
            candidate.Discard(silentlyRemoveReferences: true);
        }
        if (fallback == null) test.Skip("The test vehicle", "no design here with parts to fit");
        return fallback;
    }

    /// <summary>Puts a repair's materials in the cargo, as the haulers would.</summary>
    static void Give(VehiclePawn vehicle, RepairWork work)
    {
        foreach ((ThingDef def, int count) in work.Options[0])
        {
            int left = count;
            while (left > 0)
            {
                Thing thing = ThingMaker.MakeThing(def, def.MadeFromStuff ? GenStuff.DefaultStuffFor(def) : null);
                thing.stackCount = Math.Min(left, def.stackLimit);
                left -= thing.stackCount;
                vehicle.inventory.innerContainer.TryAdd(thing);
            }
        }
    }

    /// <summary>The whole path of one piece of work: listed, waiting for its materials, ready once they are aboard, then done.</summary>
    static bool Work(SelfTest test, CompPhobosVehicleSystems comp, string name, Func<RepairWork, bool> which, Pawn mechanic)
    {
        RepairWork? work = comp.Repairs.Pending.FirstOrDefault(which);
        test.Expect(name + ": the work is listed", work != null);
        if (work == null) return false;
        if (work.Options[0].Count > 0) test.Expect(name + ": waits for its materials", comp.Repairs.Find(work.Key) == null);
        Give(comp.Vehicle, work);
        test.Expect(name + ": ready once they are aboard", comp.Repairs.Find(work.Key) != null);
        bool done = comp.Repairs.Complete(work.Key, mechanic);
        test.Expect(name + ": the work finishes", done);
        return done;
    }

    static bool Moved(CompPhobosVehicleSystems comp, PhobosVehicleFittingDef def)
    {
        if (def.statFactors.Any(spec => comp.StatFactor(spec.stat!) != 1f)) return true;
        if (def.parameterFactors.Any(spec => comp.Props.systems.Any(system => comp.Fittings.ParameterFactor(system.def.defName, spec.parameter) != 1f))) return true;
        return def.powerFactor != 1f && comp.Fittings.PowerFactor != 1f;
    }

    static void Fittings(SelfTest test, CompPhobosVehicleSystems comp, Pawn mechanic)
    {
        VehicleFittings fittings = comp.Fittings;
        VehicleComponent? part = fittings.ListedParts().FirstOrDefault(p => fittings.OptionsFor(p).Any());
        if (part == null)
        {
            test.Skip("Fittings", "this design has no part a fitting suits");
            return;
        }
        string key = part.props.key;
        PhobosVehicleFittingDef def = fittings.OptionsFor(part).First();

        fittings.OrderFitting(part, def);
        Work(test, comp, "Fitting " + def.defName, w => w.Order?.fitting == def, mechanic);
        test.Expect("The fitting is on the part", fittings.FittedOn(key) == def);
        test.Expect("The fitting moves a figure", Moved(comp, def));

        fittings.OrderRemoval(part);
        FitOrder? order = fittings.OrderOn(key);
        test.Expect("Taking it off is ordered", order != null);
        if (order != null) fittings.Cancel(order);
        test.Expect("Cancelling leaves no work behind", fittings.OrderOn(key) == null && !comp.Repairs.Pending.Any(w => w.Kind == RepairKind.Fitting));
        test.Expect("Cancelling leaves the fitting on", fittings.FittedOn(key) == def);

        fittings.OrderRemoval(part);
        Work(test, comp, "Taking the fitting off", w => w.Order != null && w.Order.remove, mechanic);
        test.Expect("The fitting is off", fittings.FittedOn(key) == null);

        fittings.OrderFitting(part, def);
        Work(test, comp, "Fitting it again", w => w.Order?.fitting == def, mechanic);
        part.SetHealth(0f);
        fittings.Sweep();
        test.Expect("A fitting is lost with its part", fittings.FittedOn(key) == null);
        test.Expect("The figures go back when it is lost", !Moved(comp, def));

        if (VehicleSystemsMod.Settings.destroyedPartsNeedSupplies && comp.Repairs.PartBlocked(part))
        {
            Work(test, comp, "Replacing the destroyed part", w => w.Kind == RepairKind.ReplacePart && w.Part == part, mechanic);
            test.Expect("The replaced part has health", part.Health > 0f);
        }
        else
        {
            test.Skip("Replacing the destroyed part", "destroyed parts are set to mend for free");
        }
        part.SetHealth(part.MaxHealth);
    }

    static void Tunes(SelfTest test, CompPhobosVehicleSystems comp, Pawn mechanic)
    {
        VehicleFittings fittings = comp.Fittings;
        if (!fittings.CanTune(VehicleTuneAxis.Engine))
        {
            test.Skip("Tunes", "this design has no engine to tune");
            return;
        }
        double before = comp.Powertrain!.Spec.RatedPowerKw;
        PhobosVehicleTuneDef tune = VehicleFittings.TunesOf(VehicleTuneAxis.Engine).First(t => t.powerFactor > 1f);
        fittings.OrderTune(tune);
        Work(test, comp, "Tuning the engine", w => w.Order?.tune == tune, mechanic);
        test.Near("The tune raises the engine's power", comp.Powertrain!.Spec.RatedPowerKw, before * tune.powerFactor, 0.1);
        PhobosVehicleTuneDef asBuilt = VehicleFittings.TunesOf(VehicleTuneAxis.Engine).First(t => t.isDefault);
        fittings.OrderTune(asBuilt);
        Work(test, comp, "Tuning it back", w => w.Order?.tune == asBuilt, mechanic);
        test.Near("Back to the power it was built with", comp.Powertrain!.Spec.RatedPowerKw, before, 0.1);
    }

    static void Grades(SelfTest test, CompPhobosVehicleSystems comp, Pawn mechanic)
    {
        VehicleFittings fittings = comp.Fittings;
        VehicleComponent? part = fittings.ListedParts().FirstOrDefault(p => comp.Repairs.CanBeGraded(p) && p.Health > 0f && fittings.FittedOn(p.props.key) == null);
        if (part == null)
        {
            test.Skip("Grades", "this design has no part to grade");
            return;
        }
        float standard = part.MaxHealth;
        PhobosVehiclePartGradeDef heavy = VehicleFittings.Grades.First(g => g.healthFactor > 1f);
        PhobosVehiclePartGradeDef salvaged = VehicleFittings.Grades.First(g => !g.holdsFitting);

        fittings.OrderGrade(part, heavy);
        Work(test, comp, "Refitting a heavy-duty part", w => w.Order?.grade == heavy, mechanic);
        test.Near("Its maximum health rises", part.MaxHealth, standard * heavy.healthFactor);
        test.Near("It is left at full health", part.Health, part.MaxHealth);
        test.Near("It takes lighter faults", fittings.FaultFactor(part.props.key), heavy.faultFactor, 0.001);

        part.AddHealthModifiers.Remove(VehicleFittings.GradeHealthKey);
        fittings.ReapplyGrades();
        test.Near("Its health survives what a load does", part.MaxHealth, standard * heavy.healthFactor);

        part.SetHealth(0f);
        fittings.Sweep();
        fittings.OrderGrade(part, salvaged);
        test.Expect("A grade chosen for a destroyed part waits for its replacement", fittings.OrderOn(part.props.key)?.grade == salvaged);
        if (VehicleSystemsMod.Settings.destroyedPartsNeedSupplies && comp.Repairs.PartBlocked(part))
        {
            Work(test, comp, "Replacing it with a salvaged part", w => w.Kind == RepairKind.ReplacePart && w.Part == part, mechanic);
            test.Expect("The new part is salvaged", fittings.GradeOf(part.props.key) == salvaged);
            test.Near("Its maximum health falls", part.MaxHealth, standard * salvaged.healthFactor);
            test.Expect("No order is left behind", fittings.OrderOn(part.props.key) == null);
        }
        else
        {
            test.Skip("Replacing with a salvaged part", "destroyed parts are set to mend for free");
            fittings.Clear();
            part.SetHealth(standard);
        }
        PhobosVehicleFittingDef? any = fittings.OptionsFor(part).FirstOrDefault();
        if (any != null && !fittings.GradeOf(part.props.key).holdsFitting) test.Expect("A salvaged part refuses a fitting", fittings.Refusal(any, part) != null);
    }

    static void EnemySets(SelfTest test, CompPhobosVehicleSystems comp)
    {
        comp.Fittings.Clear();
        foreach (PhobosVehicleFitSetDef set in DefDatabase<PhobosVehicleFitSetDef>.AllDefsListForReading)
        {
            comp.Fittings.Apply(set);
            ReadThrough(comp);
            comp.Fittings.Clear();
        }
        test.Expect("Every enemy set goes on and reads cleanly", true);
    }
}

/// <summary>Measures what the vehicle model costs over a stretch of game time, against its budget, and shows the verdict in one window.</summary>
public static class VehicleCostMeasure
{
    static int startTick = -1;
    static int endTick = -1;

    static int Now => Find.TickManager.TicksGame;

    /// <summary>Called on every vehicle's rare tick; one comparison while nothing is being measured.</summary>
    public static void CheckDone()
    {
        if (endTick < 0) return;
        if (Now < startTick)
        {
            endTick = -1;    // another game has been loaded since it started
            return;
        }
        if (Now >= endTick) Finish();
    }

    [DebugAction(DebugCategory.Name, "Vehicle systems: measure the cost for an in-game hour", allowedGameStates = AllowedGameStates.Playing)]
    static void MeasureHour() => Start(ElapsedRule.TicksPerHour);

    [DebugAction(DebugCategory.Name, "Vehicle systems: measure the cost for an in-game day", allowedGameStates = AllowedGameStates.Playing)]
    static void MeasureDay() => Start(ElapsedRule.TicksPerHour * 24);

    static void Start(int ticks)
    {
        PhobosTiming.Reset();
        startTick = Now;
        endTick = startTick + ticks;
        Messages.Message("Measuring the vehicle model. Leave the game running unpaused, in dev mode; a window opens when it is done.", MessageTypeDefOf.SilentInput, historical: false);
    }

    /// <summary>The simulation's own labels; a tab left open is the player's choice and not part of the budget.</summary>
    static bool IsSimulation(string label) => label.StartsWith("vehicle ", StringComparison.Ordinal);

    static void Finish()
    {
        int ticks = Now - startTick;
        endTick = -1;
        PhobosVehicleRulesDef rules = PhobosVehicleRulesDef.Current<PhobosVehicleRulesDef>();
        double perTick = TimeBudgetRule.MsPerTick(PhobosTiming.TotalMs(IsSimulation), ticks);
        double worst = PhobosTiming.WorstMs(IsSimulation);
        bool within = TimeBudgetRule.Within(perTick, worst, rules.budgetMsPerTick, rules.budgetMsSingleCall);
        int full = 0, lite = 0;
        foreach (VehiclePawn vehicle in VehicleSystems.AllVehicles())
        {
            DetailTier? tier = vehicle.GetComp<CompPhobosVehicleSystems>()?.Tier;
            if (tier == DetailTier.Full) full++;
            else if (tier == DetailTier.Lite) lite++;
        }
        string verdict = perTick <= 0 ? "NOTHING WAS TIMED (dev mode must stay on, and the game unpaused)" : within ? "WITHIN BUDGET" : "OVER BUDGET";
        string report = verdict + "\n\nAverage: " + perTick.ToString("0.0000") + " ms a tick (budget " + rules.budgetMsPerTick.ToString("0.00")
            + ")\nWorst single call: " + worst.ToString("0.000") + " ms (budget " + rules.budgetMsSingleCall.ToString("0.00") + ")\nOver " + ticks + " ticks, with " + full
            + " vehicles in full detail and " + lite + " in light detail.\n\nNot counted: single stat and speed-cap lookups, and tabs left open.\n\n" + PhobosTiming.Report();
        if (perTick > 0 && !within) Log.Warning(VehicleSystemsMod.LogPrefix + " Vehicle cost: " + report);
        Find.WindowStack.Add(new Dialog_MessageBox(report));
    }
}
