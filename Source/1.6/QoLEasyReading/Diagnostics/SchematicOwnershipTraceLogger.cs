using System;
using System.Collections.Generic;
using System.Text;
using RimWorld;
using Verse;

namespace ZCN.QoL.EasyReading
{
    /// <summary>
    /// C1-L1 diagnostic formatter and bounded transfer/diff logger. Holds
    /// only metadata snapshots and row observation state, never owns Things.
    /// Existing C1 ownership decisions and UI are not changed.
    /// </summary>
    internal static class SchematicOwnershipTraceLogger
    {
        private const string Tag = "[QoL: Easy Reading][S3C1][L1]";
        private const int MaxMatchSamples = 12;
        private const int MaxDiffSamples = 12;
        private const int MaxTransferLogs = 96;
        private const int MaxTrackedTradeRows = 512;
        private const int MaxManualDumpRows = 256;
        private static long sessionSequence;
        private static long currentSequence;
        private static SchematicOwnershipSnapshot previousSnapshot;
        private static long previousSequence;
        private static readonly Dictionary<Tradeable, int> LastTransferCount =
            new Dictionary<Tradeable, int>();
        private sealed class TradeRowIdentity
        {
            internal string BookId;
            internal string FullKey;
            internal string Side;
        }
        private static readonly Dictionary<Tradeable, TradeRowIdentity> TrackedIdentities =
            new Dictionary<Tradeable, TradeRowIdentity>();
        private static int transferLogs;
        private static bool transferLimitReported;
        private static bool trackedLimitReported;

        internal static int ManualDumpLimit { get { return MaxManualDumpRows; } }

        internal static void Close(SchematicOwnershipSnapshot outgoing, string reason)
        {
            if (reason == "INACTIVE_OR_DISABLED")
            {
                previousSnapshot = null;
                previousSequence = 0;
            }
            else if (outgoing != null)
            {
                previousSnapshot = outgoing;
                previousSequence = currentSequence;
            }
            currentSequence = 0;
            LastTransferCount.Clear();
            TrackedIdentities.Clear();
            transferLogs = 0;
            transferLimitReported = false;
            trackedLimitReported = false;
        }

        internal static void Open(SchematicOwnershipSnapshot snapshot, string trigger)
        {
            currentSequence = ++sessionSequence;
            LastTransferCount.Clear();
            TrackedIdentities.Clear();
            transferLogs = 0;
            transferLimitReported = false;
            trackedLimitReported = false;
            Log.Message(Tag + "[SESSION] seq=" + currentSequence +
                "; reason=" + trigger + "; trace=READ_ONLY_DEBUG_ONLY; " +
                "snapshotOnce=True; build=" + EasyReadingBuildIdentity.BuildId + ";");
            if (snapshot == null) return;
            Log.Message(Tag + "[SCAN] seq=" + currentSequence +
                "; schematics=" + snapshot.SchematicBooks +
                "; validBookIds=" + snapshot.TraceBooks.Count +
                "; missingIds=" + snapshot.TraceMissingIds +
                "; idCollisions=" + snapshot.TraceIdCollisions +
                "; uniqueKeys=" + snapshot.FoundKeys.Count +
                "; truncated=" + snapshot.Truncated +
                "; failed=" + snapshot.InspectionFailed +
                "; traceFailed=" + snapshot.TraceFailed +
                "; traceCountMismatch=" + snapshot.TraceCountMismatch +
                "; elapsedMs=" + snapshot.ElapsedMilliseconds + ";");
            if (snapshot.TraceCountMismatch || snapshot.TraceFailed ||
                snapshot.TraceMissingIds != 0 || snapshot.TraceIdCollisions != 0)
                Log.Message(Tag + "[INCOMPLETE] seq=" + currentSequence +
                    "; reason=TRACE_ID_OR_COUNT_UNRELIABLE; originalDecisionUnchanged=True;");
            if (previousSnapshot != null)
                LogDifference(previousSnapshot, previousSequence, snapshot, currentSequence);
            else
                Log.Message(Tag + "[DIFF] seq=" + currentSequence +
                    "; status=NO_PREVIOUS_SNAPSHOT; tradeCompletion=NOT_OBSERVED;");
        }

