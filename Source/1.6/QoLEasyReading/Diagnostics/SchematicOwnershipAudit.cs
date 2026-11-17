using System;
using System.Collections.Generic;
using LudeonTK;
using RimWorld;
using UnityEngine;
using Verse;

namespace ZCN.QoL.EasyReading
{
    /// <summary>
    /// S3-C1: debug-only consumer of an immutable-in-practice ownership
    /// snapshot. Does NOT select an icon or write any trade / research state.
    /// No new Harmony patch; the existing trade row hook calls Observe.
    /// </summary>
    public static class SchematicOwnershipAudit
    {
        private const string Tag = "[QoL: Easy Reading][S3C1][OWNERSHIP]";
        private static TradeDeal currentDeal;
        private static int currentProductionSessionSequence;
        private static SchematicOwnershipSnapshot currentSnapshot;
        private static readonly HashSet<Tradeable> ObservedRows = new HashSet<Tradeable>();
        private static int totalRows;
        private static int ownedRows;
        private static int unknownRows;
        private static int invalidRows;
        private static bool loggedCap;
        private static bool selfTestRun;

        internal static void ResetIfInactive(bool activeDebug)
        {
            if (activeDebug) return;
            EndSession("INACTIVE_OR_DISABLED");
        }

        private const int MaxSummaryKinds = 14;
        private static readonly HashSet<string> SeenSummaryKinds =
            new HashSet<string>(StringComparer.Ordinal);

        internal static void Observe(Tradeable tradeable)
        {
            if (!EasyReadingDebug.Enabled || !TradeSession.Active ||
                Event.current == null || Event.current.type != EventType.Repaint ||
                tradeable == null || !tradeable.IsThing || !tradeable.HasAnyThing)
                return;
            Thing candidate = tradeable.AnyThing;
            Book book = candidate == null ? null : candidate.GetInnerIfMinified() as Book;
            if (!SchematicStatusIconResolver.IsSchematic(book)) return;
            TradeDeal deal = TradeSession.deal;
            if (deal == null) return;
            SchematicOwnershipSnapshot productionSnapshot;
            if (!SchematicTradeOwnershipSession.TryGetSnapshot(out productionSnapshot))
                return;
            int productionSequence = SchematicTradeOwnershipSession.SessionSequence;
            if (!object.ReferenceEquals(currentDeal, deal) ||
                currentProductionSessionSequence != productionSequence ||
                !object.ReferenceEquals(currentSnapshot, productionSnapshot))
            {
                EndSession("NEW_PRODUCTION_SESSION");
                currentDeal = deal;
                currentProductionSessionSequence = productionSequence;
                currentSnapshot = productionSnapshot;
                SchematicOwnershipTraceLogger.Open(currentSnapshot, "SHARED_PRODUCTION_SNAPSHOT");
                Log.Message(Tag + "[SESSION] status=OPEN; source=S3C2_PRODUCTION_SESSION; " +
                    "snapshotShared=True; debug=True; uiWrites=0; BUILD_ID=" +
                    EasyReadingBuildIdentity.BuildId + ";");
                LogScan(currentSnapshot, "SHARED_PRODUCTION_SNAPSHOT");
            }
            // Transfer count changes remain available in normal Debug mode,
            // but verbose per-book TRADE_ROW/MATCH is now manual-dump only.
            SchematicOwnershipTraceLogger.ObserveTransfer(tradeable);
            if (!ObservedRows.Add(tradeable)) return; // count distinct rows only
            totalRows++;
            SchematicTradeGroupResolver.GroupSnapshot row =
                SchematicTradeGroupResolver.Inspect(tradeable, 0);
            if (row == null || row.Decision !=
                "UNIFORM_CANDIDATE_DIRECTION_UNVERIFIED")
            {
                invalidRows++;
                return;
            }
            string key = row.Colony.SchematicBooks > 0
                ? row.Colony.UniformProjectSignature : row.Trader.UniformProjectSignature;
            SchematicOwnershipDecision decision = currentSnapshot == null
                ? SchematicOwnershipDecision.Unknown : currentSnapshot.GetDecision(key);
            if (decision == SchematicOwnershipDecision.Owned) ownedRows++;
            else unknownRows++;
            if (SeenSummaryKinds.Count >= MaxSummaryKinds) return;
            string summary = row.InventorySides + ":" + decision;
            if (!SeenSummaryKinds.Add(summary)) return;
            Log.Message(Tag + "[ROW_SUMMARY] sides=" + row.InventorySides +
                "; ownership=" + decision + "; scanShared=True; " +
                "fullThingIds=MANUAL_DUMP_ONLY; countDirection=UNVERIFIED;");
        }

