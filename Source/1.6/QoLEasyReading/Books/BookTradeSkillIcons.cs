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
    /// 4S-2H3A + S3-B2: Trade book icons with Useful Marks compatibility.
    /// The 1.6 Useful Marks transpiler replaces the vanilla Widgets.Label
    /// (Rect,string) title call with its MarkedLabel(Rect,Transferable) call.
    /// Never replace/bypass that third-party method: leave its call intact and
    /// adjust ONLY the Rect argument immediately before the matched call.
    /// Harmony Priority.Last ensures our IL step runs after normal-priority
    /// patches, including Useful Marks, regardless of mod-loading order.
    /// Fail closed if there is more than one possible label target.
    /// </summary>
    [HarmonyPatch(typeof(TransferableUIUtility), nameof(TransferableUIUtility.DrawTransferableInfo),
        new Type[] { typeof(Transferable), typeof(Rect), typeof(Color) })]
    internal static partial class BookTradeSkillIcons
    {
        private const float LeftContentWidth = 80f; // vanilla thing icon/info button
        private const float MinimumNameWidth = 65f;
        private const float SlotGap = 3f;
        private const float LeftGap = 3f;
        private const float EndPadding = 5f;
        // LoggedLeftHookWarning guards a user-relevant compatibility warning.
        private static bool LoggedLeftHookWarning;
#if DEBUG
        private static bool LoggedFirstPaint;
        private static bool LoggedMixedGroup;
        private static readonly HashSet<string> LoggedSchematicDecisions =
            new HashSet<string>(StringComparer.Ordinal);
        private static readonly HashSet<SchematicIconKind> LoggedSchematicPaints =
            new HashSet<SchematicIconKind>();
#endif
        private static bool LeftTitleHookReady;

        // DrawTransferableInfo is a GUI call. Limit the temporary title shift
        // to its current invocation, restoring it in Postfix even when OFF.
        [ThreadStatic]
        private static float activeLeftTitleShift;

        private struct RowState
        {
            internal bool Paint;
            internal bool Left;
            internal bool Schematic;
            internal SchematicIconKind SchematicKind;
            internal Texture2D SchematicTexture;
            internal string DuplicateProjectKey;
            internal SchematicOwnershipSnapshot DuplicateOwnershipSnapshot;
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

    }
}
