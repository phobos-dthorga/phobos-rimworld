using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using LudeonTK;
using Phobos.Framework.Vehicles.Rules;
using Phobos.Radio.Services;
using Phobos.VehicleSystems;
using Vehicles;
using Verse;

namespace Phobos.Radio.Vehicles;

/// <summary>
/// Where on a map a vehicle's control radio keeps its link (Radio 0.14.0; docs/development/autonomous-vehicles.md, step 8), for D'thorga
/// Framework's keeping to radio cover (<see cref="RemoteCoverCosts"/>). One sample per block of the coverage overlay's quick pass
/// (<see cref="RadioRulesDef.coverageCoarseBlockCells"/>): the first cell of the block a vehicle could stand on, put through the very
/// <see cref="LinkBudgetService.Evaluate"/> the net decides every contact with, from a vehicle's own radio to every antenna of every powered
/// station. So the route cannot disagree with the link. Whether a station has an operator is left out: it is a matter of who is at the
/// console, not of the ground, and would make routes change each time the operator eats.
/// <para>
/// <b>Performance is the radio's first concern.</b> A map has one only once a vehicle on it was asked to be driven from afar, and it works
/// only while one was within <see cref="RadioRulesDef.remoteCoverWantedTicks"/>: at most <see cref="RadioRulesDef.remoteCoverBudgetMsPerTick"/>
/// of real time a tick, through the net's own tick (<see cref="RadioNet.Ticked"/>), so nothing is added to the save. It looks for a change every
/// <see cref="RadioRulesDef.remoteCoverLookTicks"/>: the overlay's events (walls, rock, doors, coax, antenna ports, roofs, upgrades), the noise
/// sources, the cell scale, the block size and the set of powered stations. A change during a pass finishes the pass, then starts another.
/// Derived, never saved.
/// </para>
/// </summary>
public sealed class RemoteCoverMap
{
    static readonly ConditionalWeakTable<Map, RemoteCoverMap> byMap = new();

    readonly Map map;
    readonly List<RadioEndpoint> ends = new();
    readonly Stopwatch watch = new();
    RemoteCover? published;
    byte[] working = System.Array.Empty<byte>();
    int cursor = -1;
    int block = 4, across, down;
    int passSignature;
    bool again;
    int nextLookTick;
    int wantedUntilTick = -1;
    Pawn? template;
    CompRadio? templateRadio;

    RemoteCoverMap(Map map) => this.map = map;

    /// <summary>The net's tick reaches every map's cover map through here; a map with none costs one lookup.</summary>
    public static void Tick(RadioNet net)
    {
        if (byMap.TryGetValue(net.map, out var cover)) cover.TickOnce();
    }

    /// <summary>The last whole cover map, or null while none is finished. Nothing is worked out here.</summary>
    public static RemoteCover? CoverOf(Map map) => byMap.TryGetValue(map, out var cover) ? cover.published : null;

    /// <summary>A vehicle on this map is asked to be driven from afar: its radio is the one the map is worked out for, and work goes on.</summary>
    public static void Want(Pawn vehicle)
    {
        if (!vehicle.Spawned || vehicle.GetComp<CompRadio>() is not { } radio) return;
        var cover = byMap.GetValue(vehicle.Map, map => new RemoteCoverMap(map));
        cover.wantedUntilTick = Find.TickManager.TicksGame + RadioRulesDef.Current.remoteCoverWantedTicks;
        if (cover.template is not { Spawned: true } || cover.template.Map != vehicle.Map)
        {
            cover.template = vehicle;
            cover.templateRadio = radio;
            cover.nextLookTick = 0;
        }
    }

    void TickOnce()
    {
        var now = Find.TickManager.TicksGame;
        if (now > wantedUntilTick) return;
        var rules = RadioRulesDef.Current;
        if (now >= nextLookTick)
        {
            nextLookTick = now + rules.remoteCoverLookTicks;
            Look(rules);
        }
        if (cursor >= 0) Work(rules, rules.remoteCoverBudgetMsPerTick);
    }

    bool HasTemplate => template is { Spawned: true } && template.Map == map && templateRadio != null;

    void Look(RadioRulesDef rules)
    {
        if (!HasTemplate) return;
        var signature = Signature(rules);
        if (cursor >= 0)
        {
            if (signature != passSignature) again = true;
            return;
        }
        if (published != null && signature == passSignature) return;
        Start(rules, signature);
    }

    /// <summary>What the map is worked out from, as one number: the overlay's change count, the noise, the scale, the block size, the
    /// powered stations and the radio's kind.</summary>
    int Signature(RadioRulesDef rules)
    {
        var net = RadioNet.For(map);
        unchecked
        {
            var hash = 17;
            hash = hash * 31 + (RadioCoverage.For(map)?.ChangeCount ?? 0);
            hash = hash * 31 + net.Noise.Signature;
            hash = hash * 31 + RadioMod.Settings.metresPerCell.GetHashCode();
            hash = hash * 31 + rules.coverageCoarseBlockCells;
            hash = hash * 31 + (template?.def.shortHash ?? 0);
            foreach (var station in net.Stations) hash = hash * 31 + station.parent.thingIDNumber;
            return hash;
        }
    }

