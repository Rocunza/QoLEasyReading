using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace ZCN.QoL.EasyReading
{
    /// <summary>
    /// Stage 4S-2E1 / S3-A2-HF2B2. Decorate ONLY the first, vanilla 'Read now'
    /// option returned by Book.GetFloatMenuOptions. Never replace its Action,
    /// change the book's effects, or globally intercept FloatMenuOption GUI.
    /// Book.GetFloatMenuOptions uses yield return in RimWorld 1.6; the
    /// wrapper decorates each option during enumeration, before FloatMenu
    /// calculates its layout using extraPartWidth/SetSizeMode.
    /// </summary>
    [HarmonyPatch(typeof(Book), nameof(Book.GetFloatMenuOptions))]
    internal static class BookReadFloatMenuSkillIcons
    {
        private const float Gap = 3f;
        private const float EndPadding = 6f;
#if DEBUG
        private static bool LoggedFirstDecoration;
        private static bool LoggedFirstPaint;
        private static bool LoggedSchematicPaint;
        private static int LoggedSchematicDecorations;
        private static bool LoggedSchematicMissingTexture;
        private static bool LoggedSchematicBenefitUnavailable;
#endif

        [HarmonyPostfix]
        private static void Postfix(Book __instance,
            ref IEnumerable<FloatMenuOption> __result)
        {
            if (__instance == null || __result == null)
                return;
            EasyReadingSettings settings = EasyReadingMod.Settings;
            if (settings == null)
                return;
            // Avoid wrapping other books when only Schematic icons are enabled.
            if (!settings.showReadMenuSkillIcons &&
                (!settings.showSchematicReadMenuIcons ||
                 !SchematicStatusIconResolver.IsSchematic(__instance)))
                return;
            __result = DecorateFirstReadOption(__instance, __result);
        }

        private static IEnumerable<FloatMenuOption> DecorateFirstReadOption(
            Book book, IEnumerable<FloatMenuOption> vanillaOptions)
        {
            bool isFirst = true;
            foreach (FloatMenuOption option in vanillaOptions)
            {
                if (isFirst)
                {
                    isFirst = false;
                    TryDecorate(book, option);
                }
                yield return option;
            }
        }

        private static void TryDecorate(Book book, FloatMenuOption option)
        {
            // Leave other mods' own extra widgets untouched. The first
            // Book option is vanilla AssignReadNow, including its disabled
            // variant (which still benefits from a skill identifier).
            if (option == null || option.extraPartOnGUI != null ||
                option.extraPartWidth > 0f)
                return;
            try
            {
                EasyReadingSettings settings = EasyReadingMod.Settings;
                if (settings == null)
                    return;

                // Check the exact vanilla Schematic def before trying Textbook.
                // A disabled Textbook toggle must never suppress Schematic.
                if (SchematicStatusIconResolver.IsSchematic(book))
                {
                    if (settings.showSchematicReadMenuIcons)
                        TryDecorateSchematic(book, option, settings);
                    return;
                }
                if (!settings.showReadMenuSkillIcons)
                    return;

                BookIconPresentation presentation = BookIconResolver.ResolveTextbook(book);
                SkillDef first = presentation.FirstSkill;
                SkillDef second = presentation.SecondSkill;
                if (first == null)
                    return;  // Non-textbooks: exactly vanilla menu.

                // Reserve exactly the number of icons this textbook needs.
                // FloatMenuOption owns the right-justified extra region; keep
                // the final icon aligned with the right edge for 1 or 2 skills.
                float size = BookIconMetrics.IconSize(settings.bookSkillIconSizePercent);
                int slots = second == null ? 1 : 2;
                option.extraPartWidth = slots == 1
                    ? size + EndPadding : size * 2f + Gap + EndPadding;
                option.extraPartRightJustified = true;
                option.extraPartOnGUI = rect => DrawIcons(rect, first, second, size);
#if DEBUG
                if (EasyReadingDebug.Enabled && !LoggedFirstDecoration)
                {
                    LoggedFirstDecoration = true;
                    Log.Message("[QoL: Easy Reading][ReadMenu] Decorated textbook: " +
                        book.LabelNoCount + " => " + first.defName +
                        (second == null ? "" : ", " + second.defName) +
                        "; vanillaActionPreserved=True; slots=" + slots +
                        "; extraPartWidth=" + option.extraPartWidth + ".");
                }
#endif
            }
            catch (Exception ex)
            {
                // Any broken modded book must leave its Read option usable.
                Log.ErrorOnce("[QoL: Easy Reading][ReadMenu] Skipped book icon: " + ex,
                    1726194380);
            }
        }

        // S3-A2: 1-slot Schematic decoration. Only immutable drawing values are
        // captured in the callback: no Book, ResearchProjectDef or Pawn reference.
        private static void TryDecorateSchematic(Book book, FloatMenuOption option,
            EasyReadingSettings settings)
        {
            SchematicIconKind kind;
            SchematicResearchClassification classification;
            int finishedCount, projectCount;
            if (!SchematicStatusIconResolver.TryResolve(book, out kind,
                out finishedCount, out projectCount, out classification))
                return; // Unknown / UnavailableNow / NotApplicable: vanilla only.

            Texture2D texture = SchematicStatusIconCatalog.GetTexture(kind);
            if (texture == null)
            {
#if DEBUG
                if (EasyReadingDebug.Enabled && !LoggedSchematicMissingTexture)
                {
                    LoggedSchematicMissingTexture = true;
                    Log.Warning("[QoL: Easy Reading][S3A2][ReadMenu] Schematic " +
                        "icon texture unavailable; menu unchanged; BUILD_ID=" +
                        EasyReadingBuildIdentity.BuildId + ";");
                }
#endif
                return;
            }

            // Capture the vanilla benefit lines once for this Read option.
            // FloatMenuOption.DoGUI owns the whole option-row tooltip rect.
            // If another mod makes benefits unavailable, retain the icon/action.
            string benefitTooltip, benefitReason;
            if (!SchematicResearchBenefitTooltip.TryBuild(book,
                out benefitTooltip, out benefitReason))
            {
                benefitTooltip = null;
#if DEBUG
                if (EasyReadingDebug.Enabled && !LoggedSchematicBenefitUnavailable)
                {
                    LoggedSchematicBenefitUnavailable = true;
                    Log.Warning("[QoL: Easy Reading][S3A2-HF2B2][ReadMenu] " +
                        "Vanilla research benefits unavailable (icon retained): " +
                        benefitReason + "; BUILD_ID=" + EasyReadingBuildIdentity.BuildId + ";");
                }
#endif
            }

            // Verse.FloatMenuOption.tooltip is Nullable<TipSignal> in RW 1.6.
            // DoGUI registers it against the WHOLE row Rect (not iconRect).
            // Never steal another mod's existing tooltip or mouse callbacks.
#if DEBUG
            string tooltipOwner = option.tooltip.HasValue ? "EXTERNAL" : "NONE";
#endif
            if (!option.tooltip.HasValue && !string.IsNullOrEmpty(benefitTooltip))
            {
                option.tooltip = new TipSignal(benefitTooltip);
#if DEBUG
                tooltipOwner = "EASY_READING";
#endif
            }

            float size = BookIconMetrics.IconSize(settings.bookSkillIconSizePercent);
            // Reserve space only after classification and resource succeed.
            // Do not touch option.action/Chosen or a pre-existing extraPart.
            option.extraPartWidth = size + EndPadding;
            option.extraPartRightJustified = true;
            option.extraPartOnGUI = rect => DrawSchematic(rect, texture, size);

#if DEBUG
            if (EasyReadingDebug.Enabled)
            {
                int bit = kind == SchematicIconKind.AllFinished ? 2 : 1;
                if ((LoggedSchematicDecorations & bit) == 0)
                {
                    LoggedSchematicDecorations |= bit;
                    Log.Message("[QoL: Easy Reading][S3A2][ReadMenu] bookDef=Schematic; " +
                        "classification=" + classification + "; finished=" + finishedCount +
                        "; projects=" + projectCount + "; icon=" + kind +
                        "; slots=1; vanillaActionPreserved=True; extraPartExisting=False; " +
                        "tooltipOwner=" + tooltipOwner + "; tooltipScope=ROW; " +
                        "BUILD_ID=" + EasyReadingBuildIdentity.BuildId + ";");
                }
            }
#endif
        }

        private static bool DrawSchematic(Rect rect, Texture2D texture,
            float size)
        {
            try
            {
                if (Event.current == null || texture == null)
                    return false;
                Rect iconRect = new Rect(rect.x + 1f,
                    rect.y + (rect.height - size) * 0.5f, size, size);
                if (Event.current.type == EventType.Repaint)
                {
                    Color saved = GUI.color;
                    try
                    {
                        GUI.color = Color.white;
                        GUI.DrawTexture(iconRect, texture);
                    }
                    finally { GUI.color = saved; }
#if DEBUG
                    if (EasyReadingDebug.Enabled && !LoggedSchematicPaint)
                    {
                        LoggedSchematicPaint = true;
                        Log.Message("[QoL: Easy Reading][S3A2][ReadMenu] " +
                            "First schematic icon paint: slots=1; " +
                            "extraPartDoesNotConsumeClicks=True; BUILD_ID=" +
                            EasyReadingBuildIdentity.BuildId + ";");
                    }
#endif
                }
            }
            catch (Exception ex)
            {
                Log.ErrorOnce("[QoL: Easy Reading][S3A2][ReadMenu] " +
                    "Schematic icon paint skipped: " + ex, 1726194382);
            }
            return false; // Never consume the original FloatMenuOption click.
        }

        private static bool DrawIcons(Rect rect, SkillDef first, SkillDef second,
            float size)
        {
            try
            {
                if (Event.current == null)
                    return false;
                // The 1-skill row now reserves one slot rather than two.
                // A 1-skill icon and the rightmost 2-skill icon share the
                // same visual right margin; Schematic uses its own renderer.
                float x = rect.x + 1f;
                float y = rect.y + (rect.height - size) * 0.5f;
                if (second == null)
                    DrawOne(first, new Rect(x, y, size, size));
                else
                {
                    DrawOne(first, new Rect(x, y, size, size));
                    DrawOne(second, new Rect(x + size + Gap, y, size, size));
                }
#if DEBUG
                if (EasyReadingDebug.Enabled && !LoggedFirstPaint &&
                    Event.current.type == EventType.Repaint)
                {
                    LoggedFirstPaint = true;
                    Log.Message("[QoL: Easy Reading][ReadMenu] First textbook icon paint: " +
                        (second == null ? "1" : "2") + " skill(s); " +
                        "icons at row right; extraPart does not consume clicks.");
                }
#endif
            }
            catch (Exception ex)
            {
                Log.ErrorOnce("[QoL: Easy Reading][ReadMenu] Icon painting failed: " + ex,
                    1726194381);
            }
            return false; // Vanilla option still owns the entire click.
        }

        private static void DrawOne(SkillDef skill, Rect rect)
        {
            if (skill == null)
                return;
            Texture2D texture = BookSkillIconCatalog.GetTexture(skill);
            if (texture != null && Event.current.type == EventType.Repaint)
            {
                Color saved = GUI.color;
                try
                {
                    GUI.color = Color.white;
                    GUI.DrawTexture(rect, texture);
                }
                finally { GUI.color = saved; }
            }
            TooltipHandler.TipRegion(rect, skill.LabelCap.ToString());
        }
    }
}
