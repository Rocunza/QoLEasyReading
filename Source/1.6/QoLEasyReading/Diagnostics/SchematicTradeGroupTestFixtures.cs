#if DEBUG
using GroupSnapshot = ZCN.QoL.EasyReading.SchematicTradeGroupResolver.GroupSnapshot;
using SideSummary = ZCN.QoL.EasyReading.SchematicTradeGroupResolver.SideSummary;

namespace ZCN.QoL.EasyReading
{
    /// <summary>
    /// Synthetic summaries only: NO Book, Tradeable, ResearchProjectDef, Thing,
    /// TradeDeal, save-game writes or Harmony registration. These fixtures
    /// exercise the real production DecisionPolicy / IconResolver contracts,
    /// not physical Thing enumeration or BuildSignatures generation.
    /// </summary>
    internal static class SchematicTradeGroupTestFixtures
    {
        internal static SideSummary EmptySide()
        {
            return new SideSummary { Status = "EMPTY" };
        }

        internal static SideSummary ValidSide(int bookEntries = 1)
        {
            return new SideSummary
            {
                ThingEntries = bookEntries,
                ExaminedBooks = bookEntries,
                SchematicBooks = bookEntries,
                ProjectSignatures = 1,
                StateSignatures = 1,
                UniformProjectSignature = "1:A",
                UniformStateSignature = "HasGrantableResearch|1:A:0111",
                UniformClassification = SchematicResearchClassification.HasGrantableResearch,
                Projects = 1,
                Finished = 0,
                Grantable = 1,
                Status = "UNIFORM"
            };
        }

        internal static GroupSnapshot TraderSingle(int transferCount = 0)
        {
            return new GroupSnapshot
            {
                CountToTransfer = transferCount,
                Colony = EmptySide(),
                Trader = ValidSide()
            };
        }

        internal static GroupSnapshot ColonySingle(int transferCount = 0)
        {
            return new GroupSnapshot
            {
                CountToTransfer = transferCount,
                Colony = ValidSide(),
                Trader = EmptySide()
            };
        }

        internal static GroupSnapshot BothSame(int transferCount = 0)
        {
            return new GroupSnapshot
            {
                CountToTransfer = transferCount,
                Colony = ValidSide(),
                Trader = ValidSide()
            };
        }

        internal static GroupSnapshot BeyondLimitWithoutDetectedSchematic()
        {
            return new GroupSnapshot
            {
                Colony = EmptySide(),
                Trader = new SideSummary
                {
                    ThingEntries = 257,
                    ExaminedBooks = 256,
                    NullThings = 256,
                    LimitExceeded = true,
                    Status = "TRUNCATED"
                }
            };
        }
    }
}
#endif
