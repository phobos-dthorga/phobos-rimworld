using System.Collections.Generic;
using System.Linq;
using Phobos.Framework.Environment;
using Phobos.Framework.Rules;
using RimWorld;
using UnityEngine;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// What happens as a parachute load lands (D'thorga Framework 0.288.0; docs/development/airdrop.md, "Landing"). People are put on open ground
/// near where they came down, at the T-11's measured injury rate raised by night, wind, heavy kit, a novice's first jumps and a missed spot
/// (ParachuteRule); bundles spill their cargo; a vehicle lands on a clear footprint, worn a little, and now and then hard. Every canopy is left
/// as a spent parachute, every platform as a spent platform, to carry home and repack. Nothing is ever lost: what cannot go where it was
/// meant to goes on the nearest open ground.
/// </summary>
public static class ParachuteLanding
{
    /// <summary>How far from where it came down a load may be moved to open ground, cells (authored).</summary>
    const int OpenGroundSearch = 20;
    /// <summary>How far a vehicle's clear footprint is looked for, cells (authored, the sling's limit).</summary>
    const float VehicleSearch = 25f;
    /// <summary>The share of each landing part's health an ordinary platform landing wears (authored).</summary>
    const float LandingWearShare = 0.03f;

    static bool Risky => VehicleSystemsMod.Settings.airdropLandingRisk && !VehicleSystemsMod.Settings.showOnly;

    /// <summary>Lands everything the faller carries where it is now, and leaves its spent canopy or platform.</summary>
    public static void Land(PhobosParachuteFaller faller)
    {
        Map map = faller.Map;
        if (map == null) return;
        IntVec3 at = faller.Position;
        bool night = map.skyManager.CurSkyGlow < 0.3f;
        float knots = PhobosWind.KnotsAt(map, at);
        ParachuteRule.Figures figures = ParachuteRule.For(faller.load);
        List<Thing> contents = faller.innerContainer.ToList();
        foreach (Thing thing in contents) faller.innerContainer.Remove(thing);
        bool hard = Risky && faller.load != ParachuteLoad.Person && (Rand.Chance((float)ParachuteRule.HardLandingChance) || !ParachuteRule.Opens(faller.heightFeet));
        bool offTarget = false;
        foreach (Thing thing in contents)
        {
            switch (thing)
            {
                case VehiclePawn vehicle:
                    LandVehicle(vehicle, map, at, faller, hard, night, knots, figures);
                    break;
                case Pawn pawn:
                    offTarget |= LandPerson(pawn, map, at, faller, night, knots, figures, crewInside: false);
                    break;
                default:
                    LandItem(thing, map, at, hard);
                    break;
            }
        }
        ThingDef spent = faller.load switch
        {
            ParachuteLoad.Person => AirdropDefOf.Phobos_SpentParachute,
            ParachuteLoad.Bundle => AirdropDefOf.Phobos_SpentCargoParachute,
            _ => AirdropDefOf.Phobos_SpentDropPlatform,
        };
        if (ParachuteRule.Opens(faller.heightFeet) || faller.load == ParachuteLoad.Vehicle) GenPlace.TryPlaceThing(ThingMaker.MakeThing(spent), at, map, ThingPlaceMode.Near);
        if (Seen.On)
        {
            string variant = faller.load.ToString().ToLowerInvariant() + (hard ? "-hard" : "") + (offTarget ? "-moved" : "");
            if (Seen.Wants("airdrop.landed", faller, variant))
                Seen.Note("airdrop.landed", faller, variant: variant, values: new[] { new KeyValuePair<string, double>("knots", knots),
                    new KeyValuePair<string, double>("feet", faller.heightFeet) });
        }
    }

    /// <summary>The nearest cell to <paramref name="near"/> a person or a bundle can come down on: standable, open to the sky. False when it
    /// had to move (a roof, water, a wall), which a person feels.</summary>
    static IntVec3 OpenCell(Map map, IntVec3 near, out bool moved)
    {
        moved = false;
        if (near.InBounds(map) && near.Standable(map) && !near.Roofed(map)) return near;
        moved = true;
        int count = GenRadial.NumCellsInRadius(OpenGroundSearch);
        for (int i = 1; i < count; i++)
        {
            IntVec3 c = near + GenRadial.RadialPattern[i];
            if (c.InBounds(map) && c.Standable(map) && !c.Roofed(map)) return c;
        }
        return CellFinder.TryFindRandomCellNear(near, map, 40, c => c.Standable(map), out IntVec3 any) ? any : near.ClampInsideMap(map);
    }

