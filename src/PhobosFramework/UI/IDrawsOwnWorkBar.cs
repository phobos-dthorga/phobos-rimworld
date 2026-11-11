// Not "Phobos.Framework.UI": a namespace named UI would hide the game's Verse.UI class from every Phobos.Framework.* namespace.
namespace Phobos.Framework.Interface;

/// <summary>
/// Marks a job driver that already draws the game's work bar itself, so D'thorga Patches' Vehicle Framework bar patch leaves it alone and
/// no job ever shows two bars (12 Nov 2026). Our vehicle work drivers in D'thorga Framework carry it.
/// </summary>
public interface IDrawsOwnWorkBar
{
}
