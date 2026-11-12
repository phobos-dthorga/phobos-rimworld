using System.Collections.Generic;
using System.Linq;
using Phobos.Framework.Interface;
using RimWorld;
using UnityEngine;
using Vehicles;
using Verse;
using Verse.AI;

namespace Phobos.VehicleSystems;

/// <summary>The loadout role the litter kit is fitted to: the cabin (the Cherokee's upholstery).</summary>
public static class LitterRoles
{
    public const string Cabin = "Cabin";
}

public sealed class CompProperties_PhobosLitter : CompProperties
{
    public CompProperties_PhobosLitter() => compClass = typeof(CompPhobosLitter);
}

/// <summary>
/// A vehicle's litters (Framework 0.101.0, batch 3's signatures; docs/development/vehicle-upgrades-round-two.md). With the litter kit fitted
/// to its cabin, the vehicle carries downed people lying on litters, held here, not in a seat: the Cherokee has no passenger seats, and a
/// seat of Vehicle Framework's would take anyone who boards. While anyone lies here the vehicle ticks them, and a crew member aboard who can
/// do doctoring tends them as a caravan's doctor would (the game's own tend, every 1,250 ticks a doctor). In flight nothing ages, as for
/// Vehicle Framework's passengers. Anything else that takes the vehicle off the map sets them down where it stood first.
/// </summary>
public sealed class CompPhobosLitter : VehicleComp, IThingHolder
{
    /// <summary>The ability the litter kit grants.</summary>
    public const string LitterAbility = "Litter";
    /// <summary>The game's own interval between one caravan doctor's tends (CaravanTendUtility.TendIntervalTicks).</summary>
    public const int TendIntervalTicks = 1250;
    const int TendCheckTicks = GenTicks.TickRareInterval;

    ThingOwner<Pawn> held;
    bool holdThroughDespawn;
    /// <summary>When each doctor aboard last tended, by load ID. Not saved: at worst a doctor tends a little sooner after a load.</summary>
    readonly Dictionary<int, int> lastTend = new();

    public CompPhobosLitter() => held = new ThingOwner<Pawn>(this);

    static PhobosVehicleRulesDef Rules => PhobosVehicleRulesDef.Current<PhobosVehicleRulesDef>();

    public override bool TickByRequest => true;

    CompPhobosVehicleSystems? Systems => Vehicle.GetComp<CompPhobosVehicleSystems>();

    public bool KitFitted => Systems?.Fittings.Grants(LitterAbility) == true;
    public int Slots => KitFitted ? Rules.litterSlots : 0;
    public IReadOnlyList<Pawn> Patients => held.InnerListForReading;
    public int Free => Mathf.Max(0, Slots - held.Count);

    // ---- Who may be carried aboard ----

    /// <summary>Why this person cannot be put on one of this vehicle's litters now, or null.</summary>
    public string? RefusalFor(Pawn patient)
    {
        NamedArgument vehicle = Vehicle.LabelShort.Named("VEHICLE");
        if (!KitFitted) return "PhobosLitter_NoKit".Translate(vehicle).Resolve();
        if (Free <= 0) return "PhobosLitter_Full".Translate(vehicle).Resolve();
        if (!patient.Downed) return "PhobosLitter_NotDowned".Translate(patient.LabelShort.Named("PATIENT")).Resolve();
        if (patient.HostileTo(Faction.OfPlayer)) return "PhobosLitter_Hostile".Translate(patient.LabelShort.Named("PATIENT")).Resolve();
        if (!Vehicle.Spawned || patient.MapHeld != Vehicle.Map) return "PhobosLitter_NotHere".Translate(vehicle).Resolve();
        if (Vehicle.vehiclePather is { Moving: true }) return "PhobosLitter_Moving".Translate(vehicle).Resolve();
        // A helicopter that flies on the map by itself (the Hind) is spawned while in the air: nobody is carried up to it.
        if (OnMapFlight.Airborne(Vehicle)) return "PhobosLitter_Airborne".Translate(vehicle).Resolve();
        return null;
    }

