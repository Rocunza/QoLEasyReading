using UnityEngine;
using Verse;

namespace ZCN.QoL.EasyReading
{
    // S3-B1: independent bookcase, Read-menu and trade Schematic switches.
    // Trade '?' is research-only in this dev stage; not an ownership check.
    internal static class EasyReadingIconsTab
    {
        internal static void Draw(Listing_Standard listing, EasyReadingSettings settings)
        {
            if (listing == null || settings == null) return;
            Text.Font = GameFont.Medium;
            listing.Label(EasyReadingI18n.Get("ER_TextbookIcons"));
            Text.Font = GameFont.Small;
            string bookcaseToggleLabel = EasyReadingI18n.Get("ER_ShowBookcaseIcons");
            string readMenuToggleLabel = EasyReadingI18n.Get("ER_ShowReadMenuIcons");
            string tradeToggleLabel = EasyReadingI18n.Get("ER_ShowTradeIcons");
            float textbookToggleWidth = SettingsControlLayout.GroupWidth(
                GameFont.Medium, bookcaseToggleLabel, readMenuToggleLabel, tradeToggleLabel);
            bool wasBookIconEnabled = settings.showBookSkillIcons;
            SettingsTypography.FeatureToggle(
                listing,
                bookcaseToggleLabel,
                ref settings.showBookSkillIcons, textbookToggleWidth
            );
            if (wasBookIconEnabled != settings.showBookSkillIcons && EasyReadingDebug.Enabled)
            {
                Log.Message("[QoL: Easy Reading][BookUI] Bookcase Contents icons " +
                    (settings.showBookSkillIcons ? "ON" : "OFF") + ".");
            }
            SettingsTypography.FeatureToggle(
                listing,
                readMenuToggleLabel,
                ref settings.showReadMenuSkillIcons, textbookToggleWidth
            );
            SettingsTypography.Description(listing,
                EasyReadingI18n.Get("ER_ReadMenuIconsDesc"));

            SettingsTypography.FeatureToggle(
                listing,
                tradeToggleLabel,
                ref settings.showTradeSkillIcons, textbookToggleWidth
            );
            SettingsTypography.Description(listing,
                EasyReadingI18n.Get("ER_TradeIconsDesc"));

            SettingsTypography.Description(listing,
                EasyReadingI18n.Get("ER_BookIconsFooter"));

            listing.GapLine();
            Text.Font = GameFont.Medium;
            listing.Label(EasyReadingI18n.Get("ER_SchematicIcons"));
            Text.Font = GameFont.Small;
            string schematicBookcaseLabel = EasyReadingI18n.Get("ER_ShowSchematicBookcaseIcons");
            string schematicReadLabel = EasyReadingI18n.Get("ER_ShowSchematicReadMenuIcons");
            string schematicTradeLabel = EasyReadingI18n.Get("ER_ShowSchematicTradeIcons");
            float schematicToggleWidth = SettingsControlLayout.GroupWidth(
                GameFont.Medium, schematicBookcaseLabel, schematicReadLabel,
                schematicTradeLabel);
            bool wasSchematicEnabled = settings.showSchematicBookcaseIcons;
            SettingsTypography.FeatureToggle(listing, schematicBookcaseLabel,
                ref settings.showSchematicBookcaseIcons, schematicToggleWidth);
            if (wasSchematicEnabled != settings.showSchematicBookcaseIcons &&
                EasyReadingDebug.Enabled)
                Log.Message("[QoL: Easy Reading][S3A1] Schematic bookcase icons " +
                    (settings.showSchematicBookcaseIcons ? "ON" : "OFF") +
                    "; BUILD_ID=" + EasyReadingBuildIdentity.BuildId + ";");
            bool wasSchematicReadEnabled = settings.showSchematicReadMenuIcons;
            SettingsTypography.FeatureToggle(listing, schematicReadLabel,
                ref settings.showSchematicReadMenuIcons, schematicToggleWidth);
            if (wasSchematicReadEnabled != settings.showSchematicReadMenuIcons &&
                EasyReadingDebug.Enabled)
                Log.Message("[QoL: Easy Reading][S3A2] Schematic Read menu icons " +
                    (settings.showSchematicReadMenuIcons ? "ON" : "OFF") +
                    "; BUILD_ID=" + EasyReadingBuildIdentity.BuildId + ";");
            SettingsTypography.Description(listing,
                EasyReadingI18n.Get("ER_SchematicReadMenuDesc"));
            bool wasSchematicTradeEnabled = settings.showSchematicTradeIcons;
            SettingsTypography.FeatureToggle(listing, schematicTradeLabel,
                ref settings.showSchematicTradeIcons, schematicToggleWidth);
            if (wasSchematicTradeEnabled != settings.showSchematicTradeIcons &&
                EasyReadingDebug.Enabled)
                Log.Message("[QoL: Easy Reading][S3B1][Settings] showSchematicTradeIcons=" +
                    (settings.showSchematicTradeIcons ? "ON" : "OFF") +
                    "; BUILD_ID=" + EasyReadingBuildIdentity.BuildId + ";");
            SettingsTypography.Description(listing,
                EasyReadingI18n.Get("ER_SchematicTradeIconsDesc"));

            listing.GapLine();
            Text.Font = GameFont.Medium;
            listing.Label(EasyReadingI18n.Get("ER_IconDisplayHeader"));
            Text.Font = GameFont.Small;
            if (settings.showBookSkillIcons || settings.showReadMenuSkillIcons ||
                settings.showTradeSkillIcons || settings.showSchematicBookcaseIcons ||
                settings.showSchematicReadMenuIcons || settings.showSchematicTradeIcons)
            {
                listing.Gap(6f);
                listing.Label(EasyReadingI18n.Get("ER_IconSizeLabel"));
                float iconSizeGroupWidth = SettingsControlLayout.GroupWidth(GameFont.Small,
                    EasyReadingI18n.Get("ER_Size100"), EasyReadingI18n.Get("ER_Size125"),
                    EasyReadingI18n.Get("ER_Size150"));
                if (SettingsControlLayout.RadioChoice(listing, EasyReadingI18n.Get("ER_Size100"),
                    settings.bookSkillIconSizePercent == 100, iconSizeGroupWidth))
                    settings.bookSkillIconSizePercent = 100;
                if (SettingsControlLayout.RadioChoice(listing, EasyReadingI18n.Get("ER_Size125"),
                    settings.bookSkillIconSizePercent == 125, iconSizeGroupWidth))
                    settings.bookSkillIconSizePercent = 125;
                if (SettingsControlLayout.RadioChoice(listing, EasyReadingI18n.Get("ER_Size150"),
                    settings.bookSkillIconSizePercent == 150, iconSizeGroupWidth))
                    settings.bookSkillIconSizePercent = 150;
            }
            if (settings.showTradeSkillIcons || settings.showSchematicTradeIcons)
            {
                listing.Gap(4f);
                listing.Label(EasyReadingI18n.Get("ER_TradePositionLabel"));
                float tradePositionGroupWidth = SettingsControlLayout.GroupWidth(GameFont.Small,
                    EasyReadingI18n.Get("ER_TradePositionLeft"), EasyReadingI18n.Get("ER_TradePositionRight"));
                if (SettingsControlLayout.RadioChoice(listing, EasyReadingI18n.Get("ER_TradePositionLeft"),
                    settings.tradeSkillIconPosition == 0, tradePositionGroupWidth))
                    settings.tradeSkillIconPosition = 0;
                if (SettingsControlLayout.RadioChoice(listing, EasyReadingI18n.Get("ER_TradePositionRight"),
                    settings.tradeSkillIconPosition == 1, tradePositionGroupWidth))
                    settings.tradeSkillIconPosition = 1;
            }
            if (settings.showBookSkillIcons || settings.showSchematicBookcaseIcons)
            {
                listing.Gap(4f);
                listing.Label(EasyReadingI18n.Get("ER_BookcasePositionLabel"));
                float bookcasePositionGroupWidth = SettingsControlLayout.GroupWidth(GameFont.Small,
                    EasyReadingI18n.Get("ER_BookcasePositionLeft"), EasyReadingI18n.Get("ER_BookcasePositionRight"));
                if (SettingsControlLayout.RadioChoice(listing, EasyReadingI18n.Get("ER_BookcasePositionLeft"),
                    settings.bookSkillIconPosition == 0, bookcasePositionGroupWidth))
                    settings.bookSkillIconPosition = 0;
                if (SettingsControlLayout.RadioChoice(listing, EasyReadingI18n.Get("ER_BookcasePositionRight"),
                    settings.bookSkillIconPosition == 1, bookcasePositionGroupWidth))
                    settings.bookSkillIconPosition = 1;
            }
        }
    }
}
