using Verse;

namespace ZCN.QoL.EasyReading
{
    internal enum EasyReadingSettingsTab
    {
        Basic = 0,
        Icons = 1
    }

    // UI-TAB1: navigation only; state and the actual control behavior stay in the presenters.
    internal static class EasyReadingSettingsUI
    {
        internal static void Draw(Listing_Standard listing, EasyReadingSettings settings,
            EasyReadingSettingsTab selectedTab)
        {
            if (listing == null || settings == null) return;
            switch (selectedTab)
            {
                case EasyReadingSettingsTab.Icons:
                    EasyReadingIconsTab.Draw(listing, settings);
                    break;
                default:
                    EasyReadingBasicTab.Draw(listing, settings);
                    break;
            }
        }
    }
}
