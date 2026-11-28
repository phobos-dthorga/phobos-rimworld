using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Phobos.Radio.UI;

/// <summary>
/// The Assign tab's "Carry radio" column (Radio 0.76.0; docs/development/radio-life-on-the-air.md, RN20): which hand-held a colonist keeps in
/// their pack, and whether they keep one, through the game's own inventory stock (its "carry medicine" rule picks it up). The game's own Carry
/// column draws its medicine group alone, so the radio's group has this column. Presents only: each choice is one call to the game's
/// stock tracker.
/// </summary>
public class PawnColumnWorker_PhobosCarryRadio : PawnColumnWorker
{
    private const float Gap = 4f;

    private static InventoryStockGroupDef? group;

    /// <summary>The radio's stock group, or null if another mod removed it (the column then draws nothing).</summary>
    public static InventoryStockGroupDef? Group => group ??= DefDatabase<InventoryStockGroupDef>.GetNamedSilentFail("PhobosRadio_Handhelds");

    public override void DoCell(Rect rect, Pawn pawn, PawnTable table)
    {
        if (pawn.inventoryStock == null || Group is not { } radios) return;
        ThingDef set = pawn.inventoryStock.GetDesiredThingForGroup(radios);
        int count = pawn.inventoryStock.GetDesiredCountForGroup(radios);
        float half = (rect.width - Gap) / 2f;
        var setRect = new Rect(rect.x, rect.y + 2f, half, rect.height - 4f);
        var countRect = new Rect(setRect.xMax + Gap, rect.y + 2f, half, rect.height - 4f);
        TooltipHandler.TipRegion(setRect, set.LabelCap);
        if (Widgets.ButtonImage(setRect.ContractedBy(Mathf.Max(0f, (setRect.width - setRect.height) / 2f), 0f), set.uiIcon))
        {
            var options = new List<FloatMenuOption>();
            foreach (ThingDef each in radios.thingDefs)
            {
                ThingDef chosen = each;
                options.Add(new FloatMenuOption(chosen.LabelCap, () => pawn.inventoryStock.SetThingForGroup(radios, chosen), chosen));
            }
            Find.WindowStack.Add(new FloatMenu(options));
        }
        if (Widgets.ButtonText(countRect, count.ToString()))
        {
            var options = new List<FloatMenuOption>();
            for (int n = radios.min; n <= radios.max; n++)
            {
                int chosen = n;
                options.Add(new FloatMenuOption(chosen.ToString(), () =>
                {
                    pawn.inventoryStock.SetCountForGroup(radios, chosen);
                    if (chosen > 0 && Seen.Wants("radio.carry-radio-set", pawn)) Seen.Note("radio.carry-radio-set", pawn, pawn.inventoryStock.GetDesiredThingForGroup(radios).label);
                }));
            }
            Find.WindowStack.Add(new FloatMenu(options));
        }
    }

    public override int GetMinWidth(PawnTable table) => Mathf.Max(base.GetMinWidth(table), 60);

    public override int GetOptimalWidth(PawnTable table) => Mathf.Clamp(80, GetMinWidth(table), GetMaxWidth(table));

    public override int Compare(Pawn a, Pawn b) => CountOf(a).CompareTo(CountOf(b));

    private static int CountOf(Pawn pawn) => pawn.inventoryStock != null && Group != null ? pawn.inventoryStock.GetDesiredCountForGroup(Group) : 0;
}
