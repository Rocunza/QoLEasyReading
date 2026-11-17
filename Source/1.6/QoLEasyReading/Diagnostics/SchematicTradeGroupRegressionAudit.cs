#if DEBUG
using System;
using System.Collections.Generic;
using LudeonTK;
using Verse;
using GroupSnapshot = ZCN.QoL.EasyReading.SchematicTradeGroupResolver.GroupSnapshot;

namespace ZCN.QoL.EasyReading
{
    /// <summary>
    /// Opt-in DebugAction; no startup execution, per-frame hook, synthetic
    /// game entities, research changes, trade writes, or permanent state.
    /// Checks the SAME production Complete(), TryConsumeBudget(), TryResolve()
    /// that are used by actual trading. It does NOT test Thing enumeration,
    /// signature construction, or live Tradeable / Book integration.
    /// </summary>
    public static class SchematicTradeGroupRegressionAudit
    {
        private const string Tag = "[QoL: Easy Reading][S3D0EFG][PURE_AUDIT]";
        private const int MaxFailureDetails = 8;

        private sealed class AuditCounters
        {
            internal int E, F, G, Errors;
            internal readonly List<string> Details = new List<string>();
        }

        [DebugAction("Easy Reading", "S3-D0 E-G group regression (pure, read-only)",
            actionType = DebugActionType.Action,
            allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void Run()
        {
            AuditCounters counts = new AuditCounters();
            // Each case is isolated so a failure cannot break the running game.
            RunE(counts);
            RunF(counts);
            RunG(counts);
            string status = counts.Errors == 0 ? "PASS" : "FAIL";
            string summary = Tag + " status=" + status +
                "; E=" + counts.E + "; F=" + counts.F + "; G=" + counts.G +
                "; total=" + (counts.E + counts.F + counts.G) +
                "; failures=" + counts.Errors +
                "; scope=SHARED_DECISION_AND_BUDGET_NOT_LIVE_TRADE" +
                "; actualThingScan=NOT_TESTED; buildSignatures=NOT_TESTED" +
                "; exceptionCatch=NOT_TESTED; researchWrites=0; tradeWrites=0; saveWrites=0" +
                "; harmonyPatchesAdded=0; manualDebugOnly=True; build=" +
                EasyReadingBuildIdentity.BuildId + ";";
            if (counts.Errors == 0) Log.Message(summary); else Log.Error(summary);
            for (int i = 0; i < counts.Details.Count; i++)
                Log.Warning(Tag + " fail=" + counts.Details[i] + ";");
        }

        private static void Case(AuditCounters counters, char section, string name, Action action)
        {
            if (section == 'E') counters.E++;
            else if (section == 'F') counters.F++;
            else counters.G++;
            try { action(); }
            catch (Exception ex)
            {
                counters.Errors++;
                if (counters.Details.Count < MaxFailureDetails)
                    counters.Details.Add(section + "/" + name + "/" + ex.GetType().Name +
                        (ex is InvalidOperationException ? "/" + ex.Message : ""));
            }
        }

        private static void Require(bool ok, string reason)
        {
            if (!ok) throw new InvalidOperationException(reason);
        }

        private static SchematicTradeIconResolver.Decision Resolved(GroupSnapshot group)
        {
            SchematicTradeGroupDecisionPolicy.Complete(group);
            Require(group.Decision == "UNIFORM_CANDIDATE_DIRECTION_UNVERIFIED",
                "not_uniform:" + group.Decision);
            SchematicTradeIconResolver.Decision icon;
            string suppress;
            Require(SchematicTradeIconResolver.TryResolve(group, out icon, out suppress),
                "icon_suppressed:" + suppress);
            Require(suppress == null, "unexpected_suppression");
            return icon;
        }

        private static void Suppressed(GroupSnapshot group, string expectedDecision)
        {
            SchematicTradeGroupDecisionPolicy.Complete(group);
            Require(group.Decision == expectedDecision, "decision:" + group.Decision);
            SchematicTradeIconResolver.Decision icon;
            string suppress;
            Require(!SchematicTradeIconResolver.TryResolve(group, out icon, out suppress),
                "unexpected_icon");
            Require(suppress == expectedDecision, "suppress:" + suppress);
        }

        private static void RunE(AuditCounters c)
        {
            Case(c, 'E', "trader_single", () => {
                GroupSnapshot s = SchematicTradeGroupTestFixtures.TraderSingle();
                Require(Resolved(s).Kind == SchematicIconKind.ResearchAvailable, "wrong_icon");
                Require(s.InventorySides == "TRADER_ONLY" && s.DirectionHypothesis == "ZERO_TRADER_STOCK", "direction_contract");
            });
            Case(c, 'E', "colony_single", () => {
                GroupSnapshot s = SchematicTradeGroupTestFixtures.ColonySingle();
                Require(Resolved(s).Kind == SchematicIconKind.ResearchAvailable, "wrong_icon");
                Require(s.InventorySides == "COLONY_ONLY" && s.DirectionHypothesis == "ZERO_COLONY_STOCK", "direction_contract");
            });
            Case(c, 'E', "mixed_projects_one_side", () => {
                GroupSnapshot s = SchematicTradeGroupTestFixtures.TraderSingle();
                s.Trader.ProjectSignatures = 2;
                Suppressed(s, "MIXED_PROJECTS_FAIL_CLOSED");
            });
            Case(c, 'E', "mixed_projects_across_sides", () => {
                GroupSnapshot s = SchematicTradeGroupTestFixtures.BothSame();
                s.Trader.UniformProjectSignature = "1:B";
                Suppressed(s, "MIXED_PROJECTS_FAIL_CLOSED");
                Require(s.MixedProjectsAcrossSides, "missed_cross_project");
            });
            Case(c, 'E', "mixed_states_one_side", () => {
                GroupSnapshot s = SchematicTradeGroupTestFixtures.TraderSingle();
                s.Trader.StateSignatures = 2;
                Suppressed(s, "MIXED_STATES_FAIL_CLOSED");
            });
            Case(c, 'E', "mixed_states_across_sides", () => {
                GroupSnapshot s = SchematicTradeGroupTestFixtures.BothSame();
                s.Trader.UniformStateSignature = "HasGrantableResearch|1:A:1111";
                Suppressed(s, "MIXED_STATES_FAIL_CLOSED");
                Require(s.MixedStateAcrossSides, "missed_cross_state");
            });
            Case(c, 'E', "mixed_type_other_book", () => {
                GroupSnapshot s = SchematicTradeGroupTestFixtures.TraderSingle();
                s.Trader.ThingEntries++; s.Trader.OtherBooks++;
                Suppressed(s, "MIXED_TYPES_FAIL_CLOSED");
            });
            Case(c, 'E', "mixed_type_non_book_and_null", () => {
                GroupSnapshot s = SchematicTradeGroupTestFixtures.TraderSingle();
                s.Trader.ThingEntries += 2; s.Trader.NonBooks++; s.Trader.NullThings++;
                Suppressed(s, "MIXED_TYPES_FAIL_CLOSED");
            });
            Case(c, 'E', "both_uniform_no_duplicate_exclamation", () => {
                GroupSnapshot s = SchematicTradeGroupTestFixtures.BothSame(1);
                SchematicIconKind researchIcon = Resolved(s).Kind;
                Require(s.InventorySides == "BOTH", "not_both");
                string reason;
                SchematicIconKind kind = SchematicTradePurchaseIconPolicy.Select(researchIcon,
                    s.InventorySides, s.CountToTransfer, SchematicOwnershipDecision.Owned,
                    true, out reason);
                Require(kind == SchematicIconKind.ResearchAvailable &&
                    reason == "BOTH_DIRECTION_UNVERIFIED", "both_duplicate_regression");
            });
            Case(c, 'E', "mixed_project_precedes_mixed_state", () => {
                GroupSnapshot s = SchematicTradeGroupTestFixtures.BothSame();
                s.Trader.UniformProjectSignature = "1:B";
                s.Trader.UniformStateSignature = "new_state";
                Suppressed(s, "MIXED_PROJECTS_FAIL_CLOSED");
            });
            Case(c, 'E', "positive_negative_hypothesis_only", () => {
                GroupSnapshot plus = SchematicTradeGroupTestFixtures.TraderSingle(1);
                GroupSnapshot minus = SchematicTradeGroupTestFixtures.ColonySingle(-1);
                Require(Resolved(plus).Kind == SchematicIconKind.ResearchAvailable &&
                    Resolved(minus).Kind == SchematicIconKind.ResearchAvailable, "icon_changed_by_sign");
                Require(plus.DirectionHypothesis == "POSITIVE_CANDIDATE_TRADER_UNVERIFIED" &&
                    minus.DirectionHypothesis == "NEGATIVE_CANDIDATE_COLONY_UNVERIFIED", "wrong_sign_hypothesis");
            });
        }

        private static int Consume(int count, out bool exceeded)
        {
            int left = SchematicTradeGroupInspector.NewInspectionBudget();
            exceeded = false;
            for (int i = 0; i < count; i++)
            {
                if (!SchematicTradeGroupInspector.TryConsumeBudget(ref left))
                {
                    exceeded = true;
                    break;
                }
            }
            return left;
        }

        private static void RunF(AuditCounters c)
        {
            Case(c, 'F', "zero_entries", () => {
                bool excess; int left = Consume(0, out excess);
                Require(!excess && left == 256, "zero_budget");
            });
            Case(c, 'F', "one_entry", () => {
                bool excess; int left = Consume(1, out excess);
                Require(!excess && left == 255, "one_budget");
            });
            Case(c, 'F', "255_entries", () => {
                bool excess; int left = Consume(255, out excess);
                Require(!excess && left == 1, "255_budget");
            });
            Case(c, 'F', "256_entries", () => {
                bool excess; int left = Consume(256, out excess);
                Require(!excess && left == 0, "256_budget");
            });
            Case(c, 'F', "257th_entry_rejected", () => {
                bool excess; int left = Consume(257, out excess);
                Require(excess && left == 0, "257_boundary");
            });
            Case(c, 'F', "two_sides_200_plus_56", () => {
                int left = SchematicTradeGroupInspector.NewInspectionBudget();
                for (int i = 0; i < 200; i++)
                    Require(SchematicTradeGroupInspector.TryConsumeBudget(ref left), "colony_under_budget");
                for (int i = 0; i < 56; i++)
                    Require(SchematicTradeGroupInspector.TryConsumeBudget(ref left), "trader_under_budget");
                Require(left == 0, "200_plus_56_count");
            });
            Case(c, 'F', "two_sides_200_plus_57", () => {
                int left = SchematicTradeGroupInspector.NewInspectionBudget();
                for (int i = 0; i < 200; i++)
                    Require(SchematicTradeGroupInspector.TryConsumeBudget(ref left), "colony_over_budget");
                for (int i = 0; i < 56; i++)
                    Require(SchematicTradeGroupInspector.TryConsumeBudget(ref left), "trader_over_budget");
                Require(!SchematicTradeGroupInspector.TryConsumeBudget(ref left) && left == 0,
                    "200_plus_57_boundary");
            });
            Case(c, 'F', "truncated_detected_schematic", () => {
                GroupSnapshot s = SchematicTradeGroupTestFixtures.TraderSingle();
                s.Trader.LimitExceeded = true; s.Trader.ThingEntries = 257;
                Suppressed(s, "LIMIT_EXCEEDED_FAIL_CLOSED");
            });
            Case(c, 'F', "truncated_no_schematic_priority", () => {
                GroupSnapshot s = SchematicTradeGroupTestFixtures.BeyondLimitWithoutDetectedSchematic();
                Suppressed(s, "NOT_SCHEMATIC");
                Require(!s.HasSchematic && s.Trader.LimitExceeded, "no_schematic_priority");
            });
            Case(c, 'F', "truncation_precedes_invalid", () => {
                GroupSnapshot s = SchematicTradeGroupTestFixtures.TraderSingle();
                s.Trader.LimitExceeded = true; s.Trader.InvalidResearchBooks = 1;
                Suppressed(s, "LIMIT_EXCEEDED_FAIL_CLOSED");
            });
        }

        private static void RunG(AuditCounters c)
        {
            Case(c, 'G', "invalid_research_blocks_icon", () => {
                GroupSnapshot s = SchematicTradeGroupTestFixtures.TraderSingle();
                s.Trader.InvalidResearchBooks = 1;
                Suppressed(s, "INVALID_RESEARCH_FAIL_CLOSED");
            });
            Case(c, 'G', "invalid_precedes_mixed", () => {
                GroupSnapshot s = SchematicTradeGroupTestFixtures.BothSame();
                s.Trader.InvalidResearchBooks = 1;
                s.Trader.ProjectSignatures = 2;
                s.Trader.StateSignatures = 2;
                Suppressed(s, "INVALID_RESEARCH_FAIL_CLOSED");
            });
            Case(c, 'G', "empty_no_schematic", () => {
                GroupSnapshot s = new GroupSnapshot {
                    Colony = SchematicTradeGroupTestFixtures.EmptySide(),
                    Trader = SchematicTradeGroupTestFixtures.EmptySide()
                };
                Suppressed(s, "NOT_SCHEMATIC");
                Require(s.InventorySides == "EMPTY" && s.DirectionHypothesis == "ZERO_AMBIGUOUS", "empty_contract");
            });
            Case(c, 'G', "null_snapshot_fails_closed", () => {
                SchematicTradeIconResolver.Decision icon;
                string reason;
                Require(!SchematicTradeIconResolver.TryResolve(null, out icon, out reason) &&
                    reason == "NO_SNAPSHOT", "null_snapshot_allowed");
            });
            Case(c, 'G', "invalid_unknown_state", () => {
                SchematicResearchState state = SchematicResearchState.Invalid(
                    SchematicResearchValidity.Unknown, "TEST_ONLY");
                Require(SchematicResearchClassifier.Classify(state) ==
                    SchematicResearchClassification.Unknown, "invalid_classification");
            });
            Case(c, 'G', "no_research_doer_not_applicable", () => {
                SchematicResearchState state = SchematicResearchState.Invalid(
                    SchematicResearchValidity.NoResearchDoer, "TEST_ONLY");
                Require(SchematicResearchClassifier.Classify(state) ==
                    SchematicResearchClassification.NotApplicable, "not_applicable");
            });
            Case(c, 'G', "null_state_unknown", () => {
                Require(SchematicResearchClassifier.Classify(null) ==
                    SchematicResearchClassification.Unknown, "null_classification");
            });
            Case(c, 'G', "empty_project_state_unknown", () => {
                SchematicResearchState state = SchematicResearchState.FromProjects(
                    new List<SchematicResearchProject>());
                Require(state.Validity == SchematicResearchValidity.NoProjects &&
                    SchematicResearchClassifier.Classify(state) ==
                    SchematicResearchClassification.Unknown, "empty_projects");
            });
            Case(c, 'G', "inconsistent_counts_are_suppressed", () => {
                GroupSnapshot s = SchematicTradeGroupTestFixtures.TraderSingle();
                s.Trader.UniformClassification = SchematicResearchClassification.Complete;
                SchematicTradeGroupDecisionPolicy.Complete(s);
                Require(s.Decision == "UNIFORM_CANDIDATE_DIRECTION_UNVERIFIED", "fixture_changed");
                SchematicTradeIconResolver.Decision icon;
                string reason;
                Require(!SchematicTradeIconResolver.TryResolve(s, out icon, out reason) &&
                    reason == "STATE_COUNTS_CHANGED", "count_mismatch_allowed");
            });
            Case(c, 'G', "inspection_failed_synthetic_suppression_only", () => {
                // Synthetic decision only: actual Inspector catch path is NOT tested here.
                GroupSnapshot s = SchematicTradeGroupTestFixtures.TraderSingle();
                s.Decision = "INSPECTION_FAILED";
                SchematicTradeIconResolver.Decision icon;
                string reason;
                Require(!SchematicTradeIconResolver.TryResolve(s, out icon, out reason) &&
                    reason == "INSPECTION_FAILED", "failed_inspection_painted");
            });
        }
    }
}
#endif
