using RimWorld;
using SmashTools;
using Vehicles;
using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// A vehicle that sets up to work: while set up it can be neither driven nor drafted (utility vehicles, principle 3). Each vehicle says what setting up means and keeps its
/// own saved key; everything that holds it still lives here.
/// </summary>
public abstract class CompPhobosSetUp : VehicleComp
{
    protected bool deployed;

    public bool Deployed => deployed;

    /// <summary>The saved key, stable once a save can hold it.</summary>
    protected abstract string DeployedSaveKey { get; }

    /// <summary>What the player is told when they try to move it.</summary>
    protected abstract string SetUpRefusal { get; }

    protected virtual bool HeldBySetUp => deployed;

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref deployed, DeployedSaveKey);
    }

    public override AcceptanceReport CanMove(FloatMenuContext context) => HeldBySetUp ? SetUpRefusal : base.CanMove(context);

    public override AcceptanceReport CanDraft() => HeldBySetUp ? SetUpRefusal : base.CanDraft();
}

/// <summary>A comp that needs the vehicle's engine turning while it works, though nobody is driving.</summary>
public interface IVehicleEngineDemand
{
    bool WantsEngine { get; }

    /// <summary>How hard it works the engine, from 0 to 1.</summary>
    float EngineDemand { get; }
}
