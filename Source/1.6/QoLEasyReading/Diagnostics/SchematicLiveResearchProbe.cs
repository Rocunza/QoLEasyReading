using System;
using System.Text;
using LudeonTK;
using RimWorld;
using Verse;

namespace ZCN.QoL.EasyReading
{
    /// <summary>
    /// Manual read-only diagnostic for real books on a loaded map.
    /// Dev actions are registered by RimWorld; no Harmony patch or settings toggle.
    /// Nothing is run during normal map drawing / reading / trading.
    /// </summary>
    public static class SchematicLiveResearchProbe
    {
        private const string LogTag = "[QoL: Easy Reading][SCHEMATIC-LIVE][S2B1]";
        private const int MaxProjectsToPrint = 24;
        private const int MaxBooksPerCell = 24;
        private static int runNumber;

        [DebugAction("Easy Reading", "Audit selected schematic (read-only)",
            actionType = DebugActionType.Action,
            allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void AuditSelectedSchematic()
        {
            int run = ++runNumber;
            try
            {
                Thing selected = Find.Selector == null ? null : Find.Selector.SingleSelectedThing;
                Book book = selected as Book;
                if (book == null)
                {
                    Log.Message(LogTag + " run=" + run + "; source=SELECTION; status=SKIP; reason=" +
                        (selected == null ? "NoSelectedThing" : "SelectedThingIsNotBook") +
                        "; hint=Select a schematic on the map, or use the map-cell tool; " +
                        "BUILD_ID=" + EasyReadingBuildIdentity.BuildId + "; readOnly=True;");
                    return;
                }
                AuditBook(book, "SELECTION", run);
            }
            catch (Exception ex)
            {
                Log.Warning(LogTag + " run=" + run + "; source=SELECTION; status=UNKNOWN; reason=ToolException:" +
                    ex.GetType().Name + "; readOnly=True;");
            }
        }

        // A book stored on a vanilla bookcase / shelf may be under the furniture
        // in the map's thing grid, even when the book itself cannot be selected.
        // Iterate the clicked cell rather than changing a book's position.
        [DebugAction("Easy Reading", "Audit schematic(s) at map cell (read-only)",
            actionType = DebugActionType.ToolMap,
            allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void AuditSchematicAtMouseCell()
        {
            int run = ++runNumber;
            try
            {
                Map map = Find.CurrentMap;
                if (map == null)
                {
                    Log.Message(LogTag + " run=" + run + "; source=MAP_CELL; status=SKIP; " +
                        "reason=NoMap; readOnly=True;");
                    return;
                }
                IntVec3 cell = UI.MouseCell();
                if (!cell.InBounds(map))
                {
                    Log.Message(LogTag + " run=" + run + "; source=MAP_CELL; status=SKIP; " +
                        "reason=OutsideMap; readOnly=True;");
                    return;
                }

                int found = 0;
                int audited = 0;
                // ThingsAt returns the normal map-cell contents, including
                // spawned books under storage buildings. The tool never ejects
                // books or searches a nested mod-specific ThingOwner.
                foreach (Thing thing in map.thingGrid.ThingsAt(cell))
                {
                    Book book = thing as Book;
                    if (book == null) continue;
                    found++;
                    if (audited >= MaxBooksPerCell) continue;
                    audited++;
                    AuditBook(book, "MAP_CELL", run);
                }
                if (audited == 0)
                    Log.Message(LogTag + " run=" + run + "; source=MAP_CELL; cell=" + cell +
                        "; status=SKIP; reason=NoSpawnedBooksAtCell; " +
                        "hint=Books stored in non-spawned containers are not covered; readOnly=True;");
                else
                    Log.Message(LogTag + " run=" + run + "; source=MAP_CELL; cell=" + cell +
                        "; status=END; booksFound=" + found + "; booksAudited=" + audited +
                        "; truncated=" + (found > audited) + "; readOnly=True;");
            }
            catch (Exception ex)
            {
                Log.Warning(LogTag + " run=" + run + "; source=MAP_CELL; status=UNKNOWN; reason=ToolException:" +
                    ex.GetType().Name + "; readOnly=True;");
            }
        }

        private static void AuditBook(Book book, string source, int run)
        {
            // Audit errors must not interrupt vanilla book interactions.
            if (book == null) return;
            try
            {
                SchematicResearchState state = SchematicResearchResolver.Resolve(book);
                SchematicResearchClassification result = SchematicResearchClassifier.Classify(state);
                string status = result == SchematicResearchClassification.NotApplicable ? "SKIP" :
                    result == SchematicResearchClassification.Unknown ? "UNKNOWN" : "OK";
                string defName = book.def == null ? "<null>" : book.def.defName;
                string label = book.LabelNoCount ?? "<unnamed>";

                Log.Message(LogTag + " run=" + run + "; source=" + source +
                    "; status=" + status + "; BUILD_ID=" + EasyReadingBuildIdentity.BuildId +
                    "; bookDef=" + Safe(defName) + "; bookId=" + Safe(book.ThingID) +
                    "; bookLabel=" + Safe(label) +
                    "; validity=" + state.Validity + "; classification=" + result +
                    "; projects=" + state.ProjectCount +
                    "; finished=" + state.FinishedCount + "; remaining=" + state.RemainingCount +
                    "; grantable=" + state.GrantableCount + "; unavailable=" + state.UnavailableCount +
                    "; partiallyFinished=" + state.PartiallyFinished +
                    "; reason=" + Safe(state.UnknownReason) + "; readOnly=True;");

                // Only report real values returned by the live resolver, and
                // cap verbose output to prevent log flooding on modded books.
                int printed = 0;
                foreach (SchematicResearchProject entry in state.Projects)
                {
                    if (printed >= MaxProjectsToPrint) break;
                    ResearchProjectDef project = entry.ProjectDef;
                    Log.Message(LogTag + " run=" + run + "; projectIndex=" + (printed + 1) +
                        "; researchDef=" + Safe(project.defName) +
                        "; researchLabel=" + Safe(project.label) +
                        "; finished=" + entry.IsFinished +
                        "; visibleForReward=" + entry.IsVisibleForReward +
                        "; positiveReward=" + entry.HasPositiveRewardValue +
                        "; canGrantResearch=" + entry.CanGrantResearch + "; readOnly=True;");
                    printed++;
                }
                if (state.ProjectCount > printed)
                    Log.Message(LogTag + " run=" + run + "; status=TRUNCATED; " +
                        "projectLinesShown=" + printed + "; projectsTotal=" + state.ProjectCount + ";");
            }
            catch (Exception ex)
            {
                Log.Warning(LogTag + " run=" + run + "; source=" + source +
                    "; status=UNKNOWN; reason=BookAuditException:" + ex.GetType().Name +
                    "; readOnly=True;");
            }
        }

        // Keep one invocation's output on one physical log line, even for
        // unusual labels provided by mods or translations.
        private static string Safe(string s)
        {
            if (string.IsNullOrEmpty(s)) return "-";
            StringBuilder b = new StringBuilder(Math.Min(100, s.Length));
            int count = Math.Min(s.Length, 96);
            for (int i = 0; i < count; i++)
            {
                char c = s[i];
                b.Append(c == '\r' || c == '\n' || c == '\t' || c == ';' ? ' ' : c);
            }
            if (s.Length > count) b.Append("...");
            return b.ToString();
        }
    }
}
