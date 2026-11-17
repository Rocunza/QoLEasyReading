using System;
using System.Collections;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace ZCN.QoL.EasyReading
{
    /// <summary>
    /// Reads the actual projects stored in each research outcome doer of a book.
    /// No heuristics from labels/descriptions and no mutable long-lived cache.
    /// </summary>
    internal static class SchematicResearchResolver
    {
        private sealed class AccumulatedProject
        {
            internal ResearchProjectDef Project;
            internal bool Finished;
            internal bool Visible;
            internal bool Positive;
            internal bool Grantable;
        }

        internal static SchematicResearchState Resolve(Book book)
        {
            if (book == null)
                return SchematicResearchState.Invalid(SchematicResearchValidity.Unknown, "BookNull");
            try
            {
                CompBook comp = book.BookComp;
                if (comp == null || comp.Doers == null)
                    return SchematicResearchState.Invalid(SchematicResearchValidity.Unknown, "BookDoersMissing");

                bool foundResearchDoer = false;
                Dictionary<string, AccumulatedProject> byId =
                    new Dictionary<string, AccumulatedProject>(StringComparer.Ordinal);
                List<string> order = new List<string>();

                foreach (BookOutcomeDoer doer in comp.Doers)
                {
                    if (doer == null)
                        return Unknown("NullBookOutcomeDoer");
                    ReadingOutcomeDoerGainResearch researchDoer = doer as ReadingOutcomeDoerGainResearch;
                    if (researchDoer == null) continue; // excludes textbooks and anomaly-only tomes
                    foundResearchDoer = true;

                    IDictionary values;
                    string reason;
                    if (!SchematicResearchAccess.TryGetValues(researchDoer, out values, out reason))
                        return Unknown(reason);

                    foreach (DictionaryEntry value in values)
                    {
                        ResearchProjectDef project = value.Key as ResearchProjectDef;
                        if (project == null || string.IsNullOrEmpty(project.defName))
                            return Unknown("NullOrUnnamedProject");
                        if (!(value.Value is float))
                            return Unknown("RewardValueTypeChanged");
                        float reward = (float)value.Value;
                        if (float.IsNaN(reward) || float.IsInfinity(reward))
                            return Unknown("InvalidRewardValue");

                        bool finished = project.IsFinished;
                        bool visible;
                        if (!SchematicResearchAccess.TryIsProjectVisible(researchDoer,
                            project, out visible, out reason))
                            return Unknown(reason);

                        AccumulatedProject item;
                        if (!byId.TryGetValue(project.defName, out item))
                        {
                            item = new AccumulatedProject { Project = project, Finished = finished };
                            byId.Add(project.defName, item);
                            order.Add(project.defName);
                        }
                        else if (item.Finished != finished)
                        {
                            // A mod changed completion while we were inspecting this book.
                            // Never turn an inconsistent partial snapshot into a green check.
                            return Unknown("ResearchChangedDuringRead");
                        }

                        item.Visible |= visible;
                        item.Positive |= reward > 0f;
                        item.Grantable |= visible && reward > 0f;
                    }
                }

                if (!foundResearchDoer)
                    return SchematicResearchState.Invalid(SchematicResearchValidity.NoResearchDoer,
                        "NoGainResearchDoer");
                if (order.Count == 0)
                    return SchematicResearchState.Invalid(SchematicResearchValidity.NoProjects,
                        "ResearchValuesEmpty");

                List<SchematicResearchProject> projects = new List<SchematicResearchProject>(order.Count);
                for (int i = 0; i < order.Count; i++)
                {
                    AccumulatedProject item = byId[order[i]];
                    // For duplicate projects across multiple doers, visible and
                    // positive must belong to the SAME path. Do not join separate
                    // doers' flags into a false grantability decision.
                    projects.Add(new SchematicResearchProject(item.Project, item.Finished,
                        item.Visible, item.Positive, item.Grantable));
                }
                return SchematicResearchState.FromProjects(projects);
            }
            catch (Exception ex)
            {
                // Read-only error isolation: no exception should escape into a
                // future bookcase / float-menu / trade-window UI patch.
                return Unknown("Resolver:" + ex.GetType().Name);
            }
        }

        private static SchematicResearchState Unknown(string reason)
        {
            return SchematicResearchState.Invalid(SchematicResearchValidity.Unknown, reason);
        }
    }
}
