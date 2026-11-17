using UnityEngine;
using Verse;

namespace ZCN.QoL.EasyReading
{
    // UI-TAB1: repeated reading, Work schedule, and diagnostics only.
    // Presentational move: no gameplay or Scribe state changes.
    internal static class EasyReadingBasicTab
    {
        internal static void Draw(Listing_Standard listing, EasyReadingSettings settings)
        {
            if (listing == null || settings == null) return;
            Text.Font = GameFont.Medium;
            listing.Label(EasyReadingI18n.Get("ER_Intro"));
            Text.Font = GameFont.Small;
            listing.Gap(5f);
            RepeatReadingShortcutSettingsUI.Draw(listing, settings);

            listing.GapLine();
            Text.Font = GameFont.Medium;
            listing.Label(EasyReadingI18n.Get("ER_WorkHeader"));
            Text.Font = GameFont.Small;
            SettingsTypography.FeatureToggle(listing,
                EasyReadingI18n.Get("ER_WorkToggle"),
                ref settings.allowManualReadingDuringWork);
            SettingsTypography.Description(listing,
                EasyReadingI18n.Get("ER_WorkDesc"));

#if DEBUG
            listing.GapLine();
            Text.Font = GameFont.Medium;
            listing.Label(EasyReadingI18n.Get("ER_Diagnostics"));
            Text.Font = GameFont.Small;
            SettingsTypography.FeatureToggle(listing,
                EasyReadingI18n.Get("ER_DebugToggle"),
                ref settings.enableDebugLogging);
            SettingsTypography.Description(listing,
                EasyReadingI18n.Get("ER_DebugDesc"));
#endif
        }
    }
}
