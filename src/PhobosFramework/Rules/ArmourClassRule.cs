using System.Collections.Generic;

namespace Phobos.Framework.Rules;

/// <summary>
/// Which armour class a part's figures belong to (Framework 0.154.0; docs/development/vehicle-armour.md, "Armour classes"). A class is named
/// by the threat it stops, and carries a sharp and a blunt figure on each scale: millimetres and megapascals under Combat Extended, the
/// game's own rating without it. A part matches a class when both its figures are that class's on the scale in use. Game-free.
/// </summary>
public static class ArmourClassRule
{
    /// <summary>How far a figure may sit from its class's and still be that class: a thousandth, plus a thousandth of the figure.</summary>
    public static bool Same(double figure, double classFigure) => System.Math.Abs(figure - classFigure) <= 0.001 + 0.001 * System.Math.Abs(classFigure);

    /// <summary>The index of the class whose sharp and blunt figures these are, or -1 for none (an other author's figure, or no armour).</summary>
    public static int Find(double sharp, double blunt, IReadOnlyList<(double Sharp, double Blunt)> classes)
    {
        for (int i = 0; i < classes.Count; i++)
        {
            if (Same(sharp, classes[i].Sharp) && Same(blunt, classes[i].Blunt)) return i;
        }
        return -1;
    }
}