    /// <summary>A person lands: put down, perhaps hurt, a jump on their record and a thought. True when they had to come down off their spot.</summary>
    static bool LandPerson(Pawn pawn, Map map, IntVec3 near, PhobosParachuteFaller faller, bool night, float knots, ParachuteRule.Figures figures,
        bool crewInside)
    {
        bool moved = false;
        IntVec3 cell = near;
        if (!crewInside) cell = OpenCell(map, near, out moved);
        if (!crewInside && !GenPlace.TryPlaceThing(pawn, cell, map, ThingPlaceMode.Near)) GenSpawn.Spawn(pawn, cell, map);
        bool hurt = Hurt(pawn, faller.heightFeet, night, knots, figures, moved, crewInside);
        int jumps = (int)(pawn.records?.GetValue(AirdropDefOf.Phobos_Jumps) ?? 0);
        pawn.records?.Increment(AirdropDefOf.Phobos_Jumps);
        pawn.needs?.mood?.thoughts?.memories?.TryGainMemory(hurt ? AirdropDefOf.Phobos_HardLanding : AirdropDefOf.Phobos_JumpedFromAircraft);
        // Ours landing among enemies are ready to fight, as the game drafts people from a drop pod in a fight.
        if (!crewInside && pawn.Faction == Faction.OfPlayer && pawn.drafter != null && GenHostility.AnyHostileActiveThreatToPlayer(map)) pawn.drafter.Drafted = true;
        if (hurt && pawn.Faction == Faction.OfPlayer)
        {
            Messages.Message("PhobosAirdrop_MsgHurtLanding".Translate(pawn.LabelShort.Named("PERSON")).Resolve(), pawn, MessageTypeDefOf.NegativeEvent);
        }
        if (Seen.On && jumps < ParachuteRule.NoviceJumps && Seen.Wants("airdrop.novice", pawn)) Seen.Note("airdrop.novice", pawn);
        return moved;
    }

    /// <summary>A person landing on a tile with no map (a drop onto the world map): the same risk, a jump on their record and a thought.</summary>
    public static void HurtOffMap(Pawn pawn, float heightFeet, bool night, float knots)
    {
        bool hurt = Hurt(pawn, heightFeet, night, knots, ParachuteRule.Person, moved: false, crewInside: false);
        pawn.records?.Increment(AirdropDefOf.Phobos_Jumps);
        pawn.needs?.mood?.thoughts?.memories?.TryGainMemory(hurt ? AirdropDefOf.Phobos_HardLanding : AirdropDefOf.Phobos_JumpedFromAircraft);
    }

    /// <summary>Whether a landing hurts this person, and hurts them if so: a canopy that never opened is a fall; otherwise a blow to the
    /// legs or the head at the rule's chance.</summary>
    static bool Hurt(Pawn pawn, float heightFeet, bool night, float knots, ParachuteRule.Figures figures, bool moved, bool crewInside)
    {
        if (!Risky || pawn.Dead) return false;
        if (!ParachuteRule.Opens(heightFeet))
        {
            double harm = LineRule.FallHarm(heightFeet * ParachuteRule.MetresPerFoot);
            for (int i = 0; i < LineRule.FallBlows && !pawn.Dead; i++) pawn.TakeDamage(new DamageInfo(DamageDefOf.Blunt, (float)(harm / LineRule.FallBlows)));
            return harm > 0;
        }
        bool heavy = MassUtility.GearAndInventoryMass(pawn) > ParachuteRule.HeavyKitKilograms;
        bool novice = (pawn.records?.GetValue(AirdropDefOf.Phobos_Jumps) ?? 0) < ParachuteRule.NoviceJumps;
        double chance = ParachuteRule.InjuryChance(night, knots, figures.WindLimitKnots, heavy, novice, heightFeet, figures.MinimumFeet, moved);
        if (crewInside) chance = System.Math.Min(1, chance * ParachuteRule.CrewInsideFactor);
        if (!Rand.Chance((float)chance)) return false;
        BodyPartRecord? part = InjuredPart(pawn);
        pawn.TakeDamage(new DamageInfo(DamageDefOf.Blunt, (float)ParachuteRule.InjuryHarm, hitPart: part));
        return true;
    }

