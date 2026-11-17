namespace ZCN.QoL.EasyReading
{
    internal static class EasyReadingDebug
    {
#if DEBUG
        internal static bool Enabled => EasyReadingMod.Settings != null &&
            EasyReadingMod.Settings.enableDebugLogging;
#else
        // Release never enables verbose diagnostics, including when an old
        // settings XML still contains enableDebugLogging=true.
        internal static bool Enabled => false;
#endif
    }
}
