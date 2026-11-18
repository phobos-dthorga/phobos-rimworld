using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

// Not "Phobos.Framework.UI": a namespace named UI would hide the game's Verse.UI class from every Phobos.Framework.* namespace.
namespace Phobos.Framework.Interface;

/// <summary>
/// A window for telling a thing of ours what it takes, with the game's own stockpile filter, and where, if the caller asks: one of the map's
/// areas or anywhere (Framework 0.143.0; docs/development/vehicle-gathering-runs.md). It changes the filter it is given as the player clicks,
/// as a stockpile's tab does; the area goes back through the caller's setter. Any thing that is told what to take can use it: a vehicle's
/// gathering run today, a bunker or a chute later.
/// </summary>
public sealed class Dialog_PhobosThingFilter : Window
{
    const float TitleHeight = 36f;
    const float AreaButtonHeight = 30f;
    const float Gap = 6f;

    readonly string title;
    readonly ThingFilter filter;
    readonly ThingFilter? parentFilter;
    readonly Map? map;
    readonly Func<Area?>? getArea;
    readonly Action<Area?>? setArea;
    readonly ThingFilterUI.UIState state = new();

    /// <param name="parentFilter">What may be chosen at all; null for everything.</param>
    /// <param name="map">The map whose areas are offered; also lets the filter show what is on it.</param>
    /// <param name="getArea">The area chosen now, or null for anywhere. With <paramref name="setArea"/>, offers the area choice.</param>
    public Dialog_PhobosThingFilter(string title, ThingFilter filter, ThingFilter? parentFilter, Map? map = null, Func<Area?>? getArea = null,
        Action<Area?>? setArea = null)
    {
        this.title = title;
        this.filter = filter;
        this.parentFilter = parentFilter;
        this.map = map;
        this.getArea = getArea;
        this.setArea = setArea;
        doCloseX = true;
        closeOnClickedOutside = true;
        absorbInputAroundWindow = true;
    }

    public override Vector2 InitialSize => new(380f, 620f);

    bool OffersArea => map != null && getArea != null && setArea != null;

    public override void DoWindowContents(Rect inRect)
    {
        Text.Font = GameFont.Medium;
        Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width - 30f, TitleHeight), title);
        Text.Font = GameFont.Small;
        float y = inRect.y + TitleHeight + Gap;
        if (OffersArea)
        {
            var areaRect = new Rect(inRect.x, y, inRect.width, AreaButtonHeight);
            Area? area = getArea!();
            string place = area == null ? "PhobosFramework_FilterAnywhere".Translate().Resolve() : area.Label;
            if (Widgets.ButtonText(areaRect, "PhobosFramework_FilterWithin".Translate(place.Named("AREA")).Resolve()))
            {
                Find.WindowStack.Add(new FloatMenu(AreaOptions()));
            }
            TooltipHandler.TipRegion(areaRect, "PhobosFramework_FilterWithinDesc".Translate().Resolve());
            y = areaRect.yMax + Gap;
        }
        var filterRect = new Rect(inRect.x, y, inRect.width, inRect.yMax - y);
        ThingFilterUI.DoThingFilterConfigWindow(filterRect, state, filter, parentFilter, map: map);
    }

    /// <summary>Anywhere, then each of the map's areas a pawn could be kept in, by the game's own test.</summary>
    List<FloatMenuOption> AreaOptions()
    {
        var options = new List<FloatMenuOption>
        {
            new("PhobosFramework_FilterAnywhere".Translate().CapitalizeFirst(), () => setArea!(null)),
        };
        foreach (Area area in map!.areaManager.AllAreas.Where(a => a.AssignableAsAllowed()))
        {
            Area chosen = area;
            options.Add(new FloatMenuOption(area.Label, () => setArea!(chosen)));
        }
        return options;
    }
}
