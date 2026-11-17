using UnityEngine;
using Verse;

namespace ZCN.QoL.EasyReading
{
    /// <summary>
    /// UI-ALIGN1: layout-only helper for settings checkboxes and radio rows.
    /// All settings mutations remain in the callers. The original Widgets
    /// renderers are used, and the Listing's column state is never changed.
    /// </summary>
    internal static class SettingsControlLayout
    {
        private const float LabelToControlGap = 16f;
        private const float ControlFootprint = 32f;
        private const float RadioIndent = 20f;

        // Measure the translated, visible labels with the font that will draw
        // them. This aligns every icon in a logical group at the same X.
        internal static float GroupWidth(GameFont font, params string[] labels)
        {
            GameFont oldFont = Text.Font;
            try
            {
                Text.Font = font;
                float widest = 0f;
                if (labels != null)
                {
                    for (int i = 0; i < labels.Length; i++)
                    {
                        float width = Text.CalcSize(labels[i] ?? string.Empty).x;
                        if (width > widest) widest = width;
                    }
                }
                return Mathf.Ceil(widest + LabelToControlGap + ControlFootprint);
            }
            finally
            {
                Text.Font = oldFont;
            }
        }

        private static float FittedWidth(float wanted, float available)
        {
            // On a narrow window use the original full-width arrangement,
            // avoiding text/icon overlap or truncation due to the new layout.
            return wanted > 0f && wanted <= available ? wanted : available;
        }

        internal static bool RadioChoice(Listing_Standard listing, string label, bool selected, float groupWidth)
        {
            Rect row = listing.GetRect(Text.LineHeight, 1f);
            row.x += RadioIndent;
            row.width = Mathf.Max(0f, row.width - RadioIndent);
            row.width = FittedWidth(groupWidth, row.width);
            // Listing_Standard.RadioButton delegates to this vanilla widget.
            bool clicked = Widgets.RadioButtonLabeled(row, label, selected, false);
            listing.Gap(listing.verticalSpacing);
            return clicked;
        }

        internal static void Checkbox(Listing_Standard listing, string label, ref bool enabled, float groupWidth = 0f)
        {
            // Match Listing_Standard.CheckboxLabeled row height and gap.
            float available = listing.ColumnWidth;
            float height = Text.CalcHeight(label, available);
            Rect row = listing.GetRect(height, 1f);
            float wantedWidth = groupWidth > 0f ? groupWidth :
                Mathf.Ceil(Text.CalcSize(label ?? string.Empty).x + LabelToControlGap + ControlFootprint);
            row.width = FittedWidth(wantedWidth, row.width);
            // Exact 1.6 vanilla Widget flags used by Listing_Standard:
            // disabled=false, textures=null, final flags=false,false.
            Widgets.CheckboxLabeled(row, label, ref enabled, false, null, null, false, false);
            listing.Gap(listing.verticalSpacing);
        }
    }
}
