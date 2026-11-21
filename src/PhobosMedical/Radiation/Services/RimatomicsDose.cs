using System;
using System.Reflection;
using HarmonyLib;
using Phobos.Framework;
using Phobos.Radiation.Rules;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Phobos.Radiation.Services;

/// <summary>
/// Every Dubs Rimatomics dose for people comes to our model (D'thorga Medical 0.21.0; docs/development/radiation-model.md, RM1; owner,
/// 22 Nov 2026: replace it, "with a setting to disable it"). All of Rimatomics' radiation passes through <c>DubUtils.applyRads</c>: a
/// prefix there takes the dose for a person and skips the original while the model is on, the recorded reason for a skipping prefix being
/// that it is the one entry for every Rimatomics dose. Robots stay Rimatomics' (its own test). Rimatomics' radiation setting is honoured;
/// toxic environment resistance is not applied (the owner's choice: suits stop dust, not the dose). Its fallout fades by the 7:10 rule
/// from its first day, which Rimatomics' constant fallout does not. Its Geiger click and figure are redone. Patched by name, so nothing of
/// ours references Rimatomics' assembly; without Rimatomics nothing is patched.
/// </summary>
public static class RimatomicsDose
{
    public const string PackageId = "Dubwise.Rimatomics";
    const string ApplyRads = "Rimatomics.DubUtils:applyRads";
    const string NuclearFallout = "Rimatomics.GameCondition_NuclearFallout:DoCellSteadyEffects";
    const string RadioactiveFallout = "Rimatomics.GameCondition_RadioactiveFallout:DoCellSteadyEffects";

    public static bool Applied { get; private set; }

    /// <summary>The fallout's age in days while a fallout condition is dosing a cell, else below 0.</summary>
    [ThreadStatic] static double falloutDays;
    [ThreadStatic] static bool inFallout;

    static Func<Pawn, bool>? isRobot;
    static Func<float>? strengthSetting;
    static Func<bool>? geigerKnown;
    static SoundDef? geigerTick;

    public static void Apply(Harmony harmony)
    {
        if (!OptionalMods.IsActive(PackageId)) return;
        MethodInfo? rads = AccessTools.Method(ApplyRads, new[] { typeof(Pawn), typeof(float) });
        if (rads == null) return;
        try
        {
            harmony.Patch(rads, prefix: new HarmonyMethod(typeof(RimatomicsDose), nameof(Prefix)) { priority = Priority.First });
            foreach (string name in new[] { NuclearFallout, RadioactiveFallout })
            {
                MethodInfo? fallout = AccessTools.Method(name, new[] { typeof(IntVec3), typeof(Map) });
                if (fallout != null)
                {
                    harmony.Patch(fallout, prefix: new HarmonyMethod(typeof(RimatomicsDose), nameof(FalloutPrefix)),
                        finalizer: new HarmonyMethod(typeof(RimatomicsDose), nameof(FalloutFinalizer)));
                }
            }
            Applied = true;
        }
        catch (Exception error)
        {
            Log.Warning("[D'thorga Medical] Dubs Rimatomics' doses could not be taken: " + error.Message);
        }
    }

    static bool looked;

    /// <summary>Rimatomics' members, read on first use: its settings exist only once its mod has started.</summary>
    static void Look()
    {
        if (looked) return;
        looked = true;
        MethodInfo? robot = AccessTools.Method("Rimatomics.DubUtils:IsRobot", new[] { typeof(Pawn) });
        if (robot != null) isRobot = (Func<Pawn, bool>)Delegate.CreateDelegate(typeof(Func<Pawn, bool>), robot);
        (Type? _, object? settings) = OptionalMods.StaticMember("Rimatomics.RimatomicsMod", "Settings");
        PropertyInfo? strength = settings?.GetType().GetProperty("RadiationStrength");
        if (settings != null && strength != null) strengthSetting = () => strength.GetValue(settings) is float value ? value : 1f;
        (Type? _, object? research) = OptionalMods.StaticMember("Rimatomics.DubDef", "Geigercounter");
        PropertyInfo? finished = research?.GetType().GetProperty("IsFinished");
        if (research != null && finished != null) geigerKnown = () => finished.GetValue(research) is true;
        geigerTick = OptionalMods.StaticMember("Rimatomics.DubDef", "geigerTick").value as SoundDef;
    }

    /// <summary>Whether Rimatomics' Geiger research is done: the colony's dosimeter (owner, 22 Nov 2026). False without Rimatomics.</summary>
    public static bool GeigerKnown
    {
        get
        {
            Look();
            return (geigerKnown?.Invoke() ?? false) || DebugSettings.godMode;
        }
    }

    /// <summary>Takes the dose for a person while the model is on; robots, and everything with the model off, go to Rimatomics.</summary>
    static bool Prefix(Pawn pawn, float strength)
    {
        if (!RadiationPart.On || pawn == null || pawn.Dead) return true;
        Look();
        if (isRobot?.Invoke(pawn) ?? !pawn.RaceProps.IsFlesh) return true;
        float scaled = strength * (strengthSetting?.Invoke() ?? 1f);
        double grays = RadiationRule.GraysFromRimatomics(scaled) * (inFallout ? RadiationRule.FalloutDecay(falloutDays) : 1);
        RadiationService.Receive(pawn, (float)grays, inFallout ? "fallout" : "rimatomics");
        if (pawn.IsColonist && scaled > 0.1f && GeigerKnown && pawn.Spawned)
        {
            geigerTick?.PlayOneShot(new TargetInfo(pawn.Position, pawn.Map));
            MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, ((float)grays * 1000f).ToString("0") + " mGy", Color.green);
        }
        return false;
    }

    static void FalloutPrefix(Map map)
    {
        inFallout = true;
        falloutDays = 0;
        if (map?.GameConditionManager == null) return;
        foreach (GameCondition condition in map.GameConditionManager.ActiveConditions)
        {
            string name = condition.def.defName;
            if (name == "NuclearFallout" || name == "RadioactiveFallout")
            {
                falloutDays = condition.TicksPassed / (double)GenDate.TicksPerDay;
                return;
            }
        }
    }

    static Exception? FalloutFinalizer(Exception? __exception)
    {
        inFallout = false;
        return __exception;
    }
}
