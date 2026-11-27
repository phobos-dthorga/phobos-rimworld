using System.Linq;
using Phobos.Framework;
using Phobos.Framework.Interface;
using Phobos.Framework.Rules;
using RimWorld;
using UnityEngine;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// The live systems schematic in its own window (Framework 0.363.0; docs/development/vehicle-schematic.md, the owner's answers of 28 Nov
/// 2026: a first slice, its own window, the flow sheet first, flow marks a preference that starts off): the vehicle's name, whether its
/// engine runs, and the page, drawn by a <see cref="SchematicPanel"/> as the vehicle's Schematic tab draws it. Opened by the command
/// button, the Dashboard's button, the Schematic tab and, since Framework 0.378.0, a warning lamp, which outlines the part it names.
/// Presentation only.
/// </summary>
public class Window_PhobosVehicleSchematic : Window
{
    const float TitleHeight = 30f;
    const float Gap = 8f;
    const string GuardKey = "The vehicle systems schematic";

    readonly SchematicPanel panel;

    public override Vector2 InitialSize => new(820f, 520f);

    public Window_PhobosVehicleSchematic(CompPhobosVehicleSystems comp, PhobosVehicleSchematicDef page)
    {
        panel = new SchematicPanel(comp, page);
        doCloseX = true;
        draggable = true;
        resizeable = true;
        preventCameraMotion = false;
        absorbInputAroundWindow = false;
        closeOnClickedOutside = false;
    }

    /// <summary>The page this vehicle's buttons open, or null for none: a page for its kind, on a player's vehicle followed in full, in the
    /// Operator or Engineer display mode (as the Dashboard's button always was).</summary>
    public static PhobosVehicleSchematicDef? PageFor(CompPhobosVehicleSystems comp) =>
        comp.Tier == DetailTier.Full && comp.Vehicle.Faction == Faction.OfPlayer && VehicleSystemsMod.Settings.displayMode != DisplayMode.Plain
            ? PhobosVehicleSchematicDef.For(comp)
            : null;

    static Window_PhobosVehicleSchematic? OpenFor(CompPhobosVehicleSystems comp)
    {
        foreach (Window window in Find.WindowStack.Windows)
        {
            if (window is Window_PhobosVehicleSchematic page && page.panel.Comp == comp) return page;
        }
        return null;
    }

    /// <summary>Whether this vehicle's page is open, for its buttons' light.</summary>
    public static bool IsOpenFor(CompPhobosVehicleSystems comp) => OpenFor(comp) != null;

    /// <summary>The buttons' press: opens this vehicle's page, or closes it when it is open.</summary>
    public static void Toggle(CompPhobosVehicleSystems comp)
    {
        Window_PhobosVehicleSchematic? open = OpenFor(comp);
        if (open != null)
        {
            open.Close();
            return;
        }
        Open(comp);
    }

    /// <summary>Opens this vehicle's page, or brings the open one forward; null where it has none.</summary>
    public static Window_PhobosVehicleSchematic? Open(CompPhobosVehicleSystems comp)
    {
        if (OpenFor(comp) is { } open)
        {
            Find.WindowStack.Notify_ClickedInsideWindow(open);
            return open;
        }
        if (PageFor(comp) is not { } page) return null;
        var window = new Window_PhobosVehicleSchematic(comp, page);
        Find.WindowStack.Add(window);
        Seen.Note("vehicles.schematic.opened", comp.Vehicle, page.defName);
        return window;
    }

    /// <summary>
    /// The node on this vehicle's page that shows a quantity of a system, by its key, or null (Framework 0.378.0): what a warning lamp for
    /// that quantity opens the page at.
    /// </summary>
    public static string? NodeShowing(CompPhobosVehicleSystems comp, PhobosVehicleSystemDef system, string quantity) =>
        PageFor(comp)?.nodes.FirstOrDefault(n => n.system == system && n.quantity == quantity)?.key;

    /// <summary>A warning lamp's press (Framework 0.378.0; the owner's card of 28 Nov 2026): the page, with the part it names outlined.</summary>
    public static void OpenAt(CompPhobosVehicleSystems comp, string node)
    {
        if (Open(comp) is not { } window) return;
        window.panel.Focus(node);
        if (Seen.Wants("vehicles.schematic.from-lamp", comp.Vehicle)) Seen.Note("vehicles.schematic.from-lamp", comp.Vehicle, node);
    }

    public override void PostClose()
    {
        base.PostClose();
        panel.ClearHighlight();
    }

    public override void DoWindowContents(Rect inRect)
    {
        if (UiGuard.Resting(GuardKey)) { UiGuard.Fallback(inRect); return; }
        try { DrawInner(inRect); }
        catch (System.Exception e) { UiGuard.Report(GuardKey, e); }
    }

    void DrawInner(Rect inRect)
    {
        CompPhobosVehicleSystems comp = panel.Comp;
        if (comp.Vehicle == null || comp.Vehicle.Destroyed || !comp.Usable)
        {
            Close();
            return;
        }
        using (new TextBlock(GameFont.Medium, TextAnchor.MiddleLeft))
        {
            Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, TitleHeight),
                "PhobosVehicleSystems_SchematicTitle".Translate(comp.Vehicle.LabelShortCap.Named("VEHICLE")));
        }
        DrawRunning(new Rect(inRect.x, inRect.y, inRect.width - 24f, TitleHeight), panel.Running);
        panel.Draw(new Rect(inRect.x, inRect.y + TitleHeight + Gap, inRect.width, inRect.height - TitleHeight - Gap), compact: false);
    }

    /// <summary>"Running" or "engine off: nothing flows", at the right of a title row.</summary>
    public static void DrawRunning(Rect row, bool running)
    {
        using (new TextBlock(GameFont.Tiny, TextAnchor.MiddleRight))
        {
            GUI.color = running ? PhobosWidgets.Good : PhobosWidgets.Off;
            Widgets.Label(row, (running ? "PhobosVehicleSystems_SchematicRunning" : "PhobosVehicleSystems_SchematicEngineOff").Translate());
            GUI.color = Color.white;
        }
    }
}
