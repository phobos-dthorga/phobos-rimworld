using System.Collections.Generic;
using System.Text;
using Phobos.Framework.Environment;
using RimWorld;
using UnityEngine;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// Billy's cold box (Framework 0.121.0; docs/development/vehicle-field-kitchen.md): an insulated box that holds raw food. It is the game's own
/// kind of holder, as a bookcase is, so haulers fill it by its storage settings and cooks take ingredients straight from it. While its trailer
/// is plugged in, what it holds is kept at <see cref="ColdTemperature"/> (or colder, if the weather is); without power it is only a box.
/// Packed for the road it stays cold while the vehicle towing its trailer carries it as a load (ColdInTow, Framework 0.216.0).
/// Set down where it stands if it is destroyed, so nothing in it is ever lost.
/// </summary>
public class Building_PhobosColdBox : Building, IHaulDestination, IStoreSettingsParent, IHaulSource, IThingHolder, ISearchableContents, ISetOutLoad
{
    /// <summary>A fridge's chilled temperature, in degrees Celsius (authored, the record's 4 °C).</summary>
    public const float ColdTemperature = 4f;

    ThingOwner<Thing> innerContainer;
    StorageSettings? settings;

    public Building_PhobosColdBox() => innerContainer = new ThingOwner<Thing>(this, oneStackOnly: false);

    /// <summary>How many stacks it holds: the Def's items per cell over its cells (authored in the Def).</summary>
    public int MaximumStacks => Mathf.Max(1, def.building.maxItemsInCell * def.size.Area);

    /// <summary>Cold while set out and plugged in; packed for the road, while the vehicle towing its trailer runs it through the lead.</summary>
    public bool Cold => Spawned ? SetOuts.Powered(this) : ColdInTowNow;

    /// <summary>How long word from the towing vehicle keeps a packed box cold, in ticks: two of its rare looks.</summary>
    const int TowWordLastsTicks = 2 * GenTicks.TickRareInterval;
    /// <summary>When the vehicle towing its trailer last said its cold box load runs. Derived, never saved: the next look says so again.</summary>
    int chilledInTowTick = -TowWordLastsTicks - 1;

    /// <summary>The vehicle towing its trailer runs it through the lead now (ColdInTow.Chill).</summary>
    internal void ChilledInTow(int tick) => chilledInTowTick = tick;

    bool ColdInTowNow
    {
        get
        {
            int since = Find.TickManager.TicksGame - chilledInTowTick;
            return since >= 0 && since <= TowWordLastsTicks;
        }
    }

    public float WattsNow => def.GetModExtension<PhobosSetOutPowerExtension>()?.wattsWorking ?? 0f;

    /// <summary>The temperature its contents feel, while it keeps them cold; null for the weather's.</summary>
    public float? HeldTemperature => Cold ? Mathf.Min(ColdTemperature, AmbientTemperature) : null;

    static Building_PhobosColdBox() => Phobos.Framework.Environment.HeldTemperature.Register<Building_PhobosColdBox>(box => box.HeldTemperature);

    public ThingOwner SearchableContents => innerContainer;
    public bool StorageTabVisible => true;
    public bool HaulSourceEnabled => true;
    public bool HaulDestinationEnabled => true;

    public void GetChildHolders(List<IThingHolder> outChildren) => ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());

    public ThingOwner GetDirectlyHeldThings() => innerContainer;

    public StorageSettings GetStoreSettings() => settings ??= NewSettings();

    public StorageSettings GetParentStoreSettings() => def.building.fixedStorageSettings;

    StorageSettings NewSettings()
    {
        var made = new StorageSettings(this);
        if (def.building.defaultStorageSettings != null) made.CopyFrom(def.building.defaultStorageSettings);
        return made;
    }

    public bool Accepts(Thing t)
    {
        if (innerContainer.Count >= MaximumStacks && !innerContainer.Contains(t)) return innerContainer.CanAcceptAnyOf(t) && HasRoomInStackFor(t);
        return GetStoreSettings().AllowedToAccept(t) && innerContainer.CanAcceptAnyOf(t);
    }

    bool HasRoomInStackFor(Thing t)
    {
        foreach (Thing held in innerContainer)
        {
            if (held.CanStackWith(t) && held.stackCount < held.def.stackLimit) return GetStoreSettings().AllowedToAccept(t);
        }
        return false;
    }

    public int SpaceRemainingFor(ThingDef thingDef)
    {
        int free = (MaximumStacks - innerContainer.Count) * thingDef.stackLimit;
        foreach (Thing held in innerContainer)
        {
            if (held.def == thingDef) free += held.def.stackLimit - held.stackCount;
        }
        return Mathf.Max(0, free);
    }

    public void Notify_SettingsChanged()
    {
        if (Spawned) MapHeld.listerHaulables.Notify_HaulSourceChanged(this);
    }

    public override void PostMake()
    {
        base.PostMake();
        settings = NewSettings();
    }

    public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
    {
        // Nothing in it is ever lost: set down where it stood.
        if (Spawned) innerContainer.TryDropAll(Position, Map, ThingPlaceMode.Near);
        base.Destroy(mode);
    }

    public override void TickRare()
    {
        base.TickRare();
        if (Cold && innerContainer.Count > 0 && Seen.Wants("trailers.cold-box-cold", this)) Seen.Note("trailers.cold-box-cold", this, innerContainer.Count + " stacks");
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Deep.Look(ref innerContainer, "innerContainer", this);
        Scribe_Deep.Look(ref settings, "settings", this);
        if (Scribe.mode == LoadSaveMode.PostLoadInit) innerContainer ??= new ThingOwner<Thing>(this, oneStackOnly: false);
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        foreach (Gizmo gizmo in base.GetGizmos()) yield return gizmo;
        foreach (Gizmo gizmo in StorageSettingsClipboard.CopyPasteGizmosFor(GetStoreSettings())) yield return gizmo;
    }

    public override string GetInspectString()
    {
        var text = new StringBuilder(base.GetInspectString());
        if (text.Length > 0) text.AppendLine();
        text.Append("PhobosSetOut_ColdBoxHolds".Translate(innerContainer.Count.Named("COUNT"), MaximumStacks.Named("MAX")).Resolve());
        text.AppendLine();
        text.Append(Cold
            ? "PhobosSetOut_ColdBoxCold".Translate(ColdTemperature.ToStringTemperature("F0").Named("TEMPERATURE")).Resolve()
            : "PhobosSetOut_ColdBoxWarm".Translate().Resolve());
        return text.ToString();
    }
}

/// <summary>What the cold box holds, in the game's own contents tab, with its drop buttons.</summary>
public class ITab_PhobosColdBoxContents : ITab_ContentsBase
{
    readonly List<Thing> listed = new();

    public override IList<Thing> container
    {
        get
        {
            listed.Clear();
            if (SelThing is Building_PhobosColdBox box) listed.AddRange(box.GetDirectlyHeldThings());
            return listed;
        }
    }

    public ITab_PhobosColdBoxContents()
    {
        labelKey = "TabCasketContents";
        containedItemsKey = "ContainedItems";
    }
}
