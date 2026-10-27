using UnityEngine;
using Verse;

namespace ZCN.QoL.EasyReading
{
    internal static class EasyReadingSettingsUI
    {
        internal static void Draw(Listing_Standard listing, EasyReadingSettings Settings)
        {
            Text.Font = GameFont.Medium;
            listing.Label(EasyReadingI18n.Get("ER_Intro"));
            Text.Font = GameFont.Small;
            listing.Gap(5f);
            RepeatReadingShortcutSettingsUI.Draw(listing, Settings);

            listing.GapLine();
            Text.Font = GameFont.Medium;
            listing.Label(EasyReadingI18n.Get("ER_TextbookIcons"));
            Text.Font = GameFont.Small;
            bool wasBookIconEnabled = Settings.showBookSkillIcons;
            SettingsTypography.FeatureToggle(
                listing,
                EasyReadingI18n.Get("ER_ShowBookcaseIcons"),
                ref Settings.showBookSkillIcons
            );
            if (wasBookIconEnabled != Settings.showBookSkillIcons && EasyReadingDebug.Enabled)
            {
                Log.Message("[QoL: Easy Reading][BookUI] Bookcase Contents icons " +
                    (Settings.showBookSkillIcons ? "ON" : "OFF") + ".");
            }
            SettingsTypography.FeatureToggle(
                listing,
                EasyReadingI18n.Get("ER_ShowReadMenuIcons"),
                ref Settings.showReadMenuSkillIcons
            );
            SettingsTypography.Description(listing,
                EasyReadingI18n.Get("ER_ReadMenuIconsDesc"));

            SettingsTypography.FeatureToggle(
                listing,
                EasyReadingI18n.Get("ER_ShowTradeIcons"),
                ref Settings.showTradeSkillIcons
            );
            SettingsTypography.Description(listing,
                EasyReadingI18n.Get("ER_TradeIconsDesc"));

            if (Settings.showBookSkillIcons || Settings.showReadMenuSkillIcons ||
                Settings.showTradeSkillIcons)
            {
                listing.Gap(6f);
                listing.Label(EasyReadingI18n.Get("ER_IconSizeLabel"));
                if (listing.RadioButton(EasyReadingI18n.Get("ER_Size100"),
                    Settings.bookSkillIconSizePercent == 100, 20f))
                    Settings.bookSkillIconSizePercent = 100;
                if (listing.RadioButton(EasyReadingI18n.Get("ER_Size125"),
                    Settings.bookSkillIconSizePercent == 125, 20f))
                    Settings.bookSkillIconSizePercent = 125;
                if (listing.RadioButton(EasyReadingI18n.Get("ER_Size150"),
                    Settings.bookSkillIconSizePercent == 150, 20f))
                    Settings.bookSkillIconSizePercent = 150;
            }
            if (Settings.showTradeSkillIcons)
            {
                listing.Gap(4f);
                listing.Label(EasyReadingI18n.Get("ER_TradePositionLabel"));
                if (listing.RadioButton(EasyReadingI18n.Get("ER_TradePositionLeft"),
                    Settings.tradeSkillIconPosition == 0, 20f))
                    Settings.tradeSkillIconPosition = 0;
                if (listing.RadioButton(EasyReadingI18n.Get("ER_TradePositionRight"),
                    Settings.tradeSkillIconPosition == 1, 20f))
                    Settings.tradeSkillIconPosition = 1;
            }
            if (Settings.showBookSkillIcons)
            {
                listing.Gap(4f);
                listing.Label(EasyReadingI18n.Get("ER_BookcasePositionLabel"));
                if (listing.RadioButton(EasyReadingI18n.Get("ER_BookcasePositionLeft"),
                    Settings.bookSkillIconPosition == 0, 20f))
                    Settings.bookSkillIconPosition = 0;
                if (listing.RadioButton(EasyReadingI18n.Get("ER_BookcasePositionRight"),
                    Settings.bookSkillIconPosition == 1, 20f))
                    Settings.bookSkillIconPosition = 1;
            }
            SettingsTypography.Description(listing,
                EasyReadingI18n.Get("ER_BookIconsFooter"));

            listing.GapLine();
            Text.Font = GameFont.Medium;
            listing.Label(EasyReadingI18n.Get("ER_WorkHeader"));
            Text.Font = GameFont.Small;
            SettingsTypography.FeatureToggle(listing,
                EasyReadingI18n.Get("ER_WorkToggle"),
                ref Settings.allowManualReadingDuringWork);
            SettingsTypography.Description(listing,
                EasyReadingI18n.Get("ER_WorkDesc"));

            listing.GapLine();
            Text.Font = GameFont.Medium;
            listing.Label(EasyReadingI18n.Get("ER_Diagnostics"));
            Text.Font = GameFont.Small;
            SettingsTypography.FeatureToggle(listing,
                EasyReadingI18n.Get("ER_DebugToggle"),
                ref Settings.enableDebugLogging);
            SettingsTypography.Description(listing,
                EasyReadingI18n.Get("ER_DebugDesc"));
        }

    }
}
