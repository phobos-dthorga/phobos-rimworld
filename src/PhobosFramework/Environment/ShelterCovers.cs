using Phobos.Framework.Rules;
using RimWorld;
using Verse;

namespace Phobos.Framework.Environment;

/// <summary>
/// Each person's cover from a blast's light, heat and dose (moved from D'thorga Patches' strike, Framework 0.358.0, so the strike and every
/// radiation burst judge it the same way; docs/development/radiation-bursts.md, nuclear-strike-effects.md NK12 and NK17): the roof over
/// them and, under a built one, whether their room is walled mostly with stone. Judge it before any roof falls.
/// </summary>
public static class ShelterCovers
{
    /// <summary>Rooms larger than this are not one building (agent's figure, as the strike had it).</summary>
    public const int MaxRoomCells = 400;

    /// <summary>The cover over this person where they stand.</summary>
    public static ShelterCover Of(Pawn pawn)
    {
        Map? map = pawn.MapHeld;
        if (map == null || !pawn.PositionHeld.InBounds(map)) return ShelterCover.Open;
        RoofDef? roof = map.roofGrid.RoofAt(pawn.PositionHeld);
        bool natural = roof?.isNatural ?? false;
        return RadiationShelterRule.Cover(roof != null, natural, roof?.isThickRoof ?? false, roof != null && !natural && StoneWalled(pawn, map));
    }

    /// <summary>Whether a person's room is walled mostly with stone: stony stuff, or natural or smoothed rock. Rooms open to the sky or
    /// too large to be one building count as not.</summary>
    public static bool StoneWalled(Pawn pawn, Map map)
    {
        Room? room = pawn.GetRoom();
        if (room == null || room.PsychologicallyOutdoors || room.CellCount > MaxRoomCells) return false;
        int walls = 0, stone = 0;
        foreach (IntVec3 cell in room.BorderCells)
        {
            if (!cell.InBounds(map) || cell.GetEdifice(map) is not { } edifice) continue;
            walls++;
            if (edifice.def.building?.isNaturalRock == true || edifice.Stuff?.stuffProps?.categories?.Contains(StuffCategoryDefOf.Stony) == true) stone++;
        }
        return walls > 0 && stone * 2 >= walls;
    }
}
