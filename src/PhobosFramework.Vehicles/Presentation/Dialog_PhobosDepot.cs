using System.Collections.Generic;
using System.Linq;
using Phobos.Framework.Interface;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Vehicles;
using Vehicles.World;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// The depot window (Framework 0.199.0; docs/development/vehicle-depots.md, DP1, DP1(b) and DP2's swap): what a convoy standing on a tile
/// leaves there and takes from the depot already there. Vehicles to leave are ticked; items to leave and to take use the game's own count
/// box and arrows, as the trade window does. Presentation only: the button hands the lists to Depots.Exchange, which decides.
/// </summary>
public class Dialog_PhobosDepot : Window
{
    const float RowHeight = 30f;
    const float HeaderHeight = 28f;
    const float CountWidth = 240f;

    readonly VehicleCaravan caravan;
    readonly List<VehiclePawn> vehicles;
    readonly HashSet<VehiclePawn> leaving = new();
    readonly List<TransferableOneWay> leaveItems = new();
    readonly List<TransferableOneWay> takeItems = new();
    Vector2 scroll;
    float viewHeight = 400f;

    public override Vector2 InitialSize => new(Mathf.Min(760f, UI.screenWidth - 40f), Mathf.Min(720f, UI.screenHeight - 40f));

    public Dialog_PhobosDepot(VehicleCaravan caravan)
    {
        this.caravan = caravan;
        vehicles = Depots.Leavable(caravan);
        doCloseX = true;
        forcePause = true;
        absorbInputAroundWindow = true;
        foreach (Thing thing in CaravanInventoryUtility.AllInventoryItems(caravan)) Add(leaveItems, thing);
        if (Depots.At(caravan.Tile) is { } depot) foreach (Thing thing in depot.Items) Add(takeItems, thing);
    }

    static void Add(List<TransferableOneWay> list, Thing thing)
    {
        TransferableOneWay? same = TransferableUtility.TransferableMatching(thing, list, TransferAsOneMode.Normal);
        if (same == null)
        {
            same = new TransferableOneWay();
            list.Add(same);
        }
        same.things.Add(thing);
    }

    public override void DoWindowContents(Rect inRect)
    {
        if (UiGuard.Resting(GuardKey)) { UiGuard.Fallback(inRect); return; }
        try { Contents(inRect); }
        catch (System.Exception e) { UiGuard.Report(GuardKey, e); }
    }

    const string GuardKey = "The depot window";

