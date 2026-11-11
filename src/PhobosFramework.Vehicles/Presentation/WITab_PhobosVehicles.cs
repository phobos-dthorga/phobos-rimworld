using System.Collections.Generic;
using System.Linq;
using Phobos.Framework;
using Phobos.Framework.Interface;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Vehicles;
using Vehicles.World;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// The Dashboard and Systems tabs on the world map (owner, 8 Nov 2026; docs/development/vehicle-tabs-design.md), for a vehicle caravan
/// and an aircraft in flight: the systems keep running off the map. With several vehicles, a list down the left (a light for each:
/// green, amber when something is notable, red when it will not move) chooses which one the tab shows; one vehicle needs no list. The
/// drawing is the map tabs' own (their static Draw), so the two cannot drift apart. Added to Vehicle Framework's world object Defs by
/// an XML patch. Presentation only.
/// </summary>
public abstract class WITab_PhobosVehicles : WITab
{
    const float Margin = 10f;
    const float ListWidth = 160f;
    const float Row = 26f;

    sealed class Selection
    {
        public int VehicleId = -1;
        public Vector2 Scroll;
    }

    /// <summary>The chosen vehicle per world object (by its ID); cleared when the tab closes.</summary>
    static readonly TabState<Selection> selections = new();

    /// <summary>The vehicles with systems in a caravan or an aircraft in flight, in their order.</summary>
    public static List<CompPhobosVehicleSystems> Comps(WorldObject? selected)
    {
        var comps = new List<CompPhobosVehicleSystems>();
        IEnumerable<VehiclePawn> vehicles = selected switch
        {
            VehicleCaravan caravan => caravan.VehiclesListForReading,
            AerialVehicleInFlight aircraft when aircraft.Vehicle != null => new[] { aircraft.Vehicle },
            IPhobosVehicleHolder holder => holder.HeldVehicles,
            _ => Enumerable.Empty<VehiclePawn>(),
        };
        // A slung vehicle is held inside its helicopter, so it is listed after it (Framework 0.99.0).
        foreach (VehiclePawn vehicle in SlingLoads.WithLoads(vehicles))
        {
            CompPhobosVehicleSystems? comp = vehicle.GetComp<CompPhobosVehicleSystems>();
            if (comp != null && comp.Usable) comps.Add(comp);
        }
        return comps;
    }

    public override bool IsVisible => Comps(SelObject).Count > 0;

    protected override void FillTab()
    {
        List<CompPhobosVehicleSystems> comps = Comps(SelObject);
        if (comps.Count == 0 || SelObject == null) return;
        var outer = new Rect(0f, 0f, size.x, size.y).ContractedBy(Margin);
        CompPhobosVehicleSystems shown = comps[0];
        if (comps.Count > 1)
        {
            Selection selection = selections.Get(SelObject.ID);
            shown = comps.FirstOrDefault(c => c.Vehicle.thingIDNumber == selection.VehicleId) ?? comps[0];
            DrawList(new Rect(outer.x, outer.y, ListWidth, outer.height), comps, shown, selection);
            outer.xMin += ListWidth + 8f;
        }
        DrawVehicle(shown, outer);
    }

    protected abstract void DrawVehicle(CompPhobosVehicleSystems comp, Rect rect);

    static void DrawList(Rect rect, List<CompPhobosVehicleSystems> comps, CompPhobosVehicleSystems shown, Selection selection)
    {
        var view = new Rect(0f, 0f, rect.width - 16f, comps.Count * Row);
        Widgets.BeginScrollView(rect, ref selection.Scroll, view);
        float y = 0f;
        foreach (CompPhobosVehicleSystems comp in comps)
        {
            var row = new Rect(0f, y, view.width, Row);
            if (comp == shown) Widgets.DrawHighlightSelected(row);
            else if (Mouse.IsOver(row)) Widgets.DrawHighlight(row);
            var light = new Rect(4f, y + (Row - 12f) / 2f, 12f, 12f);
            Widgets.DrawBoxSolid(light, Light(comp));
            Widgets.DrawBox(light);
            using (new TextBlock(TextAnchor.MiddleLeft))
            {
                Widgets.Label(new Rect(22f, y, row.width - 22f, Row), comp.Vehicle.LabelShort.Truncate(row.width - 26f));
            }
            string? waiting = InspectText.WaitingLine(comp);
            TooltipHandler.TipRegion(row, comp.Vehicle.LabelCap + (waiting != null ? "\n" + waiting : ""));
            if (Widgets.ButtonInvisible(row)) selection.VehicleId = comp.Vehicle.thingIDNumber;
            y += Row;
        }
        Widgets.EndScrollView();
    }

    /// <summary>Red when the vehicle will not move or a stage refuses or damages; amber when any stage is notable; green otherwise.</summary>
    public static Color Light(CompPhobosVehicleSystems comp)
    {
        if (comp.MoveRefusal != null) return PhobosWidgets.Bad;
        Color worst = PhobosWidgets.Good;
        foreach (SystemState state in comp.States)
        {
            if (!comp.IsEnabled(state)) continue;
            for (int q = 0; q < state.values.Length; q++)
            {
                Color colour = ITab_VehicleDashboard.StageColour(state.CurrentStage(q));
                if (colour == PhobosWidgets.Bad) return PhobosWidgets.Bad;
                if (colour == PhobosWidgets.Caution) worst = PhobosWidgets.Caution;
            }
        }
        return worst;
    }

    protected override void CloseTab()
    {
        base.CloseTab();
        selections.Clear();
    }
}

public class WITab_PhobosVehicleDashboard : WITab_PhobosVehicles
{
    public WITab_PhobosVehicleDashboard()
    {
        size = new Vector2(630f, 318f);
        labelKey = "PhobosVehicleSystems_TabDashboard";
    }

    protected override void DrawVehicle(CompPhobosVehicleSystems comp, Rect rect) => ITab_VehicleDashboard.Draw(comp, rect, onWorld: true);
}

public class WITab_PhobosVehicleSystems : WITab_PhobosVehicles
{
    public WITab_PhobosVehicleSystems()
    {
        size = new Vector2(730f, 450f);
        labelKey = "PhobosVehicleSystems_TabSystems";
    }

    public override bool IsVisible => VehicleSystemsMod.Settings.displayMode != DisplayMode.Plain && base.IsVisible;

    protected override void DrawVehicle(CompPhobosVehicleSystems comp, Rect rect) => ITab_VehicleSystems.Draw(comp, rect, onWorld: true);

    protected override void CloseTab()
    {
        base.CloseTab();
        ITab_VehicleSystems.ClearState();
    }
}

public class WITab_PhobosVehicleHealth : WITab_PhobosVehicles
{
    public WITab_PhobosVehicleHealth()
    {
        size = new Vector2(1080f, 430f);
        labelKey = "PhobosVehicleSystems_TabHealth";
    }

    protected override void DrawVehicle(CompPhobosVehicleSystems comp, Rect rect) => ITab_PhobosVehicleHealth.Draw(comp, rect, onWorld: true);

    protected override void CloseTab()
    {
        base.CloseTab();
        ITab_PhobosVehicleHealth.ClearState();
    }
}
