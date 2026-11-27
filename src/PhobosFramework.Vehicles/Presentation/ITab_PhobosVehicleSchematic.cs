using Phobos.Framework;
using Phobos.Framework.Interface;
using Phobos.Framework.Rules;
using RimWorld;
using UnityEngine;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// The vehicle's Schematic tab (Framework 0.378.0; the owner's card of 28 Nov 2026, "Its own vehicle tab"): a small live copy of its systems
/// schematic beside the Dashboard, Systems and Health tabs, drawn by the same <see cref="SchematicPanel"/> as the window, with a button that
/// opens the window. Shown where the vehicle has a page (its kind's, on a player's vehicle followed in full, in Operator or Engineer mode).
/// Presentation only.
/// </summary>
public class ITab_PhobosVehicleSchematic : ITab
{
    const float Margin = 10f;
    const float TitleHeight = 28f;
    const float ButtonWidth = 150f;
    const string GuardKey = "The vehicle Schematic tab";

    /// <summary>The panel for the vehicle last shown; built again when the selection changes, so its view never shows another vehicle.</summary>
    SchematicPanel? panel;

    public ITab_PhobosVehicleSchematic()
    {
        size = new Vector2(600f, 400f);
        labelKey = "PhobosVehicleSystems_TabSchematic";
    }

    static CompPhobosVehicleSystems? CompOf(Thing? thing) => thing is VehiclePawn vehicle ? vehicle.GetComp<CompPhobosVehicleSystems>() : null;

    public override bool IsVisible => CompOf(SelThing) is { Usable: true } comp && Window_PhobosVehicleSchematic.PageFor(comp) != null;

    protected override void CloseTab()
    {
        base.CloseTab();
        panel?.ClearHighlight();
    }

    protected override void FillTab()
    {
        Rect inner = new Rect(0f, 0f, size.x, size.y).ContractedBy(Margin);
        if (UiGuard.Resting(GuardKey)) { UiGuard.Fallback(inner); return; }
        try { DrawInner(inner); }
        catch (System.Exception e) { UiGuard.Report(GuardKey, e); }
    }

    void DrawInner(Rect inner)
    {
        CompPhobosVehicleSystems? comp = CompOf(SelThing);
        if (comp == null || !comp.Usable || Window_PhobosVehicleSchematic.PageFor(comp) is not { } page) return;
        if (panel == null || panel.Comp != comp || panel.Page != page)
        {
            panel?.ClearHighlight();
            panel = new SchematicPanel(comp, page);
            if (Seen.Wants("vehicles.schematic.tab", comp.Vehicle)) Seen.Note("vehicles.schematic.tab", comp.Vehicle, page.defName);
        }
        var title = new Rect(inner.x, inner.y, inner.width, TitleHeight);
        bool open = Window_PhobosVehicleSchematic.IsOpenFor(comp);
        var button = new Rect(title.xMax - ButtonWidth, title.y + 2f, ButtonWidth, TitleHeight - 4f);
        if (Widgets.ButtonText(button, (open ? "PhobosVehicleSystems_SchematicTabClose" : "PhobosVehicleSystems_SchematicTabOpen").Translate()))
        {
            Window_PhobosVehicleSchematic.Toggle(comp);
        }
        Window_PhobosVehicleSchematic.DrawRunning(new Rect(title.x, title.y, title.width - ButtonWidth - 10f, TitleHeight), panel.Running);
        panel.Draw(new Rect(inner.x, inner.y + TitleHeight + 4f, inner.width, inner.height - TitleHeight - 4f), compact: true);
    }
}
