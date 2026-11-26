using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Phobos.Framework.Environment;
using Phobos.Framework.Rules;
using RimWorld;
using UnityEngine;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>What an ally drops when asked.</summary>
public enum SupplyKind
{
    Food,
    Medicine,
    Materials,
}

/// <summary>
/// Allied supply drops (D'thorga Framework 0.306.0; docs/development/airdrop.md, "Allied supply drops"; the owner's cards of 26 Nov 2026: a comms
/// console request, and the player chooses the kind). An industrial-or-better ally, asked over the comms console, sends about 800 silver's
/// worth of food, medicine or materials for 15 goodwill; within a few hours its aircraft crosses the map and its bundles drift onto the
/// colony, aimed at a drop zone marker if one is laid. With no aircraft of ours to send, the bundles come down from one unseen overhead.
/// </summary>
public sealed class GameComponent_PhobosSupplyDrops : GameComponent
{
    /// <summary>The goodwill a request costs (authored: less than military aid's 25).</summary>
    public const int GoodwillCost = 15;
    /// <summary>Days between two requests to one faction (authored: as military aid's one day).</summary>
    public const int CooldownTicks = 60000;
    /// <summary>The value of one drop, silver (authored).</summary>
    public const float ValueSilver = 800f;
    /// <summary>Hours from the request to the drop (authored: two to four).</summary>
    static readonly IntRange DelayTicks = new(5000, 10000);

    List<Pending> pending = new();
    Dictionary<Faction, int> lastAsked = new();
    List<Faction>? keys;
    List<int>? values;

    public GameComponent_PhobosSupplyDrops(Game game) { }

    public static GameComponent_PhobosSupplyDrops? Current => Verse.Current.Game?.GetComponent<GameComponent_PhobosSupplyDrops>();

    sealed class Pending : IExposable
    {
        public Faction? faction;
        public int mapId;
        public SupplyKind kind;
        public int due;

        public void ExposeData()
        {
            Scribe_References.Look(ref faction, "faction");
            Scribe_Values.Look(ref mapId, "mapId");
            Scribe_Values.Look(ref kind, "kind");
            Scribe_Values.Look(ref due, "due");
        }
    }

    // ---- The request ----

    /// <summary>Why this faction will not drop supplies on this map now, or null.</summary>
    public string? Refusal(Faction faction, Map map)
    {
        if (faction.PlayerRelationKind != FactionRelationKind.Ally) return "MustBeAlly".Translate().Resolve();
        if (faction.def.techLevel < TechLevel.Industrial) return "PhobosSupply_RefuseTech".Translate(faction.Name.Named("FACTION")).Resolve();
        int wait = (lastAsked.TryGetValue(faction, out int asked) ? asked : -CooldownTicks) + CooldownTicks - Find.TickManager.TicksGame;
        if (wait > 0) return "WaitTime".Translate(wait.ToStringTicksToPeriod()).Resolve();
        if (pending.Any(p => p.faction == faction)) return "PhobosSupply_RefuseOnItsWay".Translate(faction.Name.Named("FACTION")).Resolve();
        return null;
    }

    /// <summary>Asks for a drop of this kind: the goodwill is spent now, and the drop comes in a few hours.</summary>
    public void Request(Faction faction, Map map, SupplyKind kind)
    {
        if (Refusal(faction, map) is { } refused)
        {
            Messages.Message(refused, MessageTypeDefOf.RejectInput, historical: false);
            return;
        }
        Faction.OfPlayer.TryAffectGoodwillWith(faction, -GoodwillCost, canSendMessage: false, canSendHostilityLetter: true);
        lastAsked[faction] = Find.TickManager.TicksGame;
        pending.Add(new Pending { faction = faction, mapId = map.uniqueID, kind = kind, due = Find.TickManager.TicksGame + DelayTicks.RandomInRange });
        if (Seen.On)
        {
            string variant = kind.ToString().ToLowerInvariant();
            if (Seen.Wants("airdrop.supply-requested", null, variant)) Seen.Note("airdrop.supply-requested", null, variant: variant);
        }
    }