    void Contents(Rect inRect)
    {
        bool hasDepot = Depots.At(caravan.Tile) != null;
        using (new TextBlock(GameFont.Medium)) Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 34f),
            (hasDepot ? "PhobosDepot_WindowTitleAt" : "PhobosDepot_WindowTitleNew").Translate(caravan.LabelShort.Named("CONVOY")));
        float top = inRect.y + 38f;
        using (new TextBlock(GameFont.Small, TextAnchor.UpperLeft, ColoredText.SubtleGrayColor))
        {
            string hint = "PhobosDepot_WindowHint".Translate().Resolve();
            float h = Text.CalcHeight(hint, inRect.width);
            Widgets.Label(new Rect(inRect.x, top, inRect.width, h), hint);
            top += h + 6f;
        }

        var bottom = new Rect(inRect.x, inRect.yMax - 36f, inRect.width, 36f);
        var outer = new Rect(inRect.x, top, inRect.width, bottom.y - top - 6f);
        var view = new Rect(0f, 0f, outer.width - 16f, viewHeight);
        Widgets.BeginScrollView(outer, ref scroll, view);
        float y = 0f;
        y = Section(y, view.width, "PhobosDepot_LeaveVehicles".Translate());
        foreach (VehiclePawn vehicle in vehicles)
        {
            var row = new Rect(0f, y, view.width, RowHeight);
            bool on = leaving.Contains(vehicle);
            bool was = on;
            Widgets.CheckboxLabeled(row.LeftPartPixels(view.width - 8f), vehicle.LabelShortCap + Riders(vehicle), ref on);
            if (on != was)
            {
                if (on) leaving.Add(vehicle);
                else leaving.Remove(vehicle);
            }
            y += RowHeight;
        }
        y = Section(y + 6f, view.width, "PhobosDepot_LeaveItems".Translate());
        y = ItemRows(y, view.width, leaveItems, "PhobosDepot_NothingToLeave");
        if (hasDepot)
        {
            y = Section(y + 6f, view.width, "PhobosDepot_TakeItems".Translate());
            y = ItemRows(y, view.width, takeItems, "PhobosDepot_NothingToTake");
            using (new TextBlock(GameFont.Tiny, TextAnchor.UpperLeft, ColoredText.SubtleGrayColor))
            {
                string note = "PhobosDepot_TakeVehiclesNote".Translate().Resolve();
                float h = Text.CalcHeight(note, view.width);
                Widgets.Label(new Rect(0f, y + 4f, view.width, h), note);
                y += h + 8f;
            }
        }
        Widgets.EndScrollView();
        viewHeight = y + 8f;

        string? refusal = Depots.LeaveRefusal(caravan) ?? Depots.VehiclesRefusal(caravan, leaving);
        bool anything = leaving.Count > 0 || leaveItems.Any(t => t.CountToTransfer > 0) || takeItems.Any(t => t.CountToTransfer > 0);
        if (refusal == null && !anything) refusal = "PhobosDepot_NothingChosen".Translate().Resolve();
        var go = new Rect(bottom.xMax - 220f, bottom.y, 220f, bottom.height);
        if (refusal != null)
        {
            using (new TextBlock(GameFont.Small, TextAnchor.MiddleLeft, PhobosWidgets.Caution)) Widgets.Label(new Rect(bottom.x, bottom.y, bottom.width - 230f, bottom.height), refusal);
        }
        if (Widgets.ButtonText(go, "PhobosDepot_Confirm".Translate(), active: refusal == null) && refusal == null)
        {
            Depots.Exchange(caravan, leaving.ToList(), leaveItems, takeItems);
            Close();
        }
    }

    static string Riders(VehiclePawn vehicle)
    {
        int aboard = vehicle.AllPawnsAboard.Count;
        return aboard == 0 ? "" : " " + "PhobosDepot_RidersWalk".Translate(aboard.Named("COUNT")).Resolve();
    }

    static float Section(float y, float width, string label)
    {
        using (new TextBlock(GameFont.Small, TextAnchor.LowerLeft)) Widgets.Label(new Rect(0f, y, width, HeaderHeight), label);
        Widgets.DrawLineHorizontal(0f, y + HeaderHeight, width);
        return y + HeaderHeight + 4f;
    }

    static float ItemRows(float y, float width, List<TransferableOneWay> items, string emptyKey)
    {
        if (items.Count == 0)
        {
            using (new TextBlock(GameFont.Small, TextAnchor.MiddleLeft, ColoredText.SubtleGrayColor)) Widgets.Label(new Rect(4f, y, width, RowHeight), emptyKey.Translate());
            return y + RowHeight;
        }
        int index = 0;
        foreach (TransferableOneWay item in items.OrderBy(t => t.Label))
        {
            var row = new Rect(0f, y, width, RowHeight);
            if (index % 2 == 1) Widgets.DrawLightHighlight(row);
            Widgets.ThingIcon(new Rect(row.x + 2f, row.y + 3f, 24f, 24f), item.AnyThing);
            using (new TextBlock(GameFont.Small, TextAnchor.MiddleLeft))
            {
                Widgets.Label(new Rect(row.x + 32f, row.y, row.width - CountWidth - 100f, row.height), item.LabelCap.Truncate(row.width - CountWidth - 100f));
                Widgets.Label(new Rect(row.xMax - CountWidth - 64f, row.y, 60f, row.height), item.MaxCount.ToStringCached());
            }
            TransferableUIUtility.DoCountAdjustInterface(new Rect(row.xMax - CountWidth, row.y, CountWidth, row.height), item, index, 0, item.MaxCount);
            y += RowHeight;
            index++;
        }
        return y;
    }
}

/// <summary>The depot buttons on a convoy (Framework 0.199.0): open the depot window, and refuel from the depot standing here. Each
/// disabled button says why. Presentation only.</summary>
public static class DepotGizmos
{
    public static IEnumerable<Gizmo> For(VehicleCaravan caravan)
    {
        PhobosSlingWreck? depot = Depots.At(caravan.Tile);
        var open = new Command_Action
        {
            defaultLabel = (depot != null ? "PhobosDepot_GizmoAt" : "PhobosDepot_GizmoNew").Translate(),
            defaultDesc = "PhobosDepot_GizmoDesc".Translate(),
            // Vehicle Framework's own picture for leaving vehicles on a tile (its Stash command; a required mod's icon, confirmed to exist).
            icon = VehicleTex.StashVehicle,
            action = () => Find.WindowStack.Add(new Dialog_PhobosDepot(caravan)),
        };
        if (Depots.LeaveRefusal(caravan) is { } refusal) open.Disable(refusal);
        yield return open;
        if (depot == null) yield break;
        var refuel = new Command_Action
        {
            defaultLabel = "PhobosDepot_RefuelGizmo".Translate(),
            defaultDesc = "PhobosDepot_RefuelGizmoDesc".Translate(Depots.Summary(depot).Named("HOLDS")),
            icon = VehicleTex.RefuelFromCargo,
            action = () => Depots.RefuelConvoy(caravan),
        };
        if (Depots.RefuelRefusal(caravan, depot) is { } none) refuel.Disable(none);
        yield return refuel;
    }
}
