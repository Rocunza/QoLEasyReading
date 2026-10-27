using UnityEngine;
using Verse;

namespace ZCN.QoL.EasyReading
{
    /// <summary>
    /// UI2: limit text emphasis to top-level feature toggles. Restore
    /// global IMGUI Text.Font and GUI.color after every element so that
    /// unrelated settings and other mods keep their original styles.
    /// </summary>
    internal static class SettingsTypography
    {
        internal static void FeatureToggle(Listing_Standard listing, string label, ref bool enabled)
        {
            GameFont oldFont = Text.Font;
            try
            {
                Text.Font = GameFont.Medium;
                listing.CheckboxLabeled(label, ref enabled);
            }
            finally
            {
                Text.Font = oldFont;
            }
        }

        internal static void Description(Listing_Standard listing, string description)
        {
            GameFont oldFont = Text.Font;
            Color oldColor = GUI.color;
            try
            {
                Text.Font = GameFont.Tiny;
                GUI.color = new Color(0.78f, 0.78f, 0.78f, oldColor.a);
                listing.Label(description);
            }
            finally
            {
                GUI.color = oldColor;
                Text.Font = oldFont;
            }
        }
    }
}