    public override void GameComponentTick()
    {
        if (pending.Count == 0 || Find.TickManager.TicksGame % 250 != 0) return;
        int now = Find.TickManager.TicksGame;
        for (int i = pending.Count - 1; i >= 0; i--)
        {
            Pending p = pending[i];
            if (p.due > now) continue;
            pending.RemoveAt(i);
            Map? map = Find.Maps.FirstOrDefault(m => m.uniqueID == p.mapId);
            if (map == null || p.faction == null || p.faction.HostileTo(Faction.OfPlayer)) continue;
            Deliver(p.faction, map, p.kind);
        }
    }

    // ---- The drop ----

    static void Deliver(Faction faction, Map map, SupplyKind kind)
    {
        List<Thing> goods = Goods(faction, kind);
        if (goods.Count == 0) return;
        IntVec3 target = DropZoneMarkers.First(map)?.Position ?? DropCellFinder.TradeDropSpot(map);
        string what = KindLabel(kind);
        if (!ByAircraft(faction, map, target, goods)) Unseen(map, target, goods);
        Find.LetterStack.ReceiveLetter("PhobosSupply_LetterLabel".Translate(), "PhobosSupply_LetterText".Translate(faction.Name.Named("FACTION"), what.Named("KIND")),
            LetterDefOf.PositiveEvent, new TargetInfo(target, map), faction);
    }

    /// <summary>Its aircraft crosses the map and drops the goods on the point, then leaves: an aircraft of ours fit for the faction, crewed by
    /// its people, its cargo the goods and a cargo parachute a bundle. False when the faction has none.</summary>
    static bool ByAircraft(Faction faction, Map map, IntVec3 target, List<Thing> goods)
    {
        if (RaiderFlight.AircraftFor(faction) is not { } def || def.GetModExtension<PhobosAirdropExtension>() == null) return false;
        VehiclePawn vehicle = VehicleSpawner.GenerateVehicle(def, faction);
        if (vehicle?.GetComp<CompPhobosFlight>() is not { } flight)
        {
            vehicle?.Destroy();
            return false;
        }
        RaiderFlight.FuelAndCrew(vehicle, faction, map);
        foreach (Thing thing in goods) vehicle.inventory.innerContainer.TryAdd(thing);
        Thing chutes = ThingMaker.MakeThing(AirdropDefOf.Phobos_CargoParachute);
        chutes.stackCount = Mathf.Max(1, Airdrop.BundlesNeeded(vehicle));
        vehicle.inventory.innerContainer.TryAdd(chutes);
        if (!FlightArrival.EnterAt(vehicle, map, Rand.Range(0f, 360f)))
        {
            vehicle.inventory.innerContainer.RemoveAll(t => goods.Contains(t));
            vehicle.Destroy();
            return false;
        }
        flight.BeginSupplyDrop(target);
        return true;
    }

    /// <summary>No aircraft of ours to send: the bundles come down from one unseen overhead, released upwind so they drift onto the point.</summary>
    static void Unseen(Map map, IntVec3 target, List<Thing> goods)
    {
        float feet = (float)ParachuteRule.Bundle.PlannedFeet;
        Vector3 release = target.ToVector3Shifted() - PhobosWind.Downwind(map) * PhobosParachuteFaller.DriftCells(map, ParachuteLoad.Bundle, feet);
        float marked = DropZoneMarkers.ScatterShare(map, target);
        foreach (List<Thing> bundle in Bundles(goods))
        {
            PhobosParachuteFaller faller = PhobosParachuteFaller.Release(map, bundle[0], ParachuteLoad.Bundle, release, feet, "", scatterShare: marked);
            for (int i = 1; i < bundle.Count; i++) faller.innerContainer.TryAdd(bundle[i], canMergeWithExistingStacks: false);
        }
    }

    /// <summary>The goods in bundles of up to a tonne each.</summary>
    static IEnumerable<List<Thing>> Bundles(List<Thing> goods)
    {
        var bundle = new List<Thing>();
        float kg = 0f;
        foreach (Thing thing in goods)
        {
            float mass = thing.GetStatValue(StatDefOf.Mass) * thing.stackCount;
            if (bundle.Count > 0 && kg + mass > ParachuteRule.BundleKilograms)
            {
                yield return bundle;
                bundle = new List<Thing>();
                kg = 0f;
            }
            bundle.Add(thing);
            kg += mass;
        }
        if (bundle.Count > 0) yield return bundle;
    }

