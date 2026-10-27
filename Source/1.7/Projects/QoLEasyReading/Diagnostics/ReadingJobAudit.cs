using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ZCN.QoL.EasyReading
{

    /// <summary>
    /// Stage 3B: read-only, before/after audit of vanilla ordered reading jobs.
    /// Diagnoses successful-but-not-enqueued orders. Does NOT create, clear,
    /// reserve, clone, or enqueue any job. Dev mode only.
    /// </summary>
    internal static class ReadingJobAudit
    {
        private static bool installed;
        private static FieldInfo pawnField;
        private static FieldInfo queueField;
        private static MethodInfo jobIsSameAsMethod;

        private sealed class OrderSnapshot
        {
            public Job incoming;
            public Pawn pawn;
            public Job currentBefore;
            public int queueBefore;
            public string sameAsCurrent;
            public string eventModifiers;
            public bool physicalShift;
            public bool physicalCtrl;
        }

        public static void Install(Harmony harmony)
        {
            if (installed || harmony == null || !EasyReadingDebug.Enabled)
                return;

            try
            {
                pawnField = AccessTools.Field(typeof(Pawn_JobTracker), "pawn");
                queueField = AccessTools.Field(typeof(Pawn_JobTracker), "jobQueue");
                jobIsSameAsMethod = AccessTools.Method(typeof(Job),
                    "JobIsSameAs", new Type[] { typeof(Job) });

                int patched = 0;
                foreach (MethodInfo method in typeof(Pawn_JobTracker).GetMethods(
                    BindingFlags.Instance | BindingFlags.Public |
                    BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                    if (method.Name != "TryTakeOrderedJob" ||
                        method.ReturnType != typeof(bool))
                        continue;

                    ParameterInfo[] parameters = method.GetParameters();
                    if (parameters.Length == 0 ||
                        parameters[0].ParameterType != typeof(Job))
                        continue;

                    harmony.Patch(method,
                        prefix: new HarmonyMethod(typeof(ReadingJobAudit),
                            nameof(BeforeVanillaOrderedJob)),
                        postfix: new HarmonyMethod(typeof(ReadingJobAudit),
                            nameof(AfterVanillaOrderedJob)));
                    patched++;
                }

                if (patched == 0)
                {
                    Log.Warning("[QoL: Easy Reading] DEBUG Stage 3B audit unavailable: " +
                        "no compatible Pawn_JobTracker.TryTakeOrderedJob overload.");
                    return;
                }

                installed = true;
                Log.Message("[QoL: Easy Reading] DEBUG Stage 3B " +
                    "read-only duplicate/queue audit installed: overloads=" +
                    patched + ", JobIsSameAsAvailable=" +
                    (jobIsSameAsMethod != null) + ", modifiesJobs=False.");
            }
            catch (Exception ex)
            {
                Log.Warning("[QoL: Easy Reading] DEBUG Stage 3B " +
                    "audit installation failed; Stage 2 remains available: " + ex);
            }
        }

        private static void BeforeVanillaOrderedJob(Pawn_JobTracker __instance,
            object[] __args, out OrderSnapshot __state)
        {
            __state = null;
            if (!EasyReadingDebug.Enabled || __instance == null ||
                __args == null || __args.Length == 0)
                return;

            Job job = __args[0] as Job;
            if (!IsReadingOrder(job))
                return;

            try
            {
                OrderSnapshot state = new OrderSnapshot();
                state.incoming = job;
                state.pawn = pawnField != null
                    ? pawnField.GetValue(__instance) as Pawn : null;
                state.currentBefore = __instance.curJob;
                state.queueBefore = ReadQueueCount(__instance);
                state.sameAsCurrent = "unknown";

                if (state.currentBefore == null)
                {
                    state.sameAsCurrent = "not-applicable";
                }
                else if (jobIsSameAsMethod != null)
                {
                    // Invoke the real runtime Job comparator; do not approximate
                    // it based on defName or targetA labels.
                    object result = jobIsSameAsMethod.Invoke(
                        state.currentBefore, new object[] { job });
                    if (result is bool)
                        state.sameAsCurrent = ((bool)result) ? "True" : "False";
                }

                Event ev = Event.current;
                state.eventModifiers = ev != null
                    ? ev.modifiers.ToString() : "none";
                state.physicalShift = Input.GetKey(KeyCode.LeftShift) ||
                    Input.GetKey(KeyCode.RightShift);
                state.physicalCtrl = Input.GetKey(KeyCode.LeftControl) ||
                    Input.GetKey(KeyCode.RightControl);

                __state = state;
            }
            catch (Exception ex)
            {
                Log.Warning("[QoL: Easy Reading] DEBUG Stage 3B " +
                    "PRE audit failed (vanilla jobs unchanged): " + ex);
            }
        }

        private static void AfterVanillaOrderedJob(Pawn_JobTracker __instance,
            bool __result, OrderSnapshot __state)
        {
            if (!EasyReadingDebug.Enabled || __instance == null || __state == null)
                return;

            try
            {
                int after = ReadQueueCount(__instance);
                string delta = __state.queueBefore >= 0 && after >= 0
                    ? (after - __state.queueBefore).ToString() : "unknown";

                Job currentAfter = __instance.curJob;
                string currentBefore = DescribeJob(__state.currentBefore);
                string currentNow = DescribeJob(currentAfter);
                Job job = __state.incoming;

                Log.Message("[QoL: Easy Reading] DEBUG Stage 3B VANILLA READ ORDER: " +
                    "pawn=" + (__state.pawn != null ? __state.pawn.LabelShort : "unknown") +
                    ", incoming=" + DescribeJob(job) +
                    ", targetA=" + (job != null ? DescribeTarget(job.targetA) : "none") +
                    ", currentBefore=" + currentBefore +
                    ", currentAfter=" + currentNow +
                    ", sameAsCurrent=" + __state.sameAsCurrent +
                    ", queueBefore=" + __state.queueBefore +
                    ", queueAfter=" + after +
                    ", queueDelta=" + delta +
                    ", success=" + __result +
                    ", eventModifiers=" + __state.eventModifiers +
                    ", physicalCtrl=" + __state.physicalCtrl +
                    ", physicalShift=" + __state.physicalShift +
                    ", auditOnly=True.");
            }
            catch (Exception ex)
            {
                Log.Warning("[QoL: Easy Reading] DEBUG Stage 3B " +
                    "POST audit failed (vanilla jobs unchanged): " + ex);
            }
        }

        private static int ReadQueueCount(Pawn_JobTracker tracker)
        {
            if (queueField == null || tracker == null)
                return -1;

            object queue = queueField.GetValue(tracker);
            if (queue == null)
                return -1;

            PropertyInfo countProperty = queue.GetType().GetProperty(
                "Count", BindingFlags.Instance | BindingFlags.Public |
                BindingFlags.NonPublic);
            return countProperty != null
                ? Convert.ToInt32(countProperty.GetValue(queue, null)) : -1;
        }

        private static string DescribeJob(Job job)
        {
            return job != null && job.def != null ? job.def.defName : "none";
        }

        private static string DescribeTarget(LocalTargetInfo target)
        {
            if (!target.IsValid)
                return "none";
            Thing thing = target.Thing;
            if (thing != null)
                return thing.GetType().Name + ":" + thing.LabelCap;
            return target.Cell.ToString();
        }

        private static bool IsReadingOrder(Job job)
        {
            if (job == null)
                return false;

            if ((job.targetA.IsValid && job.targetA.Thing is Book) ||
                (job.targetB.IsValid && job.targetB.Thing is Book))
                return true;

            string name = job.def != null ? job.def.defName : null;
            return !string.IsNullOrEmpty(name) &&
                name.IndexOf("Read", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
