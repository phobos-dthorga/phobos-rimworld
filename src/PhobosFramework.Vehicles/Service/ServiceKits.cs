using Verse;

namespace Phobos.VehicleSystems;

/// <summary>
/// Whether this colony's vehicle services still take raw materials rather than the kits made at the parts bench (S4 of
/// docs/development/vehicle-crosslinks-round-two.md; owner, 10 Nov 2026). A colony begun after kits existed needs kits from the start;
/// a save from before them keeps paying in raw materials, so no service already due is stranded, until the player switches it off in
/// the vehicle systems settings. Saved per colony; the settings window only shows and changes it through <see cref="SetRawAccepted"/>.
/// </summary>
public class GameComponent_PhobosServiceKits : GameComponent
{
    static GameComponent_PhobosServiceKits? current;

    bool rawAccepted;
    bool decided;

    public GameComponent_PhobosServiceKits(Game game)
    {
        current = this;
    }

    /// <summary>This colony's services take raw materials instead of kits. False with no game.</summary>
    public static bool RawAccepted => Current.Game != null && current != null && current.rawAccepted;

    /// <summary>Whether a game is running whose choice can be changed.</summary>
    public static bool InGame => Current.Game != null && current != null;

    public static void SetRawAccepted(bool on)
    {
        if (current != null) current.rawAccepted = on;
    }

    public override void StartedNewGame()
    {
        current = this;
        rawAccepted = false;
        decided = true;
    }

    public override void LoadedGame()
    {
        current = this;
        if (decided) return;
        // A save from before kits existed: its services carry on as they were.
        rawAccepted = true;
        decided = true;
    }

    public override void ExposeData()
    {
        Scribe_Values.Look(ref rawAccepted, "PhobosServiceKits_RawAccepted", false);
        Scribe_Values.Look(ref decided, "PhobosServiceKits_Decided", false);
    }
}
