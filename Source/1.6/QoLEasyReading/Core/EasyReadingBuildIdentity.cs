using Verse;
namespace ZCN.QoL.EasyReading
{
    internal static class EasyReadingBuildIdentity
    {
        internal const string ModVersion = "1.1.0";
        internal const string BuildId = "ER-RW16-S3QC-HF1-20261117-R1";
        internal static void LogStartup()
        {
#if DEBUG
            const string configuration = "Debug";
            const int expectedActions = 5;
#else
            const string configuration = "Release";
            const int expectedActions = 0;
#endif
            Log.Message("[QoL: Easy Reading] BUILD_ID=" + BuildId +
                "; MOD_VERSION=" + ModVersion + "; assembly=QoLEasyReading; configuration=" + configuration +
                "; phase=S3RELDIAG1-StrictDebugRelease; expectedDeveloperActions=" +
                expectedActions + "; diagnosticUI=" + (expectedActions == 0 ? "EXCLUDED" : "DEBUG_ONLY") +
                "; verboseLogging=" + (expectedActions == 0 ? "COMPILED_OFF" : "OPTION_GATED") +
                "; pureStartupAudits=" + (expectedActions == 0 ? "EXCLUDED" : "DEBUG_ONLY") +
                "; runtimeHealth=SHARED; settingsSchema=UNCHANGED; tabDrawer=UNCHANGED" +
                "; tooltipTitleSanitizer=QC_TAG_STRIP_V1; displayMarkup=KNOWN_TAGS_ONLY" +
                "; ownershipAndTooltip=PRODUCTION_PRESERVED; tradeWrites=0; researchWrites=0;");
        }
    }
}
