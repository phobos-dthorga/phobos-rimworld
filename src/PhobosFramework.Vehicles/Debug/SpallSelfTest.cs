using System.Collections.Generic;
using System.Linq;
using Phobos.Framework.Rules;
using Phobos.Framework.Vehicles.Rules;
using RimWorld;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// The spall liner's part of the vehicle self-test (Framework 0.209.0; the owner's chosen follow-up of 21 Nov 2026): on a design that
/// carries a liner, made for the test and thrown away unspawned, the liner starts not fitted and leaves the vehicle healthy, is fitted
/// whole through Vehicle Framework's own fitting, catches a beaten plate's spall and wears, asks for a re-line only once a quarter is gone,
/// is re-lined with panels through the mechanic's own call, gives back panels by what is left, and is taken out again. Combat Extended
/// only, as the liner is. A pass says these run and move the right figures; it is not a play test.
/// </summary>
public static class SpallSelfTest
{
    /// <summary>The key of the armoured cab node our trucks' liners hang from, which makes the cab thick enough to spall.</summary>
    const string CabNodeKey = "PhobosArmouredCab";

    public static void Run(SelfTest test)
    {
        VehicleDef? design = DefDatabase<VehicleDef>.AllDefsListForReading.FirstOrDefault(d =>
            d.components != null && d.components.Any(p => p.key == Spall.LinerKey)
            && d.GetCompProperties<CompProperties_UpgradeTree>()?.def?.nodes?.Any(n => n.key == CabNodeKey) == true);
        if (design == null)
        {
            test.Skip("The spall liner", "no design here carries one (they need Combat Extended)");
            return;
        }
        Pawn? mechanic = PawnsFinder.AllMaps_FreeColonistsSpawned.FirstOrDefault();
        if (mechanic == null)
        {
            test.Skip("The spall liner", "no colonist on a map to stand in as the mechanic");
            return;
        }
        VehicleNotices.Quiet = true;
        VehiclePawn? vehicle = null;
        try
        {
            if (!test.Step("Making a test " + design.defName, () => vehicle = VehicleSpawner.GenerateVehicle(design, Faction.OfPlayer)) || vehicle == null) return;
            CompPhobosVehicleSystems comp = vehicle.GetComp<CompPhobosVehicleSystems>();
            VehicleComponent liner = vehicle.statHandler.GetComponent(Spall.LinerKey);
            test.Step("A new vehicle's liner", () => NotFitted(test, vehicle, comp, liner));
            test.Step("Fitting the liner", () => Fit(test, vehicle, liner));
            test.Step("The liner catches spall", () => Catches(test, vehicle, liner));
            test.Step("Re-lining", () => Reline(test, vehicle, comp, liner, mechanic));
            test.Step("Taking the liner out", () => Remove(test, vehicle, liner));
        }
        finally
        {
            VehicleNotices.Quiet = false;
            if (vehicle != null)
            {
                // The upgrade prices recorded for this vehicle are saved with the game: taken back, so a thrown-away test leaves none.
                VehiclePawn made = vehicle;
                test.Step("Forgetting the test vehicle's upgrade prices", () =>
                {
                    foreach (UpgradeNode node in made.CompUpgradeTree?.Props?.def?.nodes ?? new List<UpgradeNode>()) GameComponent_PhobosUpgradePrices.Store?.Take(made, node);
                });
                test.Step("Throwing the liner's test vehicle away", () => made.Discard(silentlyRemoveReferences: true));
            }
        }
    }

    static UpgradeNode? Node(VehiclePawn vehicle, System.Func<UpgradeNode, bool> which) => vehicle.CompUpgradeTree?.Props?.def?.nodes?.FirstOrDefault(which);

    static void NotFitted(SelfTest test, VehiclePawn vehicle, CompPhobosVehicleSystems comp, VehicleComponent liner)
    {
        comp.UpdateNow();    // the first look, which sets a liner not fitted to nothing
        test.Expect("A new vehicle's liner is not fitted", liner.Health <= 0f && !Spall.LinerFitted(vehicle), liner.Health.ToString("0.#"));
        test.Expect("A liner not fitted reads as not fitted", Spall.StateOf(vehicle, liner) == LinerState.NotFitted);
        test.Expect("A liner not fitted is never mended for free", comp.Repairs.PartBlocked(liner));
        test.Expect("A liner not fitted asks for no work", !comp.Repairs.Pending.Any(w => w.Part == liner));
        test.Expect("A liner cannot be given a grade", !comp.Repairs.CanBeGraded(liner));
        vehicle.statHandler.MarkAllDirty();
        test.Expect("A liner not fitted leaves the vehicle healthy", vehicle.statHandler.HealthPercent >= 0.999f, vehicle.statHandler.HealthPercent.ToStringPercent());
    }

