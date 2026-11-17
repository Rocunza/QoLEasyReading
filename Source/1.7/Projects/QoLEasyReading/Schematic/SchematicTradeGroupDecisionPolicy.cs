using System;
using GroupSnapshot = ZCN.QoL.EasyReading.SchematicTradeGroupResolver.GroupSnapshot;

namespace ZCN.QoL.EasyReading
{
    /// <summary>
    /// Pure final group decision shared by the production trade inspector and
    /// manual Debug regression fixtures. Input summaries are already inspected;
    /// this function does not read TradeSession, Book, research, or UI state.
    /// Original decision branches and strings are preserved from HF1.
    /// </summary>
    internal static class SchematicTradeGroupDecisionPolicy
    {
        internal static void Complete(GroupSnapshot snapshot)
        {
            int selectedCount = snapshot.CountToTransfer;
            snapshot.HasSchematic = snapshot.Colony.SchematicBooks > 0 ||
                snapshot.Trader.SchematicBooks > 0;

            bool c = snapshot.Colony.ThingEntries != 0;
            bool t = snapshot.Trader.ThingEntries != 0;
            snapshot.InventorySides = c ? (t ? "BOTH" : "COLONY_ONLY") :
                (t ? "TRADER_ONLY" : "EMPTY");
            snapshot.DirectionHypothesis = selectedCount > 0
                ? "POSITIVE_CANDIDATE_TRADER_UNVERIFIED"
                : selectedCount < 0
                    ? "NEGATIVE_CANDIDATE_COLONY_UNVERIFIED"
                    : t && !c ? "ZERO_TRADER_STOCK"
                    : c && !t ? "ZERO_COLONY_STOCK"
                    : "ZERO_AMBIGUOUS";

            bool both = snapshot.Colony.SchematicBooks > 0 &&
                snapshot.Trader.SchematicBooks > 0;
            snapshot.MixedProjectsAcrossSides = both &&
                !string.Equals(snapshot.Colony.UniformProjectSignature,
                    snapshot.Trader.UniformProjectSignature, StringComparison.Ordinal);
            snapshot.MixedStateAcrossSides = both &&
                !string.Equals(snapshot.Colony.UniformStateSignature,
                    snapshot.Trader.UniformStateSignature, StringComparison.Ordinal);
            if (!snapshot.HasSchematic)
                snapshot.Decision = "NOT_SCHEMATIC";
            else if (snapshot.Colony.LimitExceeded || snapshot.Trader.LimitExceeded)
                snapshot.Decision = "LIMIT_EXCEEDED_FAIL_CLOSED";
            else if (snapshot.Colony.InvalidResearchBooks > 0 ||
                snapshot.Trader.InvalidResearchBooks > 0)
                snapshot.Decision = "INVALID_RESEARCH_FAIL_CLOSED";
            else if (snapshot.Colony.ProjectSignatures > 1 ||
                snapshot.Trader.ProjectSignatures > 1 ||
                snapshot.MixedProjectsAcrossSides)
                snapshot.Decision = "MIXED_PROJECTS_FAIL_CLOSED";
            else if (snapshot.Colony.StateSignatures > 1 ||
                snapshot.Trader.StateSignatures > 1 || snapshot.MixedStateAcrossSides)
                snapshot.Decision = "MIXED_STATES_FAIL_CLOSED";
            else if (snapshot.Colony.OtherBooks + snapshot.Trader.OtherBooks +
                snapshot.Colony.NonBooks + snapshot.Trader.NonBooks +
                snapshot.Colony.NullThings + snapshot.Trader.NullThings > 0)
                snapshot.Decision = "MIXED_TYPES_FAIL_CLOSED";
            else
                snapshot.Decision = "UNIFORM_CANDIDATE_DIRECTION_UNVERIFIED";
        }
    }
}
