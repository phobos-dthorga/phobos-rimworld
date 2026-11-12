using System.Collections.Generic;
using System.Linq;
using Phobos.Framework.Vehicles.Rules;
using SmashTools;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// One picture of a slot: shown when <see cref="node"/> or <see cref="fitting"/> is fitted, or the stock look when neither is named.
/// </summary>
public class NodeLook
{
    /// <summary>The upgrade node's key; null for the stock look.</summary>
    public string? node;
    /// <summary>A fitting whose picture this is, shown while it is fitted on any part (Framework 0.105.0: the recovery winch on a bumper,
    /// the auxiliary fuel tank in its cradle). Named instead of <see cref="node"/>.</summary>
    public PhobosVehicleFittingDef? fitting;
    public GraphicDataOverlay? look;

    /// <summary>Whether this is the stock look: no node and no fitting.</summary>
    public bool Stock => node == null && fitting == null;
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
        foreach (NodeLookSlot slot in slots)
        {
            if (slot.looks.Any(l => l.node != null && l.fitting != null))
                yield return $"CompProperties_PhobosNodeLooks: a look in slot \"{slot.key}\" names both a node and a fitting; name one";
        }
    }

    /// <summary>Whether any look is shown by a fitting, so the comp watches the fittings.</summary>
    public bool WatchesFittings => slots.Any(s => s.looks.Any(l => l.fitting != null));
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
    /// <summary>The fittings' change count when the pictures were last set; -1 to set them again. Never saved.</summary>
    int fittingsVersion = -1;

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

    /// <summary>A fitting is fitted or taken off by a job, with no event of Vehicle Framework's, so a slot shown by a fitting checks the
    /// fittings' change count on the rare tick: one comparison, and a refresh only when something changed.</summary>
    public override void CompTickRare()
    {
        base.CompTickRare();
        if (!Props.WatchesFittings) return;
        int version = Systems?.Fittings.Version ?? -1;
        if (version != fittingsVersion) Refresh();
    }

    public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
    {
        GraphicOverlayRenderer? renderer = Vehicle.DrawTracker?.overlayRenderer;
        if (renderer != null)
        {
            foreach (NodeLookSlot slot in Props.slots) renderer.RemoveOverlays(KeyPrefix + slot.key);
        }
        fittingsVersion = -1;
        base.PostDeSpawn(map, mode);
    }

    CompPhobosVehicleSystems? Systems => Vehicle.GetComp<CompPhobosVehicleSystems>();

    bool Met(NodeLook look, VehicleFittings? fittings) =>
        look.fitting != null ? fittings?.Has(look.fitting) == true : VehicleUpgrades.Unlocked(Vehicle, look.node!);

    /// <summary>Shows each slot's chosen picture, and only that one.</summary>
    void Refresh()
    {
        GraphicOverlayRenderer? renderer = Vehicle.DrawTracker?.overlayRenderer;
        if (renderer == null) return;
        VehicleFittings? fittings = Systems?.Fittings;
        fittingsVersion = fittings?.Version ?? -1;
        foreach (NodeLookSlot slot in Props.slots)
        {
            string key = KeyPrefix + slot.key;
            renderer.RemoveOverlays(key);
            if (!Vehicle.Spawned) continue;
            int chosen = NodeLookRule.Choose(slot.looks.Select(l => l.Stock ? (bool?)null : Met(l, fittings)).ToList());
            GraphicDataOverlay? look = chosen < 0 ? null : slot.looks[chosen].look;
            if (look != null) renderer.AddOverlay(key, GraphicOverlay.Create(look, Vehicle));
        }
    }
}