    /// <summary>Takes a patient from a carrier's hands onto a litter. Says what it did, or why not.</summary>
    public bool Load(Pawn carrier, Pawn patient)
    {
        if (RefusalFor(patient) is { } refusal)
        {
            Messages.Message(refusal, Vehicle, MessageTypeDefOf.RejectInput, historical: false);
            return false;
        }
        if (carrier.carryTracker.CarriedThing != patient || !carrier.carryTracker.innerContainer.TryTransferToContainer(patient, held, canMergeWithExistingStacks: false))
        {
            return false;
        }
        Vehicle.RequestTickStart(this);
        Messages.Message("PhobosLitter_Loaded".Translate(carrier.LabelShort.Named("CARRIER"), patient.LabelShort.Named("PATIENT"), Vehicle.LabelShort.Named("VEHICLE")).Resolve(),
            Vehicle, MessageTypeDefOf.TaskCompletion, historical: false);
        return true;
    }

    /// <summary>Sets everyone on the litters down beside the vehicle, on its map. Says so unless <paramref name="quiet"/>.</summary>
    public void UnloadAll(bool quiet = false) => SetDown(Vehicle.Map, Vehicle.Position, quiet ? null : "PhobosLitter_Unloaded");

    void SetDown(Map? map, IntVec3 near, string? messageKey)
    {
        if (held.Count == 0 || map == null) return;
        int count = 0;
        foreach (Pawn patient in held.InnerListForReading.ToList())
        {
            held.Remove(patient);
            if (!GenPlace.TryPlaceThing(patient, near, map, ThingPlaceMode.Near)) GenSpawn.Spawn(patient, near, map);
            count++;
        }
        lastTend.Clear();
        Vehicle.RequestTickStop(this);
        if (messageKey != null)
        {
            Messages.Message(messageKey.Translate(Vehicle.LabelShort.Named("VEHICLE"), count.Named("COUNT")).Resolve(), new TargetInfo(near, map),
                MessageTypeDefOf.NeutralEvent, historical: false);
        }
    }

    // ---- Care aboard ----

    public override void CompTick()
    {
        base.CompTick();
        if (held.Count == 0)
        {
            Vehicle.RequestTickStop(this);
            return;
        }
        held.DoTick();
        if (Vehicle.IsHashIntervalTick(TendCheckTicks)) TendAboard();
    }

    /// <summary>Each doctor aboard tends one patient a tend interval, the one in most need first, with the best medicine the cargo holds that
    /// the patient's medical care allows (TendUtility.DoTend, as the game tends caravan patients). Not while the vehicle is on the move.</summary>
    void TendAboard()
    {
        if (Vehicle.vehiclePather is { Moving: true }) return;
        int now = Find.TickManager.TicksGame;
        foreach (Pawn doctor in Vehicle.AllPawnsAboard)
        {
            if (!CanTend(doctor)) continue;
            if (lastTend.TryGetValue(doctor.thingIDNumber, out int last) && now - last < TendIntervalTicks) continue;
            Pawn? patient = held.InnerListForReading.Where(p => !p.Dead && p.health.HasHediffsNeedingTend())
                .OrderByDescending(p => p.health.hediffSet.BleedRateTotal).FirstOrDefault();
            if (patient == null) return;
            Medicine? medicine = BestMedicine(patient);
            TendUtility.DoTend(doctor, patient, medicine);
            if (medicine != null && medicine.Destroyed) Vehicle.inventory?.innerContainer.Remove(medicine);
            lastTend[doctor.thingIDNumber] = now;
        }
    }

