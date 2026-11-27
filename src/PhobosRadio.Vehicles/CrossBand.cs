using System.Collections.Generic;
using System.Linq;
using Phobos.Radio.Services;
using Phobos.VehicleSystems;
using RimWorld;
using Vehicles;
using Verse;

namespace Phobos.Radio.Vehicles;

/// <summary>
/// The cross-band last hop (Radio 0.25.0; docs/development/radio-advice-and-relays.md, "Two kinds, by physics"; the owner's card of 27 Nov
/// 2026, "New dual-band radio fitting"): a colonist's hand-held on a map no station of theirs serves (a camp, a site, an ambush) talks to a
/// vehicle of theirs on the same map with the dual-band radio fitted, which sends the call on at once over VHF between world tiles, through
/// any relay, toward home. The hand-held's hop is the map's own link budget, walls and all; the vehicle's hop is the world's. As Hiddensoul
/// works his base radio from bed through a hand-held, and the owner's own set does: cross-band repeat.
/// <para>Cost: nothing runs on its own. Asked for one pawn at a time (advice by radio), each answer kept for <see cref="KeepTicks"/>: one
/// link budget for each such vehicle on the map, and the world search once.</para>
/// </summary>
public static class CrossBand
{
    /// <summary>What the dual-band radio fitting grants, and its load (CrossBand.xml).</summary>
    public const string Grant = "RadioCrossBand";
    public const string Load = "dualBandRadio";

    /// <summary>How long one answer for a pawn is kept, in ticks: a tenth of a game hour, as a convoy's reach is.</summary>
    const int KeepTicks = 250;

    /// <summary>A vehicle a hand-held talks home through: the vehicle, the hand-held's link to it, and the vehicle's end between tiles.</summary>
    public sealed record Gateway(VehiclePawn Vehicle, double MapMarginDb, RelayNode WorldEnd);

    static readonly Dictionary<Pawn, (int tick, Gateway? gateway)> kept = new();
    static Thing? voiceUnit;
    static Game? keptFor;

    /// <summary>The dual-band radio itself: one stateless set, its figures from its Def, never on a map.</summary>
    internal static CompRadio? Voice
    {
        get
        {
            voiceUnit ??= DefDatabase<ThingDef>.GetNamedSilentFail("PhobosRadio_DualBandUnit") is { } def ? ThingMaker.MakeThing(def) : null;
            return voiceUnit?.TryGetComp<CompRadio>();
        }
    }

    /// <summary>Whether this vehicle carries the dual-band radio, fitted and powered.</summary>
    public static bool Fitted(VehiclePawn vehicle) =>
        vehicle.GetComp<CompPhobosVehicleSystems>() is { } systems && systems.Fittings.Grants(Grant) && systems.LoadActive(Load);