        [DebugAction("Easy Reading", "S3-C1 re-scan owned schematics (read-only)",
            actionType = DebugActionType.Action,
            allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void ManualRescan()
        {
            try
            {
                if (!EasyReadingDebug.Enabled)
                {
                    Log.Message(Tag + "[MANUAL] status=SKIP; reason=DEBUG_LOGGING_OFF;");
                    return;
                }
                if (!TradeSession.Active || TradeSession.deal == null)
                {
                    Log.Message(Tag + "[MANUAL] status=SKIP; reason=NO_ACTIVE_TRADE; readOnly=True;");
                    return;
                }
                EndSession("MANUAL_RESCAN");
                currentDeal = TradeSession.deal;
                if (!SchematicTradeOwnershipSession.ForceRescan(out currentSnapshot))
                {
                    Log.Message(Tag + "[MANUAL] status=SKIP; reason=SESSION_UNAVAILABLE_OR_ICON_SETTING_OFF;");
                    currentDeal = null;
                    return;
                }
                currentProductionSessionSequence = SchematicTradeOwnershipSession.SessionSequence;
                SchematicOwnershipTraceLogger.Open(currentSnapshot, "MANUAL_RESCAN_SHARED");
                LogScan(currentSnapshot, "MANUAL_RESCAN");
                List<Tradeable> rows = currentDeal.AllTradeables;
                if (rows == null) return;
                int eligible = 0;
                for (int i = 0; i < rows.Count; i++)
                {
                    Tradeable tradeable = rows[i];
                    if (tradeable == null || !tradeable.IsThing || !tradeable.HasAnyThing)
                        continue;
                    Thing thing = tradeable.AnyThing;
                    Book book = thing == null ? null : thing.GetInnerIfMinified() as Book;
                    if (!SchematicStatusIconResolver.IsSchematic(book)) continue;
                    eligible++;
                    if (ObservedRows.Count >= SchematicOwnershipTraceLogger.ManualDumpLimit)
                        continue;
                    if (ObservedRows.Add(tradeable))
                    {
                        SchematicOwnershipTraceLogger.ObserveTransfer(tradeable);
                        LogRow(tradeable, currentSnapshot, true);
                    }
                }
                if (eligible > SchematicOwnershipTraceLogger.ManualDumpLimit)
                    Log.Message(Tag + "[MANUAL_DUMP_LIMIT] totalEligible=" + eligible +
                        "; shown=" + SchematicOwnershipTraceLogger.ManualDumpLimit +
                        "; omitted=" + (eligible - SchematicOwnershipTraceLogger.ManualDumpLimit) +
                        "; NOTE=TARGET_MAY_BE_OUTSIDE_LIMIT;");
            }
            catch (Exception ex)
            {
                Log.Warning(Tag + "[MANUAL] status=FAILED; error=" + ex.GetType().Name +
                    "; uiWrites=0; researchWrites=0; tradeWrites=0;");
            }
        }

        private static void LogScan(SchematicOwnershipSnapshot scan, string trigger)
        {
            if (scan == null) return;
            Log.Message(Tag + "[SCAN] trigger=" + trigger +
                "; maps=" + scan.MapsVisited + "; caravans=" + scan.CaravansVisited +
                "; tradeColonyRows=" + scan.ColonyTradeablesVisited +
                "; things=" + scan.ThingsExamined +
                "; mapRoots=" + scan.RootsExamined +
                "; schematics=" + scan.SchematicBooks +
                "; uniqueKeys=" + scan.FoundKeys.Count +
                "; confirmedKeys=" + scan.ConfirmedKeys.Count +
                "; invalidBooks=" + scan.InvalidBooks +
                "; duplicateReferences=" + scan.DuplicateBookReferences +
                "; foreignRootsSkipped=" + scan.ForeignRootsSkipped +
                "; ambiguousHoldersSkipped=" + scan.AmbiguousHoldersSkipped +
                "; homeLooseAssumed=" + scan.GroundBooksAssumedColony +
                "; coverage=" + SchematicOwnershipSnapshot.Coverage +
                "; truncated=" + scan.Truncated +
                "; failed=" + scan.InspectionFailed +
                "; reason=" + (scan.FailureReason ?? "NONE") +
                "; elapsedMs=" + scan.ElapsedMilliseconds +
                "; traceIds=" + scan.TraceBooks.Count +
                "; traceMissingIds=" + scan.TraceMissingIds +
                "; traceCollisions=" + scan.TraceIdCollisions +
                "; traceFailed=" + scan.TraceFailed +
                "; traceCountMismatch=" + scan.TraceCountMismatch +
                "; NOT_OWNED_PERMITTED=False; readOnly=True;");
        }

        private static void LogRow(Tradeable tradeable, SchematicOwnershipSnapshot owned, bool manual)
        {
            totalRows++;
            try
            {
                SchematicTradeGroupResolver.GroupSnapshot row =
                    SchematicTradeGroupResolver.Inspect(tradeable, 0);
                if (row == null || row.Decision != "UNIFORM_CANDIDATE_DIRECTION_UNVERIFIED")
                {
                    invalidRows++;
                    string reason = row == null ? "NULL_GROUP" : row.Decision;
                    Log.Message(Tag + "[ROW] result=SKIP; reason=" + reason +
                        "; uiWrites=0;");
                    SchematicOwnershipTraceLogger.LogIncompleteRow(tradeable, reason, manual);
                    return;
                }
                string key = row.Colony.SchematicBooks > 0
                    ? row.Colony.UniformProjectSignature : row.Trader.UniformProjectSignature;
                if (string.IsNullOrEmpty(key) || owned == null)
                {
                    invalidRows++;
                    Log.Message(Tag + "[ROW] result=UNKNOWN; reason=NO_PROJECT_KEY_OR_SCAN; uiWrites=0;");
                    SchematicOwnershipTraceLogger.LogIncompleteRow(tradeable,
                        "NO_PROJECT_KEY_OR_SCAN", manual);
                    return;
                }
                SchematicOwnershipEvidence evidence = owned.GetEvidence(key);
                SchematicOwnershipDecision decision = owned.GetDecision(key);
                if (decision == SchematicOwnershipDecision.Owned) ownedRows++;
                else unknownRows++;
                string count = "UNAVAILABLE";
                try { count = tradeable.CountToTransfer.ToString(); }
                catch (Exception) { }
                Log.Message(Tag + "[ROW] rowSource=" + row.InventorySides +
                    "; projectSetHash=" + SchematicResearchSignature.Fingerprint(key) +
                    "; projects=" + (row.Colony.SchematicBooks > 0
                        ? row.Colony.Projects : row.Trader.Projects) +
                    "; evidence=" + evidence + "; decision=" + decision +
                    "; matched=" + owned.CountMatches(key) +
                    "; provenOwned=" + owned.ConfirmedKeys.ContainsKey(key) +
                    "; selectedCount=" + count +
                    "; directionVerified=False; coverage=INCOMPLETE_FOR_NEGATIVE;" +
                    "; fullKeyCompared=True; uiWrites=0; tradeWrites=0;");
                SchematicOwnershipTraceLogger.LogRow(tradeable, row, key, owned,
                    decision, evidence, manual);
            }
            catch (Exception ex)
            {
                invalidRows++;
                Log.Warning(Tag + "[ROW] result=UNKNOWN; reason=" +
                    ex.GetType().Name + "; uiWrites=0;");
                SchematicOwnershipTraceLogger.LogIncompleteRow(tradeable,
                    "ROW_EXCEPTION_" + ex.GetType().Name, manual);
            }
        }

        private static void EndSession(string reason)
        {
            SchematicOwnershipTraceLogger.Close(currentSnapshot, reason);
            if (currentDeal != null)
                Log.Message(Tag + "[END] reason=" + reason +
                    "; rows=" + totalRows + "; owned=" + ownedRows +
                    "; unknown=" + unknownRows + "; skipped=" + invalidRows +
                    "; writes=0;");
            currentDeal = null;
            currentProductionSessionSequence = 0;
            currentSnapshot = null;
            ObservedRows.Clear();
            SeenSummaryKinds.Clear();
            totalRows = ownedRows = unknownRows = invalidRows = 0;
            loggedCap = false;
        }

        internal static void RunPureSelfTests()
        {
            if (selfTestRun) return;
            selfTestRun = true;
            int checks = 0;
            int failures = 0;
            string a, b;
            Check(SchematicResearchSignature.TryCreateFromNames(new[] { "A", "B" }, out a),
                ref checks, ref failures);
            Check(SchematicResearchSignature.TryCreateFromNames(new[] { "B", "A" }, out b) &&
                string.Equals(a, b, StringComparison.Ordinal), ref checks, ref failures);
            Check(SchematicResearchSignature.TryCreateFromNames(new[] { "A", "A", "B" }, out b) &&
                a == b, ref checks, ref failures);
            Check(SchematicResearchSignature.TryCreateFromNames(new[] { "A", "C" }, out b) &&
                a != b, ref checks, ref failures);
            Check(SchematicResearchSignature.TryCreateFromNames(new[] { "A|B", "C" }, out b) &&
                a != b, ref checks, ref failures);
            Check(!SchematicResearchSignature.TryCreateFromNames(new string[0], out b),
                ref checks, ref failures);
            Check(!SchematicResearchSignature.TryCreateFromNames(new[] { "A", (string)null }, out b),
                ref checks, ref failures);
            // B0 golden format: ordinal-sort length-prefixed project names.
            Check(a == "1:A|1:B", ref checks, ref failures);
            Check(SchematicResearchSignature.Fingerprint(a) != "NONE", ref checks, ref failures);
            SchematicOwnershipSnapshot empty = new SchematicOwnershipSnapshot();
            Check(empty.GetEvidence(a) == SchematicOwnershipEvidence.NoMatchInScannedScope,
                ref checks, ref failures);
            Check(empty.GetDecision(a) == SchematicOwnershipDecision.Unknown,
                ref checks, ref failures);
            empty.Truncated = true;
            Check(empty.GetEvidence(a) == SchematicOwnershipEvidence.InspectionFailed &&
                empty.GetDecision(a) == SchematicOwnershipDecision.Unknown,
                ref checks, ref failures);
            empty.FoundKeys[a] = 1;
            Check(empty.GetEvidence(a) == SchematicOwnershipEvidence.MatchFound &&
                empty.GetDecision(a) == SchematicOwnershipDecision.Unknown,
                ref checks, ref failures);
            empty.ConfirmedKeys[a] = 1;
            Check(empty.GetDecision(a) == SchematicOwnershipDecision.Owned,
                ref checks, ref failures);
            empty.InspectionFailed = true;
            Check(empty.GetDecision(a) == SchematicOwnershipDecision.Owned,
                ref checks, ref failures);
            Check(empty.GetDecision(null) == SchematicOwnershipDecision.Unknown,
                ref checks, ref failures);
            Check(SchematicOwnershipTraceLogger.PureSelfTest(), ref checks, ref failures);
            if (failures == 0)
                Log.Message(Tag + "[PURE_AUDIT] status=PASS; checks=" + checks +
                    "; failures=0; noGameScan=True; noTradeOrResearchWrites=True; BUILD_ID=" +
                    EasyReadingBuildIdentity.BuildId + ";");
            else
                Log.Error(Tag + "[PURE_AUDIT] status=FAIL; checks=" + checks +
                    "; failures=" + failures + "; noGameScan=True;");
        }

        private static void Check(bool ok, ref int checks, ref int failures)
        {
            checks++;
            if (!ok) failures++;
        }
    }
}