    static bool CanTend(Pawn doctor) =>
        doctor.RaceProps.Humanlike && doctor.Faction == Faction.OfPlayer && !doctor.Downed && !doctor.Dead && !doctor.InMentalState
        && (doctor.story == null || !doctor.WorkTypeIsDisabled(WorkTypeDefOf.Doctor))
        && doctor.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation);

    Medicine? BestMedicine(Pawn patient)
    {
        Medicine? best = null;
        float potency = -1f;
        if (Vehicle.inventory?.innerContainer is not { } cargo) return null;
        foreach (Thing thing in cargo)
        {
            if (thing is not Medicine medicine) continue;
            if (patient.playerSettings != null && !patient.playerSettings.medCare.AllowsMedicine(medicine.def)) continue;
            float found = medicine.def.GetStatValueAbstract(StatDefOf.MedicalPotency);
            if (found <= potency) continue;
            potency = found;
            best = medicine;
        }
        return best;
    }

    // ---- Leaving and coming back ----

    /// <summary>The vehicle is about to leave the map on a water drop run: the patients ride along. A launch needs no call: Vehicle Framework
    /// marks the aircraft in flight before it leaves the map (CompVehicleLauncher.Launch), and its launch event fires only after.</summary>
    public void HoldThroughDespawn() => holdThroughDespawn = true;

    bool Flying => holdThroughDespawn || Vehicle.CompVehicleLauncher is { inFlight: true };

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        holdThroughDespawn = false;
        if (held.Count > 0) Vehicle.RequestTickStart(this);
    }

    public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
    {
        // A caravan, or anything but a flight, takes the vehicle off the map: the patients are set down where it stood first.
        if (!Flying) SetDown(map, Vehicle.Position, held.Count > 0 ? "PhobosLitter_SetDownLeaving" : null);
        base.PostDeSpawn(map, mode);
    }

    public override void PostDestroy(DestroyMode mode, Map previousMap)
    {
        base.PostDestroy(mode, previousMap);
        // Destroyed off the map, in flight: nobody comes back from that, and the game is told of each death.
        foreach (Pawn patient in held.InnerListForReading.ToList())
        {
            held.Remove(patient);
            if (!patient.Dead) patient.Kill(null);
        }
    }

    // ---- Holding ----

    public ThingOwner GetDirectlyHeldThings() => held;

    public void GetChildHolders(List<IThingHolder> outChildren) => ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, held);

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Deep.Look(ref held, "PhobosLitter_Held", this);
        if (Scribe.mode == LoadSaveMode.PostLoadInit) held ??= new ThingOwner<Pawn>(this);
    }

    // ---- What the player sees ----

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        foreach (Gizmo gizmo in base.CompGetGizmosExtra()) yield return gizmo;
        if (Vehicle.Faction != Faction.OfPlayer || !Vehicle.Spawned || held.Count == 0) yield break;
        var unload = new Command_Action
        {
            defaultLabel = "PhobosLitter_UnloadGizmo".Translate(),
            defaultDesc = "PhobosLitter_UnloadGizmoDesc".Translate(held.Count.Named("COUNT")),
            icon = SystemIcons.For("Phobos/VehicleSystems/Litter/Unload"),
            action = () => UnloadAll(),
        };
        if (Vehicle.vehiclePather is { Moving: true }) unload.Disable("PhobosLitter_Moving".Translate(Vehicle.LabelShort.Named("VEHICLE")).Resolve());
        else if (OnMapFlight.Airborne(Vehicle)) unload.Disable("PhobosLitter_Airborne".Translate(Vehicle.LabelShort.Named("VEHICLE")).Resolve());
        yield return unload;
    }

    public override string? CompInspectStringExtra()
    {
        // Guarded: the game asks for this every frame while the thing is selected (UiGuard).
        if (UiGuard.Resting(GuardKey)) return UiGuard.FallbackText;
        try
        {
            if (Slots <= 0 && held.Count == 0) return null;
            string line = "PhobosLitter_Inspect".Translate(held.Count.Named("COUNT"), Slots.Named("SLOTS")).Resolve();
            if (held.Count > 0) line += "\n" + "PhobosLitter_InspectPatients".Translate(string.Join(", ", held.InnerListForReading.Select(p => p.LabelShort)).Named("NAMES")).Resolve();
            return line;
        }
        catch (System.Exception e) { UiGuard.Report(GuardKey, e); return null; }
    }

    const string GuardKey = "A vehicle's litter line";
}

/// <summary>Carrying a downed person to a litter aboard a vehicle (Framework 0.101.0): pick them up, take them to the vehicle, strap them in.
/// Ordered from the right-click menu only.</summary>
public class JobDriver_PhobosCarryToLitter : JobDriver
{
    const int StrapInTicks = 250;

    Pawn Patient => (Pawn)job.GetTarget(TargetIndex.A).Thing;
    VehiclePawn Vehicle => (VehiclePawn)job.GetTarget(TargetIndex.B).Thing;

