using Phobos.MedicalSaveBridge.Services;
using Verse;

namespace Phobos.MedicalSaveBridge.UI;

/// <summary>
/// The question before a save is converted: three buttons, each one call to <see cref="BridgeChoices.ChooseAndLoad"/>. Escape
/// closes it and nothing loads. It is shown from the load menu, before the colony exists, so naming the mod that changed the bodies
/// is the only way to say what happened (agent reading of the in-world rule, for the owner: the game's own mod-mismatch dialog
/// names mods at the same point).
/// </summary>
public static class BridgeChoiceDialog
{
    public static Dialog_MessageBox Make(string fileName, BridgeNeed need)
    {
        TaggedString text = "PhobosSaveBridge_AskText".Translate(string.Join(", ", need.ModNames).Named("MODS"));
        if (need.BodyLabels.Count > 0) text += "\n\n" + "PhobosSaveBridge_AskBodies".Translate(BridgeChoices.NameBodies(need.BodyLabels).Named("BODIES"));
        text += "\n\n" + "PhobosSaveBridge_AskChoices".Translate();
        var dialog = new Dialog_MessageBox(text,
            "PhobosSaveBridge_Convert".Translate(), () => BridgeChoices.ChooseAndLoad(fileName, BridgeChoice.Convert),
            "PhobosSaveBridge_NotNow".Translate(), () => BridgeChoices.ChooseAndLoad(fileName, BridgeChoice.NotNow),
            "PhobosSaveBridge_AskTitle".Translate(), cancelAction: () => { })
        {
            buttonCText = "PhobosSaveBridge_Never".Translate(),
            buttonCAction = () => BridgeChoices.ChooseAndLoad(fileName, BridgeChoice.Never),
        };
        return dialog;
    }
}
