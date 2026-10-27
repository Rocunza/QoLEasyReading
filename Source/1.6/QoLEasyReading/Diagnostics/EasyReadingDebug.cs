namespace ZCN.QoL.EasyReading
{
    internal static class EasyReadingDebug
    {
        internal static bool Enabled => EasyReadingMod.Settings != null &&
            EasyReadingMod.Settings.enableDebugLogging;
    }
}
