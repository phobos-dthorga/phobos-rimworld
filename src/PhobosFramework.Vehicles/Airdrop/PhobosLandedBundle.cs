using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// A cargo bundle on the ground after a parachute drop (D'thorga Framework 0.293.0; docs/development/airdrop.md, the follow-ups of 25 Nov
/// 2026): its cargo stays strapped in its bag until a colonist de-rigs it, as real A-22 bundles are. It uses the game's own "Open" order and
/// job, and is marked for opening as it lands when it is the player's. Opened, it puts its cargo beside it. Destroyed first, it spills what
/// it held rather than lose it.
/// </summary>
public class PhobosLandedBundle : Building, IOpenable, IThingHolder
{
    /// <summary>Ticks a colonist takes to de-rig a bundle (authored: about 15 game minutes).</summary>
    const int DeRigTicks = 625;

    ThingOwner<Thing> contents;

    public PhobosLandedBundle() => contents = new ThingOwner<Thing>(this, oneStackOnly: false);

    public bool CanOpen => contents.Count > 0;

    public int OpenTicks => DeRigTicks;

    public ThingOwner GetDirectlyHeldThings() => contents;

    public void GetChildHolders(List<IThingHolder> outChildren) => ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, contents);

    /// <summary>Puts what landed into the bundle before it is spawned.</summary>
    public void Fill(IEnumerable<Thing> things)
    {
        foreach (Thing thing in things) contents.TryAdd(thing, canMergeWithExistingStacks: true);
    }

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);
        if (!respawningAfterLoad && Faction == Faction.OfPlayer && map.designationManager.DesignationOn(this, DesignationDefOf.Open) == null)
            map.designationManager.AddDesignation(new Designation(this, DesignationDefOf.Open));
    }

    public void Open()
    {
        if (!Spawned) return;
        Spill();
        Seen.Note("airdrop.de-rigged", this);
        Destroy();
    }

    void Spill()
    {
        Map map = Map;
        IntVec3 at = Position;
        foreach (Thing thing in contents.ToList())
        {
            contents.Remove(thing);
            if (!GenPlace.TryPlaceThing(thing, at, map, ThingPlaceMode.Near)) GenSpawn.Spawn(thing, at, map);
        }
    }

    public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
    {
        if (Spawned && contents.Count > 0) Spill();
        base.Destroy(mode);
    }

    public override string GetInspectString()
    {
        string text = base.GetInspectString();
        if (contents.Count == 0) return text;
        string inside = "PhobosAirdrop_InspectBundle".Translate(contents.InnerListForReading.Select(t => t.LabelCap.ToString()).ToCommaList().Named("CONTENTS")).Resolve();
        return text.NullOrEmpty() ? inside : text + "\n" + inside;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Deep.Look(ref contents, "PhobosBundle_Contents", this);
        if (Scribe.mode == LoadSaveMode.PostLoadInit) contents ??= new ThingOwner<Thing>(this, oneStackOnly: false);
    }
}
