using System;
using Verse;

namespace ZCN.QoL.EasyReading
{
    /// <summary>
    /// S2-A startup evidence plus S3-B2 count-only trade policy vectors.
    /// Never enumerates a live map, trades, or alters a book.
    /// </summary>
    internal static class SchematicResearchAudit
    {
        private static bool initialized;

        internal static void Initialize()
        {
            if (initialized) return;
            initialized = true;
            try
            {
                bool accessReady = SchematicResearchAccess.IsReady;
                int failures = VerifyClassifier();
                Log.Message("[QoL: Easy Reading][Schematic][S2A] BUILD_ID=" +
                    EasyReadingBuildIdentity.BuildId + "; Access=" + (accessReady ? "READY" : "UNAVAILABLE") +
                    "; valuesField=" + (accessReady ? "FOUND" : "UNVERIFIED") +
                    "; visibilityMethod=" + (accessReady ? "FOUND" : "UNVERIFIED") +
                    "; invocation=" + SchematicResearchAccess.InvocationMode +
                    "; classifierFailures=" + failures + "; harmonyPatchAdded=0; readOnly=True;");
                if (!accessReady)
                    Log.Warning("[QoL: Easy Reading][Schematic][S2A] Research status disabled: " +
                        SchematicResearchAccess.FailureReason + "; vanilla reading unchanged.");
                if (failures != 0)
                    Log.Warning("[QoL: Easy Reading][Schematic][S2A] Classifier self-test FAILED (" +
                        failures + "); status must not be enabled before investigation.");

                try
                {
                    // S3-B2 trade-only policy audit. Do not conflate this result
                    // with the S2-A research classifier's failure count.
                    int tradeFailures = VerifyTradeIconPolicy();
                    Log.Message("[QoL: Easy Reading][S3B2][POLICY_AUDIT] status=" +
                        (tradeFailures == 0 ? "PASS" : "FAIL") +
                        "; checks=11; failures=" + tradeFailures +
                        "; partialGrantable=ResearchAvailable; ownershipChecked=False" +
                        "; researchWrites=0; tradeWrites=0; BUILD_ID=" +
                        EasyReadingBuildIdentity.BuildId + ";");
                    if (tradeFailures != 0)
                        Log.Warning("[QoL: Easy Reading][S3B2][POLICY_AUDIT] FAIL: " +
                            tradeFailures + " of 11 policy vectors failed; inspect the trade icon decision.");
                }
                catch (Exception tradeEx)
                {
                    Log.Warning("[QoL: Easy Reading][S3B2][POLICY_AUDIT] status=FAIL; reason=" +
                        tradeEx.GetType().Name + "; BUILD_ID=" + EasyReadingBuildIdentity.BuildId + ";");
                }
            }
            catch (Exception ex)
            {
                Log.Warning("[QoL: Easy Reading][Schematic][S2A] Audit unavailable: " +
                    ex.GetType().Name + "; vanilla reading unchanged.");
            }
        }

        private static int VerifyClassifier()
        {
            int failed = 0;
            failed += Check(1, 0, 1, 0, SchematicResearchClassification.HasGrantableResearch);
            failed += Check(1, 1, 0, 0, SchematicResearchClassification.Complete);
            failed += Check(2, 1, 1, 0, SchematicResearchClassification.HasGrantableResearch);
            failed += Check(2, 2, 0, 0, SchematicResearchClassification.Complete);
            failed += Check(1, 0, 0, 1, SchematicResearchClassification.UnavailableNow);
            failed += Check(5, 2, 3, 0, SchematicResearchClassification.HasGrantableResearch);
            failed += Check(5, 2, 0, 3, SchematicResearchClassification.UnavailableNow);
            failed += Check(0, 0, 0, 0, SchematicResearchClassification.Unknown);
            failed += Check(2, 3, 0, 0, SchematicResearchClassification.Unknown);
            failed += Check(2, 0, 1, 0, SchematicResearchClassification.Unknown);
            return failed;
        }

        private static int VerifyTradeIconPolicy()
        {
            int failed = 0;
            failed += CheckTrade(1, 0, 1, 0, true, SchematicIconKind.ResearchAvailable);
            failed += CheckTrade(2, 1, 1, 0, true, SchematicIconKind.ResearchAvailable);
            failed += CheckTrade(3, 2, 1, 0, true, SchematicIconKind.ResearchAvailable);
            failed += CheckTrade(3, 1, 1, 1, true, SchematicIconKind.ResearchAvailable);
            failed += CheckTrade(3, 0, 1, 2, true, SchematicIconKind.ResearchAvailable);
            failed += CheckTrade(2, 2, 0, 0, true, SchematicIconKind.AllFinished);
            failed += CheckTrade(2, 1, 0, 1, false, SchematicIconKind.ResearchAvailable);
            failed += CheckTrade(1, 0, 0, 1, false, SchematicIconKind.ResearchAvailable);
            failed += CheckTrade(0, 0, 0, 0, false, SchematicIconKind.ResearchAvailable);
            failed += CheckTrade(2, 3, 0, 0, false, SchematicIconKind.ResearchAvailable);
            failed += CheckTrade(2, 1, 2, 0, false, SchematicIconKind.ResearchAvailable);
            return failed;
        }

        private static int CheckTrade(int total, int finished, int grantable, int unavailable,
            bool expectedVisible, SchematicIconKind expectedKind)
        {
            SchematicIconKind actualKind;
            bool visible = SchematicTradeIconResolver.TryClassifyCounts(
                total, finished, grantable, unavailable, out actualKind);
            if (visible == expectedVisible && (!visible || actualKind == expectedKind))
                return 0;
            Log.Warning("[QoL: Easy Reading][S3B2][POLICY_AUDIT] vector failed: " +
                "total=" + total + "; finished=" + finished +
                "; grantable=" + grantable + "; unavailable=" + unavailable +
                "; expected=" + (expectedVisible ? expectedKind.ToString() : "Hidden") +
                "; actual=" + (visible ? actualKind.ToString() : "Hidden") + ";");
            return 1;
        }

        private static int Check(int total, int finished, int grantable, int unavailable,
            SchematicResearchClassification expected)
        {
            return SchematicResearchClassifier.ClassifyCounts(total, finished, grantable, unavailable)
                == expected ? 0 : 1;
        }
    }
}
