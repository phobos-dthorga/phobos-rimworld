using System.Collections.Generic;
using System.Linq;
using Phobos.Framework.Vehicles.Rules;
using SmashTools;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>One picture of a slot: shown when <see cref="node"/> is fitted, or the stock look when no node is named.</summary>
public class NodeLook
{
    /// <summary>The upgrade node's key; null for the stock look.</summary>
    public string? node;
    public GraphicDataOverlay? look;
}

/// <summary>A place on the vehicle whose picture depends on the upgrades fitted, such as a fan, a mast or a tipping body.</summary>
public class NodeLookSlot
{
    /// <summary>A stable name for the slot, unique on the vehicle; it names the slot's overlay and is never saved.</summary>
    public string key = "";
    /// <summary>In order: the first whose node is fitted is shown, else the stock look (<see cref="NodeLookRule"/>).</summary>
    public List<NodeLook> looks = new();
}

public class CompProperties_PhobosNodeLooks : VehicleCompProperties
{
    public List<NodeLookSlot> slots = new();

    public CompProperties_PhobosNodeLooks() => compClass = typeof(CompPhobosNodeLooks);

    public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
    {
        foreach (string error in base.ConfigErrors(parentDef)) yield return error;
        foreach (IGrouping<string, NodeLookSlot> twin in slots.GroupBy(s => s.key).Where(g => g.Count() > 1))
            yield return $"CompProperties_PhobosNodeLooks: slot key \"{twin.Key}\" is used {twin.Count()} times; each slot needs its own key";
    }
}

/// <summary>
/// Draws each slot's picture over the body, chosen by the upgrades fitted, and changes it when an upgrade is fitted or removed (owner, 12 Nov
/// 2026: the generator set's stock fan gives way to the heavy-duty fan). Presentation only: it reads the upgrade tree and changes nothing
/// else. Vehicle Framework's own node overlays can only add a picture, never take the stock one away, which is why this exists. Nothing is
/// saved: the pictures are set again from the fitted upgrades at every spawn.
/// </summary>
public class CompPhobosNodeLooks : VehicleComp
{
    const string KeyPrefix = "PhobosNodeLook_";
    bool registered;

    CompProperties_PhobosNodeLooks Props => (CompProperties_PhobosNodeLooks)props;

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        LongEventHandler.ExecuteWhenFinished(Refresh);
    }

    public override void EventRegistration()
    {
        base.EventRegistration();
        if (registered) return;
        registered = true;
        Vehicle.AddEvent(VehicleEventDefOf.UpgradeCompleted, Refresh);
        Vehicle.AddEvent(VehicleEventDefOf.UpgradeRefundCompleted, Refresh);
    }

    public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
    {
        GraphicOverlayRenderer? renderer = Vehicle.DrawTracker?.overlayRenderer;
        if (renderer != null)
        {
            foreach (NodeLookSlot slot in Props.slots) renderer.RemoveOverlays(KeyPrefix + slot.key);
        }
        base.PostDeSpawn(map, mode);
    }

    /// <summary>Shows each slot's chosen picture, and only that one.</summary>
    void Refresh()
    {
        GraphicOverlayRenderer? renderer = Vehicle.DrawTracker?.overlayRenderer;
        if (renderer == null) return;
        foreach (NodeLookSlot slot in Props.slots)
        {
            string key = KeyPrefix + slot.key;
            renderer.RemoveOverlays(key);
            if (!Vehicle.Spawned) continue;
            int chosen = NodeLookRule.Choose(slot.looks.Select(l => l.node).ToList(), node => VehicleUpgrades.Unlocked(Vehicle, node));
            GraphicDataOverlay? look = chosen < 0 ? null : slot.looks[chosen].look;
            if (look != null) renderer.AddOverlay(key, GraphicOverlay.Create(look, Vehicle));
        }
    }
}