        internal static void ObserveTransfer(Tradeable tradeable)
        {
            if (tradeable == null || currentSequence == 0) return;
            int count;
            try { count = tradeable.CountToTransfer; }
            catch (Exception)
            {
                Log.Message(Tag + "[INCOMPLETE] seq=" + currentSequence +
                    "; reason=TRANSFER_COUNT_UNAVAILABLE;");
                return;
            }
            int old;
            if (!LastTransferCount.TryGetValue(tradeable, out old))
            {
                if (LastTransferCount.Count >= MaxTrackedTradeRows)
                {
                    if (!trackedLimitReported)
                    {
                        trackedLimitReported = true;
                        Log.Message(Tag + "[LIMIT] seq=" + currentSequence +
                            "; reason=TRANSFER_TRACKED_ROW_LIMIT; max=" + MaxTrackedTradeRows + ";");
                    }
                    return;
                }
                LastTransferCount[tradeable] = count;
                TrackedIdentities[tradeable] = Identify(tradeable);
                return;
            }
            if (count == old) return;
            LastTransferCount[tradeable] = count;
            TradeRowIdentity identity;
            if (!TrackedIdentities.TryGetValue(tradeable, out identity))
                identity = Identify(tradeable);
            if (transferLogs >= MaxTransferLogs)
            {
                if (!transferLimitReported)
                {
                    transferLimitReported = true;
                    Log.Message(Tag + "[LIMIT] seq=" + currentSequence +
                        "; reason=TRANSFER_LOG_LIMIT; max=" + MaxTransferLogs + ";");
                }
                return;
            }
            transferLogs++;
            Log.Message(Tag + "[TRANSFER] seq=" + currentSequence +
                "; rowBookId=" + Escape(identity.BookId, 120) +
                "; projectSetHash=" + SchematicResearchSignature.Fingerprint(identity.FullKey) +
                "; side=" + Escape(identity.Side, 24) +
                "; old=" + old + "; new=" + count +
                "; direction=UNVERIFIED; tradeCompletion=NOT_OBSERVED;");
        }

        private static TradeRowIdentity Identify(Tradeable tradeable)
        {
            TradeRowIdentity info = new TradeRowIdentity();
            info.BookId = "ID_UNAVAILABLE";
            info.Side = "UNKNOWN";
            try
            {
                bool colony = tradeable.thingsColony != null && tradeable.thingsColony.Count > 0;
                bool trader = tradeable.thingsTrader != null && tradeable.thingsTrader.Count > 0;
                info.Side = colony ? (trader ? "BOTH" : "COLONY_ONLY") :
                    (trader ? "TRADER_ONLY" : "EMPTY");
                Thing thing = tradeable.AnyThing;
                Book book = thing == null ? null : thing.GetInnerIfMinified() as Book;
                if (book != null)
                {
                    info.BookId = book.ThingID;
                    SchematicResearchSignature.TryCreate(
                        SchematicResearchResolver.Resolve(book), out info.FullKey);
                }
            }
            catch (Exception) { info.Side = "INSPECTION_FAILED"; }
            return info;
        }

        internal static void LogRow(Tradeable tradeable,
            SchematicTradeGroupResolver.GroupSnapshot group,
            string fullKey, SchematicOwnershipSnapshot snapshot,
            SchematicOwnershipDecision decision, SchematicOwnershipEvidence evidence,
            bool manual)
        {
            if (tradeable == null || group == null || snapshot == null) return;
            Thing thing = null;
            Book book = null;
            string bookId = "UNAVAILABLE";
            string outerId = "NONE";
            string title = "UNAVAILABLE";
            try
            {
                thing = tradeable.AnyThing;
                book = thing == null ? null : thing.GetInnerIfMinified() as Book;
                if (book != null)
                {
                    bookId = string.IsNullOrEmpty(book.ThingID) ? "ID_UNAVAILABLE" : book.ThingID;
                    title = book.LabelCap == null ? "" : book.LabelCap.ToString();
                    if (!object.ReferenceEquals(thing, book))
                        outerId = thing.ThingID;
                }
            }
            catch (Exception) { bookId = "ID_UNAVAILABLE"; }
            string tag = manual ? "MANUAL" : "AUTO";
            Log.Message(Tag + "[TRADE_ROW] seq=" + currentSequence +
                "; mode=" + tag +
                "; rowSide=" + Escape(group.InventorySides, 24) +
                "; rowBookId=" + Escape(bookId, 120) +
                "; outerThingId=" + Escape(outerId, 120) +
                "; title=" + Escape(title, 120) +
                "; rowThingEntries=" + (group.Colony.ThingEntries + group.Trader.ThingEntries) +
                "; projectSetHash=" + SchematicResearchSignature.Fingerprint(fullKey) +
                "; projectDefNames=" + Escape(ProjectNames(fullKey), 120) +
                "; selected=" + SafeCount(tradeable) +
                "; evidence=" + evidence + "; decision=" + decision +
                "; directionVerified=False;");
            LogMatch(fullKey, bookId, snapshot);
        }

