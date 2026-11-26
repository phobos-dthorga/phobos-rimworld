using System.Collections.Generic;
using Phobos.Framework.Rules;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// Chooses the contract for the quest <c>PhobosVehicleContract</c> (Framework 0.308.0), as the game's
/// <c>QuestNode_TradeRequest_GetRequestedThing</c> chooses a trade request's thing: one the player's vehicles could do, for a settlement
/// with no contract open. It stores the offer, its worth and its words, and adds the work's own days to the quest's time limit.
/// </summary>
public class QuestNode_PhobosContract_Pick : QuestNode
{
    public SlateRef<Settlement> settlement;
    [NoTranslate] public SlateRef<string> timeoutVar;
    [NoTranslate] public SlateRef<string> storeOfferAs;
    [NoTranslate] public SlateRef<string> storeValueAs;
    [NoTranslate] public SlateRef<string> storeRequestAs;
    [NoTranslate] public SlateRef<string> storeHowToAs;

    protected override bool TestRunInt(Slate slate) => Choose(slate);

    protected override void RunInt() => Choose(QuestGen.slate);

    bool Choose(Slate slate)
    {
        if (VehicleSystemsMod.Settings.contractFrequency <= 0f) return false;
        Settlement? place = settlement.GetValue(slate);
        if (place?.GetComponent<WorldObjectComp_PhobosContract>() is not { } comp || comp.Active) return false;
        if (Contracts.Pick() is not { } offer) return false;
        slate.Set(storeOfferAs.GetValue(slate), offer);
        slate.Set(storeValueAs.GetValue(slate), offer.Value);
        slate.Set(storeRequestAs.GetValue(slate), Contracts.Request(offer.Def, offer.Thing, offer.Amount));
        slate.Set(storeHowToAs.GetValue(slate), offer.Def.howTo);
        string timeout = timeoutVar.GetValue(slate);
        if (offer.Def.kind == ContractKind.WorkOnSite && slate.TryGet(timeout, out int ticks) && ticks > 0)
            slate.Set(timeout, ticks + ContractRule.WorkDays(offer.Amount, offer.Def.worksAtNight) * GenDate.TicksPerDay);
        return true;
    }
}

/// <summary>Adds the part that opens the contract on the settlement when the quest is accepted, as <c>QuestNode_TradeRequest_Initiate</c> does.</summary>
public class QuestNode_PhobosContract_Initiate : QuestNode
{
    public SlateRef<Settlement> settlement;
    public SlateRef<ContractOffer> offer;
    public SlateRef<int> duration;

    protected override bool TestRunInt(Slate slate) =>
        settlement.GetValue(slate) != null && offer.GetValue(slate) != null && duration.GetValue(slate) > 0;

    protected override void RunInt()
    {
        Slate slate = QuestGen.slate;
        ContractOffer chosen = offer.GetValue(slate);
        QuestGen.quest.AddPart(new QuestPart_PhobosContract
        {
            settlement = settlement.GetValue(slate),
            def = chosen.Def,
            thing = chosen.Thing,
            amount = chosen.Amount,
            duration = duration.GetValue(slate),
            inSignal = slate.Get<string>("inSignal"),
        });
        if (Seen.Wants("vehicles.contract-offered", null, chosen.Def.defName))
            Seen.Note("vehicles.contract-offered", null, chosen.Def.defName + ", " + chosen.Amount + ", worth " + chosen.Value.ToString("0"), chosen.Def.defName);
    }
}

/// <summary>Opens the contract on the settlement when its signal comes, and closes it when the quest ends, as the trade request's part does.</summary>
public class QuestPart_PhobosContract : QuestPart
{
    public string? inSignal;
    public Settlement? settlement;
    public PhobosContractDef? def;
    public ThingDef? thing;
    public float amount;
    public int duration;

    public override IEnumerable<GlobalTargetInfo> QuestLookTargets
    {
        get
        {
            foreach (GlobalTargetInfo target in base.QuestLookTargets) yield return target;
            if (settlement != null) yield return settlement;
        }
    }

    public override IEnumerable<Faction> InvolvedFactions
    {
        get
        {
            foreach (Faction faction in base.InvolvedFactions) yield return faction;
            if (settlement?.Faction != null) yield return settlement.Faction;
        }
    }

    public override IEnumerable<Dialog_InfoCard.Hyperlink> Hyperlinks
    {
        get
        {
            foreach (Dialog_InfoCard.Hyperlink link in base.Hyperlinks) yield return link;
            if (thing != null) yield return new Dialog_InfoCard.Hyperlink(thing);
        }
    }

    public override void Notify_QuestSignalReceived(Signal signal)
    {
        base.Notify_QuestSignalReceived(signal);
        if (signal.tag != inSignal || def == null) return;
        if (settlement?.GetComponent<WorldObjectComp_PhobosContract>() is not { } comp) return;
        if (comp.Active)
        {
            VehicleSystemsMod.Logger.Warn(settlement.Label + " already has a contract open; this quest's is not added.");
            return;
        }
        comp.Begin(def, thing, amount, duration);
    }

    public override void Cleanup()
    {
        base.Cleanup();
        if (settlement?.GetComponent<WorldObjectComp_PhobosContract>() is { Active: true } comp && comp.def == def) comp.Disable();
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref inSignal, "inSignal");
        Scribe_References.Look(ref settlement, "settlement");
        Scribe_Defs.Look(ref def, "PhobosContractDef");
        Scribe_Defs.Look(ref thing, "PhobosContractThing");
        Scribe_Values.Look(ref amount, "PhobosContractAmount", 0f);
        Scribe_Values.Look(ref duration, "PhobosContractDuration", 0);
    }
}
