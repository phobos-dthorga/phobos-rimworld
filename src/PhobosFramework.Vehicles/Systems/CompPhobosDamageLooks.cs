using System.Collections.Generic;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>A picture of damage, shown over the vehicle while one of its parts is below a share of its health.</summary>
public class DamageLook
{
    /// <summary>The part's key in the vehicle's components.</summary>
    public string part = "";
    /// <summary>Shown while the part's health is below this share (authored; the bulk tanker's 0.6).</summary>
    public float belowHealth = 0.6f;
    public GraphicDataOverlay? overlay;
}

public class CompProperties_PhobosDamageLooks : VehicleCompProperties
{
    public List<DamageLook> looks = new();

    public CompProperties_PhobosDamageLooks() => compClass = typeof(CompPhobosDamageLooks);
}

/// <summary>
/// Damage pictures by part health, for any vehicle (12 Nov 2026; the bulk tanker's own comp did this for its tanks first). Vehicle
/// Framework never reads a part's health for an overlay, so this shows and hides them itself, on spawn and every rare tick: one
/// comparison per picture, and nothing saved, so a load shows what the parts' health calls for. Works on any faction's vehicle.
/// Presentation only.
/// </summary>
public class CompPhobosDamageLooks : VehicleComp
{
    const string KeyPrefix = "PhobosDamageLook_";
    bool[]? shown;

    CompProperties_PhobosDamageLooks Props => (CompProperties_PhobosDamageLooks)props;

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        shown = null;
        LongEventHandler.ExecuteWhenFinished(Sync);
    }

    public override void CompTickRare()
    {
        base.CompTickRare();
        Sync();
    }

    public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
    {
        GraphicOverlayRenderer? renderer = Vehicle.DrawTracker?.overlayRenderer;
        if (renderer != null)
        {
            for (int i = 0; i < Props.looks.Count; i++) renderer.RemoveOverlays(KeyPrefix + i);
        }
        shown = null;
        base.PostDeSpawn(map, mode);
    }

    void Sync()
    {
        GraphicOverlayRenderer? renderer = Vehicle.DrawTracker?.overlayRenderer;
        if (renderer == null || !Vehicle.Spawned || Props.looks.Count == 0) return;
        shown ??= new bool[Props.looks.Count];
        for (int i = 0; i < Props.looks.Count; i++)
        {
            DamageLook look = Props.looks[i];
            VehicleComponent? part = Vehicle.statHandler?.GetComponent(look.part);
            bool show = part != null && look.overlay != null && part.HealthPercent < look.belowHealth;
            if (show == shown[i]) continue;
            shown[i] = show;
            if (show) renderer.AddOverlay(KeyPrefix + i, GraphicOverlay.Create(look.overlay!, Vehicle));
            else renderer.RemoveOverlays(KeyPrefix + i);
        }
    }
}
