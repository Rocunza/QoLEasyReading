using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Verse;

namespace ZCN.QoL.EasyReading
{
    // Research status only. No book/ResearchManager mutations, UI, or ownership lookup.
    internal enum SchematicResearchValidity
    {
        Valid,
        NoResearchDoer,
        NoProjects,
        Unknown
    }

    internal enum SchematicResearchClassification
    {
        NotApplicable,
        Complete,
        HasGrantableResearch,
        UnavailableNow,
        Unknown
    }

    internal sealed class SchematicResearchProject
    {
        internal ResearchProjectDef ProjectDef { get; private set; }
        internal bool IsFinished { get; private set; }
        internal bool IsVisibleForReward { get; private set; }
        internal bool HasPositiveRewardValue { get; private set; }
        private bool hasGrantablePath;
        internal bool CanGrantResearch { get { return !IsFinished && hasGrantablePath; } }

        internal SchematicResearchProject(ResearchProjectDef project, bool finished,
            bool visible, bool positiveValue, bool hasGrantablePath)
        {
            ProjectDef = project;
            IsFinished = finished;
            IsVisibleForReward = visible;
            HasPositiveRewardValue = positiveValue;
            this.hasGrantablePath = hasGrantablePath;
        }
    }

    internal sealed class SchematicResearchState
    {
        private static readonly ReadOnlyCollection<SchematicResearchProject> EmptyProjects =
            Array.AsReadOnly(new SchematicResearchProject[0]);

        internal SchematicResearchValidity Validity { get; private set; }
        internal string UnknownReason { get; private set; }
        internal ReadOnlyCollection<SchematicResearchProject> Projects { get; private set; }
        internal int ProjectCount { get { return Projects.Count; } }
        internal int FinishedCount { get; private set; }
        internal int RemainingCount { get { return ProjectCount - FinishedCount; } }
        internal int GrantableCount { get; private set; }
        internal int UnavailableCount { get; private set; }
        internal bool PartiallyFinished { get { return Validity == SchematicResearchValidity.Valid &&
            FinishedCount > 0 && FinishedCount < ProjectCount; } }

        private SchematicResearchState() { }

        internal static SchematicResearchState Invalid(SchematicResearchValidity validity, string reason)
        {
            return new SchematicResearchState
            {
                Validity = validity,
                UnknownReason = reason ?? string.Empty,
                Projects = EmptyProjects
            };
        }

        internal static SchematicResearchState FromProjects(List<SchematicResearchProject> items)
        {
            if (items == null || items.Count == 0)
                return Invalid(SchematicResearchValidity.NoProjects, "EmptyProjectSet");

            // Copy before exposing to prevent accidental changes after aggregation.
            SchematicResearchProject[] snapshot = items.ToArray();
            int finished = 0;
            int grantable = 0;
            int unavailable = 0;
            for (int i = 0; i < snapshot.Length; i++)
            {
                SchematicResearchProject entry = snapshot[i];
                if (entry == null || entry.ProjectDef == null)
                    return Invalid(SchematicResearchValidity.Unknown, "NullResearchEntry");
                if (entry.IsFinished) finished++;
                else if (entry.CanGrantResearch) grantable++;
                else unavailable++;
            }
            return new SchematicResearchState
            {
                Validity = SchematicResearchValidity.Valid,
                UnknownReason = string.Empty,
                Projects = Array.AsReadOnly(snapshot),
                FinishedCount = finished,
                GrantableCount = grantable,
                UnavailableCount = unavailable
            };
        }
    }
}