    /// <summary>Whether any vehicle of the player's on this map carries a working dual-band radio: so a map with no station is still looked at.</summary>
    public static bool AnyOn(Map map) =>
        map.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer).Any(p => p is VehiclePawn vehicle && Fitted(vehicle));

    /// <summary>
    /// The gateway this pawn's hand-held reaches, the best link first, or null: a spawned colonist with a worn hand-held, on a map where no
    /// station serves them, and a dual-band vehicle of the player's on the map in reach of the hand-held. Kept a short while.
    /// </summary>
    public static Gateway? For(Pawn pawn)
    {
        if (keptFor != Current.Game)
        {
            kept.Clear();
            keptFor = Current.Game;
        }
        int now = Find.TickManager.TicksGame;
        if (kept.TryGetValue(pawn, out var held) && now - held.tick < KeepTicks) return held.gateway;
        if (kept.Count > 256) kept.Clear();
        Gateway? found = Search(pawn);
        kept[pawn] = (now, found);
        if (found != null && Seen.Wants("radio.cross-band", pawn)) Seen.Note("radio.cross-band", pawn, found.Vehicle.LabelShort);
        return found;
    }

    /// <summary>This colonist's kept gateway, while it is fresh, or null: never searches (the mood and the vehicle's line read it).</summary>
    public static Gateway? Kept(Pawn pawn) =>
        keptFor == Current.Game && kept.TryGetValue(pawn, out var held) && Find.TickManager.TicksGame - held.tick < KeepTicks * 2 ? held.gateway : null;

    /// <summary>The colonists whose hand-helds this vehicle's dual-band radio passes on now, from the kept answers.</summary>
    public static IEnumerable<Pawn> Serving(VehiclePawn vehicle)
    {
        if (keptFor != Current.Game) yield break;
        int now = Find.TickManager.TicksGame;
        foreach (var entry in kept)
            if (entry.Value.gateway?.Vehicle == vehicle && now - entry.Value.tick < KeepTicks * 2 && entry.Key.Spawned) yield return entry.Key;
    }

    /// <summary>
    /// Keeps the answers fresh on a map no station of the player's serves where a dual-band vehicle stands (Radio 0.34.0): each colonist with
    /// a worn hand-held is asked once. Called by <see cref="MapComponent_PhobosCrossBand"/> every 250 ticks.
    /// </summary>
    public static void Refresh(Map map)
    {
        if (map.IsPlayerHome || !AnyOn(map)) return;
        foreach (Pawn pawn in map.mapPawns.FreeColonistsSpawned)
        {
            if (pawn.apparel?.WornApparel.Any(a => a.TryGetComp<CompRadio>() is { Props.isStation: false }) == true) For(pawn);
        }
    }

    static Gateway? Search(Pawn pawn)
    {
        if (!pawn.Spawned || pawn.apparel == null || Voice is not { } voice) return null;
        Map map = pawn.Map;
        RadioNet net = RadioNet.For(map);
        if (net.ServedBy(pawn) != null) return null;
        CompRadio? handheld = pawn.apparel.WornApparel.Select(a => a.TryGetComp<CompRadio>()).FirstOrDefault(r => r != null && !r.Props.isStation);
        if (handheld == null) return null;
        var rules = RadioRulesDef.Current;
        RadioEndpoint handEnd = RadioEndpoint.BuiltIn(handheld.parent, pawn.Position, handheld);
        Gateway? best = null;
        foreach (Pawn other in map.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer))
        {
            if (other is not VehiclePawn vehicle || !Fitted(vehicle)) continue;
            RadioEndpoint vehicleEnd = RadioEndpoint.BuiltIn(vehicle, vehicle.Position, voice);
            // Any contact counts, a marginal one too: advice by radio gives a quarter of its help on a marginal link, as from a station.
            if (LinkBudgetService.Evaluate(map, handEnd, vehicleEnd, net.Noise, rules, RadioMod.Settings.metresPerCell) is not { } link
                || link.MarginDb < rules.marginalDb) continue;
            if (best != null && link.MarginDb <= best.MapMarginDb) continue;
            var worldEnd = new RelayNode(RadioEndpoint.BuiltIn(vehicle, IntVec3.Zero, voice), map.Tile, vehicle.LabelShort, vehicle,
                SiteMetres: RelayNode.SiteOf(map.Tile));
            best = new Gateway(vehicle, link.MarginDb, worldEnd);
        }
        return best;
    }
}

/// <summary>
/// Keeps the cross-band answers fresh on a map of the player's that is not a home (Radio 0.34.0): every 250 ticks, only where a dual-band
/// vehicle stands. Cost: nothing elsewhere; there, one map link budget per colonist with a hand-held for each such vehicle, and the vehicle's
/// world chain kept as for advice. Saves nothing.
/// </summary>
public sealed class MapComponent_PhobosCrossBand : MapComponent
{
    public MapComponent_PhobosCrossBand(Map map) : base(map) { }

    public override void MapComponentTick()
    {
        if (Find.TickManager.TicksGame % 250 != 137) return;
        CrossBand.Refresh(map);
    }
}

/// <summary>Adds the line that says whom a vehicle's dual-band radio passes calls on for (Radio 0.34.0), to every design of ours.</summary>
public class CompProperties_PhobosCrossBand : CompProperties
{
    public CompProperties_PhobosCrossBand() => compClass = typeof(CompPhobosCrossBand);
}

/// <summary>The vehicle's inspect line while its dual-band radio carries colonists' calls home. Reads the kept answers only.</summary>
public class CompPhobosCrossBand : ThingComp
{
    public override string? CompInspectStringExtra()
    {
        if (parent is not VehiclePawn vehicle || !vehicle.Spawned || !CrossBand.Fitted(vehicle)) return null;
        var serving = CrossBand.Serving(vehicle).Select(p => p.LabelShort).ToList();
        if (serving.Count == 0) return null;
        if (Seen.Wants("radio.cross-band-serving", vehicle)) Seen.Note("radio.cross-band-serving", vehicle, serving.Count + " people");
        return "PhobosRadio_CrossBandServing".Translate(serving.ToCommaList(useAnd: true).Named("PEOPLE")).Resolve();
    }
}

/// <summary>Registers the cross-band answers with the radio core, and the vehicle's line on every design of ours (Radio 0.34.0).</summary>
[StaticConstructorOnStartup]
public static class CrossBandStartup
{
    static CrossBandStartup()
    {
        AwayReach.Register(pawn => CrossBand.Kept(pawn) is { } gateway && ConvoyRemoteReach.KeptFirstHome(gateway) is { } home
            ? System.Math.Min(gateway.MapMarginDb, home.MarginDb) : null);
        foreach (var def in DefDatabase<VehicleDef>.AllDefsListForReading)
        {
            if (def.comps.Exists(c => c is CompProperties_PhobosVehicleSystems) && !def.comps.Exists(c => c is CompProperties_PhobosCrossBand))
                def.comps.Add(new CompProperties_PhobosCrossBand());
        }
    }
}
