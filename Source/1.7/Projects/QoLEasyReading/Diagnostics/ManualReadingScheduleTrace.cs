using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace ZCN.QoL.EasyReading
{
    /// <summary>
    /// WORK-DIAG1. Read-only, debug-only trace. Never modifies a Job, timetable,
    /// toils, queues, toil reservations or the result of an original method.
    /// All hooks are installed only when verbose logging is enabled at startup.
    /// </summary>
    internal static class ManualReadingScheduleTrace
    {
        private static bool installed;
        private static readonly FieldInfo PawnField =
            AccessTools.Field(typeof(Pawn_JobTracker), "pawn");

        internal static void Install(Harmony harmony)
        {
            if (installed || harmony == null || !EasyReadingDebug.Enabled)
                return;

            int ordered = 0, started = 0, ended = 0, driverEnded = 0;
            try
            {
                // Exact overloads are discovered on the running RimWorld build.
                ordered = PatchAllOverloads(harmony, typeof(Pawn_JobTracker),
                    "TryTakeOrderedJob", nameof(BeforeOrder), null);
                started = PatchAllOverloads(harmony, typeof(Pawn_JobTracker),
                    "StartJob", nameof(BeforeStart), null);
                ended = PatchAllOverloads(harmony, typeof(Pawn_JobTracker),
                    "EndCurrentJob", nameof(BeforeEnd), null);
                driverEnded = PatchAllOverloads(harmony, typeof(JobDriver),
                    "EndJobWith", nameof(BeforeDriverEnd), null);
                installed = true;

                JobDef reading = DefDatabase<JobDef>.GetNamedSilentFail("Reading");
                Log.Message("[QoL: Easy Reading][WORK-DIAG1] installed " +
                    "ordered=" + ordered + ", start=" + started +
                    ", end=" + ended + ", driverEnd=" + driverEnded +
                    ", Reading.driverClass=" +
                    (reading != null && reading.driverClass != null
                        ? reading.driverClass.FullName : "unknown") +
                    ", auditOnly=True. No gameplay modifications.");
                if (started == 0 || ended == 0)
                    Log.Warning("[QoL: Easy Reading][WORK-DIAG1] " +
                        "Not all essential tracker hooks were found. Please send log and ILSpy code.");
            }
            catch (Exception ex)
            {
                Log.Warning("[QoL: Easy Reading][WORK-DIAG1] " +
                    "Patch setup failed (gameplay remains unmodified): " + ex);
            }
        }

        private static int PatchAllOverloads(Harmony harmony, Type type,
            string name, string prefixName, string postfixName)
        {
            int count = 0;
            foreach (MethodInfo method in type.GetMethods(
                BindingFlags.Instance | BindingFlags.Public |
                BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            {
                if (method.Name != name || method.IsAbstract || method.ContainsGenericParameters)
                    continue;
                HarmonyMethod prefix = prefixName == null ? null :
                    new HarmonyMethod(typeof(ManualReadingScheduleTrace), prefixName);
                HarmonyMethod postfix = postfixName == null ? null :
                    new HarmonyMethod(typeof(ManualReadingScheduleTrace), postfixName);
                try
                {
                    harmony.Patch(method, prefix: prefix, postfix: postfix);
                    count++;
                }
                catch (Exception ex)
                {
                    Log.Warning("[QoL: Easy Reading][WORK-DIAG1] " +
                        "Skipped incompatible overload " + type.Name + "." + name +
                        "(" + method.GetParameters().Length + "): " + ex.GetType().Name);
                }
            }
            return count;
        }

        private static void BeforeOrder(Pawn_JobTracker __instance, object[] __args)
        {
            if (!EasyReadingDebug.Enabled) return;
            Job incoming = FirstJob(__args);
            if (!IsReading(incoming)) return;
            Pawn pawn = GetPawn(__instance);
            Trace("ORDER", pawn, incoming, __args);
        }

        private static void BeforeStart(Pawn_JobTracker __instance, object[] __args)
        {
            if (!EasyReadingDebug.Enabled) return;
            Job incoming = FirstJob(__args);
            if (!IsReading(incoming)) return;
            Trace("START", GetPawn(__instance), incoming, __args);
        }

        private static void BeforeEnd(Pawn_JobTracker __instance, object[] __args)
        {
            if (!EasyReadingDebug.Enabled || __instance == null) return;
            Job current = __instance.curJob;
            if (!IsReading(current)) return;
            Trace("END-CURRENT", GetPawn(__instance), current, __args);
        }

        private static void BeforeDriverEnd(JobDriver __instance, object[] __args)
        {
            if (!EasyReadingDebug.Enabled || __instance == null ||
                !IsReading(__instance.job)) return;
            Trace("DRIVER-END", __instance.pawn, __instance.job, __args);
        }

        private static Job FirstJob(object[] args)
        {
            if (args == null) return null;
            foreach (object arg in args)
                if (arg is Job) return (Job)arg;
            return null;
        }

        private static Pawn GetPawn(Pawn_JobTracker tracker)
        {
            if (tracker == null || PawnField == null) return null;
            try { return PawnField.GetValue(tracker) as Pawn; }
            catch { return null; }
        }

        private static bool IsReading(Job job)
        {
            if (job == null) return false;
            if (job.def != null && job.def.defName == "Reading") return true;
            return (job.targetA.IsValid && job.targetA.Thing is Book) ||
                   (job.targetB.IsValid && job.targetB.Thing is Book);
        }

        private static string CurrentSchedule(Pawn pawn)
        {
            if (pawn == null) return "no-pawn";
            try
            {
                // Reflection here deliberately tolerates minor timetable API changes.
                object tracker = AccessTools.Field(pawn.GetType(), "timetable")?.GetValue(pawn);
                if (tracker == null)
                    tracker = AccessTools.Property(pawn.GetType(), "timetable")?.GetValue(pawn, null);
                if (tracker == null) return "no-timetable";
                Type trackerType = tracker.GetType();
                object assignment = AccessTools.Property(trackerType, "CurrentAssignment")
                    ?.GetValue(tracker, null);
                if (assignment == null)
                    assignment = AccessTools.Field(trackerType, "CurrentAssignment")
                        ?.GetValue(tracker);
                if (assignment is Def) return ((Def)assignment).defName;
                return assignment != null ? assignment.ToString() : "unknown";
            }
            catch (Exception ex) { return "unavailable:" + ex.GetType().Name; }
        }

        private static string DescribeArgs(object[] args)
        {
            if (args == null || args.Length == 0) return "none";
            string result = "";
            int n = Math.Min(args.Length, 3);
            for (int i = 0; i < n; i++)
            {
                if (i > 0) result += ",";
                object arg = args[i];
                result += arg is Job ? "Job" :
                    (arg == null ? "null" : arg.ToString());
            }
            return result;
        }

        private static void Trace(string phase, Pawn pawn, Job job, object[] args)
        {
            try
            {
                string carrying = "none";
                if (pawn != null && pawn.carryTracker != null &&
                    pawn.carryTracker.CarriedThing != null)
                    carrying = pawn.carryTracker.CarriedThing.def.defName;
                string joy = "none";
                if (pawn != null && pawn.needs != null && pawn.needs.joy != null)
                    joy = pawn.needs.joy.CurLevelPercentage.ToString("0.000");
                string target = job.targetA.Thing != null
                    ? job.targetA.Thing.def.defName : "none";
                string tick = Find.TickManager != null
                    ? Find.TickManager.TicksGame.ToString() : "none";

                Log.Message("[QoL: Easy Reading][WORK-DIAG1] " + phase +
                    " tick=" + tick +
                    " pawn=" + (pawn != null ? pawn.LabelShort : "unknown") +
                    " schedule=" + CurrentSchedule(pawn) +
                    " job=" + (job.def != null ? job.def.defName : "none") +
                    " forced=" + job.playerForced +
                    " target=" + target +
                    " carried=" + carrying +
                    " joy=" + joy +
                    " args=" + DescribeArgs(args) +
                    " auditOnly=True");
            }
            catch (Exception ex)
            {
                Log.Warning("[QoL: Easy Reading][WORK-DIAG1] " +
                    "Trace failed without changing gameplay: " + ex.GetType().Name);
            }
        }
    }
}
