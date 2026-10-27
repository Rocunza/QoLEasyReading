using Verse;

namespace ZCN.QoL.EasyReading
{
    internal static class EasyReadingSettingsData
    {
        internal static void Expose(EasyReadingSettings settings)
        {
            Scribe_Values.Look(
                ref settings.showRepeatReadingDialog,
                "showRepeatReadingDialog",
                true
            );

            Scribe_Values.Look(
                ref settings.repeatReadingAskEveryTime,
                "repeatReadingAskEveryTime",
                true
            );

            Scribe_Values.Look(
                ref settings.repeatReadingPresetCount,
                "repeatReadingPresetCount",
                17
            );

            Scribe_Values.Look(
                ref settings.repeatReadingPresetShortcutEnabled,
                "repeatReadingPresetShortcutEnabled", true
            );
            Scribe_Values.Look(
                ref settings.repeatReadingDialogShortcutEnabled,
                "repeatReadingDialogShortcutEnabled", true
            );
            Scribe_Values.Look(
                ref settings.repeatReadingPresetKey,
                "repeatReadingPresetKey", RepeatReadingShortcut.DefaultPresetKey
            );
            Scribe_Values.Look(
                ref settings.repeatReadingDialogKey,
                "repeatReadingDialogKey", RepeatReadingShortcut.DefaultDialogKey
            );
            RepeatReadingShortcut.NormalizeSettings(settings);

            // WORK-ON1: ON when no setting key exists; saving OFF remains possible.
            // Legacy configs written with an OFF default may omit this key,
            // so this changes their effective fallback to ON.
            Scribe_Values.Look(
                ref settings.allowManualReadingDuringWork,
                "allowManualReadingDuringWork", true
            );

            Scribe_Values.Look(
                ref settings.showBookSkillIcons,
                "showBookSkillIcons",
                true
            );

            Scribe_Values.Look(
                ref settings.showReadMenuSkillIcons,
                "showReadMenuSkillIcons", true
            );

            Scribe_Values.Look(
                ref settings.showTradeSkillIcons,
                "showTradeSkillIcons", true
            );

            Scribe_Values.Look(
                ref settings.tradeSkillIconPosition,
                "tradeSkillIconPosition", 0
            );
            settings.tradeSkillIconPosition = settings.tradeSkillIconPosition == 0 ? 0 : 1;

            Scribe_Values.Look(
                ref settings.bookSkillIconSizePercent,
                "bookSkillIconSizePercent", 125
            );
            settings.bookSkillIconSizePercent = BookContentsSkillIcons.NormalizeSize(
                settings.bookSkillIconSizePercent);
            Scribe_Values.Look(
                ref settings.bookSkillIconPosition,
                "bookSkillIconPosition", 0
            );
            settings.bookSkillIconPosition = settings.bookSkillIconPosition == 1 ? 1 : 0;
        }
    }
}
