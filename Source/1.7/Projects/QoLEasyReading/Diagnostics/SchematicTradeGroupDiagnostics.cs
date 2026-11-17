using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using GroupSnapshot = ZCN.QoL.EasyReading.SchematicTradeGroupResolver.GroupSnapshot;
using SideSummary = ZCN.QoL.EasyReading.SchematicTradeGroupResolver.SideSummary;

namespace ZCN.QoL.EasyReading
{
    /// <summary>
    /// S3-B0 read-only trade group observation/logging only. Debug-only cache;
    /// production Inspect() never reads or updates these diagnostic fields.
    /// Called by the existing trade-row Harmony Prefix on Repaint only.
    /// </summary>
    internal static class SchematicTradeGroupDiagnostics
    {
        private const int MaxTrackedRows = 96;
        private const int MaxNonSchematicRows = 512;
        private const int MaxLogLines = 48;
        // Same conservative gap used by the existing production ownership session.
        // TradeDeal reference identity is primary; the frame gap handles deal reuse
        // only when the trade UI was not observed for sufficiently long.
        private const int IdleFrameGapForNewSession = 120;

        // Debug session observation only; never read by production decisions.
        private static TradeDeal observedDeal;
        private static int lastObservedFrame = -1;
        private static int diagnosticSessionSequence;

        // Bounded Repaint observation cache; never used for production decisions.
        private static readonly Dictionary<Tradeable, int> LastObservedSign =
            new Dictionary<Tradeable, int>();
        private static readonly HashSet<Tradeable> NonSchematicRows =
            new HashSet<Tradeable>();
        private static int logLines;
        private static bool warnedLimit;
        private static bool loggedCountError;

        internal static void ResetIfInactive(bool activeAndDebug)
        {
            if (activeAndDebug) return;
            ResetObservationCache();
            observedDeal = null;
            lastObservedFrame = -1;
        }

        private static void ResetObservationCache()
        {
            if (LastObservedSign.Count != 0)
                LastObservedSign.Clear();
            if (NonSchematicRows.Count != 0)
                NonSchematicRows.Clear();
            logLines = 0;
            warnedLimit = false;
            loggedCountError = false;
        }

        /// <summary>
        /// Called BEFORE checking the 48-group quota and before sign de-duplication.
        /// Otherwise, an exhausted previous session can hide every new trade session.
        /// No extra Harmony patch, trade write, or production ownership scan.
        /// </summary>
        private static void ObserveSession()
        {
            if (!TradeSession.Active || TradeSession.deal == null)
            {
                ResetIfInactive(false);
                return;
            }

            TradeDeal deal = TradeSession.deal;
            int frame = Time.frameCount;
            bool newDeal = !object.ReferenceEquals(deal, observedDeal);
            bool idleGap = lastObservedFrame >= 0 &&
                (frame < lastObservedFrame ||
                 frame - lastObservedFrame > IdleFrameGapForNewSession);
            if (newDeal || idleGap)
            {
                ResetObservationCache();
                observedDeal = deal;
                diagnosticSessionSequence++;
                Log.Message("[QoL: Easy Reading][S3D0HF1][SESSION] status=OPEN; seq=" +
                    diagnosticSessionSequence +
                    "; reason=" + (newDeal ? "NEW_DEAL" : "WINDOW_DRAW_GAP") +
                    "; groupLogBudget=" + MaxLogLines +
                    "; cacheCleared=True; debugOnly=True; tradeWrites=0; build=" +
                    EasyReadingBuildIdentity.BuildId + ";");
            }
            // Track every observed Repaint, including cached/no-log trade rows.
            lastObservedFrame = frame;
        }

