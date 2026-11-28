using System.Linq;
using Phobos.Framework.Persistence;
using Phobos.Radio.Physics;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Phobos.Radio.Services;

/// <summary>
/// Habits from hours on the air (Radio 0.76.0; docs/development/radio-life-on-the-air.md, RN10): each over a colonist talks or answers in
/// adds to their hours on the air (the Framework's practice record), and at the rules' hours they go by a callsign of their own, worked out
/// as the colony's QSL callsigns are and never saved.
/// <para>Cost: one addition to the practice record when an over ends; a callsign is worked out only when shown.</para>
/// </summary>
public static class RadioOperators
{
    /// <summary>The kind of practice hours on the air are counted under in the Framework's record.</summary>
    public const string PracticeKind = "radio/air";

    public static float Hours(Pawn pawn) => Practice.Hours(pawn, PracticeKind);

    /// <summary>The share of a practised voice's gain these hours give, 0 to just under 1 (the Framework's rule).</summary>
    public static float Share(Pawn pawn) => Practice.Share(pawn, PracticeKind);

    /// <summary>An over has ended at <paramref name="endTick"/>: its length counts for whoever talked and answered in it.</summary>
    public static void OverEnded(Over over, int endTick)
    {
        int ticks = endTick - over.Start;
        if (ticks <= 0) return;
        float hours = ticks / (float)GenDate.TicksPerHour;
        Credit(over.Starter, hours);
        if (over.Other != null) Credit(over.Other, hours);
    }

    private static void Credit(Pawn pawn, float hours)
    {
        if (pawn.Dead || !pawn.RaceProps.Humanlike || pawn.Faction != Faction.OfPlayer) return;
        float before = Hours(pawn);
        Practice.Add(pawn, PracticeKind, hours);
        float threshold = RadioRulesDef.Current.operatorCallsignHours;
        if (before >= threshold || before + hours < threshold || CallsignOf(pawn) is not { } call) return;
        RadioLog.Add(RadioLogKind.Talk, "PhobosRadioTalk_CallsignEarned".Translate(pawn.LabelShort.Named("NAME"),
            ((int)threshold).Named("HOURS"), call.Named("CALLSIGN")).Resolve());
        if (Seen.Wants("radio.operator-callsign", pawn)) Seen.Note("radio.operator-callsign", pawn, call);
    }

    /// <summary>
    /// A colonist's own callsign once they have the rules' hours on the air, else null: the colony's place prefix and call area, and letters
    /// from the colonist, as a QSL card's callsign is made. It follows the colony's first home [A], so it is the same each time it is shown.
    /// </summary>
    public static string? CallsignOf(Pawn pawn)
    {
        if (Hours(pawn) < RadioRulesDef.Current.operatorCallsignHours) return null;
        Map? home = Find.Maps.Where(map => map.IsPlayerHome).OrderBy(map => map.uniqueID).FirstOrDefault();
        PlanetTile tile = home?.Tile ?? pawn.Tile;
        if (!tile.Valid) return null;
        return QslRule.Callsign(QslCalls.PlacePrefixOf(tile), Find.WorldGrid.LongLatOf(tile).y, pawn.thingIDNumber);
    }

    /// <summary>A colonist's short name, with their callsign once they have one.</summary>
    public static string Name(Pawn pawn) =>
        CallsignOf(pawn) is { } call ? "PhobosRadioTalk_NameWithCall".Translate(pawn.LabelShort.Named("NAME"), call.Named("CALLSIGN")).Resolve()
        : pawn.LabelShort;
}
