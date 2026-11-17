namespace ZCN.QoL.EasyReading
{
    /// <summary>
    /// UI-neutral classification: does not pick textures or inspect ownership.
    /// The shop's future ! can use state.PartiallyFinished independently.
    /// </summary>
    internal static class SchematicResearchClassifier
    {
        internal static SchematicResearchClassification Classify(SchematicResearchState state)
        {
            if (state == null) return SchematicResearchClassification.Unknown;
            if (state.Validity == SchematicResearchValidity.NoResearchDoer)
                return SchematicResearchClassification.NotApplicable;
            if (state.Validity != SchematicResearchValidity.Valid)
                return SchematicResearchClassification.Unknown;
            return ClassifyCounts(state.ProjectCount, state.FinishedCount,
                state.GrantableCount, state.UnavailableCount);
        }

        // Pure count classifier permits safe startup self-tests without creating
        // fake ResearchProjectDef or reading a user's save or game state.
        internal static SchematicResearchClassification ClassifyCounts(
            int total, int finished, int grantable, int unavailable)
        {
            if (total <= 0 || finished < 0 || finished > total ||
                grantable < 0 || unavailable < 0 ||
                grantable + unavailable != total - finished)
                return SchematicResearchClassification.Unknown;
            if (finished == total)
                return SchematicResearchClassification.Complete;
            if (grantable > 0)
                return SchematicResearchClassification.HasGrantableResearch;
            return SchematicResearchClassification.UnavailableNow;
        }
    }
}
