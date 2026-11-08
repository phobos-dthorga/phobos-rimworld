using System.Collections.Generic;
using System.Globalization;

namespace Phobos.Framework.Rules;

/// <summary>
/// A table of renamed item Defs, game-free, for carrying a save across a rename: the old names of the Defs and of the recipes the game
/// makes for them ("Make_" + name), and the old load IDs other things in the save refer to them by.
/// </summary>
public sealed class DefRenameTable
{
    const string ThingPrefix = "Thing_";
    const string RecipePrefix = "Make_";
    const string BillPrefix = "Bill_";

    readonly Dictionary<string, string> names = new();
    readonly List<(string oldName, string newName)> items = new();

    public void AddItem(string oldName, string newName)
    {
        names[oldName] = newName;
        names[RecipePrefix + oldName] = RecipePrefix + newName;
        items.Add((oldName, newName));
    }

    /// <summary>The Def's current name, or the name given when it was not renamed.</summary>
    public string CurrentName(string name) => names.TryGetValue(name, out string? current) ? current : name;

    /// <summary>
    /// The load ID an old one has become: a thing's ("Thing_" + def name + number) and a bill's ("Bill_Make_" + def name + "_" + number).
    /// A thing's number is read as the game reads it, from every digit at the end, so an old name ending in a digit gives that digit to
    /// the number; a number too long to hold reads as 0, as it does in the game. Anything else comes back unchanged.
    /// </summary>
    public string CurrentLoadId(string id)
    {
        foreach ((string oldName, string newName) in items)
        {
            if (id.StartsWith(BillPrefix + RecipePrefix + oldName + "_", System.StringComparison.Ordinal))
            {
                return BillPrefix + RecipePrefix + newName + id.Substring(BillPrefix.Length + RecipePrefix.Length + oldName.Length);
            }
            string thing = ThingPrefix + oldName;
            if (id.StartsWith(thing, System.StringComparison.Ordinal) && AllDigits(id, thing.Length))
            {
                return ThingPrefix + newName + NumberAtEnd(id).ToString(CultureInfo.InvariantCulture);
            }
        }
        return id;
    }

    static bool AllDigits(string text, int from)
    {
        for (int i = from; i < text.Length; i++)
        {
            if (!char.IsDigit(text[i])) return false;
        }
        return true;
    }

    static int NumberAtEnd(string text)
    {
        int start = text.Length;
        while (start > 0 && char.IsDigit(text[start - 1])) start--;
        return int.TryParse(text.Substring(start), NumberStyles.None, CultureInfo.InvariantCulture, out int number) ? number : 0;
    }
}
