using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace ZCN.QoL.EasyReading
{
    /// <summary>
    /// WORK-FIX1 (RimWorld 1.6): do not mutate a pawn's timetable or
    /// permanently modify a Reading job. Temporarily bypass ONLY the
    /// joy-time-assignment check while JoyTickCheckEnd is executing for
    /// a player-forced Reading job during Work hours, with settings ON.
    /// </summary>
    internal static class ManualReadingWorkScheduleOverride
    {
        private static bool installed;
        private static int debugHitCount;

        // A value type is cheap to store per invocation, and does not keep
        // references beyond the Harmony method call.
        private struct TemporaryFlag
        {
            public Job Job;
            public bool PreviousValue;
            public bool Changed;
        }

        internal static void Install(Harmony harmony)
        {
            if (harmony == null || installed) return;
            MethodInfo target = AccessTools.Method(
                typeof(JoyUtility), nameof(JoyUtility.JoyTickCheckEnd),
                new[] { typeof(Pawn), typeof(int),
                    typeof(JoyTickFullJoyAction), typeof(float), typeof(Building) });

            if (target == null)
            {
                Log.Warning("[QoL: Easy Reading][WORK-FIX1] " +
                    "RimWorld 1.6 JoyTickCheckEnd signature not found. " +
                    "Manual Work reading feature disabled; vanilla behavior preserved.");
                return;
            }

            try
            {
                harmony.Patch(target,
                    prefix: new HarmonyMethod(typeof(ManualReadingWorkScheduleOverride), nameof(BeforeJoyCheck)),
                    postfix: new HarmonyMethod(typeof(ManualReadingWorkScheduleOverride), nameof(AfterJoyCheck)),
                    finalizer: new HarmonyMethod(typeof(ManualReadingWorkScheduleOverride), nameof(FinalizeJoyCheck)));
                installed = true;
                Log.Message("[QoL: Easy Reading][WORK-FIX1] " +
                    "JoyTickCheckEnd scoped patch installed; " +
                    "manual Reading + Work only; option default ON; " +
                    "job flag restored after every call.");
            }
            catch (Exception ex)
            {
                Log.Error("[QoL: Easy Reading][WORK-FIX1] " +
                    "Could not install scoped Work reading patch: " + ex);
            }
        }

        private static void BeforeJoyCheck(Pawn pawn, out TemporaryFlag __state)
        {
            __state = default(TemporaryFlag);
            EasyReadingSettings settings = EasyReadingMod.Settings;
            if (settings == null || !settings.allowManualReadingDuringWork || pawn == null)
                return;

            Job job = pawn.CurJob;
            if (job == null || job.def == null || job.def.defName != "Reading" ||
                !job.playerForced || job.ignoreJoyTimeAssignment ||
                job.doUntilGatheringEnded)
                return;

            // Work only. Do NOT bypass Sleep or any other non-joy assignment.
            if (pawn.GetTimeAssignment() != TimeAssignmentDefOf.Work)
                return;

            __state.Job = job;
            __state.PreviousValue = job.ignoreJoyTimeAssignment;
            __state.Changed = true;
            job.ignoreJoyTimeAssignment = true;

            // Debug-only and capped; no per-tick log spam during normal play.
            if (EasyReadingDebug.Enabled)
            {
                debugHitCount++;
                if (debugHitCount <= 4 || debugHitCount == 10 || debugHitCount == 50)
                    Log.Message("[QoL: Easy Reading][WORK-FIX1] " +
                        "Work reading override HIT #" + debugHitCount +
                        " pawn=" + pawn.LabelShort +
                        " forced=" + job.playerForced +
                        " job=" + job.def.defName +
                        " temporaryFlag=True");
            }
        }

        private static void AfterJoyCheck(ref TemporaryFlag __state)
        {
            Restore(ref __state);
        }

        private static Exception FinalizeJoyCheck(Exception __exception,
            ref TemporaryFlag __state)
        {
            // Harmony finalizers also execute when the original throws.
            // Do not suppress, replace or otherwise modify that exception.
            Restore(ref __state);
            return __exception;
        }

        private static void Restore(ref TemporaryFlag state)
        {
            if (!state.Changed) return;
            if (state.Job != null)
                state.Job.ignoreJoyTimeAssignment = state.PreviousValue;
            state.Changed = false;
        }
    }
}