    /// <summary>About <see cref="ValueSilver"/> worth of the kind asked for (authored: survival packs or pemmican; industrial medicine; steel
    /// and components half and half).</summary>
    static List<Thing> Goods(Faction faction, SupplyKind kind)
    {
        var goods = new List<Thing>();
        switch (kind)
        {
            case SupplyKind.Food:
                Add(goods, faction.def.techLevel >= TechLevel.Industrial ? ThingDefOf.MealSurvivalPack : ThingDefOf.Pemmican, ValueSilver);
                break;
            case SupplyKind.Medicine:
                Add(goods, ThingDefOf.MedicineIndustrial, ValueSilver);
                break;
            default:
                Add(goods, ThingDefOf.Steel, ValueSilver / 2f);
                Add(goods, ThingDefOf.ComponentIndustrial, ValueSilver / 2f);
                break;
        }
        return goods;
    }

    static void Add(List<Thing> goods, ThingDef def, float value)
    {
        int count = Mathf.Max(1, Mathf.RoundToInt(value / Mathf.Max(0.1f, def.BaseMarketValue)));
        while (count > 0)
        {
            Thing thing = ThingMaker.MakeThing(def);
            thing.stackCount = Mathf.Min(count, def.stackLimit);
            count -= thing.stackCount;
            goods.Add(thing);
        }
    }

    public static string KindLabel(SupplyKind kind) => kind switch
    {
        SupplyKind.Food => "PhobosSupply_KindFood".Translate().Resolve(),
        SupplyKind.Medicine => "PhobosSupply_KindMedicine".Translate().Resolve(),
        _ => "PhobosSupply_KindMaterials".Translate().Resolve(),
    };

    public override void ExposeData()
    {
        Scribe_Collections.Look(ref pending, "PhobosSupply_Pending", LookMode.Deep);
        Scribe_Collections.Look(ref lastAsked, "PhobosSupply_LastAsked", LookMode.Reference, LookMode.Value, ref keys, ref values);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            pending ??= new List<Pending>();
            pending.RemoveAll(p => p.faction == null);
            lastAsked ??= new Dictionary<Faction, int>();
        }
    }
}

/// <summary>
/// "Request a supply drop" on the comms console, for an ally (Framework 0.306.0): the game's faction dialog has no place for another mod's
/// option, so a postfix adds one before its last ("Disconnect"). It only adds an option; the choice is handed to the service above.
/// </summary>
[HarmonyPatch(typeof(FactionDialogMaker), nameof(FactionDialogMaker.FactionDialogFor))]
public static class Patch_FactionDialogFor_SupplyDrop
{
    public static void Postfix(Pawn negotiator, Faction faction, DiaNode __result)
    {
        if (__result?.options == null || faction == null || faction.IsPlayer || negotiator?.Map is not { } map) return;
        if (GameComponent_PhobosSupplyDrops.Current is not { } drops) return;
        var option = new DiaOption("PhobosSupply_Option".Translate(GameComponent_PhobosSupplyDrops.GoodwillCost.Named("GOODWILL")));
        if (drops.Refusal(faction, map) is { } refused) option.Disable(refused);
        else option.link = Choose(faction, map, drops, __result);
        __result.options.Insert(Mathf.Max(0, __result.options.Count - 1), option);
    }

    static DiaNode Choose(Faction faction, Map map, GameComponent_PhobosSupplyDrops drops, DiaNode back)
    {
        var node = new DiaNode("PhobosSupply_Choose".Translate(faction.Name.Named("FACTION"), GameComponent_PhobosSupplyDrops.ValueSilver.ToString("0").Named("VALUE")));
        foreach (SupplyKind kind in new[] { SupplyKind.Food, SupplyKind.Medicine, SupplyKind.Materials })
        {
            node.options.Add(new DiaOption(GameComponent_PhobosSupplyDrops.KindLabel(kind))
            {
                action = () => drops.Request(faction, map, kind),
                link = new DiaNode("PhobosSupply_Sent".Translate(faction.Name.Named("FACTION")))
                {
                    options = { new DiaOption("OK".Translate()) { link = back } },
                },
            });
        }
        node.options.Add(new DiaOption("GoBack".Translate()) { link = back });
        return node;
    }
}
