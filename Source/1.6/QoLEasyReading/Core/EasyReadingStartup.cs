using HarmonyLib;

namespace ZCN.QoL.EasyReading
{
    // Stage 4S-1C: Read & Learn-owned explicit startup.
    // Deliberately leaves the proven ReadingRepeatProbe/Achtung! patch intact.
    internal static class EasyReadingStartup
    {
        internal static void InstallReadingInput(Harmony harmony)
        {
            ReadingRepeatProbe.Install(harmony);
            ManualReadingWorkScheduleOverride.Install(harmony);
        }
    }
}
