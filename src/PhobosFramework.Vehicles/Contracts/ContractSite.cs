using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using Vehicles;
using Verse;
using Verse.Grammar;

namespace Phobos.VehicleSystems;

/// <summary>
/// The place on the world map where a contract waits (Framework 0.311.0; docs/development/vehicle-contracts.md; EM9's tow and casualty
/// flight), shaped as the game's own <c>SitePartWorker_DownedRefugee</c>: the target is made when the quest is made, held by the site
/// part until the map is, and its words go into the quest's text.
/// </summary>
public class SitePartWorker_PhobosContractTarget : SitePartWorker
{
    public override void Notify_GeneratedByQuestGen(SitePart part, Slate slate, List<Rule> outExtraDescriptionRules, Dictionary<string, string> outExtraDescriptionConstants)
    {
        base.Notify_GeneratedByQuestGen(part, slate, outExtraDescriptionRules, outExtraDescriptionConstants);
        if (!slate.TryGet("contractOffer", out ContractOffer offer) || !slate.TryGet("faction", out Faction faction) || faction == null) return;
        if (Contracts.MakeTarget(offer.Def, faction, out string words) is not { } target) return;
        part.things = new ThingOwner<Pawn>(part, oneStackOnly: true);
        part.things.TryAdd(target);
        slate.Set("contractTarget", target);
        slate.Set("contractTargetWords", words);
    }

    public override string GetPostProcessedThreatLabel(Site site, SitePart sitePart)
    {
        string text = base.GetPostProcessedThreatLabel(site, sitePart);
        if (sitePart.things != null && sitePart.things.Any) text += ": " + sitePart.things[0].LabelShortCap;
        if (site.HasWorldObjectTimeout) text += " (" + "DurationLeft".Translate(site.WorldObjectTimeoutTicksLeft.ToStringTicksToPeriod()) + ")";
        return text;
    }
}

/// <summary>
/// Puts what waits on the place's map, as the game's <c>GenStep_DownedRefugee</c> does, and takes it into the player's care: a stranded
/// vehicle on open ground it fits, a casualty downed and unable to walk, with no bleeding wounds (the game's own rule for its refugee).
/// </summary>
public class GenStep_PhobosContractTarget : GenStep_Scatterer
{
    /// <summary>How far from the scatter point to look for ground the whole vehicle fits on.</summary>
    const float VehicleSpotRadius = 20f;

    public override int SeedPart => 518274913;

    protected override bool CanScatterAt(IntVec3 c, Map map) => base.CanScatterAt(c, map) && c.Standable(map) && !c.Fogged(map);

    protected override void ScatterAt(IntVec3 loc, Map map, GenStepParams parms, int count = 1)
    {
        if (parms.sitePart?.things is not { Any: true } held) return;
        Pawn target = (Pawn)held.Take(held[0]);
        string kind;
        if (target is VehiclePawn vehicle)
        {
            if (!SlingPlacement.TryFindClear(vehicle.VehicleDef, Rot4.North, map, loc, VehicleSpotRadius, CellRect.Empty, out IntVec3 spot)) spot = loc;
            GenSpawn.Spawn(vehicle, spot, map, Rot4.North);
            kind = ContractKind.TowVehicle.ToString();
        }
        else
        {
            HealthUtility.DamageUntilDowned(target, allowBleedingWounds: false);
            HealthUtility.DamageLegsUntilIncapableOfMoving(target, allowBleedingWounds: false);
            GenSpawn.Spawn(target, loc, map);
            kind = ContractKind.Casualty.ToString();
        }
        Contracts.TakeIntoCare(target);
        MapGenerator.SetVar("RectOfInterest", CellRect.CenteredOn(target.Position, 1, 1));
        if (Seen.Wants("vehicles.contract-site", target, kind)) Seen.Note("vehicles.contract-site", target, target.LabelShort, kind);
    }
}
