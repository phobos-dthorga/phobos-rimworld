using System.Collections.Generic;
using System.Linq;
using Phobos.Framework.Rules;
using RimWorld;
using RimWorld.Planet;
using Vehicles;
using Vehicles.World;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>A contract chosen for a quest: what is asked, how much, and its worth.</summary>
public sealed class ContractOffer
{
    public PhobosContractDef Def = null!;
    public ThingDef? Thing;
    public float Amount;
    public float Value;
}

/// <summary>
/// Settlements' contracts for the player's vehicles (Framework 0.308.0; docs/development/vehicle-contracts.md; EM45 with EM9; the owner's
/// cards of 26 Nov 2026: all seven kinds, the game's own rewards, a caravan that stays for the work, and a frequency the player sets).
/// Every rule is here: which vehicles fit, what is offered, handing over a delivery and the work on site. The settlement's comp and the
/// quest's nodes only ask.
/// </summary>
public static class Contracts
{
    /// <summary>Ticks between two steps of work on site (a tenth of an hour).</summary>
    public const int WorkInterval = 250;
    /// <summary>Skill experience a working hand gains for each tick of work (agent's figure, about half a mining job's).</summary>
    public const float LearnPerTick = 0.04f;
    /// <summary>The quest that offers contracts.</summary>
    public const string QuestDefName = "PhobosVehicleContract";

    // ---- Which vehicles fit ----

    /// <summary>Whether this vehicle can do this contract: it carries the comp named, and for a fluid, a tank that can hold it.</summary>
    public static bool Fits(PhobosContractDef def, VehiclePawn vehicle)
    {
        if (def.vehicleComp != null)
        {
            if (!vehicle.AllComps.Any(c => def.vehicleComp.IsInstanceOfType(c))) return false;
        }
        else if (vehicle.GetComp<CompPhobosVehicleSystems>() == null)
        {
            return false;
        }
        if (def.kind == ContractKind.DeliverFluid)
            return def.fluid != null && vehicle.GetComp<CompPhobosTank>() is { } tank && tank.ContentsChoices().Contains(def.fluid);
        return true;
    }