        internal static void LogIncompleteRow(Tradeable tradeable, string reason, bool manual)
        {
            string id = "UNAVAILABLE";
            try { if (tradeable != null && tradeable.AnyThing != null) id = tradeable.AnyThing.ThingID; }
            catch (Exception) { }
            Log.Message(Tag + "[INCOMPLETE] seq=" + currentSequence +
                "; mode=" + (manual ? "MANUAL" : "AUTO") +
                "; rowBookId=" + Escape(id, 120) +
                "; reason=" + Escape(reason, 120) + ";");
        }

        private static void LogMatch(string fullKey, string rowBookId, SchematicOwnershipSnapshot snapshot)
        {
            int traceCount = 0, verified = 0;
            List<string> ids = new List<string>();
            foreach (SchematicOwnershipTraceRecord record in snapshot.TraceBooks.Values)
            {
                if (!string.Equals(record.ProjectFullKey, fullKey, StringComparison.Ordinal)) continue;
                traceCount++;
                if (record.HasVerifiedSource) verified++;
                if (ids.Count < MaxMatchSamples)
                    ids.Add(record.BookThingId + "@" + record.SourceNames());
            }
            ids.Sort(StringComparer.Ordinal);
            int aggregate = snapshot.CountMatches(fullKey);
            Log.Message(Tag + "[MATCH] seq=" + currentSequence +
                "; rowBookId=" + Escape(rowBookId, 120) +
                "; projectSetHash=" + SchematicResearchSignature.Fingerprint(fullKey) +
                "; matchedTotal=" + aggregate +
                "; traceCount=" + traceCount +
                "; verifiedSourceCount=" + verified +
                "; confirmedKeyCount=" + ConfirmedCount(snapshot, fullKey) +
                "; samples=" + Escape(string.Join(",", ids.ToArray()), 500) +
                "; omittedSamples=" + Math.Max(0, traceCount - ids.Count) +
                "; countMismatch=" + (traceCount != aggregate) +
                "; matchesByFullKey=True; titleEqualityNotRequired=True;");
            if (traceCount != aggregate || verified != ConfirmedCount(snapshot, fullKey))
                Log.Message(Tag + "[INCOMPLETE] seq=" + currentSequence +
                    "; reason=TRACE_COUNT_MISMATCH; projectSetHash=" +
                    SchematicResearchSignature.Fingerprint(fullKey) +
                    "; ownershipDecisionUnchanged=True;");
        }

        private static int ConfirmedCount(SchematicOwnershipSnapshot snapshot, string fullKey)
        {
            int count;
            return snapshot.ConfirmedKeys.TryGetValue(fullKey, out count) ? count : 0;
        }

        private static void LogDifference(SchematicOwnershipSnapshot before, long beforeSeq,
            SchematicOwnershipSnapshot after, long afterSeq)
        {
            if (!SchematicOwnershipTrace.CanDiff(before, after))
            {
                Log.Message(Tag + "[DIFF] beforeSeq=" + beforeSeq +
                    "; afterSeq=" + afterSeq +
                    "; status=INCONCLUSIVE; reason=INCOMPLETE_SCAN_OR_ID_TRACE; " +
                    "tradeCompletion=NOT_OBSERVED;");
                return;
            }
            List<string> removed = new List<string>();
            List<string> added = new List<string>();
            List<string> retained = new List<string>();
            List<string> retainedMatchingRemoved = new List<string>();
            HashSet<string> removedKeys = new HashSet<string>(StringComparer.Ordinal);
            int changedSources = 0;
            foreach (KeyValuePair<string, SchematicOwnershipTraceRecord> entry in before.TraceBooks)
            {
                SchematicOwnershipTraceRecord now;
                if (!after.TraceBooks.TryGetValue(entry.Key, out now))
                {
                    removed.Add(entry.Key + "@" + SchematicResearchSignature.Fingerprint(entry.Value.ProjectFullKey));
                    removedKeys.Add(entry.Value.ProjectFullKey);
                }
                else
                {
                    if (!SchematicOwnershipTrace.SameIdentity(entry.Value, now))
                    {
                        Log.Message(Tag + "[DIFF] beforeSeq=" + beforeSeq +
                            "; afterSeq=" + afterSeq +
                            "; status=INCONCLUSIVE; reason=ID_PROJECT_CHANGED; " +
                            "tradeCompletion=NOT_OBSERVED;");
                        return;
                    }
                    retained.Add(entry.Key);
                    if (entry.Value.SourceNames() != now.SourceNames()) changedSources++;
                }
            }
            foreach (KeyValuePair<string, SchematicOwnershipTraceRecord> entry in after.TraceBooks)
                if (!before.TraceBooks.ContainsKey(entry.Key))
                    added.Add(entry.Key + "@" + SchematicResearchSignature.Fingerprint(entry.Value.ProjectFullKey));
            foreach (SchematicOwnershipTraceRecord record in after.TraceBooks.Values)
                if (removedKeys.Contains(record.ProjectFullKey))
                    retainedMatchingRemoved.Add(record.BookThingId + "@" +
                        SchematicResearchSignature.Fingerprint(record.ProjectFullKey));
            removed.Sort(StringComparer.Ordinal);
            added.Sort(StringComparer.Ordinal);
            retained.Sort(StringComparer.Ordinal);
            retainedMatchingRemoved.Sort(StringComparer.Ordinal);
            Log.Message(Tag + "[DIFF] beforeSeq=" + beforeSeq +
                "; afterSeq=" + afterSeq + "; status=COMPARED; " +
                "removedCount=" + removed.Count +
                "; removedBookIds=" + Escape(FirstN(removed, MaxDiffSamples), 450) +
                "; omittedRemoved=" + Math.Max(0, removed.Count - MaxDiffSamples) +
                "; addedCount=" + added.Count +
                "; addedBookIds=" + Escape(FirstN(added, MaxDiffSamples), 450) +
                "; omittedAdded=" + Math.Max(0, added.Count - MaxDiffSamples) +
                "; retainedCount=" + retained.Count +
                "; retainedBookIds=" + Escape(FirstN(retained, MaxDiffSamples), 450) +
                "; retainedSameProjectAsRemoved=" +
                    Escape(FirstN(retainedMatchingRemoved, MaxDiffSamples), 450) +
                "; omittedSameProject=" +
                    Math.Max(0, retainedMatchingRemoved.Count - MaxDiffSamples) +
                "; provenanceChanged=" + changedSources +
                "; tradeCompletion=NOT_OBSERVED; causality=NOT_PROVEN;");
        }

