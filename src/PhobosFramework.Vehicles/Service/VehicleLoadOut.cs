using System.Linq;
using Phobos.Framework.Rules;
using RimWorld;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// The "Vehicles may load from here" switch on storage (Framework 0.125.0; docs/development/universal-hoppers.md, UH12): which storage types
/// carry it, what it lets a truck's grab lift, and the one place it is changed. Every storage building type gets it at start-up, from every mod,
/// so any shelf, hopper or bunker can become a load-out point (owner, 17 Nov 2026).
/// </summary>
public static class VehicleLoadOut
{
    /// <summary>The loading grab's own button picture, reused: the switch says what the grab may reach.</summary>
    public const string IconPath = "Phobos/VehicleSystems/Stations/Grab";

    /// <summary>How many storage types were given the switch at start-up, for the load line.</summary>
    public static int SwitchesAdded { get; private set; }

    /// <summary>Adds the switch to every storage building type that has none. Run once, when every Def has loaded.</summary>
    public static void AddSwitches()
    {
        int added = 0;
        foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading)
        {
            if (def.thingClass == null || !typeof(Building_Storage).IsAssignableFrom(def.thingClass)) continue;
            def.comps ??= new System.Collections.Generic.List<CompProperties>();
            if (def.comps.Any(props => props is CompProperties_PhobosVehicleLoadOut)) continue;
            def.comps.Add(new CompProperties_PhobosVehicleLoadOut { unloadStartsOn = VehicleUnloadBays.StartsOn(def) });
            added++;
        }
        SwitchesAdded = added;
        VehicleSystemsMod.Logger.Debug($"Added the \"Vehicles may load from here\" and \"Vehicles may unload here\" switches to {added} storage building types.");
    }

    /// <summary>Turns the switch on or off and says so.</summary>
    public static void Set(CompPhobosVehicleLoadOut comp, bool on)
    {
        comp.VehiclesMayLoad = on;
        Messages.Message((on ? "PhobosVehicleSystems_MayLoadOn" : "PhobosVehicleSystems_MayLoadOff").Translate(comp.parent.LabelShort.Named("STORAGE")),
            comp.parent, MessageTypeDefOf.SilentInput, historical: false);
    }

    /// <summary>The storage building a thing sits in, or null for a loose thing or one in a stockpile zone.</summary>
    public static Building_Storage? StoredIn(Thing thing) => thing.IsInAnyStorage() ? thing.StoringThing() as Building_Storage : null;

    /// <summary>Whether a vehicle of this side may lift this thing out of where it lies (GrabRule.MayLift decides).</summary>
    public static bool MayLift(Thing thing, Faction? side)
    {
        if (!thing.IsInAnyStorage()) return true;
        Building_Storage? store = StoredIn(thing);
        bool switchedOn = store?.GetComp<CompPhobosVehicleLoadOut>()?.VehiclesMayLoad ?? false;
        bool trucksOwn = store != null && side != null && store.Faction == side;
        bool claimed = thing.Spawned && thing.Map.reservationManager.IsReservedByAnyoneOf(thing, side ?? Faction.OfPlayer);
        return GrabRule.MayLift(true, trucksOwn, switchedOn, claimed);
    }
}