    /// <summary>The player's vehicles on their maps and in their caravans, towed trailers too: what the storyteller asks of before offering.</summary>
    public static IEnumerable<VehiclePawn> PlayerVehicles()
    {
        foreach (Map map in Find.Maps)
        {
            foreach (Pawn pawn in map.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer))
            {
                if (pawn is VehiclePawn vehicle) yield return vehicle;
            }
        }
        foreach (Caravan caravan in Find.WorldObjects.Caravans)
        {
            if (caravan.Faction != Faction.OfPlayer) continue;
            foreach (VehiclePawn vehicle in TankLarder.Vehicles(caravan)) yield return vehicle;
        }
    }

    /// <summary>The most this contract may ask: the largest fitting tank for a fluid, no cap for the rest.</summary>
    static float Most(PhobosContractDef def, List<VehiclePawn> fitting)
    {
        if (def.kind != ContractKind.DeliverFluid) return float.MaxValue;
        return fitting.Max(v => v.GetComp<CompPhobosTank>()?.Capacity ?? 0f);
    }

    // ---- What is offered ----

    /// <summary>
    /// A contract the player could do, chosen by its kind's weight, or null when none fits: no vehicle of the player's can do any kind, or a
    /// fluid's largest tank is too small for one step.
    /// </summary>
    public static ContractOffer? Pick(bool onSite, Faction? asker)
    {
        List<VehiclePawn> fleet = PlayerVehicles().ToList();
        var choices = new List<(ContractOffer offer, float weight)>();
        foreach (PhobosContractDef def in DefDatabase<PhobosContractDef>.AllDefsListForReading)
        {
            if (def.selectionWeight <= 0f || def.OnSite != onSite) continue;
            if (def.minTechLevel != TechLevel.Undefined && (asker == null || asker.def.techLevel < def.minTechLevel)) continue;
            if (def.kind == ContractKind.TowVehicle && (asker == null || StrandedDesignFor(asker) == null)) continue;
            List<VehiclePawn> fitting = def.needsVehicle ? fleet.Where(v => Fits(def, v)).ToList() : fleet;
            if (def.needsVehicle && fitting.Count == 0) continue;
            float amount = ContractRule.Amount(def.amountRange.RandomInRange, def.needsVehicle ? Most(def, fitting) : float.MaxValue, def.step);
            if (amount <= 0f) continue;
            ThingDef? thing = def.things.Count > 0 ? def.things.RandomElement() : null;
            choices.Add((new ContractOffer { Def = def, Thing = thing, Amount = amount, Value = ContractRule.Value(amount, PerUnit(def, thing)) }, def.selectionWeight));
        }
        return choices.Count == 0 ? null : choices.RandomElementByWeight(c => c.weight).offer;
    }

    // ---- At a place on the world map: the stranded vehicle and the casualty (Framework 0.311.0) ----

    /// <summary>
    /// A land vehicle of ours (with the systems) a settlement of this faction could own: no more advanced than the faction (a design with no
    /// tech level counts as industrial), never a trailer. Null when there is none.
    /// </summary>
    public static VehicleDef? StrandedDesignFor(Faction faction) => DefDatabase<VehicleDef>.AllDefsListForReading
        .Where(d => d.type == VehicleType.Land && !LoadoutSelector.IsTrailer(d) && d.GetCompProperties<CompProperties_PhobosVehicleSystems>() != null
            && (d.techLevel == TechLevel.Undefined ? TechLevel.Industrial : d.techLevel) <= faction.def.techLevel)
        .RandomElementWithFallback();

    /// <summary>
    /// What waits at the place: the faction's broken-down vehicle, a part that moves it wrecked and some fuel left, or one of its people,
    /// made new so it is never a leader or anyone the player knows. Returns it with the words for the quest (the vehicle and its wrecked part,
    /// or the person), or null when nothing could be made.
    /// </summary>
    public static Pawn? MakeTarget(PhobosContractDef def, Faction faction, out string words)
    {
        words = "";
        if (def.kind == ContractKind.Casualty)
        {
            PawnKindDef kind = faction.def.basicMemberKind ?? faction.RandomPawnKind();
            Pawn person = PawnGenerator.GeneratePawn(kind, faction);
            words = "PhobosContract_CasualtyWords".Translate(person.LabelShort.Named("PERSON")).Resolve();
            return person;
        }
        if (StrandedDesignFor(faction) is not { } design) return null;
        VehiclePawn vehicle = VehicleSpawner.GenerateVehicle(design, faction);
        if (vehicle == null) return null;
        if (vehicle.CompFueledTravel is { } fuel) fuel.Refuel(fuel.FuelCapacity * StrandedFuelShare);
        string part = Strand(vehicle);
        words = "PhobosContract_StrandedWords".Translate(vehicle.LabelShort.Named("VEHICLE"), part.Named("PART")).Resolve();
        return vehicle;
    }

    /// <summary>The fuel a stranded vehicle is left with, so once mended it can drive home (agent's figure).</summary>
    public const float StrandedFuelShare = 0.4f;

    /// <summary>
    /// Wrecks the parts that move it, most important first, until it cannot move: one is usually enough. Returns the first part's name, the
    /// named part from the fault table EM9 asks for.
    /// </summary>
    static string Strand(VehiclePawn vehicle)
    {
        string? first = null;
        foreach (VehicleComponent part in vehicle.statHandler.components
                     .Where(c => c.props.categories?.Contains(VehicleStatDefOf.MoveSpeed) == true)
                     .OrderByDescending(c => c.props.priorityStatEfficiency).ThenByDescending(c => c.MaxHealth).ToList())
        {
            part.SetHealth(0f);
            first ??= part.props.label ?? part.props.key;
            if (Mobility.Stopped(vehicle)) break;
        }
        return first ?? vehicle.LabelShort;
    }

    /// <summary>
    /// The player takes it into their care for the contract: the target joins the player's faction while the settlement's own is kept on the
    /// contract, so it can be rescued, carried, winched, mended and taken in a caravan as the player's own (the game lets only the player's
    /// people and prisoners into a caravan). It goes back at the hand-over, or when the quest ends any other way.
    /// </summary>
    public static void TakeIntoCare(Pawn target)
    {
        if (target.Faction != Faction.OfPlayer) target.SetFaction(Faction.OfPlayer);
    }

    /// <summary>
    /// Sends the target back to its own: out of any vehicle, caravan, tow or map, back to its faction, into the world. Nothing happens to one
    /// already gone back, dead or destroyed.
    /// </summary>
    public static void SendHome(Pawn? target, Faction? home)
    {
        if (target == null || target.Dead || target.Destroyed || target.Faction != Faction.OfPlayer) return;
        if (target is VehiclePawn vehicle)
        {
            foreach (Pawn rider in vehicle.AllPawnsAboard.ToList()) vehicle.DisembarkPawn(rider);
            foreach (Caravan caravan in Find.WorldObjects.Caravans)
            {
                if (caravan.PawnsListForReading.Contains(vehicle))
                {
                    caravan.RemovePawn(vehicle);
                    break;
                }
                if (caravan is VehicleCaravan convoy
                    && convoy.VehiclesListForReading.FirstOrDefault(v => CompPhobosHitch.TowedAlong(v) == vehicle)?.GetComp<CompPhobosHitch>() is { } hitch)
                {
                    hitch.LetGoHeld();
                    break;
                }
            }
        }
        else
        {
            if (target.GetVehicle() is { } carrier) carrier.DisembarkPawn(target);
            target.GetCaravan()?.RemovePawn(target);
        }
        if (target.Spawned) target.DeSpawn();
        if (home != null && !home.defeated) target.SetFaction(home);
        if (!target.IsWorldPawn()) Find.WorldPawns.PassToWorld(target, PawnDiscardDecideMode.Decide);
    }

    /// <summary>Whether the caravan has the target with it: among its people or vehicles, aboard one of its vehicles, or in its tow.</summary>
    static bool HasWithIt(Caravan caravan, Pawn target) =>
        caravan.PawnsListForReading.Contains(target) || Hitches.InTow(caravan).Contains(target)
        || (target.GetVehicle() is { } carrier && TankLarder.Vehicles(caravan).Contains(carrier));

    /// <summary>Why the caravan cannot hand the target over now, in-world, or null.</summary>
    public static string? HandOverRefusal(WorldObjectComp_PhobosContract contract, Caravan caravan)
    {
        Pawn? target = contract.target;
        if (target == null || target.Dead || target.Destroyed) return "PhobosContract_TargetLost".Translate().Resolve();
        if (!HasWithIt(caravan, target))
            return (target is VehiclePawn ? "PhobosContract_NoVehicleWithIt" : "PhobosContract_NoCasualtyWithIt").Translate(target.LabelShort.Named("TARGET")).Resolve();
        if (target is VehiclePawn vehicle && caravan is VehicleCaravan convoy && convoy.VehiclesListForReading.Count == 1 && convoy.VehiclesListForReading[0] == vehicle)
            return "PhobosContract_LastVehicle".Translate(vehicle.LabelShort.Named("TARGET")).Resolve();
        return null;
    }

    /// <summary>Hands the target over at the settlement and closes the contract.</summary>
    public static void HandOver(WorldObjectComp_PhobosContract contract, Caravan caravan)
    {
        if (contract.def == null || !contract.Active || HandOverRefusal(contract, caravan) != null) return;
        Pawn target = contract.target!;
        SendHome(target, contract.home);
        Messages.Message("PhobosContract_HandedOver".Translate(target.LabelShort.Named("TARGET"), contract.parent.LabelCap.Named("SETTLEMENT")),
            caravan, MessageTypeDefOf.PositiveEvent, historical: false);
        Done(contract, caravan, null);
    }

    /// <summary>
    /// Every game hour, for a stranded vehicle in the player's care: once it can move again, it is driven home by its own people and the
    /// contract is met where it stands (the owner's card of 26 Nov 2026: tow it home or mend it on the spot).
    /// </summary>
    public static void CheckMended(WorldObjectComp_PhobosContract contract)
    {
        if (contract.target is not VehiclePawn vehicle || !vehicle.Spawned || vehicle.Faction != Faction.OfPlayer || vehicle.Destroyed) return;
        if (Mobility.Stopped(vehicle)) return;
        Map map = vehicle.Map;
        IntVec3 at = vehicle.Position;
        string label = vehicle.LabelShort;
        SendHome(vehicle, contract.home);
        Messages.Message("PhobosContract_Mended".Translate(label.Named("TARGET"), contract.parent.LabelCap.Named("SETTLEMENT")), new TargetInfo(at, map),
            MessageTypeDefOf.PositiveEvent, historical: false);
        if (Seen.Wants("vehicles.contract-mended")) Seen.Note("vehicles.contract-mended", null, label + " for " + contract.parent.Label);
        Done(contract, null, null);
    }

    /// <summary>Silver a unit is worth: the Def's figure, else the thing's market value, else the fluid's item's market value a unit.</summary>
    static float PerUnit(PhobosContractDef def, ThingDef? thing)
    {
        if (def.valuePerUnit > 0f) return def.valuePerUnit;
        if (thing != null) return thing.BaseMarketValue;
        if (def.fluid?.ports.OfType<Port_Items>().FirstOrDefault(p => p.thing != null) is { } port && port.unitsPerItem > 0f)
            return port.thing!.BaseMarketValue / port.unitsPerItem;
        return 0f;
    }

    /// <summary>What is asked, in words: the Def's request with its amount and thing.</summary>
    public static string Request(PhobosContractDef def, ThingDef? thing, float amount)
    {
        string what = thing != null ? (amount == 1f ? thing.label : Find.ActiveLanguageWorker.Pluralize(thing.label, (int)amount))
            : def.fluid?.label ?? def.thingCategory?.label ?? "";
        return def.request.Formatted(amount.ToString("N0").Named("AMOUNT"), what.Named("THING")).Resolve();
    }

    // ---- Deliveries ----

    /// <summary>How much of the fluid the caravan's tanks hold, towed trailers too.</summary>
    static float FluidHeld(Caravan caravan, PhobosTankContentsDef fluid) =>
        TankLarder.Vehicles(caravan).Sum(v => v.GetComp<CompPhobosTank>()?.Compartments.Where(c => c.contents == fluid).Sum(c => c.amount) ?? 0f);

    /// <summary>Everything the caravan carries, its towed trailers' cargo too: a flatbed of stone counts.</summary>
    public static IEnumerable<Thing> Carried(Caravan caravan)
    {
        foreach (Thing thing in CaravanInventoryUtility.AllInventoryItems(caravan)) yield return thing;
        foreach (VehiclePawn towed in Hitches.InTow(caravan))
        {
            foreach (Thing thing in towed.inventory.innerContainer) yield return thing;
        }
    }

    static bool Counts(WorldObjectComp_PhobosContract contract, Thing thing)
    {
        if (thing.GetRotStage() != RotStage.Fresh) return false;
        if (contract.thing != null) return thing.def == contract.thing;
        return contract.def?.thingCategory is { } category && thing.def.IsWithinCategory(category);
    }

    /// <summary>Why this caravan cannot hand the contract over now, in-world, or null.</summary>
    public static string? FulfilRefusal(WorldObjectComp_PhobosContract contract, Caravan caravan)
    {
        PhobosContractDef? def = contract.def;
        if (def == null) return null;
        if (def.kind == ContractKind.DeliverFluid && def.fluid != null)
        {
            float held = FluidHeld(caravan, def.fluid);
            if (held < contract.amount)
                return "PhobosContract_ShortFluid".Translate(held.ToString("N0").Named("HAVE"), contract.amount.ToString("N0").Named("WANT"),
                    def.fluid.unitLabel.Named("UNIT"), def.fluid.label.Named("FLUID")).Resolve();
            return null;
        }
        int have = Carried(caravan).Where(t => Counts(contract, t)).Sum(t => t.stackCount);
        if (have < contract.amount)
            return "PhobosContract_ShortThings".Translate(have.Named("HAVE"), Request(def, contract.thing, contract.amount).Named("REQUEST")).Resolve();
        return null;
    }

    /// <summary>Asks the player to confirm the hand-over, as the game's trade request does.</summary>
    public static void AskToFulfil(WorldObjectComp_PhobosContract contract, Caravan caravan)
    {
        if (contract.def == null) return;
        if (FulfilRefusal(contract, caravan) is { } refused)
        {
            Messages.Message(refused, caravan, MessageTypeDefOf.RejectInput, historical: false);
            return;
        }
        Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("PhobosContract_FulfilConfirm".Translate(
            Request(contract.def, contract.thing, contract.amount).Named("REQUEST"), contract.parent.LabelCap.Named("SETTLEMENT")), () => Fulfil(contract, caravan)));
    }

    /// <summary>Takes what was asked from the caravan's tanks or cargo and closes the contract.</summary>
    public static void Fulfil(WorldObjectComp_PhobosContract contract, Caravan caravan)
    {
        PhobosContractDef? def = contract.def;
        if (def == null || !contract.Active || FulfilRefusal(contract, caravan) != null) return;
        VehiclePawn? doneBy = null;
        if (def.kind == ContractKind.DeliverFluid && def.fluid != null)
        {
            float left = contract.amount;
            foreach (VehiclePawn vehicle in TankLarder.Vehicles(caravan).ToList())
            {
                if (left <= 0f) break;
                if (vehicle.GetComp<CompPhobosTank>() is not { } tank) continue;
                float taken = tank.TakeOut(def.fluid, left, out _);
                if (taken > 0f) doneBy ??= vehicle;
                left -= taken;
            }
        }
        else
        {
            int left = (int)contract.amount;
            foreach (Thing thing in Carried(caravan).Where(t => Counts(contract, t)).ToList())
            {
                if (left <= 0) break;
                int take = System.Math.Min(left, thing.stackCount);
                doneBy ??= thing.ParentHolder is Pawn_InventoryTracker { pawn: VehiclePawn carrier } ? carrier : null;
                thing.SplitOff(take).Destroy();
                left -= take;
            }
        }
        Done(contract, caravan, doneBy ?? FittingIn(def, caravan));
    }

    // ---- Work on site ----

    /// <summary>
    /// Whether the caravan stands at the settlement: the game's own visit, and for a convoy not driving on, since the game reads only its own
    /// pather and Vehicle Framework drives a convoy with another (the depots read it the same way).
    /// </summary>
    public static bool AtSettlement(Caravan caravan, WorldObject settlement) =>
        CaravanVisitUtility.SettlementVisitedNow(caravan) == settlement && caravan is not VehicleCaravan { vehiclePather.MovingNow: true };

    /// <summary>The caravan's vehicle that can do the work, the one with the most fuel first.</summary>
    static VehiclePawn? FittingIn(PhobosContractDef def, Caravan caravan) =>
        TankLarder.Vehicles(caravan).Where(v => Fits(def, v)).OrderByDescending(v => v.CompFueledTravel?.Fuel ?? 0f).FirstOrDefault();

    /// <summary>The caravan's best hand for the work: a person fit to work, with the most of the contract's skill.</summary>
    static Pawn? BestHand(PhobosContractDef def, Caravan caravan) => caravan.PawnsListForReading
        .Where(p => p.RaceProps.Humanlike && p.Faction == Faction.OfPlayer && !p.Downed && !p.Dead && p.skills != null
            && (def.skill == null || !p.skills.GetSkill(def.skill).TotallyDisabled))
        .OrderByDescending(p => def.skill == null ? 0 : p.skills.GetSkill(def.skill).Level).FirstOrDefault();

    /// <summary>Why this caravan cannot do the work now, in-world, or null; with the vehicle and the hand that would.</summary>
    public static string? WorkRefusal(WorldObjectComp_PhobosContract contract, Caravan caravan, out VehiclePawn? vehicle, out Pawn? hand)
    {
        hand = null;
        vehicle = null;
        PhobosContractDef? def = contract.def;
        if (def == null) return null;
        vehicle = FittingIn(def, caravan);
        if (vehicle == null) return "PhobosContract_NoVehicle".Translate().Resolve();
        hand = BestHand(def, caravan);
        if (hand == null)
            return def.skill != null ? "PhobosContract_NoHand".Translate(def.skill.label.Named("SKILL")).Resolve() : "PhobosContract_NoHandAny".Translate().Resolve();
        if (def.fuelPerHour > 0f && vehicle.CompFueledTravel is { EmptyTank: true })
            return "PhobosContract_NoFuel".Translate(vehicle.LabelShort.Named("VEHICLE")).Resolve();
        return null;
    }

    /// <summary>Sets this caravan to the work; it goes on at each step while the caravan stays.</summary>
    public static void StartWork(WorldObjectComp_PhobosContract contract, Caravan caravan)
    {
        if (contract.def == null) return;
        if (WorkRefusal(contract, caravan, out _, out _) is { } refused)
        {
            Messages.Message(refused, caravan, MessageTypeDefOf.RejectInput, historical: false);
            return;
        }
        contract.workCaravan = caravan;
        contract.heldUp = null;
        Messages.Message("PhobosContract_Started".Translate(caravan.LabelCap.Named("CARAVAN"), contract.def.working.Named("WORKING"),
            contract.parent.LabelCap.Named("SETTLEMENT")), caravan, MessageTypeDefOf.TaskCompletion, historical: false);
        if (Seen.Wants("vehicles.contract-work", null, contract.def.defName)) Seen.Note("vehicles.contract-work", null, contract.def.defName + " at " + contract.parent.Label, contract.def.defName);
    }

    /// <summary>The player's own stop: the work done so far is kept, for this caravan or another to finish.</summary>
    public static void StopWork(WorldObjectComp_PhobosContract contract)
    {
        Caravan? caravan = contract.workCaravan;
        contract.workCaravan = null;
        contract.heldUp = null;
        Messages.Message("PhobosContract_StoppedByOrder".Translate(contract.parent.LabelCap.Named("SETTLEMENT")), caravan, MessageTypeDefOf.TaskCompletion, historical: false);
    }

    /// <summary>
    /// One step of work on site: the caravan must still be at the settlement; it rests at night unless the work goes on through it; the
    /// vehicle burns its own fuel and the best hand works at the pace of their skill, learning as they go. What holds the work up is said
    /// once, when it changes; the work picks up again by itself when it clears.
    /// </summary>
    public static void WorkStep(WorldObjectComp_PhobosContract contract)
    {
        PhobosContractDef? def = contract.def;
        Caravan? caravan = contract.workCaravan;
        if (def == null || caravan == null) return;
        if (caravan.Destroyed || !AtSettlement(caravan, contract.parent))
        {
            contract.workCaravan = null;
            contract.heldUp = null;
            if (!caravan.Destroyed)
                Messages.Message("PhobosContract_Left".Translate(caravan.LabelCap.Named("CARAVAN"), contract.parent.LabelCap.Named("SETTLEMENT")),
                    caravan, MessageTypeDefOf.CautionInput, historical: false);
            return;
        }
        if (!def.worksAtNight && caravan.NightResting) return;
        string? refused = WorkRefusal(contract, caravan, out VehiclePawn? vehicle, out Pawn? hand);
        if (refused != null || vehicle == null || hand == null)
        {
            if (refused != null && refused != contract.heldUp)
                Messages.Message("PhobosContract_HeldUp".Translate(contract.parent.LabelCap.Named("SETTLEMENT"), refused.Named("REASON")),
                    caravan, MessageTypeDefOf.CautionInput, historical: false);
            contract.heldUp = refused;
            return;
        }
        contract.heldUp = null;
        float hours = WorkInterval / (float)GenDate.TicksPerHour;
        float pace = def.skill != null ? ContractRule.Pace(hand.skills.GetSkill(def.skill).Level) : 1f;
        contract.done += hours * pace;
        if (def.fuelPerHour > 0f) vehicle.CompFueledTravel?.ConsumeFuel(def.fuelPerHour * hours);
        if (def.skill != null) hand.skills.Learn(def.skill, LearnPerTick * WorkInterval);
        if (contract.done >= contract.amount) Done(contract, caravan, vehicle);
    }

    // ---- Done ----

    /// <summary>
    /// The contract is met: the settlement's faction is pleased, as for the game's trade request, the quest hears it and pays its reward to
    /// the caravan, and the vehicle's logbook keeps it.
    /// </summary>
    static void Done(WorldObjectComp_PhobosContract contract, Caravan? caravan, VehiclePawn? vehicle)
    {
        PhobosContractDef def = contract.def!;
        string request = Request(def, contract.thing, contract.amount);
        if (contract.parent.Faction != null)
            Faction.OfPlayer.TryAffectGoodwillWith(contract.parent.Faction, DiplomacyTuning.Goodwill_QuestTradeRequestCompleted, canSendMessage: true,
                canSendHostilityLetter: true, HistoryEventDefOf.QuestGoodwillReward);
        if (vehicle != null) VehicleStory.Of(vehicle, ContractDefOf.PhobosLog_Contract, subject: contract.parent.Label, detail: request);
        else VehicleStory.Convoy(caravan, ContractDefOf.PhobosLog_Contract, subject: contract.parent.Label, detail: request);
        if (Seen.Wants("vehicles.contract-done", vehicle, def.kind.ToString()))
            Seen.Note("vehicles.contract-done", vehicle, def.defName + " for " + contract.parent.Label, def.kind.ToString());
        // A caravan named in the signal is where a quest that pays into the caravan puts its rewards; the site quest pays by drop pod.
        if (caravan != null)
            QuestUtility.SendQuestTargetSignals(contract.parent.questTags, SignalDone, contract.parent.Named("SUBJECT"), caravan.Named("CARAVAN"));
        else QuestUtility.SendQuestTargetSignals(contract.parent.questTags, SignalDone, contract.parent.Named("SUBJECT"));
        contract.Disable();
    }

    /// <summary>The signal a met contract sends its quest, as the game's trade request sends "TradeRequestFulfilled".</summary>
    public const string SignalDone = "PhobosContractDone";

    // ---- How often ----

    /// <summary>The quest that offers the contracts waiting at a place on the world map (Framework 0.311.0).</summary>
    public const string SiteQuestDefName = "PhobosVehicleSiteContract";

    /// <summary>Each contract quest's own weight, read once from its Def, which the setting scales.</summary>
    static readonly Dictionary<string, float> baseWeights = new();

    /// <summary>Sets the contract quests' weights from the setting: zero takes them out of the storyteller's choices and traders' alike.</summary>
    public static void ApplyFrequency()
    {
        foreach (string name in new[] { QuestDefName, SiteQuestDefName })
        {
            if (DefDatabase<QuestScriptDef>.GetNamedSilentFail(name) is not { } quest) continue;
            if (!baseWeights.TryGetValue(name, out float weight)) baseWeights[name] = weight = quest.rootSelectionWeight;
            quest.rootSelectionWeight = weight * VehicleSystemsMod.Settings.contractFrequency;
        }
    }
}

[DefOf]
public static class ContractDefOf
{
    public static Phobos.Framework.Persistence.PhobosLogEntryDef PhobosLog_Contract = null!;

    static ContractDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(ContractDefOf));
}
