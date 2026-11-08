using System;
using System.Collections.Generic;
using System.Linq;
using Outposts;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Vehicles;
using Vehicles.World;
using Verse;

namespace Phobos.VehicleSystems.Garage;

/// <summary>
/// The outposts code's own button icons, found by path: its TexOutposts fields can't be read from our 1.7 build (their Unity type comes
/// from a different assembly), and the same path gives the same texture. A lookup that fails falls back to a visible icon of ours.
/// </summary>
[StaticConstructorOnStartup]
public static class GarageIcons
{
    public static readonly Texture2D Pack = Load("UI/Gizmo/AbandonOutpost");
    public static readonly Texture2D Add = Load("UI/Gizmo/AddToOutpost");
    public static readonly Texture2D Remove = Load("UI/Gizmo/RemovePawnFromOutpost");
    public static readonly Texture2D Create = Load("UI/Gizmo/SetUpOutpost");

    static Texture2D Load(string path) =>
        ContentFinder<Texture2D>.Get(path, false) ?? ContentFinder<Texture2D>.Get("Phobos/VehicleSystems/Service", false) ?? BaseContent.WhiteTex;
}

/// <summary>The garage's buttons. Presentation only: each reads a refusal from <see cref="GarageService"/> and delegates to it.</summary>
public static class GarageCommands
{
    public static Gizmo Park(Outpost_PhobosGarage garage, VehicleCaravan convoy)
    {
        var command = new Command_Action
        {
            defaultLabel = "PhobosGarage_Park".Translate(),
            defaultDesc = "PhobosGarage_ParkDesc".Translate(),
            icon = GarageIcons.Add,
            action = () => GarageService.Park(garage, convoy),
        };
        string? refusal = GarageService.ParkRefusal(garage, convoy);
        if (refusal != null) command.Disable(refusal);
        return command;
    }

    public static Gizmo TakeOut(Outpost_PhobosGarage garage, Caravan caravan) => new Command_Action
    {
        defaultLabel = "PhobosGarage_TakeOut".Translate(),
        defaultDesc = "PhobosGarage_TakeOutDesc".Translate(),
        icon = GarageIcons.Remove,
        action = () => Find.WindowStack.Add(new Dialog_PhobosGaragePick("PhobosGarage_TakeOutTitle".Translate(), "PhobosGarage_TakeOutAccept".Translate(),
            garage.HeldVehicles.Cast<Pawn>().ToList(), p => p.LabelCap,
            chosen => GarageService.TakeOutRefusal(caravan, chosen),
            chosen => GarageService.TakeOut(garage, caravan, chosen.Cast<VehiclePawn>().ToList()))),
    };

    public static void OpenFounding(VehicleCaravan convoy, WorldObjectDef def) => Find.WindowStack.Add(new Dialog_PhobosGaragePick(
        "PhobosGarage_FoundTitle".Translate(), "PhobosGarage_FoundAccept".Translate(), GarageService.PeopleOf(convoy).ToList(),
        p => "PhobosGarage_PersonLine".Translate(p.LabelShortCap.Named("NAME"), p.skills.GetSkill(SkillDefOf.Construction).Level.Named("SKILL")),
        chosen => GarageService.FoundRefusal(convoy, def, chosen),
        chosen =>
        {
            Outpost_PhobosGarage? garage = GarageService.Found(convoy, def, chosen);
            if (garage == null) return;
            Find.WorldSelector.Select(garage);
            Messages.Message("PhobosGarage_FoundDone".Translate(garage.Name.Named("NAME")), garage, MessageTypeDefOf.TaskCompletion, false);
        }));
}

/// <summary>On every vehicle convoy: the button that founds a garage where it stands. Added to Vehicle Framework's convoy Def by an XML patch.</summary>
public class WorldObjectComp_PhobosGarageFounder : WorldObjectComp
{
    public override IEnumerable<Gizmo> GetGizmos()
    {
        if (parent is not VehicleCaravan convoy || convoy.Faction != Faction.OfPlayer) yield break;
        WorldObjectDef? def = GarageService.GarageDef;
        if (def == null || GarageService.GarageAt(convoy.Tile)) yield break;
        var command = new Command_Action
        {
            defaultLabel = "PhobosGarage_Found".Translate(),
            defaultDesc = "PhobosGarage_FoundDesc".Translate(),
            icon = GarageIcons.Create,
            action = () => GarageCommands.OpenFounding(convoy, def),
        };
        string? refusal = GarageService.ConvoyRefusal(convoy);
        if (refusal != null) command.Disable(refusal);
        yield return command;
    }
}

/// <summary>A small window for choosing from a list of pawns or vehicles; what the choice means and whether it is allowed come from the caller.</summary>
public sealed class Dialog_PhobosGaragePick : Window
{
    const float Row = 28f;
    const float ButtonHeight = 36f;
    const float ReasonHeight = 48f;

    readonly string title;
    readonly string acceptLabel;
    readonly List<Pawn> options;
    readonly Func<Pawn, string> describe;
    readonly Func<List<Pawn>, string?> refusal;
    readonly Action<List<Pawn>> accept;
    readonly HashSet<Pawn> chosen = new();
    Vector2 scroll;

    public Dialog_PhobosGaragePick(string title, string acceptLabel, List<Pawn> options, Func<Pawn, string> describe,
        Func<List<Pawn>, string?> refusal, Action<List<Pawn>> accept)
    {
        this.title = title;
        this.acceptLabel = acceptLabel;
        this.options = options;
        this.describe = describe;
        this.refusal = refusal;
        this.accept = accept;
        doCloseX = true;
        absorbInputAroundWindow = true;
    }

    public override Vector2 InitialSize => new(440f, 520f);

    public override void DoWindowContents(Rect inRect)
    {
        Text.Font = GameFont.Medium;
        var titleRect = new Rect(inRect.x, inRect.y, inRect.width, 36f);
        Widgets.Label(titleRect, title);
        Text.Font = GameFont.Small;

        var outer = new Rect(inRect.x, titleRect.yMax + 6f, inRect.width, inRect.height - titleRect.height - ReasonHeight - ButtonHeight - 16f);
        var view = new Rect(0f, 0f, outer.width - 16f, options.Count * Row);
        Widgets.BeginScrollView(outer, ref scroll, view);
        for (int i = 0; i < options.Count; i++)
        {
            Pawn option = options[i];
            bool on = chosen.Contains(option);
            bool before = on;
            Widgets.CheckboxLabeled(new Rect(0f, i * Row, view.width, Row), describe(option), ref on);
            if (on == before) continue;
            if (on) chosen.Add(option);
            else chosen.Remove(option);
        }
        Widgets.EndScrollView();

        List<Pawn> picked = options.Where(chosen.Contains).ToList();
        string? reason = refusal(picked);
        if (reason != null)
        {
            GUI.color = Phobos.Framework.SettingsUi.WarningColor;
            Widgets.Label(new Rect(inRect.x, outer.yMax + 4f, inRect.width, ReasonHeight), reason);
            GUI.color = Color.white;
        }
        var button = new Rect(inRect.x, inRect.yMax - ButtonHeight, inRect.width, ButtonHeight);
        if (Widgets.ButtonText(button, acceptLabel, active: reason == null) && reason == null)
        {
            accept(picked);
            Close();
        }
    }
}