    void Start(RadioRulesDef rules, int signature)
    {
        passSignature = signature;
        again = false;
        block = System.Math.Max(1, rules.coverageCoarseBlockCells);
        across = RemoteCoverRule.Blocks(map.Size.x, block);
        down = RemoteCoverRule.Blocks(map.Size.z, block);
        working = new byte[across * down];
        cursor = 0;
        ends.Clear();
        var metresPerCell = RadioMod.Settings.metresPerCell;
        foreach (var station in RadioNet.For(map).Stations) ends.AddRange(RadioNet.StationEndpoints(station, metresPerCell));
    }

    void Work(RadioRulesDef rules, float budgetMs)
    {
        if (!HasTemplate)
        {
            cursor = -1;
            return;
        }
        var noise = RadioNet.For(map).Noise;
        var metresPerCell = RadioMod.Settings.metresPerCell;
        watch.Restart();
        while (cursor < working.Length)
        {
            working[cursor] = Sample(cursor % across * block, cursor / across * block, noise, rules, metresPerCell);
            cursor++;
            if ((cursor & 3) == 0 && watch.Elapsed.TotalMilliseconds >= budgetMs) return;
        }
        Finish();
    }

    void Finish()
    {
        published = new RemoteCover(block, map.Size.x, map.Size.z, working);
        working = System.Array.Empty<byte>();
        cursor = -1;
        if (again) nextLookTick = 0;
        if (template != null && Seen.Wants("radio.remote-cover", template))
        {
            Seen.Note("radio.remote-cover", template, published.Blocks.Length + " blocks, " + RemoteCoverRule.UncoveredCount(published.Blocks)
                + " out of cover, through " + ends.Count + " antennas");
        }
    }

    /// <summary>One block: whether a vehicle on its first cell it could stand on would be in touch with any station's antenna.</summary>
    byte Sample(int x0, int z0, NoiseService noise, RadioRulesDef rules, float metresPerCell)
    {
        for (var z = z0; z < z0 + block && z < map.Size.z; z++)
        {
            for (var x = x0; x < x0 + block && x < map.Size.x; x++)
            {
                var cell = new IntVec3(x, 0, z);
                if (cell.Fogged(map) || !cell.Standable(map)) continue;
                var vehicleEnd = RadioEndpoint.BuiltIn(template!, cell, templateRadio!);
                foreach (var end in ends)
                {
                    if (LinkBudgetService.Evaluate(map, vehicleEnd, end, noise, rules, metresPerCell) is { InTouch: true }) return RemoteCoverRule.Covered;
                }
                return RemoteCoverRule.Uncovered;
            }
        }
        return RemoteCoverRule.NoSample;
    }

    /// <summary>
    /// The cover map's cost on this colony: one whole pass at once for the first selected vehicle with a radio, timed (the radio's
    /// performance rule: measured, never assumed). It pauses the game for as long as the pass takes, and leaves the map it uses.
    /// </summary>
    [DebugAction("Phobos Radio", "Time radio cover for selected vehicle", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
    private static void TimeWholePass()
    {
        Pawn? vehicle = null;
        foreach (var selected in Find.Selector.SelectedObjects)
        {
            if (selected is VehiclePawn pawn && pawn.GetComp<CompRadio>() != null) vehicle = pawn;
        }
        if (vehicle == null)
        {
            Log.Message("[PhobosRadio] Select a vehicle with a control radio first (the dev action \"Fit a control radio\" gives one).");
            return;
        }
        Want(vehicle);
        var cover = byMap.GetValue(vehicle.Map, map => new RemoteCoverMap(map));
        var rules = RadioRulesDef.Current;
        var clock = Stopwatch.StartNew();
        cover.Start(rules, cover.Signature(rules));
        cover.Work(rules, float.MaxValue);
        clock.Stop();
        var blocks = cover.published?.Blocks ?? System.Array.Empty<byte>();
        var samples = 0;
        foreach (var b in blocks) if (b != RemoteCoverRule.NoSample) samples++;
        var budget = rules.remoteCoverBudgetMsPerTick;
        Log.Message($"[PhobosRadio] Radio cover for {vehicle.LabelShort} ({vehicle.Map.Size.x} x {vehicle.Map.Size.z} map, blocks of {cover.block}): " +
                    $"{samples} samples through {cover.ends.Count} antenna(s) in {clock.Elapsed.TotalMilliseconds:0.0} ms " +
                    $"({(samples == 0 ? 0 : clock.Elapsed.TotalMilliseconds * 1000 / samples):0.0} µs a sample); " +
                    $"{RemoteCoverRule.UncoveredCount(blocks)} blocks out of cover; about {clock.Elapsed.TotalMilliseconds / budget:0} ticks at {budget} ms a tick.");
    }
}