        /// <summary>
        /// Called only on EventType.Repaint while TradeSession.Active and the
        /// mod's existing detailed debug toggle is enabled. The key is the
        /// current Tradeable instance, NOT the book label or ThingDef.
        /// </summary>
        internal static void Observe(Tradeable tradeable)
        {
            if (!EasyReadingDebug.Enabled || !TradeSession.Active || TradeSession.deal == null)
            {
                ResetIfInactive(false);
                return;
            }
            // Session rollover must run even when the prior 48-line budget is used.
            ObserveSession();
            if (tradeable == null || !tradeable.IsThing || !tradeable.HasAnyThing)
                return;
            if (logLines >= MaxLogLines || NonSchematicRows.Contains(tradeable)) return;
            int count;
            try { count = tradeable.CountToTransfer; }
            catch (Exception ex)
            {
                if (!loggedCountError)
                {
                    loggedCountError = true;
                    Log.Warning("[QoL: Easy Reading][S3B0][COUNT] unavailable=" +
                        ex.GetType().Name + "; directions=NOT_VERIFIED");
                }
                return;
            }
            int sign = count > 0 ? 1 : count < 0 ? -1 : 0;
            int prior;
            if (LastObservedSign.TryGetValue(tradeable, out prior) && prior == sign)
                return;
            if (!LastObservedSign.ContainsKey(tradeable) &&
                LastObservedSign.Count >= MaxTrackedRows)
            {
                if (!warnedLimit)
                {
                    warnedLimit = true;
                    Log.Message("[QoL: Easy Reading][S3B0] Debug sample row limit reached (" +
                        MaxTrackedRows + "); further rows omitted, no UI change.");
                }
                return;
            }
            GroupSnapshot group = SchematicTradeGroupResolver.Inspect(tradeable, count);
            if (group == null || !group.HasSchematic)
            {
                // Do not let hundreds of non-book trade rows consume the
                // diagnostic quota reserved for actual schematic groups.
                if (NonSchematicRows.Count < MaxNonSchematicRows)
                    NonSchematicRows.Add(tradeable);
                return;
            }
            LastObservedSign[tradeable] = sign;
            logLines++;
            Log.Message("[QoL: Easy Reading][S3B0][GROUP] build=" +
                EasyReadingBuildIdentity.BuildId +
                "; readOnly=True; selectedCount=" + count +
                "; transferSign=" + (sign > 0 ? "POS" : sign < 0 ? "NEG" : "ZERO") +
                "; inventorySides=" + group.InventorySides +
                "; directionHypothesis=" + group.DirectionHypothesis +
                "; directionVerified=False" +
                "; colony=" + FormatSide(group.Colony) +
                "; trader=" + FormatSide(group.Trader) +
                "; mixedProjectsAcrossSides=" + group.MixedProjectsAcrossSides +
                "; mixedStateAcrossSides=" + group.MixedStateAcrossSides +
                "; decision=" + group.Decision +
                (string.IsNullOrEmpty(group.Error) ? "" : "; error=" + group.Error) +
                "; uiWrites=0; researchWrites=0" +
                "; diagnosticSession=" + diagnosticSessionSequence);
            if (logLines == MaxLogLines)
            {
                Log.Message("[QoL: Easy Reading][S3D0HF1][SESSION] status=GROUP_LOG_LIMIT; seq=" +
                    diagnosticSessionSequence + "; logged=" + logLines +
                    "; nextSessionResets=True; debugOnly=True; build=" +
                    EasyReadingBuildIdentity.BuildId + ";");
            }
        }

        private static string FormatSide(SideSummary side)
        {
            if (side == null) return "NULL";
            return "{entries=" + side.ThingEntries +
                ",schematics=" + side.SchematicBooks +
                ",otherBooks=" + side.OtherBooks +
                ",nonBooks=" + side.NonBooks +
                ",minified=" + side.MinifiedBooks +
                ",null=" + side.NullThings +
                ",examined=" + side.ExaminedBooks +
                ",invalid=" + side.InvalidResearchBooks +
                ",projectSignatures=" + side.ProjectSignatures +
                ",stateSignatures=" + side.StateSignatures +
                ",projects=" + side.Projects +
                ",finished=" + side.Finished +
                ",grantable=" + side.Grantable +
                ",classification=" + side.UniformClassification +
                ",projectSetHash=" + StableFingerprint(side.UniformProjectSignature) +
                ",status=" + side.Status + "}";
        }

        // Deterministic FNV-1a is for compact log fingerprints only.
        // Actual group equality above always compares the FULL signatures.
        private static string StableFingerprint(string value)
        {
            if (string.IsNullOrEmpty(value)) return "NONE";
            unchecked
            {
                uint hash = 2166136261;
                for (int i = 0; i < value.Length; i++)
                    hash = (hash ^ value[i]) * 16777619;
                return hash.ToString("X8");
            }
        }
    }
}
