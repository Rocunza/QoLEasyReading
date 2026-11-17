using System;

namespace ZCN.QoL.EasyReading
{
    /// <summary>
    /// S3-B2: research-only trade-row icon decision (partial completion uses ?). A successful '?' is NOT
    /// evidence that the player does not own an equivalent schematic.
    /// The three-state purchase recommendation is intentionally not implemented.
    /// No Unity drawing, ownership scans, trade-count writes or research writes.
    /// </summary>
    internal static class SchematicTradeIconResolver
    {
        internal struct Decision
        {
            internal SchematicIconKind Kind;
            internal int Finished;
            internal int Total;
            internal int Grantable;
        }

        internal static bool TryResolve(SchematicTradeGroupResolver.GroupSnapshot snapshot,
            out Decision result, out string suppressionReason)
        {
            result = default(Decision);
            suppressionReason = "UNKNOWN";
            if (snapshot == null)
            {
                suppressionReason = "NO_SNAPSHOT";
                return false;
            }

            // S3-B0 inspects BOTH trading sides, grouping by full research
            // project and state signatures. Never use CountToTransfer sign as
            // a proxy for which Schematic's research state should be shown.
            if (!string.Equals(snapshot.Decision,
                "UNIFORM_CANDIDATE_DIRECTION_UNVERIFIED", StringComparison.Ordinal))
            {
                suppressionReason = snapshot.Decision ?? "NO_GROUP_DECISION";
                return false;
            }
            if (!snapshot.HasSchematic || snapshot.Colony == null ||
                snapshot.Trader == null)
            {
                suppressionReason = "MISSING_SCHEMATIC_GROUP";
                return false;
            }

            SchematicTradeGroupResolver.SideSummary colony = snapshot.Colony;
            SchematicTradeGroupResolver.SideSummary trader = snapshot.Trader;
            SchematicTradeGroupResolver.SideSummary side = colony.SchematicBooks > 0
                ? colony : trader;
            if (side.SchematicBooks == 0 || side.ProjectSignatures != 1 ||
                side.StateSignatures != 1 || side.Projects <= 0)
            {
                suppressionReason = "NON_UNIFORM_RESEARCH";
                return false;
            }

            // Defense-in-depth: full signatures are checked by S3-B0.
            // Also require equal counts when both sides contain Schematics.
            if (colony.SchematicBooks > 0 && trader.SchematicBooks > 0 &&
                (colony.Projects != trader.Projects ||
                 colony.Finished != trader.Finished ||
                 colony.Grantable != trader.Grantable ||
                 colony.UniformClassification != trader.UniformClassification))
            {
                suppressionReason = "CROSS_SIDE_COUNTS_MISMATCH";
                return false;
            }

            int unavailable = side.Projects - side.Finished - side.Grantable;
            if (SchematicResearchClassifier.ClassifyCounts(side.Projects,
                    side.Finished, side.Grantable, unavailable) !=
                side.UniformClassification)
            {
                suppressionReason = "STATE_COUNTS_CHANGED";
                return false;
            }

            SchematicIconKind kind;
            if (!TryClassifyCounts(side.Projects, side.Finished, side.Grantable,
                unavailable, out kind))
            {
                suppressionReason = "NOT_RESEARCH_AVAILABLE";
                return false;
            }

            result = new Decision
            {
                Kind = kind,
                Finished = side.Finished,
                Total = side.Projects,
                Grantable = side.Grantable
            };
            suppressionReason = null;
            return true;
        }

        /// <summary>Pure count decision, for offline boundary tests too.</summary>
        internal static bool TryClassifyCounts(int total, int finished, int grantable,
            int unavailable, out SchematicIconKind kind)
        {
            kind = SchematicIconKind.ResearchAvailable;
            SchematicResearchClassification classification =
                SchematicResearchClassifier.ClassifyCounts(total, finished,
                    grantable, unavailable);
            if (classification == SchematicResearchClassification.Complete)
            {
                kind = SchematicIconKind.AllFinished;
                return true;
            }
            // A partially finished book still grants research.  The shared
            // research classifier already guarantees grantable > 0 here.
            // Do not make an independent "finished == 0" trade-only policy.
            if (classification == SchematicResearchClassification.HasGrantableResearch)
            {
                kind = SchematicIconKind.ResearchAvailable;
                return true;
            }
            // ReviewRequired is reserved for a separately verified future phase.
            return false;
        }
    }
}
