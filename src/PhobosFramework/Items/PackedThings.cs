using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace Phobos.Framework.Items;

/// <summary>
/// Supplies that may be packed buildings (Framework 0.265.0; the owner's choice of 23 Nov 2026, "genericize the code where reasonable because
/// I see it having other uses elsewhere"). A list of materials can name a building that packs up, a sleep accelerator or a vitals monitor,
/// and the thing that answers it is the packed building: a minified thing whose own Def is the game's generic one, with the building inside.
/// Every place that finds, counts, takes or gives back a supply by its Def asks here instead of comparing Defs, so a building named in a list
/// is found packed in storage, counted packed in a cargo, used up packed, and given back packed. Any other Def behaves exactly as before.
/// </summary>
public static class PackedThings
{
    /// <summary>Whether this Def is answered by a packed building rather than by things of its own Def.</summary>
    public static bool Packs(ThingDef? def) => def != null && def.Minifiable;

    /// <summary>The Def a supply counts as: a packed building's inner building, else its own.</summary>
    public static ThingDef? CountsAs(Thing? thing) => thing is MinifiedThing { InnerThing: { } inner } ? inner.def : thing?.def;

    /// <summary>Whether this thing answers a supply of this Def: of that Def, or that building packed.</summary>
    public static bool Answers(Thing? thing, ThingDef? wanted) => thing != null && wanted != null && CountsAs(thing) == wanted;

    /// <summary>How many of a supply these things hold: stacks of the Def, or packed buildings of it, one each.</summary>
    public static int Count(IEnumerable<Thing>? things, ThingDef? wanted)
    {
        if (things == null || wanted == null) return 0;
        int count = 0;
        foreach (Thing thing in things)
        {
            if (Answers(thing, wanted)) count += thing.stackCount;
        }
        return count;
    }

    /// <summary>What to ask the map's lists for, to find a supply of this Def: the packed things' group for a building, else the Def.</summary>
    public static ThingRequest Request(ThingDef wanted) =>
        Packs(wanted) ? ThingRequest.ForGroup(ThingRequestGroup.MinifiedThing) : ThingRequest.ForDef(wanted);

    /// <summary>Every thing on the map that answers a supply of this Def, from the map's own lists.</summary>
    public static IEnumerable<Thing> OnMap(Map? map, ThingDef? wanted)
    {
        if (map == null || wanted == null) return Enumerable.Empty<Thing>();
        return Packs(wanted) ? map.listerThings.ThingsInGroup(ThingRequestGroup.MinifiedThing).Where(t => Answers(t, wanted)) : map.listerThings.ThingsOfDef(wanted);
    }

    /// <summary>Uses up so many of a supply from these things, destroying what is taken. Returns how many were taken.</summary>
    public static int Take(IEnumerable<Thing>? things, ThingDef? wanted, int count)
    {
        if (things == null || wanted == null || count <= 0) return 0;
        int taken = 0;
        foreach (Thing thing in things.Where(t => Answers(t, wanted)).ToList())
        {
            if (taken >= count) break;
            int take = System.Math.Min(count - taken, thing.stackCount);
            thing.SplitOff(take).Destroy();
            taken += take;
        }
        return taken;
    }

    /// <summary>
    /// Makes a supply to give back: a building packed, made from its default material if it needs one, else a stack of the Def. A packed
    /// building is one thing; ask again for more.
    /// </summary>
    public static Thing Make(ThingDef def, int count = 1)
    {
        if (Packs(def))
        {
            Thing building = ThingMaker.MakeThing(def, def.MadeFromStuff ? GenStuff.DefaultStuffFor(def) : null);
            return building.MakeMinified();
        }
        Thing thing = ThingMaker.MakeThing(def);
        thing.stackCount = System.Math.Max(1, count);
        return thing;
    }
}