    static void Fit(SelfTest test, VehiclePawn vehicle, VehicleComponent liner)
    {
        UpgradeNode? node = Node(vehicle, Spall.IsLinerNode);
        test.Expect("The design has a liner upgrade", node != null);
        if (node == null) return;
        vehicle.CompUpgradeTree.FinishUnlock(node);
        test.Expect("Fitting puts a whole liner in", liner.Health >= liner.MaxHealth - 0.01f, liner.Health.ToString("0.#"));
        test.Expect("A fitted liner reads as sound", Spall.StateOf(vehicle, liner) == LinerState.Sound);
    }

    static void Catches(SelfTest test, VehiclePawn vehicle, VehicleComponent liner)
    {
        if (!VehicleSystemsMod.Settings.spallInsideVehicles || !Spall.Ready)
        {
            test.Skip("The liner catches spall", "spall is off (its setting, or Combat Extended's functions were not found)");
            return;
        }
        UpgradeNode? cab = Node(vehicle, n => n.key == CabNodeKey);
        if (cab == null) return;
        vehicle.CompUpgradeTree.FinishUnlock(cab);
        VehicleComponent? plate = vehicle.statHandler.GetComponent("Cab");
        test.Expect("The design has a cab plate", plate != null);
        if (plate == null) return;
        float armour = plate.ArmorRating(DamageArmorCategoryDefOf.Sharp, out _);
        float before = liner.Health;
        // A round just over the cab's armour, as Combat Extended reports one that beat the face.
        var hit = new DamageInfo(DamageDefOf.Bullet, 40f, armour + 1f, 0f);
        Spall.Hit(vehicle, plate, hit, VehicleComponent.Penetration.Penetrated);
        test.Expect("A beaten cab plate wears its liner", liner.Health < before, before.ToString("0.#") + " to " + liner.Health.ToString("0.#") + " (cab " + armour.ToString("0.#") + " mm)");
    }

    static void Reline(SelfTest test, VehiclePawn vehicle, CompPhobosVehicleSystems comp, VehicleComponent liner, Pawn mechanic)
    {
        liner.SetHealth(liner.MaxHealth * 0.9f);
        comp.Repairs.Invalidate();
        test.Expect("A liner a tenth worn asks for no re-line yet", !comp.Repairs.Pending.Any(w => w.Kind == RepairKind.Reline));
        liner.SetHealth(liner.MaxHealth * 0.4f);
        comp.Repairs.Invalidate();
        test.Expect("A liner under half reads as worn", Spall.StateOf(vehicle, liner) == LinerState.Worn);
        RepairWork? work = comp.Repairs.Pending.FirstOrDefault(w => w.Kind == RepairKind.Reline);
        test.Expect("A worn liner asks for a re-line", work != null);
        if (work == null) return;
        int panels = work.Options[0].Sum(line => line.count);
        test.Expect("A re-line takes a panel for each quarter missing", panels == SpallRule.PanelsToReline(0.4, SpallFigures.Design), panels.ToString());
        test.Expect("A re-line waits for its panels", comp.Repairs.Find(work.Key) == null);
        foreach ((ThingDef def, int count) in work.Options[0])
        {
            Thing thing = ThingMaker.MakeThing(def);
            thing.stackCount = count;
            vehicle.inventory.innerContainer.TryAdd(thing);
        }
        test.Expect("A re-line is ready once its panels are aboard", comp.Repairs.Find(work.Key) != null);
        test.Expect("The re-line finishes", comp.Repairs.Complete(work.Key, mechanic));
        test.Expect("A re-lined liner is whole", liner.Health >= liner.MaxHealth - 0.01f, liner.Health.ToString("0.#"));
    }

    static void Remove(SelfTest test, VehiclePawn vehicle, VehicleComponent liner)
    {
        if (Spall.Panel is { } panel)
        {
            liner.SetHealth(liner.MaxHealth * 0.5f);
            List<ThingDefCountClass> back = Spall.RefundFor(vehicle, new List<ThingDefCountClass> { new(panel, 4) });
            test.Expect("A liner half worn gives back half its panels", back.Sum(line => line.count) == 2, back.Sum(line => line.count).ToString());
        }
        UpgradeNode? node = Node(vehicle, Spall.IsLinerNode);
        if (node?.upgrades.FirstOrDefault(u => u is PhobosSpallLinerUpgrade) is not { } upgrade) return;
        upgrade.Refund(vehicle);
        test.Expect("Taking the liner out leaves nothing", liner.Health <= 0f, liner.Health.ToString("0.#"));
    }
}
