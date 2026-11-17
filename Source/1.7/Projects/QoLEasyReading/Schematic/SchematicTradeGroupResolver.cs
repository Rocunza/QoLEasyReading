using RimWorld;

namespace ZCN.QoL.EasyReading
{
    /// <summary>
    /// Stable, read-only production contract for schematic groups in trade rows.
    /// Data types and the Inspect(Tradeable,int) signature stay unchanged.
    /// Inspection logic lives in SchematicTradeGroupInspector; Debug observers
    /// live exclusively in SchematicTradeGroupDiagnostics.
    /// No Harmony patch, trading/research write, or UI drawing is performed here.
    /// </summary>
    internal static class SchematicTradeGroupResolver
    {
        internal sealed class SideSummary
        {
            internal int ThingEntries;
            internal int SchematicBooks;
            internal int OtherBooks;
            internal int NonBooks;
            internal int NullThings;
            internal int MinifiedBooks;
            internal int InvalidResearchBooks;
            internal int ExaminedBooks;
            internal int ProjectSignatures;
            internal int StateSignatures;
            internal bool LimitExceeded;
            internal string UniformProjectSignature;
            internal string UniformStateSignature;
            internal SchematicResearchClassification UniformClassification;
            internal int Finished;
            internal int Projects;
            internal int Grantable;
            internal string Status;
        }

        internal sealed class GroupSnapshot
        {
            internal int CountToTransfer;
            internal SideSummary Colony;
            internal SideSummary Trader;
            internal bool HasSchematic;
            internal bool MixedProjectsAcrossSides;
            internal bool MixedStateAcrossSides;
            internal string InventorySides;
            internal string DirectionHypothesis;
            internal string Decision;
            internal string Error;
        }

        /// <summary>Independent, read-only inspection. No caching of books or research state.</summary>
        internal static GroupSnapshot Inspect(Tradeable tradeable, int selectedCount)
        {
            return SchematicTradeGroupInspector.Inspect(tradeable, selectedCount);
        }
    }
}
