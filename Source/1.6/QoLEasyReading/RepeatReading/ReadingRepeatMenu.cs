using System;
using System.Collections;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ZCN.QoL.EasyReading
{
    /// <summary>
    /// Stage 3H: guarded interception of Book.GetFloatMenuOptions' Read action.
    /// Available without development mode; vanilla clicks remain untouched otherwise.
    /// </summary>
    internal static class ReadingRepeatProbe
    {
        private static bool installed;

        public static void Install(Harmony harmony)
        {
            if (harmony == null || installed)
            {
                return;
            }

            try
            {
                MethodInfo target = AccessTools.Method(
                    typeof(FloatMenuOption), "Chosen");

                if (target == null)
                {
                    Log.Warning("[QoL: Easy Reading] DEBUG repeat-reading Stage 2 " +
                        "unavailable: FloatMenuOption.Chosen not found.");
                    return;
                }

                harmony.Patch(
                    target,
                    prefix: new HarmonyMethod(
                        typeof(ReadingRepeatProbe), nameof(BeforeChosen)),
                    postfix: new HarmonyMethod(
                        typeof(ReadingRepeatProbe), nameof(AfterChosen)));
                installed = true;

                // The old Stage 3B read-only order audit is intentionally
                // not installed in normal or verbose mode. Repeat-queue
                // diagnostics remain available behind EasyReadingDebug.Enabled.

                Log.Message("[QoL: Easy Reading] repeat-reading Stage 3H installed " +
                    "(developer mode not required; 1..50 jobs; vanilla Read preserved otherwise).");
            }
            catch (Exception ex)
            {
                Log.Warning("[QoL: Easy Reading] DEBUG repeat-reading Stage 2 " +
                    "skipped; vanilla clicks unchanged. " + ex);
            }
        }

        /// <summary>
        /// Swap the action only for a configured shortcut on a verified Read
        /// option. Vanilla Chosen still closes the menu; restore afterward.
        /// </summary>
        private static void BeforeChosen(FloatMenuOption __instance,
            ref Action __state)
        {
            __state = null;

            if (__instance == null || __instance.action == null ||
                EasyReadingMod.Settings == null ||
                !EasyReadingMod.Settings.showRepeatReadingDialog)
            {
                return;
            }

            RepeatReadingIntent intent = RepeatReadingShortcut.Detect(EasyReadingMod.Settings);
            if (intent == RepeatReadingIntent.None)
                return;

            Action originalAction = __instance.action;
            Book book;
            Pawn closurePawn;
            bool achtungWrapped = false;

            if (IsVanillaBookReadAction(originalAction))
            {
                if (!TryFindBookAndPawn(originalAction.Target, out book, out closurePawn))
                {
                    if (EasyReadingDebug.Enabled)
                        Log.Warning("[QoL: Easy Reading] DEBUG repeat-reading Stage 2 " +
                            "found Book.Read action but could not resolve its book. " +
                            "Falling back to vanilla click.");
                    return;
                }
            }
            else
            {
                // Achtung! Force Commands wraps vanilla FloatMenuOption actions.
                // Only unwrap a single, positively identified Book.Read action;
                // never capture grouped orders or arbitrary menu labels.
                if (!TryFindAchtungWrappedBookRead(originalAction, out book, out closurePawn))
                    return;
                achtungWrapped = true;
            }

            Pawn selectedPawn = Find.Selector.SingleSelectedThing as Pawn;
            if (selectedPawn == null ||
                (closurePawn != null && selectedPawn != closurePawn))
            {
                if (EasyReadingDebug.Enabled)
                    Log.Warning("[QoL: Easy Reading] DEBUG repeat-reading Stage 2 " +
                        "pawn unavailable or selection changed; vanilla click retained.");
                return;
            }

            // Keep vanilla's Chosen logic and menu close: replace only the
            // action delegate, temporarily, for this click.
            __state = originalAction;
            Pawn pawn = closurePawn ?? selectedPawn;
            bool askEveryTime = intent == RepeatReadingIntent.Dialog;
            int presetCount = Mathf.Clamp(
                EasyReadingMod.Settings.repeatReadingPresetCount, 1, 50);

            __instance.action = delegate
            {
                if (EasyReadingDebug.Enabled)
                    Log.Message("[QoL: Easy Reading] DEBUG repeat-reading Stage 3F " +
                        "INTERCEPTED: pawn=" + pawn.LabelShort +
                    ", book=" + book.LabelCap +
                    ", pawnSource=" + (closurePawn != null ? "closure" : "selection") +
                    ", actionSource=" + (achtungWrapped ? "achtung-single" : "vanilla") +
                    ", mode=" + (askEveryTime ? "dialog" : "preset") +
                    ", vanillaActionSuppressed=True, presetCount=" +
                    presetCount + ", maxAllowed=50.");

                if (askEveryTime)
                {
                    Find.WindowStack.Add(
                        new Dialog_RepeatReadingStage3F(pawn, book, presetCount));
                }
                else
                {
                    // Preset mode: queue the actual configured count (1..50).
                    RepeatReadingQueueStage3F.QueueRequested(pawn, book, "preset", presetCount);
                }
            };
        }

        private static void AfterChosen(FloatMenuOption __instance, Action __state)
        {
            if (__instance != null && __state != null)
            {
                __instance.action = __state;
            }
        }

        private static bool IsVanillaBookReadAction(Action action)
        {
            if (action == null || action.Method == null)
            {
                return false;
            }

            MethodInfo method = action.Method;
            Type type = method.DeclaringType;
            return type != null && type.DeclaringType == typeof(Book) &&
                method.Name.IndexOf("GetFloatMenuOptions",
                    StringComparison.Ordinal) >= 0;
        }

        /// <summary>
        /// Achtung! (optional dependency) rebuilds the vanilla order menu in
        /// MultiActions.GetOption and captures a list of MultiAction objects.
        /// Each MultiAction retains the original FloatMenuOption in 'option'.
        /// Inspect only this exact wrapper shape: no Achtung assembly reference,
        /// no labels/translation matching, no invocation of the original action.
        /// A single selected pawn and exactly one wrapped order are required.
        /// Any unexpected layout safely falls back to Achtung's ordinary click.
        /// </summary>
        private static bool TryFindAchtungWrappedBookRead(Action wrapper,
            out Book book, out Pawn pawn)
        {
            book = null;
            pawn = null;

            if (wrapper == null || wrapper.Target == null || wrapper.Method == null)
                return false;

            Type methodType = wrapper.Method.DeclaringType;
            if (methodType == null || methodType.DeclaringType == null ||
                methodType.DeclaringType.FullName != "AchtungMod.MultiActions" ||
                wrapper.Method.Name.IndexOf("<GetOption>", StringComparison.Ordinal) < 0)
                return false;

            try
            {
                // This field is captured by the actual GetOption lambda;
                // don't traverse arbitrary game/mod objects.
                FieldInfo actionsField = methodType.GetField("actions",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                IEnumerable actions = actionsField == null
                    ? null : actionsField.GetValue(wrapper.Target) as IEnumerable;
                if (actions == null)
                    return AchtungFallback("GetOption.actions not found");

                object singleAction = null;
                int count = 0;
                foreach (object action in actions)
                {
                    if (++count != 1)
                        return AchtungFallback("grouped or duplicate actions");
                    singleAction = action;
                }
                if (count != 1 || singleAction == null ||
                    singleAction.GetType().FullName != "AchtungMod.MultiAction")
                    return AchtungFallback("not exactly one MultiAction");

                Type multiActionType = singleAction.GetType();
                FieldInfo optionField = multiActionType.GetField("option",
                    BindingFlags.Instance | BindingFlags.Public);
                FieldInfo colonistField = multiActionType.GetField("colonist",
                    BindingFlags.Instance | BindingFlags.Public);
                FloatMenuOption innerOption = optionField == null
                    ? null : optionField.GetValue(singleAction) as FloatMenuOption;
                object colonist = colonistField == null
                    ? null : colonistField.GetValue(singleAction);

                if (innerOption == null || innerOption.Disabled ||
                    !IsVanillaBookReadAction(innerOption.action) ||
                    colonist == null || colonist.GetType().FullName != "AchtungMod.Colonist")
                    return AchtungFallback("inner action is not vanilla Book.Read");

                FieldInfo pawnField = colonist.GetType().GetField("pawn",
                    BindingFlags.Instance | BindingFlags.Public);
                Pawn achtungPawn = pawnField == null
                    ? null : pawnField.GetValue(colonist) as Pawn;
                Pawn selectedPawn = Find.Selector.SingleSelectedThing as Pawn;
                if (achtungPawn == null || selectedPawn != achtungPawn)
                    return AchtungFallback("pawn mismatch or not single selected");

                Book foundBook;
                Pawn bookClosurePawn;
                if (!TryFindBookAndPawn(innerOption.action.Target,
                    out foundBook, out bookClosurePawn) ||
                    (bookClosurePawn != null && bookClosurePawn != achtungPawn))
                    return AchtungFallback("book/pawn closure not verified");

                book = foundBook;
                pawn = achtungPawn;
                return true;
            }
            catch (Exception ex)
            {
                // A future Achtung version may change its closure layout.
                // Fail closed: never run a different job by accident.
                if (EasyReadingDebug.Enabled)
                    Log.Warning("[QoL: Easy Reading] DEBUG Achtung! reading " +
                        "wrapper inspection failed; vanilla click retained. " + ex);
                return false;
            }
        }

        private static bool AchtungFallback(string reason)
        {
            if (EasyReadingDebug.Enabled)
                Log.Message("[QoL: Easy Reading] DEBUG Achtung! wrapped menu " +
                    "left unchanged: " + reason + ".");
            return false;
        }

        private static bool TryFindBookAndPawn(object closure,
            out Book book, out Pawn pawn)
        {
            book = null;
            pawn = null;

            if (closure == null)
            {
                return false;
            }

            // Inspect compiler-generated capture fields, not arbitrary Thing
            // graphs. Runtime field names may differ between game builds.
            ScanClosure(closure, 0, ref book, ref pawn);
            return book != null;
        }

        private static void ScanClosure(object value, int depth,
            ref Book book, ref Pawn pawn)
        {
            if (value == null || depth > 2)
            {
                return;
            }

            Book directBook = value as Book;
            if (directBook != null)
            {
                book = directBook;
                return;
            }

            Pawn directPawn = value as Pawn;
            if (directPawn != null)
            {
                pawn = directPawn;
                return;
            }

            Type type = value.GetType();
            if (type.FullName == null ||
                type.FullName.IndexOf("DisplayClass", StringComparison.Ordinal) < 0)
            {
                return;
            }

            FieldInfo[] fields = type.GetFields(
                BindingFlags.Instance | BindingFlags.Public |
                BindingFlags.NonPublic);

            for (int i = 0; i < fields.Length; i++)
            {
                try
                {
                    object captured = fields[i].GetValue(value);
                    if (captured is Book)
                    {
                        book = (Book)captured;
                    }
                    else if (captured is Pawn)
                    {
                        pawn = (Pawn)captured;
                    }
                    else if (captured != null &&
                        captured.GetType().FullName != null &&
                        captured.GetType().FullName.IndexOf(
                            "DisplayClass", StringComparison.Ordinal) >= 0)
                    {
                        ScanClosure(captured, depth + 1, ref book, ref pawn);
                    }
                }
                catch (Exception)
                {
                    // Inaccessible future capture layout: skip this field.
                }
            }
        }
    }
}
