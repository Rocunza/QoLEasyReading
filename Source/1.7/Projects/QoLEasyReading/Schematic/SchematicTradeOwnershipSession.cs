using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace ZCN.QoL.EasyReading
{
    /// <summary>
    /// S3-C2: the one production owner of read-only Schematic ownership snapshots.
    /// It does not depend on debug logging or on the diagnostic class. No writes
    /// to TradeDeal, Thing, research or any player-owned storage.
    /// </summary>
    internal static class SchematicTradeOwnershipSession
    {
        private const string Tag = "[QoL: Easy Reading][S3C2][SESSION]";
        // Detect a trade window that ceased drawing without adding a new Harmony
        // hook. The deal normally changes on SetupWith, but do not rely on that
        // identity alone if a third-party implementation reuses the object.
        private const int IdleFrameGapForNewSession = 120;
#if DEBUG
        private const int MaxLoggedIconChoices = 20;
#endif
        private static TradeDeal currentDeal;
        private static SchematicOwnershipSnapshot currentSnapshot;
        private static bool scanAttempted;
        private static int lastObservedFrame = -1;
        private static int sessionSequence;
#if DEBUG
        private static int scansThisSession;
        private static int cacheHitsThisSession;
        private static readonly HashSet<string> LoggedChoices =
            new HashSet<string>(StringComparer.Ordinal);
#endif

        internal static int SessionSequence { get { return sessionSequence; } }
#if DEBUG
        internal static int ScansThisSession { get { return scansThisSession; } }
#endif

        internal static void ResetIfInactive()
        {
            if (TradeSession.Active && EasyReadingMod.Settings != null &&
                EasyReadingMod.Settings.showSchematicTradeIcons)
                return;
            Forget("INACTIVE_OR_SETTING_OFF");
        }

        internal static bool TryGetSnapshot(out SchematicOwnershipSnapshot snapshot)
        {
            snapshot = null;
            if (!TradeSession.Active || TradeSession.deal == null ||
                EasyReadingMod.Settings == null ||
                !EasyReadingMod.Settings.showSchematicTradeIcons)
            {
                Forget("INACTIVE_OR_SETTING_OFF");
                return false;
            }

            TradeDeal deal = TradeSession.deal;
            int frame = Time.frameCount;
            bool differentDeal = !object.ReferenceEquals(deal, currentDeal);
            bool idleGap = lastObservedFrame >= 0 &&
                (frame < lastObservedFrame || frame - lastObservedFrame > IdleFrameGapForNewSession);
            if (differentDeal || idleGap)
            {
                Forget(differentDeal ? "NEW_DEAL" : "WINDOW_DRAW_GAP");
                currentDeal = deal;
                sessionSequence++;
#if DEBUG
                if (EasyReadingDebug.Enabled)
                    Log.Message(Tag + " status=OPEN; seq=" + sessionSequence +
                        "; reason=" + (differentDeal ? "NEW_DEAL" : "WINDOW_DRAW_GAP") +
                        "; debug=True; scanCount=0; build=" + EasyReadingBuildIdentity.BuildId + ";");
#endif
            }
            lastObservedFrame = frame;
            if (!scanAttempted)
                ScanOnce(deal);
#if DEBUG
            else
                cacheHitsThisSession++;
#endif
            snapshot = currentSnapshot;
            return snapshot != null;
        }

        /// <summary>Manual developer action only. Replaces the production
        /// snapshot too, so the UI and debug dump never use different evidence.</summary>
#if DEBUG
        internal static bool ForceRescan(out SchematicOwnershipSnapshot snapshot)
        {
            snapshot = null;
            SchematicOwnershipSnapshot ignored;
            if (!TryGetSnapshot(out ignored)) return false;
            ScanOnce(currentDeal);
            snapshot = currentSnapshot;
            return snapshot != null;
        }

#endif

        private static void ScanOnce(TradeDeal deal)
        {
            scanAttempted = true; // failures are cached; never scan once per row
            currentSnapshot = null;
#if DEBUG
            scansThisSession++;
#endif
            try
            {
                currentSnapshot = SchematicOwnershipResolver.Scan(deal);
#if DEBUG
                if (EasyReadingDebug.Enabled && currentSnapshot != null)
                    Log.Message(Tag + " status=SCANNED; seq=" + sessionSequence +
                        "; scanCount=" + scansThisSession +
                        "; cacheHits=" + cacheHitsThisSession +
                        "; elapsedMs=" + currentSnapshot.ElapsedMilliseconds +
                        "; schematics=" + currentSnapshot.SchematicBooks +
                        "; confirmedKeys=" + currentSnapshot.ConfirmedKeys.Count +
                        "; truncated=" + currentSnapshot.Truncated +
                        "; failed=" + currentSnapshot.InspectionFailed +
                        "; readOnly=True; debugIndependent=True;");
#endif
            }
            catch (Exception ex)
            {
                // The base ?/check research result must survive a broken scan.
                Log.ErrorOnce(Tag + " scan failure: " + ex.GetType().Name +
                    "; fallback=RESEARCH_ICON; no trade writes", 1726194672);
            }
        }

#if DEBUG
        internal static void LogIconDecisionOnce(string inventorySides, string fullKey,
            SchematicOwnershipDecision ownership, SchematicIconKind selected, string reason)
        {
            if (!EasyReadingDebug.Enabled || Event.current == null ||
                Event.current.type != EventType.Repaint ||
                LoggedChoices.Count >= MaxLoggedIconChoices)
                return;
            string compact = (inventorySides ?? "NONE") + ":" +
                SchematicResearchSignature.Fingerprint(fullKey) + ":" + selected;
            if (!LoggedChoices.Add(compact)) return;
            Log.Message("[QoL: Easy Reading][S3C2][ICON_DECISION] seq=" + sessionSequence +
                "; sides=" + inventorySides +
                "; ownership=" + ownership + "; result=" + selected +
                "; projectSetHash=" + SchematicResearchSignature.Fingerprint(fullKey) +
                "; reason=" + reason + "; tradeWrites=0; researchWrites=0;");
        }

#endif

        private static void Forget(string reason)
        {
            if (currentDeal == null && currentSnapshot == null && !scanAttempted)
                return;
#if DEBUG
            if (EasyReadingDebug.Enabled)
                Log.Message(Tag + " status=INVALIDATED; reason=" + reason +
                    "; seq=" + sessionSequence + "; scans=" + scansThisSession +
                    "; cacheHits=" + cacheHitsThisSession + ";");
#endif
            currentDeal = null;
            currentSnapshot = null;
            scanAttempted = false;
            lastObservedFrame = -1;
#if DEBUG
            scansThisSession = 0;
            cacheHitsThisSession = 0;
            LoggedChoices.Clear();
#endif
        }
    }
}
