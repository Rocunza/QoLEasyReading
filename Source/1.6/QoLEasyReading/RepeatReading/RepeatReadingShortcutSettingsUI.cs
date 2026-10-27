using UnityEngine;
using Verse;

namespace ZCN.QoL.EasyReading
{
    /// <summary>
    /// A narrow settings presenter for repeat-reading. Retains the existing
    /// master enabled state and preset count, with two independent bindings.
    /// </summary>
    internal static class RepeatReadingShortcutSettingsUI
    {
        public static void Draw(Listing_Standard listing, EasyReadingSettings settings)
        {
            if (listing == null || settings == null) return;
            SettingsTypography.FeatureToggle(listing,
                EasyReadingI18n.Get("ER_RepeatEnabled"), ref settings.showRepeatReadingDialog);
            if (!settings.showRepeatReadingDialog) return;

            RepeatReadingShortcut.NormalizeSettings(settings);
            listing.Gap(6f);
            listing.Label(EasyReadingI18n.Get("ER_PresetHeader"));
            listing.CheckboxLabeled(EasyReadingI18n.Get("ER_PresetToggle"), ref settings.repeatReadingPresetShortcutEnabled);
            if (listing.ButtonText(EasyReadingI18n.Format("ER_ChangeKey", RepeatReadingShortcut.KeyLabel(settings.repeatReadingPresetKey)),
                    widthPct: 0.52f))
                Find.WindowStack.Add(new Dialog_RepeatReadingKeyBinding(settings, true));

            listing.Gap(6f);
            listing.Label(EasyReadingI18n.Get("ER_CustomHeader"));
            listing.CheckboxLabeled(EasyReadingI18n.Get("ER_CustomToggle"), ref settings.repeatReadingDialogShortcutEnabled);
            if (listing.ButtonText(EasyReadingI18n.Format("ER_ChangeKey", RepeatReadingShortcut.KeyLabel(settings.repeatReadingDialogKey)),
                    widthPct: 0.52f))
                Find.WindowStack.Add(new Dialog_RepeatReadingKeyBinding(settings, false));

            listing.Gap(7f);
            settings.repeatReadingPresetCount = Mathf.Clamp(settings.repeatReadingPresetCount, 1, 50);
            listing.Label(EasyReadingI18n.Format("ER_StartingCount", settings.repeatReadingPresetCount));
            settings.repeatReadingPresetCount = Mathf.Clamp(
                Mathf.RoundToInt(listing.Slider(settings.repeatReadingPresetCount, 1, 50)), 1, 50);

            listing.Gap(6f);
            if (listing.ButtonText(EasyReadingI18n.Get("ER_RestoreShortcuts"), widthPct: 0.52f))
                RepeatReadingShortcut.RestoreDefaults(settings);

            SettingsTypography.Description(listing,
                EasyReadingI18n.Get("ER_KeysDesc1"));
            SettingsTypography.Description(listing,
                EasyReadingI18n.Get("ER_KeysDesc2"));
        }
    }
}
