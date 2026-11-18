using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

// Not "Phobos.Framework.UI": a namespace named UI would hide the game's Verse.UI class from every Phobos.Framework.* namespace.
namespace Phobos.Framework.Interface;

/// <summary>
/// A filter said in a few words, for a button or a message: "chunks and raw resources" (Framework 0.143.0). The game's own summary says
/// "usable ingredients" for any filter of more than one kind. A category whose every kind is allowed is named once; a partly allowed one is
/// opened. Walks the category tree, so the caller keeps the words and asks again only when the filter changes.
/// </summary>
public static class ThingFilterWords
{
    /// <param name="parent">What may be chosen at all; kinds outside it are not counted. Null for everything.</param>
    /// <param name="most">How many names before the rest are counted instead.</param>
    public static string Summary(ThingFilter filter, ThingFilter? parent, int most = 4)
    {
        ThingCategoryDef root = parent?.DisplayRootCategory?.catDef ?? ThingCategoryDefOf.Root;
        var names = new List<string>();
        if (Covers(root, filter, parent, out bool any) && any) return "PhobosFramework_FilterEverything".Translate().Resolve();
        Walk(root, filter, parent, names);
        if (names.Count == 0) return "PhobosFramework_FilterNothing".Translate().Resolve();
        if (names.Count <= most) return names.ToCommaList(useAnd: true);
        int shown = System.Math.Max(1, most - 1);
        return "PhobosFramework_FilterSomeAndMore".Translate(names.Take(shown).ToCommaList().Named("NAMES"), (names.Count - shown).Named("COUNT")).Resolve();
    }

    /// <summary>Whether every kind under the category that may be chosen is allowed; <paramref name="any"/> whether at least one is.</summary>
    static bool Covers(ThingCategoryDef category, ThingFilter filter, ThingFilter? parent, out bool any)
    {
        any = false;
        bool all = true;
        foreach (ThingDef def in category.DescendantThingDefs)
        {
            if (parent != null && !parent.Allows(def)) continue;
            if (filter.Allows(def)) any = true;
            else all = false;
            if (any && !all) return false;
        }
        return all;
    }

    static void Walk(ThingCategoryDef category, ThingFilter filter, ThingFilter? parent, List<string> names)
    {
        foreach (ThingCategoryDef child in category.childCategories)
        {
            bool all = Covers(child, filter, parent, out bool any);
            if (!any) continue;
            if (all) names.Add(child.label);
            else Walk(child, filter, parent, names);
        }
        foreach (ThingDef def in category.childThingDefs)
        {
            if ((parent == null || parent.Allows(def)) && filter.Allows(def)) names.Add(def.label);
        }
    }
}
