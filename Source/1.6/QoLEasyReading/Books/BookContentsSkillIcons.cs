using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace ZCN.QoL.EasyReading
{
    /// <summary>
    /// BookUI-2-Fix2: only the bookcase Contents row is patched.
    /// Prefix associates the one book with its vanilla label call;
    /// transpiler replaces that single Widgets.Label with a layout-aware call;
    /// postfix draws passive icons after all vanilla buttons have run.
    /// No global GUI or input interception, and no gameplay changes.
    /// </summary>
    [HarmonyPatch(typeof(ITab_ContentsBooks), "DoRow")]
    internal static class BookContentsSkillIcons
    {
        private const float RowHeight = 28f;
        private const float EjectWidth = 24f;
        private const float LabelStartX = 60f;
        private const float IconStartPadding = 3f;
        private const float SlotGap = 2f;
        private const float TitleGap = 5f;
        private const float MinimumTitleWidth = 65f;
        private static Book ActiveBook;
        private static bool TitleHookActive;
        private static bool LoggedFirstRow;
        private static bool LoggedFirstLayout;

        internal static int NormalizeSize(int size)
        {
            return size == 100 || size == 125 || size == 150 ? size : 125;
        }

        private static float IconSize(EasyReadingSettings settings)
        {
            switch (NormalizeSize(settings.bookSkillIconSizePercent))
            {
                case 100: return 18f;
                case 150: return 27f;
                default: return 23f;
            }
        }

        private static bool TryLayout(Book book, float rowWidth, EasyReadingSettings settings,
            out SkillDef first, out SkillDef second, out float iconX, out float slot)
        {
            first = null;
            second = null;
            iconX = 0f;
            slot = 0f;
            if (book == null || settings == null || !settings.showBookSkillIcons)
                return false;
            BookSkillIconCatalog.GetBookSkills(book, out first, out second);
            if (first == null)
                return false;
            slot = IconSize(settings) + SlotGap;
            float labelEnd = rowWidth - EjectWidth;
            if (settings.bookSkillIconPosition == 1)
                iconX = labelEnd - 2f * slot - IconStartPadding;
            else
                iconX = LabelStartX + IconStartPadding;

            // Reserve 2 aligned slots even for a one-skill book. If the
            // tab is too narrow, leave the entire vanilla row untouched.
            float available = settings.bookSkillIconPosition == 1
                ? iconX - TitleGap - LabelStartX
                : labelEnd - (iconX + 2f * slot + TitleGap);
            return available >= MinimumTitleWidth;
        }

        [HarmonyPrefix]
        private static void Prefix(Book book)
        {
            ActiveBook = book;
        }

        // One method-call replacement inside ITab_ContentsBooks.DoRow only.
        // Avoid a global Widgets.Label prefix (high-frequency method).
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo vanillaLabel = AccessTools.Method(typeof(Widgets), "Label",
                new Type[] { typeof(Rect), typeof(string) });
            MethodInfo customLabel = AccessTools.Method(typeof(BookContentsSkillIcons),
                "DrawAdjustedBookTitle", new Type[] { typeof(Rect), typeof(string) });
            bool replaced = false;
            foreach (CodeInstruction instr in instructions)
            {
                if (!replaced && instr.Calls(vanillaLabel))
                {
                    instr.opcode = OpCodes.Call;
                    instr.operand = customLabel;
                    replaced = true;
                }
                yield return instr;
            }
            TitleHookActive = replaced;
            // Keep vanilla method if the target is missing in a future update;
            // never add a global replacement or disable book interactions.
        }

        private static void DrawAdjustedBookTitle(Rect rect, string title)
        {
            try
            {
                EasyReadingSettings settings = EasyReadingMod.Settings;
                SkillDef first, second;
                float x, slot;
                if (!TryLayout(ActiveBook, rect.xMax + EjectWidth, settings,
                    out first, out second, out x, out slot))
                {
                    Widgets.Label(rect, title);
                    return;
                }
                if (settings.bookSkillIconPosition == 1)
                    rect.xMax = x - TitleGap;
                else
                    rect.xMin = x + 2f * slot + TitleGap;
                if (rect.width > 0f)
                    Widgets.Label(rect, title.Truncate(rect.width));
            }
            catch (Exception ex)
            {
                // Do not break vanilla bookcase UI if a modded book throws.
                Log.ErrorOnce("[QoL: Easy Reading][BookUI] Title layout failed: " + ex,
                    1726194279);
                Widgets.Label(rect, title);
            }
        }

        [HarmonyPostfix]
        private static void Postfix(Book book, float width, ref float curY)
        {
            ActiveBook = null;
            EasyReadingSettings settings = EasyReadingMod.Settings;
            if (book == null || settings == null || !settings.showBookSkillIcons)
                return;
            try
            {
                if (Event.current == null)
                    return;
                if (EasyReadingDebug.Enabled && Event.current.type == EventType.Repaint)
                    BookSkillIconCatalog.AuditAssetsIfDebug();

                // If another RimWorld release or a mod changed DoRow's IL,
                // do not risk drawing over unshifted text.
                if (!TitleHookActive)
                {
                    if (EasyReadingDebug.Enabled)
                        Log.WarningOnce("[QoL: Easy Reading][BookUI] Book title layout hook not found; skill icons suppressed for safety.",
                            1726194281);
                    return;
                }
                SkillDef first, second;
                float x, slot;
                if (!TryLayout(book, width, settings, out first, out second, out x, out slot))
                    return;
                if (EasyReadingDebug.Enabled && !LoggedFirstRow &&
                    Event.current.type == EventType.Repaint)
                {
                    LoggedFirstRow = true;
                    Log.Message("[QoL: Easy Reading][BookUI] First Contents textbook: " +
                        book.LabelNoCount + " -> " + first.defName +
                        (second == null ? "" : ", " + second.defName));
                }
                if (EasyReadingDebug.Enabled && !LoggedFirstLayout &&
                    Event.current.type == EventType.Repaint)
                {
                    LoggedFirstLayout = true;
                    Log.Message("[QoL: Easy Reading][BookUI] Layout ACTIVE, " +
                        "icon size=" + NormalizeSize(settings.bookSkillIconSizePercent) +
                        "%, position=" + (settings.bookSkillIconPosition == 0 ? "Left" : "Right") +
                        ", title reserved=yes.");
                }
                float size = IconSize(settings);
                float y = curY - RowHeight + (RowHeight - size) / 2f;
                PaintOne(first, new Rect(x, y, size, size));
                if (second != null)
                    PaintOne(second, new Rect(x + slot, y, size, size));
            }
            catch (Exception ex)
            {
                Log.ErrorOnce("[QoL: Easy Reading][BookUI] Contents layout/draw failed: " + ex,
                    1726194275);
            }
        }

        private static void PaintOne(SkillDef skill, Rect rect)
        {
            if (skill == null) return;
            Texture2D texture = BookSkillIconCatalog.GetTexture(skill);
            if (texture == null) return;
            if (Event.current.type == EventType.Repaint)
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