    /// <summary>Where a landing injury falls: the head at the rule's share, otherwise a foot or a leg; any part where neither is found.</summary>
    static BodyPartRecord? InjuredPart(Pawn pawn)
    {
        var parts = pawn.health.hediffSet.GetNotMissingParts().ToList();
        if (Rand.Chance((float)ParachuteRule.HeadInjuryShare) && parts.FirstOrDefault(p => p.def == BodyPartDefOf.Head) is { } head) return head;
        var legs = parts.Where(p => p.def.defName is "Foot" or "Leg" || p.def == BodyPartDefOf.Leg).ToList();
        return legs.Count > 0 ? legs.RandomElement() : null;
    }

    /// <summary>A bundle's cargo lands beside it; a hard landing damages what can be damaged.</summary>
    static void LandItem(Thing thing, Map map, IntVec3 near, bool hard)
    {
        if (hard && thing.def.useHitPoints)
        {
            int loss = Mathf.RoundToInt(thing.MaxHitPoints * Rand.Range(0.1f, (float)ParachuteRule.HardLandingItemShare));
            thing.HitPoints = Mathf.Max(1, thing.HitPoints - loss);
        }
        IntVec3 cell = OpenCell(map, near, out _);
        if (!GenPlace.TryPlaceThing(thing, cell, map, ThingPlaceMode.Near)) GenSpawn.Spawn(thing, cell, map);
    }

    /// <summary>
    /// A vehicle lands on its platform: on the nearest clear footprint (never on another thing, never under a roof), worn a little, now and
    /// then hard. With its crew inside (the player's setting), each of them takes a person's landing chance several times over.
    /// </summary>
    static void LandVehicle(VehiclePawn vehicle, Map map, IntVec3 near, PhobosParachuteFaller faller, bool hard, bool night, float knots, ParachuteRule.Figures figures)
    {
        Rot4 rot = faller.vehicleRot;
        if (!SlingPlacement.TryFindClear(vehicle.VehicleDef, rot, map, near, VehicleSearch, CellRect.Empty, out IntVec3 spot)
            && !SlingPlacement.TryFindClear(vehicle.VehicleDef, rot, map, near, GenRadial.MaxRadialPatternRadius - 1f, CellRect.Empty, out spot))
        {
            // Nowhere clear at all: where it came down, moving aside what can be moved; plants and filth are lost under it.
            spot = near.ClampInsideMap(map);
        }
        GenSpawn.Spawn(vehicle, spot, map, rot, WipeMode.VanishOrMoveAside);
        if (!vehicle.Spawned) return;
        vehicle.EventRegistry[VehicleEventDefOf.AerialVehicleLanding]?.ExecuteEvents();
        if (Risky) CompPhobosVehicleSystems.Of(vehicle)?.WearLanding(LandingWearShare);
        string? harm = hard ? SlingDrops.Fall(vehicle, (float)ParachuteRule.HardLandingMetres) : null;
        foreach (Pawn crew in vehicle.AllPawnsAboard.ToList())
        {
            if (!crew.Dead) LandPerson(crew, map, spot, faller, night, knots, ParachuteRule.Person, crewInside: true);
        }
        if (vehicle.Faction == Faction.OfPlayer)
        {
            Messages.Message(hard
                    ? "PhobosAirdrop_MsgVehicleLandedHard".Translate(vehicle.LabelShort.Named("VEHICLE"), (harm ?? "").Named("HARM")).Resolve()
                    : "PhobosAirdrop_MsgVehicleLanded".Translate(vehicle.LabelShort.Named("VEHICLE")).Resolve(),
                vehicle, hard ? MessageTypeDefOf.NegativeEvent : MessageTypeDefOf.PositiveEvent);
        }
    }
}
