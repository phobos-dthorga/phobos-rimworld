using System;
using System.Collections.Generic;
using Phobos.Framework;
using Phobos.Framework.Defs;
using Phobos.Framework.Vehicles.Rules;
using RimWorld;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>The magazine of an aircraft's chaff and flare dispensers: given to every design whose loadout carries countermeasures.</summary>
public sealed class CompProperties_PhobosDispensers : CompProperties
{
    /// <summary>How many cartridges the magazine holds: the loadout's avionics figure.</summary>
    public int capacity = 30;

    public CompProperties_PhobosDispensers() => compClass = typeof(CompPhobosDispensers);
}

/// <summary>
/// The dispensers' magazine (Framework 0.180.0; owner, 20 Nov 2026: countermeasures "handled as ammunition", as loading them as cargo met a
/// different refusal on almost every aircraft). The cartridges sit here, not in the cargo, so Vehicle Framework's loading rules, cargo room
/// and other authors' cargo checks never stand between the stockpile and the dispensers. Haulers keep it topped up through D'thorga
/// Framework's topping up (a PhobosTopUpDef names this comp and the members below), walking to the parked aircraft as Vehicle Framework's
/// own loading job does. Real dispensers are loaded on the ground into fixed magazines; nothing reloads them in the air.
///
/// Once per aircraft, at its first look (<see cref="Settle"/>): cartridges an older save left in its cargo move in while there is room, and
/// another faction's aircraft is given a part-full magazine (owner, 20 Nov 2026), so raiders and the aircraft at a base the player attacks fire
/// countermeasures too. A loaded magazine weighs on the engine as a turret's loaded rounds do, and takes no cargo room. Destroyed with the
/// aircraft. Holds cartridges only; the service (AirCombat) is the only thing that spends them.
/// </summary>
public sealed class CompPhobosDispensers : VehicleComp, IThingHolder
{
    ThingOwner<Thing> magazine;
    bool settled;

    public CompPhobosDispensers() => magazine = new ThingOwner<Thing>(this);

    public CompProperties_PhobosDispensers Props => (CompProperties_PhobosDispensers)props;

    static PhobosAirCombatRulesDef Rules => PhobosRulesDef.Current<PhobosAirCombatRulesDef>();

    public override bool TickByRequest => true;

    // ---- What topping up reads (by name, from PhobosTopUp_Dispensers; keep the names) ----

    /// <summary>The cartridge the dispensers fire.</summary>
    public ThingDef? Cartridge => Rules.cartridgeDef;

    /// <summary>How many cartridges the magazine holds when full.</summary>
    /// <summary>The magazine: the loadout's, and more where dispensers have been fitted (aircraft service-life upgrades).</summary>
    /// Framework 0.316.0: none until fitted, where the dispensers come with an upgrade node.
    public int Capacity => parent is VehiclePawn fitted && !AirCombat.CarriesCountermeasures(fitted) ? 0
        : Math.Max(0, (int)Math.Round(Props.capacity * ((parent as VehiclePawn) is { } vehicle && CompPhobosVehicleSystems.Of(vehicle) is { } systems
        ? systems.Fittings.CartridgeFactor : 1f)));

    /// <summary>How many cartridges are loaded.</summary>
    public int Held => magazine.TotalStackCount;

    /// <summary>How many more it takes to fill: the magazine's own fill level is full.</summary>
    public int Wanted => Math.Max(0, Capacity - Held);

    /// <summary>The least shortfall worth a hauler's walk: the rules' figure, never more than the whole magazine.</summary>
    public int LeastLoad => Math.Max(1, Math.Min(Rules.magazineLeastLoad, Capacity));

    // ---- The service's side ----

    /// <summary>What the loaded cartridges weigh, kilograms.</summary>
    public float Kilograms
    {
        get
        {
            float total = 0f;
            List<Thing> held = magazine.InnerListForReading;
            for (int i = 0; i < held.Count; i++) total += held[i].GetStatValue(StatDefOf.Mass) * held[i].stackCount;
            return total;
        }
    }

