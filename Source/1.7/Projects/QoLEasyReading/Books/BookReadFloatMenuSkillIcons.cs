using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace ZCN.QoL.EasyReading
{
    /// <summary>
    /// Stage 4S-2E1. Decorate ONLY the first, vanilla 'Read now' option
    /// returned by Book.GetFloatMenuOptions. Never replace its Action,
    /// change the book's effects, or globally intercept FloatMenuOption GUI.
    /// Book.GetFloatMenuOptions uses yield return in RimWorld 1.6 and the original 1.7 source; the
    /// wrapper decorates each option during enumeration, before FloatMenu
    /// calculates its layout using extraPartWidth/SetSizeMode.
    /// </summary>
    [HarmonyPatch(typeof(Book), nameof(Book.GetFloatMenuOptions))]
    internal static class BookReadFloatMenuSkillIcons
    {
        private const float Gap = 3f;
        private const float EndPadding = 6f;
        private static bool LoggedFirstDecoration;
        private static bool LoggedFirstPaint;

        [HarmonyPostfix]
        private static void Postfix(Book __instance,
            ref IEnumerable<FloatMenuOption> __result)
        {
            if (__instance == null || __result == null)
                return;
            EasyReadingSettings settings = EasyReadingMod.Settings;
            if (settings == null || !settings.showReadMenuSkillIcons)
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
                SkillDef first;
                SkillDef second;
                BookSkillIconCatalog.GetBookSkills(book, out first, out second);
                if (first == null)
                    return;  // Non-textbooks: exactly vanilla menu.

                // Reserve 2 slots for consistent alignment regardless of
                // the number of skills. The vanilla label automatically
                // gets a smaller layout rect, avoiding overlap.
                EasyReadingSettings settings = EasyReadingMod.Settings;
                if (settings == null || !settings.showReadMenuSkillIcons)
                    return;
                float size = IconSize(settings.bookSkillIconSizePercent);
                option.extraPartWidth = size * 2f + Gap + EndPadding;
                option.extraPartRightJustified = true;
                option.extraPartOnGUI = rect => DrawIcons(rect, first, second, size);
                if (EasyReadingDebug.Enabled && !LoggedFirstDecoration)
                {
                    LoggedFirstDecoration = true;
                    Log.Message("[QoL: Easy Reading][ReadMenu] Decorated textbook: " +
                        book.LabelNoCount + " => " + first.defName +
                        (second == null ? "" : ", " + second.defName) +
                        "; vanillaActionPreserved=True; slots=2.");
                }
            }
            catch (Exception ex)
            {
                // Any broken modded book must leave its Read option usable.
                Log.ErrorOnce("[QoL: Easy Reading][ReadMenu] Skipped textbook icon: " + ex,
                    1726194380);
            }
        }

        private static float IconSize(int percent)
        {
            switch (BookContentsSkillIcons.NormalizeSize(percent))
            {
                case 100: return 18f;
                case 150: return 27f;
                default: return 23f;
            }
        }

        private static bool DrawIcons(Rect rect, SkillDef first, SkillDef second,
            float size)
        {
            try
            {
                if (Event.current == null)
                    return false;
                float x = rect.x + 1f;
                float y = rect.y + (rect.height - size) * 0.5f;
                DrawOne(first, new Rect(x, y, size, size));
                if (second != null)
                    DrawOne(second, new Rect(x + size + Gap, y, size, size));
                if (EasyReadingDebug.Enabled && !LoggedFirstPaint &&
                    Event.current.type == EventType.Repaint)
                {
                    LoggedFirstPaint = true;
                    Log.Message("[QoL: Easy Reading][ReadMenu] First textbook icon paint: " +
                        (second == null ? "1" : "2") + " skill(s); " +
                        "icons at row right; extraPart does not consume clicks.");
                }
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
