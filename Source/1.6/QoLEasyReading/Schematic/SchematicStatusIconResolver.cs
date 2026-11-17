using System;
using RimWorld;
using Verse;

namespace ZCN.QoL.EasyReading
{
    /// <summary>
    /// S3-A2: UI-neutral, read-only mapping from audited
    /// Schematic research classification to one presentation icon.
    /// Never scans owned books, writes research, or draws Unity UI.
    /// </summary>
    internal static class SchematicStatusIconResolver
    {
        internal static bool IsSchematic(Book book)
        {
            return book != null && book.def != null &&
                string.Equals(book.def.defName, "Schematic", StringComparison.Ordinal);
        }

        internal static bool TryResolve(Book book, out SchematicIconKind kind,
            out int finishedCount, out int projectCount,
            out SchematicResearchClassification classification)
        {
            kind = SchematicIconKind.ResearchAvailable;
            finishedCount = 0;
            projectCount = 0;
            classification = SchematicResearchClassification.NotApplicable;
            if (!IsSchematic(book)) return false;

            try
            {
                SchematicResearchState state = SchematicResearchResolver.Resolve(book);
                classification = SchematicResearchClassifier.Classify(state);
                if (state != null && state.Validity == SchematicResearchValidity.Valid)
                {
                    finishedCount = state.FinishedCount;
                    projectCount = state.ProjectCount;
                }
                switch (classification)
                {
                    case SchematicResearchClassification.Complete:
                        kind = SchematicIconKind.AllFinished;
                        return true;
                    case SchematicResearchClassification.HasGrantableResearch:
                        kind = SchematicIconKind.ResearchAvailable;
                        return true;
                    default:
                        // Unknown, NotApplicable and UnavailableNow are not a grey '?'.
                        return false;
                }
            }
            catch (Exception)
            {
                classification = SchematicResearchClassification.Unknown;
                return false;
            }
        }
    }
}
