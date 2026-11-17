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
    /// BookUI-2-Fix2 / S3-A1: retain the one ITab_ContentsBooks.DoRow patch.
    /// Only a single existing Widgets.Label call is replaced. The title and
    /// icon share one row plan; no global UI hook or book/research mutation.
    /// </summary>
    [HarmonyPatch(typeof(ITab_ContentsBooks), "DoRow")]
    internal static class BookContentsSkillIcons
    {
        private const float RowHeight = 28f;
        private static Book ActiveBook;
        private static Book PlannedBook;
        private static BookContentsIconLayout ActiveLayout;
        private static bool HasLayout;
        private static bool TitleHookActive;
#if DEBUG
        private static bool LoggedFirstRow;
        private static bool LoggedFirstLayout;
        private static int LoggedSchematicKinds;
        private static readonly HashSet<string> LoggedSchematicSuppressions =
            new HashSet<string>(StringComparer.Ordinal);
#endif

        [HarmonyPrefix]
        private static void Prefix(Book book)
        {
            ActiveBook = book;
            PlannedBook = null;
            HasLayout = false;
            ActiveLayout = default(BookContentsIconLayout);
        }

        // Same existing one-method-call transpiler; never patch Widgets.Label globally.
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
        }

        private static void DrawAdjustedBookTitle(Rect rect, string title)
        {
            try
            {
                BookContentsIconLayout layout;
                string suppressed;
                if (!BookContentsIconLayout.TryCreate(ActiveBook, rect,
                    EasyReadingMod.Settings, out layout, out suppressed))
                {
#if DEBUG
                    MaybeLogSchematicSuppression(ActiveBook, suppressed);
#endif
                    Widgets.Label(rect, title);
                    return;
                }
                Rect adjusted = layout.AdjustTitle(rect);
                if (adjusted.width <= 0f)
                {
                    Widgets.Label(rect, title);
                    return;
                }
                Widgets.Label(adjusted, title.Truncate(adjusted.width));
                // Publish after title drawing succeeds; postfix consumes precisely
                // this state without running the research classifier a second time.
                ActiveLayout = layout;
                PlannedBook = ActiveBook;
                HasLayout = true;
            }
            catch (Exception ex)
            {
                HasLayout = false;
                PlannedBook = null;
                Log.ErrorOnce("[QoL: Easy Reading][BookUI] Title layout failed: " + ex,
                    1726194279);
                Widgets.Label(rect, title);
            }
        }

        [HarmonyPostfix]
        private static void Postfix(Book book, float width, ref float curY)
        {
            // Clear row state even when the screen is not repainting or the
            // title hook was skipped by another UI patch.
            BookContentsIconLayout layout = ActiveLayout;
            bool hasMatchingLayout = HasLayout && object.ReferenceEquals(PlannedBook, book);
            ActiveBook = null;
            PlannedBook = null;
            HasLayout = false;
            ActiveLayout = default(BookContentsIconLayout);
            if (!TitleHookActive)
            {
                Log.WarningOnce("[QoL: Easy Reading][BookUI] Book title layout hook not found; icons suppressed for safety.",
                        1726194281);
                return;
            }
            if (!hasMatchingLayout || book == null)
                return;

            try
            {
                if (Event.current == null) return;
#if DEBUG
                if (layout.Source == BookContentsIconSource.Textbook &&
                    EasyReadingDebug.Enabled && Event.current.type == EventType.Repaint)
                    BookSkillIconCatalog.AuditAssetsIfDebug();
#endif

                if (layout.Source == BookContentsIconSource.Textbook)
                {
#if DEBUG
                    LogExistingTextbookAudit(book, layout);
#endif
                    float y = curY - RowHeight + (RowHeight - layout.Size) / 2f;
                    PaintOne(layout.FirstSkill, new Rect(layout.IconX, y,
                        layout.Size, layout.Size));
                    if (layout.SecondSkill != null)
                        PaintOne(layout.SecondSkill, new Rect(layout.IconX + layout.Slot,
                            y, layout.Size, layout.Size));
                }
                else if (layout.Source == BookContentsIconSource.Schematic)
                {
#if DEBUG
                    LogSchematicAudit(layout);
#endif
                    float y = curY - RowHeight + (RowHeight - layout.Size) / 2f;
                    PaintSchematic(layout, new Rect(layout.IconX, y,
                        layout.Size, layout.Size));
                }
            }
            catch (Exception ex)
            {
                Log.ErrorOnce("[QoL: Easy Reading][BookUI] Contents layout/draw failed: " + ex,
                    1726194275);
            }
        }

#if DEBUG
        private static void LogExistingTextbookAudit(Book book, BookContentsIconLayout layout)
        {
            if (!EasyReadingDebug.Enabled || Event.current.type != EventType.Repaint)
                return;
            if (!LoggedFirstRow)
            {
                LoggedFirstRow = true;
                Log.Message("[QoL: Easy Reading][BookUI] First Contents textbook: " +
                    book.LabelNoCount + " -> " + layout.FirstSkill.defName +
                    (layout.SecondSkill == null ? "" : ", " + layout.SecondSkill.defName));
            }
            if (!LoggedFirstLayout)
            {
                LoggedFirstLayout = true;
                Log.Message("[QoL: Easy Reading][BookUI] Layout ACTIVE, " +
                    "icon size=" + BookIconMetrics.NormalizeSize(
                        EasyReadingMod.Settings.bookSkillIconSizePercent) +
                    "%, position=" + (layout.Position == 0 ? "Left" : "Right") +
                    ", title reserved=yes.");
            }
        }

        private static void LogSchematicAudit(BookContentsIconLayout layout)
        {
            if (!EasyReadingDebug.Enabled || Event.current.type != EventType.Repaint)
                return;
            int bit = layout.SchematicKind == SchematicIconKind.AllFinished ? 2 : 1;
            if ((LoggedSchematicKinds & bit) != 0) return;
            LoggedSchematicKinds |= bit;
            Log.Message("[QoL: Easy Reading][S3A1][BookcaseIcon] bookDef=Schematic; " +
                "classification=" + layout.Classification +
                "; finished=" + layout.FinishedCount +
                "; projects=" + layout.ProjectCount +
                "; icon=" + layout.SchematicKind +
                "; slots=" + layout.SlotCount +
                "; titleReserved=True; BUILD_ID=" + EasyReadingBuildIdentity.BuildId + ";");
        }

        private static void MaybeLogSchematicSuppression(Book book, string reason)
        {
            if (reason == null || !EasyReadingDebug.Enabled ||
                Event.current == null || Event.current.type != EventType.Repaint ||
                !SchematicBookcaseIconResolver.IsSchematic(book) ||
                LoggedSchematicSuppressions.Count >= 8 ||
                !LoggedSchematicSuppressions.Add(reason))
                return;
            Log.Message("[QoL: Easy Reading][S3A1][BookcaseIcon] " +
                "suppressed=" + reason + "; BUILD_ID=" + EasyReadingBuildIdentity.BuildId + ";");
        }

#endif

        private static void PaintSchematic(BookContentsIconLayout layout, Rect rect)
        {
            if (layout.SchematicTexture == null) return;
            if (Event.current.type == EventType.Repaint)
            {
                Color saved = GUI.color;
                try
                {
                    GUI.color = Color.white;
                    GUI.DrawTexture(rect, layout.SchematicTexture);
                }
                finally { GUI.color = saved; }
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
        }
    }
}
