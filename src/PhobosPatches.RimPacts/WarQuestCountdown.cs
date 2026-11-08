using System;
using System.Reflection;
using Phobos.Framework;
using Phobos.RimPactsPatches.Rules;
using RimWorld;
using Verse;

namespace Phobos.RimPactsPatches;

/// <summary>
/// Gives RimPacts' world war quest the game's own countdown (owner, 7 Nov 2026: "bring it in line with the game's vanilla style"). The
/// quest window shows a red time left in the quest list and "Expires in ..." on the quest itself for any accepted quest holding an
/// enabled, bad QuestPart_Delay with expiry text (MainTabWindow_Quests, RimWorld 1.7). RimPacts keeps its deadline in its own world
/// component, so its quest showed only "accepted 3.7 days ago" and the owner lost a war without knowing time had run out.
///
/// This adds one vanilla QuestPart_Delay to the running quest, ending at the daily check that would fail the war
/// (<see cref="WarDeadlineRule"/>), with the game's own "Expires in {0}" and "Expires on {0}" text, so every language the game speaks
/// shows it. It changes nothing RimPacts decides: the part's completion signal is one nothing listens to. The part is a vanilla class
/// saved in the quest, so removing this mod leaves a countdown that finishes harmlessly. RimPacts is read by name; if its fields
/// change, the countdown stays off and the load line says so.
/// </summary>
public static class WarQuestCountdown
{
    const string ComponentType = "RimPacts.WorldComponent_RimPacts";
    /// <summary>Marks our part among the quest's parts. Nothing sends this signal; it is only read back.</summary>
    public const string Marker = "PhobosPatches.WarQuestDeadline";

    static PropertyInfo? instance;
    static FieldInfo? questField, acceptedField, deadlineField, nextDailyField;
    static MethodInfo? enable;

    public static bool Ready { get; private set; }

    public static void Prepare()
    {
        Type? type = OptionalMods.TypeNamed(ComponentType);
        const BindingFlags field = BindingFlags.NonPublic | BindingFlags.Instance;
        instance = type?.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
        questField = type?.GetField("wwQuestObj", field);
        acceptedField = type?.GetField("wwQuestAccepted", field);
        deadlineField = type?.GetField("wwQuestDeadlineTick", field);
        nextDailyField = type?.GetField("nextDailyCheckTick", field);
        enable = typeof(QuestPartActivable).GetMethod("Enable", BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof(SignalArgs) }, null);
        Ready = instance != null && questField != null && acceptedField != null && deadlineField != null && nextDailyField != null && enable != null;
        if (!Ready)
            RimPactsPatchesMod.Logger.Warn("RimPacts' world war quest has changed, so its quest shows no countdown.");
    }

    public static void Refresh()
    {
        try
        {
            RefreshInner();
        }
        catch (Exception error)
        {
            Ready = false;
            RimPactsPatchesMod.Logger.Error("could not show the world war quest's countdown, so it is off until the game restarts: " + error);
        }
    }

    static void RefreshInner()
    {
        object? component = instance!.GetValue(null);
        if (component == null) return;
        if (questField!.GetValue(component) is not Quest quest || quest.State != QuestState.Ongoing) return;
        if (acceptedField!.GetValue(component) is not true) return;
        if (deadlineField!.GetValue(component) is not int deadline || deadline <= 0) return;
        int nextDaily = nextDailyField!.GetValue(component) is int next ? next : 0;
        int now = Find.TickManager.TicksGame;
        int ends = WarDeadlineRule.EndTick(deadline, nextDaily, GenDate.TicksPerDay);

        QuestPart_Delay? part = null;
        foreach (QuestPart existing in quest.PartsListForReading)
        {
            if (existing is QuestPart_Delay delay && delay.inSignalEnable == Marker)
            {
                part = delay;
                break;
            }
        }
        if (part == null)
        {
            if (ends <= now) return;
            part = new QuestPart_Delay
            {
                inSignalEnable = Marker,
                isBad = true,
                delayTicks = ends - now,
                expiryInfoPart = "QuestExpiresIn".Translate(),
                expiryInfoPartTip = "QuestExpiresOn".Translate(),
            };
            quest.AddPart(part);
            enable!.Invoke(part, new object[] { default(SignalArgs) });
            RimPactsPatchesMod.Logger.Info("The world war quest \"" + quest.name + "\" now shows its deadline: "
                + (ends - now).ToStringTicksToPeriod() + " left.");
            return;
        }
        // RimPacts' daily schedule can move (a load, its own slicing); keep the countdown on the check that decides the war.
        if (part.State == QuestPartState.Enabled && ends > now && part.EnableTick + part.delayTicks != ends)
            part.delayTicks = ends - part.EnableTick;
    }
}

/// <summary>Checks RimPacts' world war quest four times a game hour; it saves nothing and reads everything from RimPacts.</summary>
public class GameComponent_PhobosWarQuestCountdown : GameComponent
{
    const int CheckIntervalTicks = 250;

    public GameComponent_PhobosWarQuestCountdown(Game game) { }

    public override void GameComponentTick()
    {
        if (!WarQuestCountdown.Ready || Find.TickManager.TicksGame % CheckIntervalTicks != 0) return;
        WarQuestCountdown.Refresh();
    }
}