        private static string FirstN(List<string> values, int max)
        {
            return string.Join(",", values.GetRange(0, Math.Min(max, values.Count)).ToArray());
        }

        internal static string Escape(string value, int limit)
        {
            if (string.IsNullOrEmpty(value)) return "NONE";
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < value.Length; i++)
            {
                string token;
                char c = value[i];
                switch (c)
                {
                    case '\\': token = "\\\\"; break;
                    case '\r': token = "\\r"; break;
                    case '\n': token = "\\n"; break;
                    case '\t': token = "\\t"; break;
                    case ';': token = "\\u003B"; break;
                    case '"': token = "\\\""; break;
                    default: token = c.ToString(); break;
                }
                if (sb.Length + token.Length > limit) { sb.Append("~TRUNCATED"); break; }
                sb.Append(token);
            }
            return sb.ToString();
        }

        internal static string ProjectNames(string fullKey)
        {
            if (string.IsNullOrEmpty(fullKey)) return "NONE";
            List<string> names = new List<string>();
            int pos = 0;
            while (pos < fullKey.Length)
            {
                int colon = fullKey.IndexOf(':', pos);
                if (colon < 0) return "INVALID_FULL_KEY";
                int count;
                if (!int.TryParse(fullKey.Substring(pos, colon - pos), out count) ||
                    count <= 0 || colon + 1 + count > fullKey.Length)
                    return "INVALID_FULL_KEY";
                names.Add(fullKey.Substring(colon + 1, count));
                pos = colon + 1 + count;
                if (pos == fullKey.Length) break;
                if (fullKey[pos] != '|') return "INVALID_FULL_KEY";
                pos++;
            }
            return string.Join(",", names.ToArray());
        }

        private static string SafeCount(Tradeable tradeable)
        {
            try { return tradeable.CountToTransfer.ToString(); }
            catch (Exception) { return "UNAVAILABLE"; }
        }

        internal static bool PureSelfTest()
        {
            SchematicOwnershipSnapshot x = new SchematicOwnershipSnapshot();
            SchematicOwnershipSnapshot y = new SchematicOwnershipSnapshot();
            SchematicOwnershipTraceRecord a = new SchematicOwnershipTraceRecord("id1", "1:A", "one", null);
            a.AddSource("HOME_MAP_LOOSE");
            a.AddSource("TRADE_COLONY");
            if (!a.HasVerifiedSource || !a.SourcePromoted || a.Sources.Count != 2) return false;
            x.TraceBooks["id1"] = a;
            y.TraceBooks["id1"] = new SchematicOwnershipTraceRecord("id1", "1:A", "two", null);
            if (!SchematicOwnershipTrace.CanDiff(x, y) ||
                !SchematicOwnershipTrace.SameIdentity(a, y.TraceBooks["id1"])) return false;
            y.TraceMissingIds = 1;
            if (SchematicOwnershipTrace.CanDiff(x, y)) return false;
            if (ProjectNames("1:A|3:B;C") != "A,B;C") return false;
            if (ProjectNames("invalid") != "INVALID_FULL_KEY") return false;
            if (Escape("a;b\nc", 120) != "a\\u003Bb\\nc") return false;
            return true;
        }
    }
}
