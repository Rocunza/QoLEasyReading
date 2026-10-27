namespace ZCN.QoL.EasyReading
{
    internal static class EasyReadingSettingsPresets
    {
        internal static void RestoreReadDefaults(EasyReadingSettings settings)
        {
            if (settings == null) return;
            EasyReadingSettings defaults = new EasyReadingSettings();
            settings.showRepeatReadingDialog = defaults.showRepeatReadingDialog;
            settings.repeatReadingAskEveryTime = defaults.repeatReadingAskEveryTime;
            settings.repeatReadingPresetCount = defaults.repeatReadingPresetCount;
            settings.repeatReadingPresetShortcutEnabled = defaults.repeatReadingPresetShortcutEnabled;
            settings.repeatReadingDialogShortcutEnabled = defaults.repeatReadingDialogShortcutEnabled;
            settings.repeatReadingPresetKey = defaults.repeatReadingPresetKey;
            settings.repeatReadingDialogKey = defaults.repeatReadingDialogKey;
            settings.showBookSkillIcons = defaults.showBookSkillIcons;
            settings.showReadMenuSkillIcons = defaults.showReadMenuSkillIcons;
            settings.showTradeSkillIcons = defaults.showTradeSkillIcons;
            settings.tradeSkillIconPosition = defaults.tradeSkillIconPosition;
            settings.bookSkillIconSizePercent = defaults.bookSkillIconSizePercent;
            settings.bookSkillIconPosition = defaults.bookSkillIconPosition;
            settings.allowManualReadingDuringWork = defaults.allowManualReadingDuringWork;
            settings.enableDebugLogging = defaults.enableDebugLogging;
        }

        internal static void DisableReadFeatures(EasyReadingSettings settings)
        {
            if (settings == null) return;
            settings.showRepeatReadingDialog = false;
            settings.repeatReadingPresetShortcutEnabled = false;
            settings.repeatReadingDialogShortcutEnabled = false;
            settings.showBookSkillIcons = false;
            settings.showReadMenuSkillIcons = false;
            settings.showTradeSkillIcons = false;
            settings.allowManualReadingDuringWork = false;
            settings.enableDebugLogging = false;
        }

    }
}
