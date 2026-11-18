using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

// Not "Phobos.Framework.UI": a namespace named UI would hide the game's Verse.UI class from every Phobos.Framework.* namespace.
namespace Phobos.Framework.Interface;

/// <summary>
/// One choice above a filter in <see cref="Dialog_PhobosThingFilter"/>: a button that says what is chosen now and opens a menu of the
/// options, as the game's bill window does for where a bill's products go (Framework 0.144.0).
/// </summary>
public sealed class FilterDropdown
{
    public readonly Func<string> Label;
    public readonly Func<List<FloatMenuOption>> Options;
    public readonly string? Tooltip;

    public FilterDropdown(Func<string> label, Func<List<FloatMenuOption>> options, string? tooltip = null)
    {
        Label = label;
        Options = options;
        Tooltip = tooltip;
    }

    /// <summary>The choice of one of the map's areas a pawn could be kept in, or anywhere.</summary>
    public static FilterDropdown Area(Map map, Func<Area?> get, Action<Area?> set) => new(
        () =>
        {
            Area? area = get();
            string place = area == null ? "PhobosFramework_FilterAnywhere".Translate().Resolve() : area.Label;
            return "PhobosFramework_FilterWithin".Translate(place.Named("AREA")).Resolve();
        },
        () =>
        {
            var options = new List<FloatMenuOption>
            {
                new("PhobosFramework_FilterAnywhere".Translate().CapitalizeFirst(), () => set(null)),
            };
            foreach (Area area in map.areaManager.AllAreas.Where(a => a.AssignableAsAllowed()))
            {
                Area chosen = area;
                options.Add(new FloatMenuOption(area.Label, () => set(chosen)));
            }
            return options;
        },
        "PhobosFramework_FilterWithinDesc".Translate().Resolve());
}

/// <summary>
/// A window for telling a thing of ours what it takes, with the game's own stockpile filter, and any choices the caller adds above it, such
/// as where it may go (Framework 0.143.0; docs/development/vehicle-gathering-runs.md). It changes the filter it is given as the player
/// clicks, as a stockpile's tab does; each choice goes back through the caller's own setter. Any thing that is told what to take can use it:
/// a vehicle's gathering run today, a bunker or a chute later.
/// </summary>
public sealed class Dialog_PhobosThingFilter : Window
{
    const float TitleHeight = 36f;
    const float DropdownHeight = 30f;
    const float Gap = 6f;

    readonly string title;
    readonly ThingFilter filter;
    readonly ThingFilter? parentFilter;
    readonly Map? map;
    readonly IReadOnlyList<FilterDropdown> dropdowns;
    readonly ThingFilterUI.UIState state = new();

    /// <param name="parentFilter">What may be chosen at all; null for everything.</param>
    /// <param name="map">The map, so the filter can show what is on it.</param>
    /// <param name="dropdowns">Choices shown above the filter, in order.</param>
    public Dialog_PhobosThingFilter(string title, ThingFilter filter, ThingFilter? parentFilter, Map? map = null,
        IReadOnlyList<FilterDropdown>? dropdowns = null)
    {
        this.title = title;
        this.filter = filter;
        this.parentFilter = parentFilter;
        this.map = map;
        this.dropdowns = dropdowns ?? Array.Empty<FilterDropdown>();
        doCloseX = true;
        closeOnClickedOutside = true;
        absorbInputAroundWindow = true;
    }

    public override Vector2 InitialSize => new(380f, 620f + 36f * Mathf.Max(0, dropdowns.Count - 1));

    public override void DoWindowContents(Rect inRect)
    {
        Text.Font = GameFont.Medium;
        Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width - 30f, TitleHeight), title);
        Text.Font = GameFont.Small;
        float y = inRect.y + TitleHeight + Gap;
        foreach (FilterDropdown dropdown in dropdowns)
        {
            var row = new Rect(inRect.x, y, inRect.width, DropdownHeight);
            if (Widgets.ButtonText(row, dropdown.Label()))
            {
                List<FloatMenuOption> options = dropdown.Options();
                if (options.Count > 0) Find.WindowStack.Add(new FloatMenu(options));
            }
            if (dropdown.Tooltip != null) TooltipHandler.TipRegion(row, dropdown.Tooltip);
            y = row.yMax + Gap;
        }
        var filterRect = new Rect(inRect.x, y, inRect.width, inRect.yMax - y);
        ThingFilterUI.DoThingFilterConfigWindow(filterRect, state, filter, parentFilter, map: map);
    }
}
