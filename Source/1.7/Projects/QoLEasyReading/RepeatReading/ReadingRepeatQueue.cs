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
    /// Stage 3H controlled queue path: append selected count (1..50) of
    /// separate Reading jobs. Preserve existing curJob and queued jobs.
    /// Functionality available with developer mode OFF; comprehensive stress
    /// testing must finish before general release.
    /// </summary>
    internal static class RepeatReadingQueueStage3F
    {
        private const int MaxAllowedAdds = 50;
        private static readonly FieldInfo QueueField =
            AccessTools.Field(typeof(Pawn_JobTracker), "jobQueue");

        public static void QueueRequested(Pawn pawn, Book book, string mode, int requestedCount)
        {
            int count = Mathf.Clamp(requestedCount, 1, MaxAllowedAdds);

            string pawnName = pawn != null ? pawn.LabelShort : "none";
            string bookName = book != null ? book.LabelCap : "none";
            int added = 0;
            int before = -1;
            int after = -1;
            string reason = "none";

            try
            {
                if (pawn == null || pawn.jobs == null || !pawn.Spawned ||
                    pawn.Dead || book == null || book.Destroyed ||
                    book.MapHeld != pawn.Map)
                {
                    reason = "invalid-pawn-or-book-state";
                    return;
                }

                JobDef readingDef = DefDatabase<JobDef>.GetNamedSilentFail("Reading");
                if (readingDef == null)
                {
                    reason = "Reading-JobDef-missing";
                    return;
                }

                // The vanilla menu does not offer a 'Read' action for the
                // currently-read book. If the situation changed while a
                // dialog was open, stay equally conservative.
                Job currentJob = pawn.jobs.curJob;
                if (currentJob != null && currentJob.def == readingDef &&
                    currentJob.targetA.Thing == book)
                {
                    reason = "book-is-current-reading-target";
                    return;
                }

                if (QueueField == null)
                {
                    reason = "jobQueue-field-missing";
                    return;
                }

                object queue = QueueField.GetValue(pawn.jobs);
                if (queue == null)
                {
                    reason = "jobQueue-null";
                    return;
                }

                PropertyInfo countProp = queue.GetType().GetProperty(
                    "Count", BindingFlags.Instance | BindingFlags.Public |
                    BindingFlags.NonPublic);
                MethodInfo enqueue = null;
                foreach (MethodInfo method in queue.GetType().GetMethods(
                    BindingFlags.Instance | BindingFlags.Public |
                    BindingFlags.NonPublic))
                {
                    if (method.Name != "EnqueueLast")
                        continue;
                    ParameterInfo[] ps = method.GetParameters();
                    if (ps.Length != 2 || ps[0].ParameterType != typeof(Job))
                        continue;
                    Type tagType = ps[1].ParameterType;
                    if (tagType == typeof(JobTag) ||
                        tagType == typeof(JobTag?))
                    {
                        enqueue = method;
                        break;
                    }
                }
                if (enqueue == null || countProp == null)
                {
                    reason = "EnqueueLast-or-Count-signature-not-found";
                    return;
                }

                before = Convert.ToInt32(countProp.GetValue(queue, null));
                after = before;

                for (int i = 0; i < count; i++)
                {
                    // Distinct Job objects, never reuse an existing/current Job.
                    Job job = JobMaker.MakeJob(readingDef, book);
                    job.playerForced = true;
                    if (!job.TryMakePreToilReservations(pawn, true))
                    {
                        pawn.ClearReservationsForJob(job);
                        reason = "reservation-rejected-at-" + (i + 1);
                        break;
                    }

                    try
                    {
                        enqueue.Invoke(queue, new object[] { job, (JobTag?)JobTag.Misc });
                    }
                    catch (Exception)
                    {
                        pawn.ClearReservationsForJob(job);
                        throw;
                    }

                    // Do not infer success from reflection invocation alone.
                    // Require exactly one new queue entry before continuing.
                    int previous = after;
                    after = Convert.ToInt32(countProp.GetValue(queue, null));
                    if (after != previous + 1)
                    {
                        reason = "queue-step-mismatch-at-" + (i + 1);
                        break;
                    }

                    added++;
                    // Keep 50-job tests readable; log the first three and last.
                    if (EasyReadingDebug.Enabled && (i < 3 || i == count - 1))
                    {
                        Log.Message("[QoL: Easy Reading] DEBUG Stage 3F ENQUEUED: " +
                            "pawn=" + pawnName + ", book=" + bookName +
                            ", index=" + (i + 1) + ", requested=" + count +
                            ", queueNow=" + after +
                            ", jobDef=" + job.def.defName +
                            ", targetAIsBook=" + (job.targetA.Thing == book) + ".");
                    }
                }

                after = Convert.ToInt32(countProp.GetValue(queue, null));
                if (reason == "none" && after - before != added)
                    reason = "queue-delta-mismatch";
            }
            catch (Exception ex)
            {
                reason = "exception-" + ex.GetType().Name;
                Log.Error("[QoL: Easy Reading] DEBUG Stage 3F QUEUE exception: " + ex);
            }
            finally
            {
                // A rejected insertion can leave earlier successful jobs in
                // the queue. We never delete user queue entries to roll back.
                if (EasyReadingDebug.Enabled)
                    Log.Message("[QoL: Easy Reading] DEBUG Stage 3F QUEUE RESULT: " +
                    "pawn=" + pawnName + ", book=" + bookName +
                    ", mode=" + mode + ", requestedInUI=" + requestedCount +
                    ", targetAdds=" + count + ", maxAllowed=" + MaxAllowedAdds +
                    ", actualAdded=" + added +
                    ", queueBefore=" + before + ", queueAfter=" + after +
                    ", queueDelta=" +
                    (before >= 0 && after >= 0 ? (after - before).ToString() : "unknown") +
                    ", reason=" + reason + ".");

                if (reason != "none" && !reason.StartsWith("exception-", StringComparison.Ordinal))
                {
                    Log.Warning("[QoL: Easy Reading] Repeat reading only queued " +
                        added + "/" + count + " jobs for " + pawnName +
                        " (reason=" + reason + ").");
                }
            }
        }
    }
}
