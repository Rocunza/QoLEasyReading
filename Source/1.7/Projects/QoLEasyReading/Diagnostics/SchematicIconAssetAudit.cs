using System;
using LudeonTK;
using UnityEngine;
using Verse;

namespace ZCN.QoL.EasyReading
{
    /// <summary>
    /// Explicit developer action: no runtime scanning, file writes, research
    /// changes, Harmony patch or rendering hook. Runs on the game's UI thread.
    /// </summary>
    public static class SchematicIconAssetAudit
    {
        private const string LogTag = "[QoL: Easy Reading][SCHEMATIC-ASSETS][S2B2]";
        private static int runNumber;

        [DebugAction("Easy Reading", "Audit schematic icon assets (read-only)",
            actionType = DebugActionType.Action,
            allowedGameStates = AllowedGameStates.PlayingOnMap)]
        public static void AuditAssets()
        {
            int run = ++runNumber;
            int found = 0;
            int failed = 0;
            SchematicIconKind[] kinds =
            {
                SchematicIconKind.ResearchAvailable,
                SchematicIconKind.ReviewRequired,
                SchematicIconKind.AllFinished,
                SchematicIconKind.DuplicateOwned
            };

            for (int i = 0; i < kinds.Length; i++)
            {
                SchematicIconKind kind = kinds[i];
                string path = SchematicStatusIconCatalog.GetPath(kind);
                try
                {
                    Texture2D texture = SchematicStatusIconCatalog.GetTexture(kind);
                    bool valid = texture != null;
                    if (valid) found++; else failed++;
                    Log.Message(LogTag + " run=" + run + "; icon=" + kind +
                        "; path=" + path + "; status=" + (valid ? "FOUND" : "MISSING_OR_RETRY_PENDING") +
                        (valid ? "; dimensions=" + texture.width + "x" + texture.height : "") +
                        "; readOnly=True;");
                }
                catch (Exception ex)
                {
                    failed++;
                    Log.Warning(LogTag + " run=" + run + "; icon=" + kind +
                        "; path=" + path + "; status=FAILED; reason=" + ex.GetType().Name +
                        "; readOnly=True;");
                }
            }

            Log.Message(LogTag + " run=" + run + "; ResourceFound=" + found + "/4" +
                "; failed=" + failed + "; status=" + (found == 4 ? "PASS" : "INCOMPLETE") +
                "; liveRendering=NOT_TESTED; settingsWrites=0; harmonyPatchAdded=0" +
                "; BUILD_ID=" + EasyReadingBuildIdentity.BuildId + "; readOnly=True;");
        }
    }
}
