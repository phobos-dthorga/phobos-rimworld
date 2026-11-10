using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace Phobos.Framework.Protection;

/// <summary>A control point: a sign that closes off the room it faces behind a protective-gear line.</summary>
public class CompProperties_PhobosControlPoint : CompProperties
{
    public ProtectionZoneDef zone = null!;

    public CompProperties_PhobosControlPoint() => compClass = typeof(CompPhobosControlPoint);

    public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
    {
        foreach (string error in base.ConfigErrors(parentDef)) yield return error;
        if (zone == null) yield return "a control point names no protection zone";
        if (!parentDef.rotatable) yield return "a control point must be rotatable: it closes off the room it faces";
    }
}

/// <summary>
/// Registers with its map's <see cref="ProtectionZones"/> while it stands, and works out the room it faces whenever asked, so a wall
/// built or knocked down changes what it closes off at once. Says in its inspect text which room that is, or why it closes off none.
/// </summary>
public class CompPhobosControlPoint : ThingComp
{
    public CompProperties_PhobosControlPoint Props => (CompProperties_PhobosControlPoint)props;

    public ProtectionZoneDef Zone => Props.zone;

    public Room? ControlledRoom => parent.Spawned ? ProtectionLine.RoomFaced(parent.Map, parent.Position, parent.Rotation) : null;

    public override void PostSpawnSetup(bool respawningAfterLoad) => ProtectionZones.For(parent.Map)?.Register(this);

    public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish) => ProtectionZones.For(map)?.Unregister(this);

    public override string CompInspectStringExtra() =>
        ProtectionLine.Describe(Zone, parent.Map, parent.Position, parent.Rotation);

    public override void PostDrawExtraSelectionOverlays() => ProtectionLine.DrawRoom(ControlledRoom, Zone);
}

/// <summary>While placing a control point, shows the room it would close off, or says it faces none.</summary>
public class PlaceWorker_PhobosControlPoint : PlaceWorker
{
    public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol, Thing? thing = null)
    {
        Map map = Find.CurrentMap;
        var props = def.GetCompProperties<CompProperties_PhobosControlPoint>();
        if (map == null || props == null) return;
        ProtectionLine.DrawRoom(ProtectionLine.RoomFaced(map, center, rot), props.zone);
    }
}

/// <summary>Paints or clears one kind of protective-gear line's area, as the game's home area designators do. Content mods subclass it
/// to name their kind and icons, because the build menu lists designators by class.</summary>
public abstract class Designator_PhobosProtectionArea : Designator_Cells
{
    readonly bool add;

    protected Designator_PhobosProtectionArea(bool add, string iconPath)
    {
        this.add = add;
        soundDragSustain = SoundDefOf.Designate_DragStandard;
        soundDragChanged = SoundDefOf.Designate_DragStandard_Changed;
        soundSucceeded = add ? SoundDefOf.Designate_ZoneAdd : SoundDefOf.Designate_ZoneDelete;
        useMouseIcon = true;
        // A missing picture falls back to the game's own home-area icons, never to its red X.
        icon = ContentFinder<Texture2D>.Get(iconPath, reportFailure: false)
            ?? ContentFinder<Texture2D>.Get(add ? FallbackAddIcon : FallbackClearIcon, reportFailure: false) ?? BaseContent.WhiteTex;
    }

    const string FallbackAddIcon = "UI/Designators/HomeAreaOn";
    const string FallbackClearIcon = "UI/Designators/HomeAreaOff";

    /// <summary>The kind of line this designator paints, by defName, so the class can load before the Def does.</summary>
    protected abstract string ZoneDefName { get; }

    ProtectionZoneDef? Zone => DefDatabase<ProtectionZoneDef>.GetNamedSilentFail(ZoneDefName);

    public override string Label => (add ? "PhobosProtection_AreaExpand" : "PhobosProtection_AreaClear").Translate((Zone?.label ?? ZoneDefName).Named("ZONE"));

    public override string Desc => (add ? "PhobosProtection_AreaExpandDesc" : "PhobosProtection_AreaClearDesc").Translate((Zone?.label ?? ZoneDefName).Named("ZONE"));

    public override bool DragDrawMeasurements => true;

    public override DrawStyleCategoryDef DrawStyleCategory => DrawStyleCategoryDefOf.Areas;

    public override AcceptanceReport CanDesignateCell(IntVec3 c)
    {
        ProtectionZoneDef? zone = Zone;
        if (zone == null || !c.InBounds(Map)) return false;
        bool painted = ProtectionZones.For(Map)?.IsPainted(zone, c) ?? false;
        return add ? !painted : painted;
    }

    public override void DesignateSingleCell(IntVec3 c)
    {
        ProtectionZoneDef? zone = Zone;
        if (zone != null) ProtectionZones.For(Map)?.SetPainted(zone, c, add);
    }

    public override void SelectedUpdate()
    {
        GenUI.RenderMouseoverBracket();
        ProtectionZoneDef? zone = Zone;
        if (zone != null) ProtectionZones.For(Map)?.MarkForDraw(zone);
    }
}
