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
    /// 4S-2H3A: Trade book skill markers (left/right) with Useful Marks compatibility.
    /// The vanilla Useful Marks transpiler replaces the vanilla Widgets.Label
    /// (Rect,string) title call with its MarkedLabel(Rect,Transferable) call.
    /// Never replace/bypass that third-party method: leave its call intact and
    /// adjust ONLY the Rect argument immediately before the matched call.
    /// Harmony Priority.Last ensures our IL step runs after normal-priority
    /// patches, including Useful Marks, regardless of mod-loading order.
    /// Fail closed if there is more than one possible label target.
    /// </summary>
    [HarmonyPatch(typeof(TransferableUIUtility), nameof(TransferableUIUtility.DrawTransferableInfo),
        new Type[] { typeof(Transferable), typeof(Rect), typeof(Color) })]
    internal static class BookTradeSkillIcons
    {
        private const float LeftContentWidth = 80f; // vanilla thing icon/info button
        private const float MinimumNameWidth = 65f;
        private const float SlotGap = 3f;
        private const float LeftGap = 3f;
        private const float EndPadding = 5f;
        private static bool LoggedFirstPaint;
        private static bool LoggedMixedGroup;
        private static bool LoggedLeftHookWarning;
        private static bool LeftTitleHookReady;

        // DrawTransferableInfo is a GUI call. Limit the temporary title shift
        // to its current invocation, restoring it in Postfix even when OFF.
        [ThreadStatic]
        private static float activeLeftTitleShift;

        private struct RowState
        {
            internal bool Paint;
            internal bool Left;
            internal SkillDef First;
            internal SkillDef Second;
            internal Rect FirstIcon;
            internal Rect SecondIcon;
        }

        // Method signature differs under Useful Marks:
        // vanilla:        Widgets.Label(Rect, string)
        // Useful Marks:  MarkedLabel(Rect, Transferable)
        // Consequently a wrapper that assumes the second arg is a string is
        // invalid when Useful Marks is installed. We preserve the original
        // callee and insert: stloc secondArg; call IndentNameRect; ldloc secondArg.
        // This modifies only the Rect and keeps the third-party draw behavior.
        [HarmonyTranspiler]
        [HarmonyPriority(Priority.Last)]
        private static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            List<CodeInstruction> code = new List<CodeInstruction>(instructions);
            MethodInfo vanillaLabel = AccessTools.Method(typeof(Widgets),
                "Label", new Type[] { typeof(Rect), typeof(string) });
            MethodInfo usefulMarksLabel = AccessTools.Method(
                "UsefulMarks.TransferableOneWayWidget_Patch:MarkedLabel",
                new Type[] { typeof(Rect), typeof(Transferable) });
            MethodInfo indentRect = AccessTools.Method(typeof(BookTradeSkillIcons),
                nameof(IndentNameRect));

            int matches = 0;
            int matchIndex = -1;
            MethodInfo matchedMethod = null;
            for (int i = 0; i < code.Count; i++)
            {
                if (code[i].opcode != OpCodes.Call &&
                    code[i].opcode != OpCodes.Callvirt)
                    continue;
                MethodInfo invoked = code[i].operand as MethodInfo;
                if (invoked == null)
                    continue;
                if ((vanillaLabel != null && Equals(invoked, vanillaLabel)) ||
                    (usefulMarksLabel != null && Equals(invoked, usefulMarksLabel)))
                {
                    matches++;
                    matchIndex = i;
                    matchedMethod = invoked;
                }
            }
            if (matches != 1 || matchedMethod == null || indentRect == null ||
                generator == null)
            {
                LeftTitleHookReady = false;
                Log.Warning("[QoL: Easy Reading][TradeUI] Left title hook unavailable " +
                    "(matches=" + matches + "); markers safely disabled.");
                return code;
            }

            // Never insert inside an exception block boundary whose position
            // may have special Harmony semantics. Failing closed is safer.
            if (code[matchIndex].blocks.Count != 0)
            {
                LeftTitleHookReady = false;
                Log.Warning("[QoL: Easy Reading][TradeUI] Left title hook " +
                    "skipped at exception boundary; markers safely disabled.");
                return code;
            }

            ParameterInfo[] ps = matchedMethod.GetParameters();
            if (ps.Length != 2 || ps[0].ParameterType != typeof(Rect) ||
                (ps[1].ParameterType != typeof(string) &&
                 ps[1].ParameterType != typeof(Transferable)))
            {
                LeftTitleHookReady = false;
                Log.Warning("[QoL: Easy Reading][TradeUI] Unexpected label signature; " +
                    "left markers safely disabled.");
                return code;
            }

            // The evaluation stack before the original call is:
            //   [..., Rect, string]       (vanilla)
            //   [..., Rect, Transferable] (Useful Marks)
            // Temporarily store the second argument to allow the Rect to be
            // adjusted, then restore it without modifying the original call.
            LocalBuilder secondArg = generator.DeclareLocal(ps[1].ParameterType);
            CodeInstruction store = new CodeInstruction(OpCodes.Stloc, secondArg);
            CodeInstruction adjust = new CodeInstruction(OpCodes.Call, indentRect);
            CodeInstruction restore = new CodeInstruction(OpCodes.Ldloc, secondArg);
            if (code[matchIndex].labels.Count > 0)
            {
                store.labels.AddRange(code[matchIndex].labels);
                code[matchIndex].labels.Clear();
            }
            code.InsertRange(matchIndex, new CodeInstruction[] {
                store, adjust, restore
            });
            LeftTitleHookReady = true;
            Log.Message("[QoL: Easy Reading][TradeUI] Left title hook applied " +
                "(target=" + (ps[1].ParameterType == typeof(Transferable)
                    ? "UsefulMarks.MarkedLabel" : "Vanilla.Widgets.Label") +
                ", matches=" + matches + "); original draw preserved.");
            return code;
        }

        private static Rect IndentNameRect(Rect rect)
        {
            float shift = activeLeftTitleShift;
            if (shift > 0f)
            {
                rect.xMin += shift;
                if (rect.width < 0f)
                    rect.width = 0f;
            }
            return rect;
        }

        [HarmonyPrefix]
        private static void Prefix(Transferable trad, ref Rect idRect, out RowState __state)
        {
            __state = default(RowState);
            activeLeftTitleShift = 0f;
            EasyReadingSettings settings = EasyReadingMod.Settings;
            if (settings == null || !settings.showTradeSkillIcons ||
                !TradeSession.Active || !(trad is Tradeable) ||
                Event.current == null)
                return;
            try
            {
                Tradeable tradeable = (Tradeable)trad;
                // The check prevents error logging inside AnyThing on empty rows.
                if (!tradeable.IsThing || !tradeable.HasAnyThing ||
                    !(tradeable.AnyThing is Book))
                    return;

                SkillDef first;
                SkillDef second;
                if (!TryGetUniformSkills(tradeable, out first, out second))
                    return;

                float iconSize = IconSize(settings.bookSkillIconSizePercent);
                // Fit even the largest (27px) icon within the current row.
                // Smaller UI scales may make the row shorter.
                iconSize = Mathf.Min(iconSize, idRect.height - 2f);
                if (iconSize < 8f)
                    return;

                // Reserve only the actual skill slots (one or two).
                // Two-skill layout reserves the same width as the previous
                // calculation; one-skill titles no longer waste a blank slot.
                float reserve = LeftGap + iconSize +
                    (second != null ? iconSize + SlotGap : 0f) + EndPadding;
                if (idRect.width < LeftContentWidth + MinimumNameWidth + reserve)
                    return; // Tiny windows: vanilla label wins; never overlap.

                bool placeLeft = settings.tradeSkillIconPosition == 0;
                if (placeLeft && !LeftTitleHookReady)
                {
                    if (!LoggedLeftHookWarning)
                    {
                        LoggedLeftHookWarning = true;
                        Log.Warning("[QoL: Easy Reading][TradeUI] Left markers " +
                            "hidden because title-indent hook is unavailable.");
                    }
                    return;
                }

                // RIGHT: F1 placement at the end of the item's name column.
                // LEFT: after the vanilla 27px thing icon and info button,
                // before the title that the narrow transpiler shifts right.
                float startX = placeLeft
                    ? idRect.x + LeftContentWidth + LeftGap
                    : idRect.xMax - reserve + LeftGap;
                float y = idRect.y + (idRect.height - iconSize) * 0.5f;
                __state.First = first;
                __state.Second = second;
                __state.Left = placeLeft;
                __state.FirstIcon = new Rect(startX, y, iconSize, iconSize);
                __state.SecondIcon = new Rect(startX + iconSize + SlotGap, y,
                    iconSize, iconSize);
                __state.Paint = true;

                if (placeLeft)
                {
                    // Only Widgets.Label's book title gets indented. The
                    // ordinary info button, icon and full-row tooltip remain.
                    activeLeftTitleShift = reserve;
                }
                else
                {
                    // Existing F1 path: clip label by narrowing idRect;
                    // do not shift the title or the trade controls.
                    idRect.width -= reserve;
                }
            }
            catch (Exception ex)
            {
                // Never impair an otherwise functional trade window.
                __state = default(RowState);
                activeLeftTitleShift = 0f;
                Log.ErrorOnce("[QoL: Easy Reading][TradeUI] Skill layout skipped: " + ex,
                    1726194620);
            }
        }

        [HarmonyPostfix]
        private static void Postfix(RowState __state)
        {
            // Clear the title shift before painting tooltips/icons, avoiding
            // any spillover to later GUI rows or unrelated label rendering.
            activeLeftTitleShift = 0f;
            if (!__state.Paint || Event.current == null)
                return;
            try
            {
                PaintOne(__state.First, __state.FirstIcon);
                if (__state.Second != null)
                    PaintOne(__state.Second, __state.SecondIcon);
                if (EasyReadingDebug.Enabled && !LoggedFirstPaint &&
                    Event.current.type == EventType.Repaint)
                {
                    LoggedFirstPaint = true;
                    Log.Message("[QoL: Easy Reading][TradeUI] First trade textbook icons " +
                        "painted: " + __state.First.defName +
                        (__state.Second == null ? "" : ", " + __state.Second.defName) +
                        "; position=" + (__state.Left ? "Left" : "Right") +
                        "; titleReserved=True; input/price/count untouched=True.");
                }
            }
            catch (Exception ex)
            {
                Log.ErrorOnce("[QoL: Easy Reading][TradeUI] Skill painting skipped: " + ex,
                    1726194621);
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

        private static bool TryGetUniformSkills(Tradeable tradeable,
            out SkillDef first, out SkillDef second)
        {
            first = null;
            second = null;
            Book sample = tradeable.AnyThing as Book;
            if (sample == null)
                return false;
            BookSkillIconCatalog.GetBookSkills(sample, out first, out second);
            if (first == null)
                return false; // Ordinary books remain entirely vanilla.

            bool matching = GroupMatches(tradeable.thingsColony, first, second) &&
                GroupMatches(tradeable.thingsTrader, first, second);
            if (!matching && EasyReadingDebug.Enabled && !LoggedMixedGroup &&
                Event.current.type == EventType.Repaint)
            {
                LoggedMixedGroup = true;
                Log.Warning("[QoL: Easy Reading][TradeUI] Mixed book rewards in " +
                    "one Tradeable; icons deliberately hidden for accuracy.");
            }
            return matching;
        }

        private static bool GroupMatches(List<Thing> group,
            SkillDef expectedFirst, SkillDef expectedSecond)
        {
            if (group == null)
                return true;
            for (int i = 0; i < group.Count; i++)
            {
                Thing thing = group[i];
                if (thing == null)
                    return false;
                Book book = thing.GetInnerIfMinified() as Book;
                if (book == null)
                    return false;
                SkillDef first;
                SkillDef second;
                BookSkillIconCatalog.GetBookSkills(book, out first, out second);
                if (!((first == expectedFirst && second == expectedSecond) ||
                      (expectedSecond != null && first == expectedSecond &&
                       second == expectedFirst)))
                    return false;
            }
            return true;
        }

        private static void PaintOne(SkillDef skill, Rect rect)
        {
            if (skill == null)
                return;
            Texture2D texture = BookSkillIconCatalog.GetTexture(skill);
            if (texture != null && Event.current.type == EventType.Repaint)
            {
                Color oldColor = GUI.color;
                try
                {
                    GUI.color = Color.white;
                    GUI.DrawTexture(rect, texture);
                }
                finally
                {
                    GUI.color = oldColor;
                }
            }
            // The icon is passive. It supplies its own localized tooltip
            // and never intercepts any mouse/key action.
            TooltipHandler.TipRegion(rect, skill.LabelCap.ToString());
        }
    }
}
