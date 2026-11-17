using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using GroupSnapshot = ZCN.QoL.EasyReading.SchematicTradeGroupResolver.GroupSnapshot;
using SideSummary = ZCN.QoL.EasyReading.SchematicTradeGroupResolver.SideSummary;

namespace ZCN.QoL.EasyReading
{
    /// <summary>
    /// Stateless production inspector of both trade inventory sides.
    /// No diagnostic cache, new Harmony hook, or additional ownership scan.
    /// Limits, signatures and fail-closed decision strings are preserved from S3-R1.
    /// </summary>
    internal static class SchematicTradeGroupInspector
    {
        // A single budget is shared by colony and trader items in each row.
        private const int MaxBooksInspectedPerRow = 256;

        /// <summary>Independent, read-only inspection. No caching of books or research state.</summary>
        internal static GroupSnapshot Inspect(Tradeable tradeable, int selectedCount)
        {
            GroupSnapshot snapshot = new GroupSnapshot();
            snapshot.CountToTransfer = selectedCount;
            try
            {
                int remaining = MaxBooksInspectedPerRow;
                snapshot.Colony = InspectSide(tradeable == null ? null : tradeable.thingsColony,
                    ref remaining);
                snapshot.Trader = InspectSide(tradeable == null ? null : tradeable.thingsTrader,
                    ref remaining);
                SchematicTradeGroupDecisionPolicy.Complete(snapshot);
            }
            catch (Exception ex)
            {
                snapshot.HasSchematic = true; // report a failed attempt, never paint
                snapshot.Decision = "INSPECTION_FAILED";
                snapshot.Error = ex.GetType().Name;
                if (snapshot.Colony == null) snapshot.Colony = new SideSummary { Status = "ERROR" };
                if (snapshot.Trader == null) snapshot.Trader = new SideSummary { Status = "ERROR" };
            }
            return snapshot;
        }

        // Shared by production InspectSide and Debug-only boundary regression tests.
        // One entry (including a null Thing) consumes exactly one of 256 slots.
        internal static bool TryConsumeBudget(ref int remaining)
        {
            if (remaining <= 0) return false;
            remaining--;
            return true;
        }

        internal static int NewInspectionBudget()
        {
            return MaxBooksInspectedPerRow;
        }

        private static SideSummary InspectSide(List<Thing> things, ref int remaining)
        {
            SideSummary result = new SideSummary();
            HashSet<string> projects = new HashSet<string>(StringComparer.Ordinal);
            HashSet<string> states = new HashSet<string>(StringComparer.Ordinal);
            if (things == null) { result.Status = "EMPTY"; return result; }
            result.ThingEntries = things.Count;
            for (int i = 0; i < things.Count; i++)
            {
                if (!TryConsumeBudget(ref remaining)) { result.LimitExceeded = true; break; }
                result.ExaminedBooks++;
                Thing thing = things[i];
                if (thing == null) { result.NullThings++; continue; }
                Thing inner = thing.GetInnerIfMinified();
                Book book = inner as Book;
                if (book == null) { result.NonBooks++; continue; }
                if (!object.ReferenceEquals(inner, thing)) result.MinifiedBooks++;
                if (!SchematicStatusIconResolver.IsSchematic(book))
                {
                    result.OtherBooks++;
                    continue;
                }
                result.SchematicBooks++;
                SchematicResearchState state = SchematicResearchResolver.Resolve(book);
                if (state == null || state.Validity != SchematicResearchValidity.Valid)
                {
                    result.InvalidResearchBooks++;
                    continue;
                }
                SchematicResearchClassification classification =
                    SchematicResearchClassifier.Classify(state);
                if (classification == SchematicResearchClassification.Unknown ||
                    classification == SchematicResearchClassification.NotApplicable)
                {
                    result.InvalidResearchBooks++;
                    continue;
                }
                string projectSignature, stateSignature;
                BuildSignatures(state, classification, out projectSignature, out stateSignature);
                projects.Add(projectSignature);
                states.Add(stateSignature);
                if (result.SchematicBooks == 1)
                {
                    result.UniformProjectSignature = projectSignature;
                    result.UniformStateSignature = stateSignature;
                    result.UniformClassification = classification;
                    result.Finished = state.FinishedCount;
                    result.Projects = state.ProjectCount;
                    result.Grantable = state.GrantableCount;
                }
            }
            result.ProjectSignatures = projects.Count;
            result.StateSignatures = states.Count;
            result.Status = result.LimitExceeded ? "TRUNCATED" :
                result.InvalidResearchBooks > 0 ? "INVALID" :
                result.ProjectSignatures > 1 ? "MIXED_PROJECTS" :
                result.StateSignatures > 1 ? "MIXED_STATES" :
                result.OtherBooks + result.NonBooks + result.NullThings > 0 ? "MIXED_TYPES" :
                result.SchematicBooks > 0 ? "UNIFORM" : "NO_SCHEMATIC";
            return result;
        }

        private static void BuildSignatures(SchematicResearchState state,
            SchematicResearchClassification classification,
            out string projects, out string status)
        {
            List<string> projectKeys = new List<string>(state.ProjectCount);
            List<string> stateKeys = new List<string>(state.ProjectCount);
            for (int i = 0; i < state.Projects.Count; i++)
            {
                SchematicResearchProject entry = state.Projects[i];
                string name = entry.ProjectDef.defName;
                string key = name.Length + ":" + name;
                projectKeys.Add(key);
                stateKeys.Add(key + ":" + (entry.IsFinished ? "1" : "0") +
                    (entry.IsVisibleForReward ? "1" : "0") +
                    (entry.HasPositiveRewardValue ? "1" : "0") +
                    (entry.CanGrantResearch ? "1" : "0"));
            }
            projectKeys.Sort(StringComparer.Ordinal);
            stateKeys.Sort(StringComparer.Ordinal);
            projects = string.Join("|", projectKeys.ToArray());
            status = classification + "|" + string.Join("|", stateKeys.ToArray());
        }

    }
}
