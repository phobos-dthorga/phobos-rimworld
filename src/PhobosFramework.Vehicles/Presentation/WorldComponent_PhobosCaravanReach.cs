using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Vehicles.World;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// Draws the edge of where a selected vehicle caravan's fuel takes it (Framework 0.85.0; VehicleRange works it out). Until 0.112.1 a
/// caravan comp drew it from PostDrawExtraSelectionOverlays, but Vehicle Framework's VehicleCaravan.DrawExtraSelectionOverlays (1.6 and
/// 1.7) never calls the base method that runs its comps' overlays, so the edge was never drawn (found by the owner, 16 Nov 2026). The game
/// gives every world component a per-frame update, and its own selection overlays are drawn from the world interface's update the same
/// way, so this draws while the world is shown. No Harmony; saves nothing. Presentation only.
/// </summary>
public sealed class WorldComponent_PhobosCaravanReach : WorldComponent
{
    /// <summary>The colour of the edge (authored: the dashboard's amber, so it reads as "fuel").</summary>
    static Material? rangeMaterial;
    static Material RangeMaterial => rangeMaterial ??= MaterialPool.MatFrom(GenDraw.LineTexPath, ShaderDatabase.WorldOverlayTransparent, new Color(0.95f, 0.70f, 0.25f, 0.9f), 3590);

    public WorldComponent_PhobosCaravanReach(World world) : base(world)
    {
    }

    public override void WorldComponentUpdate()
    {
        if (!WorldRendererUtility.WorldSelected) return;
        foreach (WorldObject selected in Find.WorldSelector.SelectedObjects)
        {
            if (selected is not VehicleCaravan caravan || caravan.Faction != Faction.OfPlayer) continue;
            VehicleRange.Reach? reach = VehicleRange.For(caravan);
            if (reach == null) continue;
            foreach ((Vector3 a, Vector3 b) in reach.Edge) GenDraw.DrawWorldLineBetween(a, b, RangeMaterial, 0.6f);
        }
    }
}
