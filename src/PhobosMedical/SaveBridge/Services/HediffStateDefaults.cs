using System;
using System.Collections.Generic;
using System.Xml;

namespace Phobos.MedicalSaveBridge.Services;

/// <summary>
/// The saved fields a hediff class loads with a default that no live hediff of that class would carry. When the bridge upgrades a
/// saved vanilla wound to one of these classes, it writes these into the saved node first, so the wound loads as if the class had
/// written it. Nothing is copied from the mods named: these are the names and values their save files hold.
/// </summary>
public static class HediffStateDefaults
{
    /// <summary>
    /// Read from More Injuries 1.6.0.2's BetterInjuryState.ExposeData on 3 November 2026: it loads overriddenBleedRate (the
    /// coagulation multiplier) with a default of 0, which would stop an old wound bleeding for good, while every live wound saves
    /// 1; temporarilyTamponadedMultiplierBase likewise; reducedBleedRateTicksRemaining defaults to 30,000 where a live wound saves 0.
    /// If a later More Injuries renames them, the wound simply loads with that version's own defaults, as it would without the bridge.
    /// </summary>
    static readonly (string element, string value)[] moreInjuriesBleedState =
    {
        ("overriddenBleedRate", "1"),
        ("temporarilyTamponadedMultiplierBase", "1"),
        ("reducedBleedRateTicksRemaining", "0"),
    };

    static readonly Dictionary<string, (string element, string value)[]> byClassName = new Dictionary<string, (string, string)[]>
    {
        ["MoreInjuries.HealthConditions.HeavyBleeding.Overrides.BetterInjury"] = moreInjuriesBleedState,
        ["MoreInjuries.HealthConditions.HeavyBleeding.Overrides.BetterMissingPart"] = moreInjuriesBleedState,
    };

    /// <summary>Adds the class's live defaults to a saved node that lacks them. Returns how many elements were added.</summary>
    public static int Apply(Type newClass, XmlNode node)
    {
        if (newClass.FullName == null || !byClassName.TryGetValue(newClass.FullName, out (string element, string value)[]? defaults))
        {
            return 0;
        }
        int added = 0;
        foreach ((string element, string value) in defaults)
        {
            if (node[element] != null) continue;
            XmlDocument? document = node.OwnerDocument;
            if (document == null) continue;
            XmlElement child = document.CreateElement(element);
            child.InnerText = value;
            node.AppendChild(child);
            added++;
        }
        return added;
    }
}
