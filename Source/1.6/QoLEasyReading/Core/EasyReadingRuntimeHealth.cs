using System;
using Verse;

namespace ZCN.QoL.EasyReading
{
    /// <summary>
    /// S3-REL-DIAG1: production API availability check, not a regression audit.
    /// Runs once at startup in BOTH configurations; writes no game state.
    /// </summary>
    internal static class EasyReadingRuntimeHealth
    {
        internal static void Initialize()
        {
            try
            {
                // Reflection is initialized once and the live resolver keeps its
                // existing fail-closed behavior if an internal API changed.
                if (!SchematicResearchAccess.IsReady)
                {
                    Log.Warning("[QoL: Easy Reading][Schematic][HEALTH] Research status " +
                        "unavailable: " + SchematicResearchAccess.FailureReason +
                        "; fallback=VANILLA; build=" + EasyReadingBuildIdentity.BuildId + ";");
                }
            }
            catch (Exception ex)
            {
                // Health monitoring must not abort Harmony or mod startup.
                Log.Warning("[QoL: Easy Reading][Schematic][HEALTH] API initialization " +
                    "failed: " + ex.GetType().Name +
                    "; existing fail-closed guards retained; build=" +
                    EasyReadingBuildIdentity.BuildId + ";");
            }
        }
    }
}
