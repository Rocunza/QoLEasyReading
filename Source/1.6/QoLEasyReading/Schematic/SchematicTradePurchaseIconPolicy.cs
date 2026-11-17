using System;
using Verse;

namespace ZCN.QoL.EasyReading
{
    /// <summary>
    /// S3-C2: a pure presentation-only selector. No scanning, logging or GUI.
    /// The group inspector and research status resolver run first and keep
    /// their original fail-closed policy.
    /// </summary>
    internal static class SchematicTradePurchaseIconPolicy
    {
        internal static SchematicIconKind Select(SchematicIconKind researchKind,
            string inventorySides, int transferCount,
            SchematicOwnershipDecision ownership, bool sessionValid,
            out string reason)
        {
            if (researchKind != SchematicIconKind.ResearchAvailable)
            {
                reason = "RESEARCH_STATUS_PRECEDENCE";
                return researchKind;
            }
            if (!string.Equals(inventorySides, "TRADER_ONLY", StringComparison.Ordinal))
            {
                // BOTH sign is deliberately NOT trusted until separately
                // validated in-game. COLONY_ONLY always means no buy warning.
                reason = inventorySides == "BOTH" ? "BOTH_DIRECTION_UNVERIFIED" :
                    "NOT_TRADER_ONLY";
                return researchKind;
            }
            if (!sessionValid || ownership != SchematicOwnershipDecision.Owned)
            {
                reason = !sessionValid ? "NO_VALID_SESSION" : "OWNERSHIP_UNCONFIRMED";
                return researchKind;
            }
            reason = "CONFIRMED_DUPLICATE_PURCHASE_ROW";
            return SchematicIconKind.DuplicateOwned;
        }

#if DEBUG
        internal static void RunPureSelfTests()
        {
            int checks = 0;
            int failures = 0;
            string reason;
            Check(Select(SchematicIconKind.AllFinished, "TRADER_ONLY", 0,
                SchematicOwnershipDecision.Owned, true, out reason) ==
                SchematicIconKind.AllFinished, ref checks, ref failures);
            Check(Select(SchematicIconKind.AllFinished, "TRADER_ONLY", 0,
                SchematicOwnershipDecision.Unknown, false, out reason) ==
                SchematicIconKind.AllFinished, ref checks, ref failures);
            Check(Select(SchematicIconKind.ResearchAvailable, "TRADER_ONLY", 0,
                SchematicOwnershipDecision.Owned, true, out reason) ==
                SchematicIconKind.DuplicateOwned, ref checks, ref failures);
            Check(Select(SchematicIconKind.ResearchAvailable, "TRADER_ONLY", 1,
                SchematicOwnershipDecision.Owned, true, out reason) ==
                SchematicIconKind.DuplicateOwned, ref checks, ref failures);
            Check(Select(SchematicIconKind.ResearchAvailable, "TRADER_ONLY", 0,
                SchematicOwnershipDecision.Unknown, true, out reason) ==
                SchematicIconKind.ResearchAvailable, ref checks, ref failures);
            Check(Select(SchematicIconKind.ResearchAvailable, "TRADER_ONLY", 0,
                SchematicOwnershipDecision.NotOwned, true, out reason) ==
                SchematicIconKind.ResearchAvailable, ref checks, ref failures);
            Check(Select(SchematicIconKind.ResearchAvailable, "TRADER_ONLY", 0,
                SchematicOwnershipDecision.Owned, false, out reason) ==
                SchematicIconKind.ResearchAvailable, ref checks, ref failures);
            Check(Select(SchematicIconKind.ResearchAvailable, "COLONY_ONLY", -1,
                SchematicOwnershipDecision.Owned, true, out reason) ==
                SchematicIconKind.ResearchAvailable, ref checks, ref failures);
            Check(Select(SchematicIconKind.ResearchAvailable, "COLONY_ONLY", 0,
                SchematicOwnershipDecision.Owned, true, out reason) ==
                SchematicIconKind.ResearchAvailable, ref checks, ref failures);
            Check(Select(SchematicIconKind.ResearchAvailable, "BOTH", 0,
                SchematicOwnershipDecision.Owned, true, out reason) ==
                SchematicIconKind.ResearchAvailable, ref checks, ref failures);
            Check(Select(SchematicIconKind.ResearchAvailable, "BOTH", 1,
                SchematicOwnershipDecision.Owned, true, out reason) ==
                SchematicIconKind.ResearchAvailable, ref checks, ref failures);
            Check(Select(SchematicIconKind.ResearchAvailable, "BOTH", -1,
                SchematicOwnershipDecision.Owned, true, out reason) ==
                SchematicIconKind.ResearchAvailable, ref checks, ref failures);
            Check(Select(SchematicIconKind.ReviewRequired, "TRADER_ONLY", 1,
                SchematicOwnershipDecision.Owned, true, out reason) ==
                SchematicIconKind.ReviewRequired, ref checks, ref failures);
            Check(Select(SchematicIconKind.ResearchAvailable, "EMPTY", 0,
                SchematicOwnershipDecision.Owned, true, out reason) ==
                SchematicIconKind.ResearchAvailable, ref checks, ref failures);
            if (failures == 0)
                Log.Message("[QoL: Easy Reading][S3C2][POLICY_AUDIT] status=PASS; checks=" +
                    checks + "; failures=0; debugIndependent=True; BOTH_WARNING_DISABLED=True; " +
                    "noTradeWrites=True; BUILD_ID=" + EasyReadingBuildIdentity.BuildId + ";");
            else
                Log.Error("[QoL: Easy Reading][S3C2][POLICY_AUDIT] status=FAIL; checks=" +
                    checks + "; failures=" + failures + ";");
        }

        private static void Check(bool ok, ref int checks, ref int failures)
        {
            checks++;
            if (!ok) failures++;
        }
#endif
    }
}
