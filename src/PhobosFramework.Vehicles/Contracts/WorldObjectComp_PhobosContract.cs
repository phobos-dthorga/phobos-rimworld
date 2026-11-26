using System.Collections.Generic;
using System.Text;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace Phobos.VehicleSystems;

public class WorldObjectCompProperties_PhobosContract : WorldObjectCompProperties
{
    public WorldObjectCompProperties_PhobosContract() => compClass = typeof(WorldObjectComp_PhobosContract);
}

/// <summary>
/// A settlement's contract for the player's vehicles (Framework 0.308.0; docs/development/vehicle-contracts.md), shaped as the game's own
/// trade request is: the quest sets it, a visiting caravan fulfils it, the quest pays. It holds what was asked and, for work on site, how
/// much is done and which caravan is doing it. Presentation only: every change goes through <see cref="Contracts"/>.
/// </summary>
[StaticConstructorOnStartup]
public class WorldObjectComp_PhobosContract : WorldObjectComp
{
    /// <summary>The game's own trade request button, which exists in every install.</summary>
    static readonly Texture2D FulfilIcon = ContentFinder<Texture2D>.Get("UI/Commands/FulfillTradeRequest");

    public PhobosContractDef? def;
    /// <summary>For a delivery from a list of things: the one asked for.</summary>
    public ThingDef? thing;
    /// <summary>Litres, things or hours of work.</summary>
    public float amount;
    /// <summary>Hours of work done so far.</summary>
    public float done;
    public int expiration = -1;
    /// <summary>The caravan doing the work now, or null.</summary>
    public Caravan? workCaravan;
    /// <summary>Why the work is held up, said once when it changes; not saved, the next step finds it again.</summary>
    public string? heldUp;

    public bool Active => def != null && expiration > Find.TickManager.TicksGame;

    public Settlement? Settlement => parent as Settlement;

    public void Begin(PhobosContractDef contract, ThingDef? asked, float units, int ticks)
    {
        def = contract;
        thing = asked;
        amount = units;
        done = 0f;
        workCaravan = null;
        heldUp = null;
        expiration = Find.TickManager.TicksGame + ticks;
    }

    public void Disable()
    {
        def = null;
        thing = null;
        expiration = -1;
        workCaravan = null;
        heldUp = null;
    }

    public override void CompTick()
    {
        // Every 250 ticks, staggered by settlement, and only while a caravan is at work: nothing runs for a settlement with no contract.
        if (workCaravan == null || (Find.TickManager.TicksGame + parent.ID) % Contracts.WorkInterval != 0) return;
        if (!Active)
        {
            workCaravan = null;
            return;
        }
        Contracts.WorkStep(this);
    }

    public override string? CompInspectStringExtra()
    {
        if (!Active || def == null) return null;
        var text = new StringBuilder();
        text.Append("PhobosContract_Inspect".Translate(Contracts.Request(def, thing, amount).Named("REQUEST"),
            (expiration - Find.TickManager.TicksGame).ToStringTicksToPeriod().Named("TIME")).Resolve());
        if (def.kind == ContractKind.WorkOnSite)
        {
            text.Append('\n').Append("PhobosContract_InspectWork".Translate(done.ToString("0.#").Named("DONE"), amount.ToString("0").Named("HOURS")).Resolve());
            if (workCaravan != null)
            {
                string line = heldUp != null
                    ? "PhobosContract_InspectHeldUp".Translate(heldUp.Named("REASON")).Resolve()
                    : !def.worksAtNight && workCaravan.NightResting
                        ? "PhobosContract_InspectResting".Translate(workCaravan.LabelCap.Named("CARAVAN")).Resolve()
                        : "PhobosContract_InspectWorking".Translate(workCaravan.LabelCap.Named("CARAVAN"), def.working.Named("WORKING")).Resolve();
                text.Append('\n').Append(line);
            }
        }
        return text.ToString();
    }

    public override IEnumerable<Gizmo> GetCaravanGizmos(Caravan caravan)
    {
        if (!Active || def == null || !Contracts.AtSettlement(caravan, parent)) yield break;
        if (def.kind != ContractKind.WorkOnSite)
        {
            var fulfil = new Command_Action
            {
                defaultLabel = "PhobosContract_Fulfil".Translate(),
                defaultDesc = "PhobosContract_FulfilDesc".Translate(parent.LabelCap.Named("SETTLEMENT"), Contracts.Request(def, thing, amount).Named("REQUEST")),
                icon = FulfilIcon,
                action = () => Contracts.AskToFulfil(this, caravan),
            };
            if (Contracts.FulfilRefusal(this, caravan) is { } why) fulfil.Disable(why);
            yield return fulfil;
            yield break;
        }
        bool here = workCaravan == caravan;
        var work = new Command_Action
        {
            defaultLabel = here ? "PhobosContract_StopWork".Translate() : "PhobosContract_StartWork".Translate(),
            defaultDesc = here
                ? "PhobosContract_StopWorkDesc".Translate(parent.LabelCap.Named("SETTLEMENT"))
                : "PhobosContract_StartWorkDesc".Translate(parent.LabelCap.Named("SETTLEMENT"), Contracts.Request(def, thing, amount).Named("REQUEST")),
            icon = FulfilIcon,
            action = here ? () => Contracts.StopWork(this) : () => Contracts.StartWork(this, caravan),
        };
        if (!here && Contracts.WorkRefusal(this, caravan, out _, out _) is { } refused) work.Disable(refused);
        yield return work;
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Defs.Look(ref def, "PhobosContractDef");
        Scribe_Defs.Look(ref thing, "PhobosContractThing");
        Scribe_Values.Look(ref amount, "PhobosContractAmount", 0f);
        Scribe_Values.Look(ref done, "PhobosContractDone", 0f);
        Scribe_Values.Look(ref expiration, "PhobosContractExpiration", -1);
        Scribe_References.Look(ref workCaravan, "PhobosContractWorkCaravan");
    }
}
