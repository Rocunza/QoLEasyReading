using Verse;
namespace ZCN.QoL.EasyReading
{
    internal static class EasyReadingBuildIdentity
    {
        internal const string BuildId = "EASY-READING-RW17-PORT1-20261027";
        internal static void LogStartup()
        {
            Log.Message("[QoL: Easy Reading] BUILD_ID=" + BuildId + "; assembly=QoLEasyReading;");
        }
    }
}