    public override bool TryMakePreToilReservations(bool errorOnFailed) => pawn.Reserve(Patient, job, 1, -1, null, errorOnFailed);

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDestroyedOrNull(TargetIndex.A);
        this.FailOnDestroyedOrNull(TargetIndex.B);
        this.FailOn(() => Vehicle.GetComp<CompPhobosLitter>() is not { } litter || litter.RefusalFor(Patient) != null);
        yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.OnCell).FailOnDespawnedNullOrForbidden(TargetIndex.A).FailOnSomeonePhysicallyInteracting(TargetIndex.A);
        yield return Toils_Haul.StartCarryThing(TargetIndex.A);
        yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.Touch);
        yield return Toils_General.Wait(StrapInTicks).FailOnCannotTouch(TargetIndex.B, PathEndMode.Touch).WithProgressBarToilDelay(TargetIndex.B);
        yield return new Toil
        {
            initAction = () => Vehicle.GetComp<CompPhobosLitter>()?.Load(pawn, Patient),
            defaultCompleteMode = ToilCompleteMode.Instant,
        };
    }
}

/// <summary>
/// Right-click a downed person with a colonist selected (Framework 0.101.0): "Carry to a litter aboard..." for each of the player's vehicles
/// on the map with a litter kit, nearest first; one that cannot take them says why. Presentation only: the job loads through the litter.
/// </summary>
public class FloatMenuOptionProvider_PhobosLitter : FloatMenuOptionProvider
{
    const int MostVehicles = 4;

    protected override bool Drafted => true;
    protected override bool Undrafted => true;
    protected override bool Multiselect => false;
    protected override bool RequiresManipulation => true;

    public override bool SelectedPawnValid(Pawn pawn, FloatMenuContext context) =>
        pawn is not VehiclePawn && pawn.RaceProps.Humanlike && pawn.IsColonistPlayerControlled && base.SelectedPawnValid(pawn, context);

    public override IEnumerable<FloatMenuOption> GetOptionsFor(Pawn clickedPawn, FloatMenuContext context)
    {
        if (clickedPawn is VehiclePawn || !clickedPawn.Downed || !clickedPawn.Spawned || clickedPawn.HostileTo(Faction.OfPlayer)) yield break;
        Pawn carrier = context.FirstSelectedPawn;
        if (carrier == clickedPawn) yield break;
        Map map = clickedPawn.Map;
        var litters = map.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer).OfType<VehiclePawn>()
            .Select(v => v.GetComp<CompPhobosLitter>()).Where(c => c is { KitFitted: true })
            .OrderBy(c => c!.Vehicle.Position.DistanceToSquared(clickedPawn.Position)).Take(MostVehicles).ToList();
        foreach (CompPhobosLitter? litter in litters)
        {
            VehiclePawn vehicle = litter!.Vehicle;
            string label = "PhobosLitter_CarryTo".Translate(clickedPawn.LabelShort.Named("PATIENT"), vehicle.LabelShort.Named("VEHICLE")).Resolve();
            string? refusal = litter.RefusalFor(clickedPawn);
            if (refusal == null && !carrier.CanReserveAndReach(clickedPawn, PathEndMode.OnCell, Danger.Deadly)) refusal = "PhobosLitter_NoPath".Translate(clickedPawn.LabelShort.Named("PATIENT")).Resolve();
            if (refusal == null && !carrier.CanReach(vehicle, PathEndMode.Touch, Danger.Deadly)) refusal = "PhobosLitter_NoPath".Translate(vehicle.LabelShort.Named("PATIENT")).Resolve();
            if (refusal != null)
            {
                yield return new FloatMenuOption("PhobosLitter_CarryToCannot".Translate(label.Named("ORDER"), refusal.Named("REASON")), null);
                continue;
            }
            yield return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(label, () =>
            {
                Job job = JobMaker.MakeJob(LitterJobDefOf.PhobosVehicleSystems_CarryToLitter, clickedPawn, vehicle);
                job.count = 1;
                carrier.jobs.TryTakeOrderedJob(job, JobTag.Misc);
            }), carrier, clickedPawn);
        }
    }
}

[DefOf]
public static class LitterJobDefOf
{
    public static JobDef PhobosVehicleSystems_CarryToLitter = null!;

    static LitterJobDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(LitterJobDefOf));
}
