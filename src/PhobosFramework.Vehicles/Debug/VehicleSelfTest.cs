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
/// through the same service calls a mechanic's job makes. From Framework 0.94.0 it also covers what fittings cost, graph marks, wear
/// trends, axles and the diagnosis report on that vehicle, and part three lifts a light vehicle with a helicopter's sling and sets it down
/// again, on two vehicles spawned on clear open ground and destroyed afterwards. A pass says these run and move the right figures; it is
/// not a play test, and nothing drawn is covered: a screen needs the game's own drawing pass.
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
        test.Step("Parts bills", () => PartsBillsRead(test));
        ExerciseTestVehicle(test);
        SlingLift(test);
        string report = "Vehicle self-test: " + test.Report()
            + "\n\nNot covered: anything drawn (tabs, windows and the range on the world map need to be looked at), and loading a report back from a save.";
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

    /// <summary>
    /// Every parts bill as the game holds it now (Framework 0.95.0): the thing has a cost, its shell's first item is still first (Vehicle
    /// Framework reads it for the destruction sound), every role came to something or is covered by a kit, and a bill worked out again
    /// gives the same cost, so nothing depends on the order things ran in at start-up.
    /// </summary>
    static void PartsBillsRead(SelfTest test)
    {
        List<Phobos.Framework.Bills.PhobosBillDef> bills = DefDatabase<Phobos.Framework.Bills.PhobosBillDef>.AllDefsListForReading;
        test.Expect("No parts bill failed at start-up", Phobos.Framework.Bills.PartsBills.Failed == 0, Phobos.Framework.Bills.PartsBills.Failed + " failed");
        foreach (Phobos.Framework.Bills.PhobosBillDef bill in bills)
        {
            string name = bill.target?.label ?? bill.defName;
            List<ThingDefCountClass>? cost = bill.target?.costList;
            test.Expect(name + ": has a cost", cost != null && cost.Count > 0);
            if (cost == null || cost.Count == 0) continue;
            if (bill.shell is { Count: > 0 } shell) test.Expect(name + ": its shell's first item is still first", cost[0].thingDef == shell[0].thingDef, cost[0].thingDef.defName);
            test.Expect(name + ": every role was read", bill.rows.Count == bill.parts.Count, bill.rows.Count + " of " + bill.parts.Count);
            foreach (Phobos.Framework.Bills.BillRow row in bill.rows)
            {
                bool covered = row.Lines.Count > 0 || (row.Supply is { coversWholeBuild: true } && row.Source == BillSource.Ours);
                test.Expect(name + ": the role " + row.Role + " came to something", covered);
            }
            List<ThingDefCountClass> again = Phobos.Framework.Bills.PartsBills.Resolve(bill.shell, bill.parts, bill.Size, bill.tags, Phobos.Framework.Bills.PartsBills.OthersAllowed);
            test.Expect(name + ": worked out again, the same cost", Phobos.Framework.Bills.PartsBills.Describe(again) == Phobos.Framework.Bills.PartsBills.Describe(cost),
                Phobos.Framework.Bills.PartsBills.Describe(again));
        }
    }

    // ---- Part one: every vehicle the player has ----

    static void ReadEveryVehicle(SelfTest test)
    {
        foreach (VehiclePawn vehicle in VehicleSystems.AllVehicles().ToList())
        {
            CompPhobosVehicleSystems? comp = vehicle.GetComp<CompPhobosVehicleSystems>();
            if (comp == null) continue;
            string name = "Reading the " + vehicle.LabelShort;
            if (!test.Step(name, () => ReadThrough(comp))) continue;
            test.Expect(name, GradesInForce(comp), "a graded part's health does not match its grade");
            string? over = OverfullPart(comp);
            test.Expect("No part of the " + vehicle.LabelShort + " holds more than its full health", over == null, over ?? "");
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

    /// <summary>A part holding more than its maximum health (the L-ATV's drive belt at 300%, fixed in 0.83.1), named; null when none does.</summary>
    static string? OverfullPart(CompPhobosVehicleSystems comp)
    {
        if (comp.Vehicle.statHandler == null) return null;
        foreach (VehicleComponent part in comp.Vehicle.statHandler.components)
        {
            if (part.Health > part.MaxHealth + 0.01f) return part.props.label + " holds " + part.Health.ToString("0.#") + " of " + part.MaxHealth.ToString("0.#");
        }
        return null;
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
        VehicleFittings.AssumeRigForTest = true;
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
            test.Step("Costs", () => Costs(test, comp));
            test.Step("Graph marks", () => Marks(test, comp));
            test.Step("Wear trends", () => WearTrends(test, comp));
            test.Step("Axles", () => Axles(test, comp));
            test.Step("The diagnosis report", () => Report(test, comp, mechanic));
        }
        finally
        {
            VehicleNotices.Quiet = false;
            VehicleFittings.AssumeRigForTest = false;
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
        if (VehicleSystemsMod.Settings.tuningNeedsRig)
        {
            RepairWork? back = comp.Repairs.Pending.FirstOrDefault(w => w.Order?.tune == asBuilt);
            VehicleFittings.AssumeRigForTest = false;
            test.Expect("A tune waits for a tuning rig", back != null && comp.Repairs.Find(back.Key) == null);
            VehicleFittings.AssumeRigForTest = true;
        }
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

    // ---- Framework 0.94.0: the day's work, on the same throwaway vehicle ----

    /// <summary>What a fitting costs here is the listed figure times the weight rule and the player's presets, and never nothing.</summary>
    static void Costs(SelfTest test, CompPhobosVehicleSystems comp)
    {
        PhobosVehicleFittingDef? def = DefDatabase<PhobosVehicleFittingDef>.AllDefsListForReading.FirstOrDefault(d => d.materials.Count > 0);
        if (def == null)
        {
            test.Skip("Costs", "no fitting here has materials");
            return;
        }
        PhobosVehicleRulesDef rules = PhobosVehicleRulesDef.Current<PhobosVehicleRulesDef>();
        VehicleSystemsSettings settings = VehicleSystemsMod.Settings;
        double kilograms = comp.Repairs.Kilograms;
        double byWeight(double exponent) => settings.fitCostScalesWithWeight
            ? FitCostRule.WeightFactor(kilograms, rules.fitCostReferenceKilograms, exponent, rules.fitWeightFactorMin, rules.fitWeightFactorMax) : 1;
        List<ThingDefCountClass> materials = comp.Fittings.MaterialsFor(def.materials);
        for (int i = 0; i < def.materials.Count; i++)
        {
            int expected = FitCostRule.Count(def.materials[i].count, byWeight(rules.fitMaterialWeightExponent) * settings.fitMaterialFactor);
            test.Expect("A fitting's " + def.materials[i].thingDef.label + " follows the weight and the preset", materials[i].count == expected,
                "asked " + materials[i].count + ", the rule gives " + expected);
            test.Expect("A fitting never costs none of a material it lists", materials[i].count >= 1);
        }
        test.Near("A fitting's work follows the weight and the preset", comp.Fittings.WorkFor(def.work), def.work * byWeight(rules.fitWorkWeightExponent) * settings.fitTimeFactor, 1);
    }

    /// <summary>An event is marked once; the same event again within the hour is not; and it can be read.</summary>
    static void Marks(SelfTest test, CompPhobosVehicleSystems comp)
    {
        string system = comp.States.FirstOrDefault()?.Def.defName ?? "";
        int before = comp.Events.Events.Count;
        comp.RecordEvent(VehicleEventKind.Service, system);
        test.Expect("A service is marked", comp.Events.Events.Count == before + 1, "the player's vehicle recorded nothing");
        comp.RecordEvent(VehicleEventKind.Service, system);
        test.Expect("The same event within the hour is one mark", comp.Events.Events.Count == before + 1);
        if (comp.Events.Events.Count > 0) test.Expect("A mark can be read", !VehicleEventText.Message(comp.Events.Events[comp.Events.Events.Count - 1]).NullOrEmpty());
    }

    /// <summary>After the test curve, every slow reading has its fifteen days and the trend rule answers.</summary>
    static void WearTrends(SelfTest test, CompPhobosVehicleSystems comp)
    {
        comp.History.DebugFill(comp.States);
        int readings = 0;
        foreach (SystemState state in comp.States)
        {
            for (int q = 0; q < state.values.Length; q++)
            {
                QuantitySpec quantity = state.Def.quantities[q];
                if (!quantity.wearTrend) continue;
                readings++;
                HistoryRing? ring = comp.History.WearFor(state, q);
                test.Expect(quantity.label + " keeps fifteen days", ring != null && ring.Count == QuantityHistory.WearCapacity,
                    ring == null ? "no history" : ring.Count + " samples of " + QuantityHistory.WearCapacity);
                if (ring == null) continue;
                double? until = TrendRule.SamplesUntil(ring.Count, i => ring[i], quantity.max, 6, 1e-6, 1e-9);
                test.Expect(quantity.label + " has a trend the rule can read", until == null || (!double.IsNaN(until.Value) && until.Value >= 0));
            }
        }
        if (readings == 0) test.Skip("Wear trends", "this design has no slow reading");
    }

    /// <summary>Every listed part is in exactly one row of the fittings window, and only wheels share a row.</summary>
    static void Axles(SelfTest test, CompPhobosVehicleSystems comp)
    {
        List<VehicleComponent> listed = comp.Fittings.ListedParts().ToList();
        List<List<VehicleComponent>> rows = comp.Fittings.PartRows();
        List<VehicleComponent> inRows = rows.SelectMany(r => r).ToList();
        test.Expect("Every listed part is in exactly one row", inRows.Count == listed.Count && inRows.Distinct().Count() == listed.Count && listed.All(inRows.Contains),
            listed.Count + " listed, " + inRows.Count + " in rows");
        test.Expect("Only wheels share a row", rows.Where(r => r.Count > 1).All(r => r.All(part => comp.PartPlays(part, "Wheels"))));
    }

    /// <summary>A diagnosis lists every part, reads as text, and can be written to a save.</summary>
    static void Report(SelfTest test, CompPhobosVehicleSystems comp, Pawn mechanic)
    {
        DiagnosisReport report = comp.Diagnostics.Take(mechanic);
        int parts = comp.Vehicle.statHandler?.components.Count ?? 0;
        test.Expect("The report lists every part", report.parts.Count == parts, report.parts.Count + " of " + parts);
        test.Expect("The report reads as text", !DiagnosisText.Body(report, figures: true).NullOrEmpty() && !DiagnosisText.Body(report, figures: false).NullOrEmpty());
        string saved = Scribe.saver.DebugOutputFor(report);
        test.Expect("The report can be written to a save", saved.Contains("vehicleLabel") && saved.Contains("parts"));
    }

    // ---- Part three: a sling lift, on two vehicles made for it ----

    /// <summary>How far from the mechanic clear open ground is looked for (authored).</summary>
    const float SlingTestRadius = 40f;

    /// <summary>
    /// A helicopter that takes a cargo hook and a light vehicle, spawned on clear open ground near a colonist: the hook and sling set
    /// refusals, the weight rule, choose, hook up, the lift (the code the launch event runs, with no launch), and the set-down. Both are
    /// destroyed afterwards. Skipped, with its reason, where no such helicopter is active or no ground is clear.
    /// </summary>
    static void SlingLift(SelfTest test)
    {
        Pawn? mechanic = PawnsFinder.AllMaps_FreeColonistsSpawned.FirstOrDefault();
        VehicleDef? helicopterDef = DefDatabase<VehicleDef>.AllDefsListForReading.FirstOrDefault(d => d.comps != null && d.comps.Any(c => c is CompProperties_PhobosSling));
        if (mechanic == null || helicopterDef == null)
        {
            test.Skip("The sling", mechanic == null ? "no colonist on a map" : "no helicopter here takes a cargo hook");
            return;
        }
        VehicleDef? loadDef = DefDatabase<VehicleDef>.AllDefsListForReading
            .Where(d => d != helicopterDef && d.GetCompProperties<CompProperties_PhobosVehicleSystems>() != null && d.GetCompProperties<CompProperties_VehicleLauncher>() == null)
            .OrderBy(d => d.Size.x * d.Size.z).FirstOrDefault();
        if (loadDef == null)
        {
            test.Skip("The sling", "no land vehicle here to lift");
            return;
        }
        Map map = mechanic.Map;
        VehiclePawn? helicopter = null, load = null;
        VehicleNotices.Quiet = true;
        try
        {
            if (!test.Step("Making a test " + helicopterDef.defName, () => helicopter = VehicleSpawner.GenerateVehicle(helicopterDef, Faction.OfPlayer)) || helicopter == null) return;
            if (!test.Step("Making a test " + loadDef.defName, () => load = VehicleSpawner.GenerateVehicle(loadDef, Faction.OfPlayer)) || load == null) return;
            if (!SlingPlacement.TryFindClear(helicopterDef, Rot4.North, map, mechanic.Position, SlingTestRadius, CellRect.Empty, out IntVec3 pad))
            {
                test.Skip("The sling", "no clear open ground within " + SlingTestRadius + " cells of " + mechanic.LabelShort + " for the helicopter");
                return;
            }
            VehiclePawn heli = helicopter, cargo = load;
            if (!test.Step("Standing the helicopter on clear ground", () => GenSpawn.Spawn(heli, pad, map, Rot4.North))) return;
            if (!SlingPlacement.TryFindSpot(cargo, heli, map, PhobosVehicleRulesDef.Current<PhobosVehicleRulesDef>().slingReachCells - 2f, out IntVec3 spot))
            {
                test.Skip("The sling", "no clear open ground beside the helicopter for the load");
                return;
            }
            if (!test.Step("Standing the load beside it", () => GenSpawn.Spawn(cargo, spot, map, Rot4.North))) return;
            test.Step("The sling", () => SlingSteps(test, heli, cargo, mechanic));
        }
        finally
        {
            VehicleNotices.Quiet = false;
            foreach (VehiclePawn? vehicle in new[] { load, helicopter })
            {
                if (vehicle == null || vehicle.Destroyed) continue;
                VehiclePawn gone = vehicle;
                test.Step("Throwing the test " + gone.def.defName + " away", () =>
                {
                    if (gone.Spawned) gone.Destroy();
                    else gone.Discard(silentlyRemoveReferences: true);
                });
            }
        }
    }

    static void SlingSteps(SelfTest test, VehiclePawn helicopter, VehiclePawn load, Pawn mechanic)
    {
        CompPhobosSling? sling = helicopter.GetComp<CompPhobosSling>();
        CompPhobosVehicleSystems? comp = helicopter.GetComp<CompPhobosVehicleSystems>();
        test.Expect("The helicopter has a sling", sling != null && comp != null);
        if (sling == null || comp == null) return;
        test.Expect("With no hook it says so", sling.Refusal() != null);
        string? mount = comp.Props.roleKeys.TryGetValue(SlingRoles.HookMount, out List<string>? keys) ? keys.FirstOrDefault() : null;
        PhobosVehicleFittingDef? hook = DefDatabase<PhobosVehicleFittingDef>.AllDefsListForReading.FirstOrDefault(d => d.grants.Contains(CompPhobosSling.HookAbility));
        test.Expect("The helicopter has a hook mount, and a cargo hook exists", mount != null && hook != null);
        if (mount == null || hook == null) return;
        comp.Fittings.FitForTest(mount, hook);
        test.Expect("With a hook but no sling set it says so", sling.Refusal() != null);
        helicopter.inventory.innerContainer.TryAdd(ThingMaker.MakeThing(VehicleSystemsThingDefOf.PhobosVehicleSystems_SlingSet));
        test.Expect("With both it is ready to lift", sling.Refusal() == null, sling.Refusal() ?? "");

        PhobosVehicleRulesDef rules = PhobosVehicleRulesDef.Current<PhobosVehicleRulesDef>();
        double expected = SlingRule.Capacity(sling.RatedKilograms, helicopter.Map.mapTemperature.OutdoorTemp, VehicleSystemsMod.Settings.slingHeatDerating,
            rules.slingDerateFromCelsius, rules.slingLossPerDegree, rules.slingMaxLoss);
        test.Near("Today's capacity follows the hot-day rule", sling.CapacityKilograms(helicopter.Map), expected, 0.5);
        test.Expect("A load weighs at least its empty weight", CompPhobosSling.LoadKilograms(load) > 0);
        string? refusal = sling.RefusalFor(load);
        if (refusal != null)
        {
            test.Skip("Lifting the load", "the helicopter refuses the " + load.LabelShort + ": " + refusal);
            return;
        }
        sling.Choose(load);
        test.Expect("The load is chosen", sling.Hooked == load);
        test.Expect("It waits for a crew only with that setting on", sling.AwaitingCrew == VehicleSystemsMod.Settings.slingGroundCrew);
        if (sling.AwaitingCrew)
        {
            test.Expect("The launch waits for the hook-up", sling.LaunchRefusal() != null);
            test.Expect("The hook-up job can find the load", SlingWaits.WaitingOn(load) == sling && SlingWaits.LoadsWaiting(load.Map).Contains(load));
            sling.HookedUp(mechanic);
        }
        test.Expect("Hooked up, nothing stops the launch", sling.LaunchRefusal() == null, sling.LaunchRefusal() ?? "");
        test.Expect("Nothing still waits for a crew", SlingWaits.WaitingOn(load) == null);

        sling.TakeLoad();
        test.Expect("The load is on the hook and off the map", sling.Carried == load && !load.Spawned);
        var holders = new List<IThingHolder>();
        helicopter.GetChildHolders(holders);
        test.Expect("The game can find what the sling holds", holders.Contains(sling));

        sling.SetDown();
        test.Expect("The load is set down on the map", load.Spawned && sling.Carried == null);
        if (load.Spawned)
        {
            test.Expect("It is set down beside the helicopter, not on it", !load.OccupiedRect().Overlaps(helicopter.OccupiedRect())
                && load.Position.DistanceTo(helicopter.Position) <= rules.slingReachCells + 1f);
        }
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
