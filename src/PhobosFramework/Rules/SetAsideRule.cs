using System.Collections.Generic;

namespace Phobos.Framework.Rules;

/// <summary>
/// Work set aside (Framework 0.179.0; owner, 20 Nov 2026: submarines to the back burner, "visible for colonies with pre-existing data but
/// for those otherwise ... invisible and out-of-sight"), game-free. A research tab set aside stays in view for a colony that has put any
/// research into it, so nothing it already owns goes missing, and is out of view for every other colony. Outside a game (no colony to
/// ask) it stays in view: hiding is a choice about a colony, never about the main menu.
/// </summary>
public static class SetAsideRule
{
    /// <summary>Whether a set-aside tab is out of view, given the colony's progress on each of its projects, or null outside a game.</summary>
    public static bool Hidden(bool setAside, IEnumerable<float>? progressOfItsProjects)
    {
        if (!setAside || progressOfItsProjects == null) return false;
        foreach (float progress in progressOfItsProjects)
        {
            if (progress > 0f) return false;
        }
        return true;
    }
}
