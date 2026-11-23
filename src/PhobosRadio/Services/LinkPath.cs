using System.Collections.Generic;
using System.Runtime.CompilerServices;
using RimWorld;
using Verse;

namespace Phobos.Radio.Services;

/// <summary>
/// The way one contact crosses the map, for drawing (Radio 0.3.0, the radio visuals record's RV2 and RV4): the straight line from the
/// hand-held to the station's antenna, everything it met, and the roof over each end. It is worked out by the same walk that decided
/// the contact, once for each contact the net hands out (about once a second), and only when something draws it.
/// </summary>
public sealed class LinkPath
{
    private static readonly ConditionalWeakTable<LinkEvaluation, LinkPath> byLink = new();

    public IntVec3 From { get; }
    public IntVec3 To { get; }
    public float HeightFrom { get; }
    public float HeightTo { get; }
    public BandDef Band { get; }
    public double DistanceMetres { get; }
    public IReadOnlyList<CrossingMark> Marks { get; }
    /// <summary>Runs of the same kind of thing next to each other on the line: what the Engineer labels name.</summary>
    public IReadOnlyList<CrossingRun> Runs { get; }
    public float RoofFromDb { get; }
    public bool RoofFromNatural { get; }
    public float RoofToDb { get; }
    public bool RoofToNatural { get; }
    /// <summary>
    /// Where the line passes wall height, from 0 at the hand-held to 1 at the antenna, or null when it never does. Walls count only on
    /// the side of it where the line is below wall height.
    /// </summary>
    public float? WallHeightCrossing { get; }
    public float WallHeightMetres { get; }

    private LinkPath(Map map, LinkEvaluation link, RadioRulesDef rules)
    {
        From = link.A.Cell;
        To = link.B.Cell;
        HeightFrom = link.A.AntennaHeightMetres;
        HeightTo = link.B.AntennaHeightMetres;
        Band = link.Band;
        DistanceMetres = link.DistanceMetres;
        WallHeightMetres = rules.wallHeightMetres;
        var marks = new List<CrossingMark>();
        ObstructionService.Walk(map, From, HeightFrom, To, HeightTo, rules, marks: marks);
        Marks = marks;
        Runs = CrossingRun.Group(map, marks, Band);
        RoofFromDb = ObstructionService.RoofLossDb(map, From, rules);
        RoofFromNatural = ObstructionService.RoofIsNatural(map, From);
        RoofToDb = ObstructionService.RoofLossDb(map, To, rules);
        RoofToNatural = ObstructionService.RoofIsNatural(map, To);
        var rise = HeightTo - HeightFrom;
        if (rise != 0)
        {
            var crossing = (WallHeightMetres - HeightFrom) / rise;
            if (crossing > 0f && crossing < 1f) WallHeightCrossing = crossing;
        }
    }

    /// <summary>The path of a contact, worked out the first time it is asked for and kept as long as the contact is.</summary>
    public static LinkPath For(Map map, LinkEvaluation link) =>
        byLink.GetValue(link, l => new LinkPath(map, l, RadioRulesDef.Current));

    /// <summary>What a roof costs on this contact's band: a built roof by the band's factor, a natural one in full.</summary>
    public double RoofCostDb(float lossDb, bool natural) => natural ? lossDb : lossDb * Band.obstructionFactor;

    /// <summary>The map's straight line is below wall height at this fraction from the hand-held.</summary>
    public bool BelowWallHeight(float fraction) =>
        Physics.Propagation.RayBlockedByWall(fraction, HeightFrom, HeightTo, WallHeightMetres);
}

/// <summary>Things of one kind next to each other on a line, with what they cost on the contact's band together.</summary>
public sealed class CrossingRun
{
    public CrossingKind Kind { get; }
    public bool IsDoor { get; }
    /// <summary>What the first of them is, in the game's words ("granite wall"), for the Engineer label.</summary>
    public string Label { get; }
    public int Count { get; }
    public float FirstFraction { get; }
    public float LastFraction { get; }
    public IntVec3 MiddleCell { get; }
    /// <summary>What they cost on the band: built things by its factor, rock in full, cleared ones nothing.</summary>
    public double CostDb { get; }

    private CrossingRun(CrossingKind kind, bool isDoor, string label, int count, float firstFraction, float lastFraction, IntVec3 middle,
        double costDb)
    {
        Kind = kind;
        IsDoor = isDoor;
        Label = label;
        Count = count;
        FirstFraction = firstFraction;
        LastFraction = lastFraction;
        MiddleCell = middle;
        CostDb = costDb;
    }

    public static List<CrossingRun> Group(Map map, List<CrossingMark> marks, BandDef band)
    {
        var runs = new List<CrossingRun>();
        var start = 0;
        for (var i = 1; i <= marks.Count; i++)
        {
            if (i < marks.Count && Continues(map, marks[i - 1], marks[i])) continue;
            var first = marks[start];
            var last = marks[i - 1];
            double cost = 0;
            for (var k = start; k < i; k++)
            {
                cost += marks[k].Kind switch
                {
                    CrossingKind.Rock => marks[k].LossDb,
                    CrossingKind.Built => marks[k].LossDb * band.obstructionFactor,
                    _ => 0,
                };
            }
            var edifice = first.Cell.GetEdifice(map);
            runs.Add(new CrossingRun(first.Kind, first.IsDoor, edifice?.LabelShort ?? string.Empty, i - start, first.Fraction,
                last.Fraction, marks[start + (i - start) / 2].Cell, cost));
            start = i;
        }
        return runs;
    }

    /// <summary>Next to each other on the line, of the same kind and the same thing.</summary>
    private static bool Continues(Map map, CrossingMark previous, CrossingMark next) =>
        next.Step == previous.Step + 1 && next.Kind == previous.Kind && next.IsDoor == previous.IsDoor
        && next.Cell.GetEdifice(map)?.def == previous.Cell.GetEdifice(map)?.def
        && next.Cell.GetEdifice(map)?.Stuff == previous.Cell.GetEdifice(map)?.Stuff;
}