    /// <summary>Takes <paramref name="count"/> cartridges out and destroys them (fired). False, and nothing taken, when fewer are loaded.</summary>
    public bool Spend(int count)
    {
        if (count <= 0) return true;
        if (Held < count) return false;
        int left = count;
        List<Thing> held = magazine.InnerListForReading;
        for (int i = held.Count - 1; i >= 0 && left > 0; i--)
        {
            Thing thing = held[i];
            int take = Math.Min(left, thing.stackCount);
            if (take >= thing.stackCount)
            {
                magazine.Remove(thing);
                thing.Destroy();
            }
            else
            {
                thing.SplitOff(take).Destroy();
            }
            left -= take;
        }
        return true;
    }

    /// <summary>
    /// The once-per-aircraft first look: cartridges an older save left in the cargo move in while there is room (the rest stay as cargo), and
    /// another faction's aircraft is given its part-full magazine. Saved, so it never runs twice. Moves things between containers; spawns
    /// nothing, so it is safe at spawn and in flight.
    /// </summary>
    public void Settle()
    {
        if (settled || Vehicle == null) return;
        settled = true;
        ThingDef? cartridge = Cartridge;
        if (cartridge == null || Capacity <= 0) return;
        if (Vehicle.inventory?.innerContainer is { } cargo)
        {
            int moving = AirCombatRule.CargoIntoMagazine(cargo.TotalStackCountOfDef(cartridge), Held, Capacity);
            int moved = 0;
            for (int i = cargo.Count - 1; i >= 0 && moved < moving; i--)
            {
                Thing thing = cargo[i];
                if (thing.def != cartridge) continue;
                // Counted truly (Framework 0.286.2): the game's own answer counts a stack merged into the magazine as nought.
                moved += Phobos.Framework.Items.ThingMoves.Transfer(cargo, thing, magazine, Math.Min(moving - moved, thing.stackCount));
            }
            if (moved > 0 && Seen.Wants("aircraft.magazine-from-cargo", Vehicle))
            {
                Seen.Note("aircraft.magazine-from-cargo", Vehicle, values: new List<KeyValuePair<string, double>> { new("cartridges", moved) });
            }
        }
        if (Vehicle.Faction != null && Vehicle.Faction != Faction.OfPlayer && Held == 0)
        {
            PhobosAirCombatRulesDef rules = Rules;
            // One roll per aircraft, from its ID, so a reload gives the same answer.
            float roll = Rand.ValueSeeded(Vehicle.thingIDNumber ^ 0x5C0FFEE);
            int stock = AirCombatRule.OtherFactionStock(Capacity, rules.cartridgesPerSalvo, rules.otherFactionsFillMin, rules.otherFactionsFillMax, roll);
            Add(cartridge, stock);
            if (stock > 0 && Seen.Wants("aircraft.magazine-stocked", Vehicle))
            {
                Seen.Note("aircraft.magazine-stocked", Vehicle, values: new List<KeyValuePair<string, double>>
                {
                    new("cartridges", stock), new("capacity", Capacity),
                });
            }
        }
    }

    /// <summary>Puts new cartridges in, a stack at a time.</summary>
    void Add(ThingDef cartridge, int count)
    {
        int limit = Math.Max(1, cartridge.stackLimit);
        while (count > 0)
        {
            Thing thing = ThingMaker.MakeThing(cartridge);
            thing.stackCount = Math.Min(limit, count);
            count -= thing.stackCount;
            if (!magazine.TryAdd(thing))
            {
                thing.Destroy();
                return;
            }
        }
    }

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        Settle();
    }

    public override void PostDestroy(DestroyMode mode, Map previousMap)
    {
        base.PostDestroy(mode, previousMap);
        // The cartridges burn with the aircraft.
        magazine.ClearAndDestroyContents();
    }

    public override string CompInspectStringExtra()
    {
        // Only the player's own: their people cannot count another faction's cartridges.
        if (Vehicle?.Faction != Faction.OfPlayer || Capacity <= 0) return "";
        return "PhobosAirCombat_MagazineInspect".Translate(Held.Named("HELD"), Capacity.Named("CAPACITY"));
    }

    // ---- Holding ----

    public ThingOwner GetDirectlyHeldThings() => magazine;

    public void GetChildHolders(List<IThingHolder> outChildren) => ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, magazine);

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Deep.Look(ref magazine, "PhobosDispensers_Magazine", this);
        Scribe_Values.Look(ref settled, "PhobosDispensers_Settled");
        if (Scribe.mode == LoadSaveMode.PostLoadInit) magazine ??= new ThingOwner<Thing>(this);
    }
}
