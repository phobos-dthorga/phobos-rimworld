using System.Collections.Generic;
using HarmonyLib;
using Phobos.Framework.Health;
using RimWorld;
using UnityEngine;
using Verse;

namespace Phobos.Medical;

/// <summary>
/// The per-person care choices (D'thorga Framework's PatientPreferences: today triage's "how far the doctors may go") as small buttons
/// on the health tab, right beside the game's medicine setting (owner, 7 Nov 2026). Each opens the same menu as the pawn's own button.
/// The game draws the medicine row inside the private HealthCardUtility.DrawOverviewTab, with no room for a row of our own short of a
/// transpiler, so a postfix on MedicalCareUtility.MedicalCareSelectButton draws the buttons just left of the medicine button, only while
/// the health tab is drawing (a flag set around DrawOverviewTab, since other windows call the same button). Presentation only: picking a
/// choice goes through the framework's menu, which saves it on the pawn's patient comp.
/// </summary>
public static class HealthTabChoices
{
    const float Gap = 4f;

    [System.ThreadStatic]
    static bool drawingHealthTab;

    [HarmonyPatch(typeof(HealthCardUtility), "DrawOverviewTab")]
    public static class Patch_DrawOverviewTab
    {
        public static void Prefix() => drawingHealthTab = true;

        public static void Finalizer() => drawingHealthTab = false;
    }

    [HarmonyPatch(typeof(MedicalCareUtility), nameof(MedicalCareUtility.MedicalCareSelectButton))]
    public static class Patch_MedicalCareSelectButton
    {
        public static void Postfix(Rect rect, Pawn pawn)
        {
            if (!drawingHealthTab || pawn == null || pawn.TryGetComp<CompPhobosPatient>() == null || Patients.GroupOf(pawn) == null) return;
            IReadOnlyList<PatientPreference> preferences = PatientPreferences.All;
            float size = rect.height;
            float x = rect.x - Gap - size;
            for (int i = 0; i < preferences.Count; i++)
            {
                PatientPreference preference = preferences[i];
                if (preference.Applies != null && !preference.Applies(pawn)) continue;
                var button = new Rect(x, rect.y, size, size);
                TooltipHandler.TipRegion(button, PatientPreferences.LabelFor(pawn, preference) + "\n\n" + preference.DescKey.Translate());
                if (Widgets.ButtonImage(button.ContractedBy(1f), PatientPreferences.IconFor(preference)))
                {
                    Find.WindowStack.Add(new FloatMenu(PatientPreferences.MenuFor(pawn, preference)));
                }
                x -= size + Gap;
            }
        }
    }
}
