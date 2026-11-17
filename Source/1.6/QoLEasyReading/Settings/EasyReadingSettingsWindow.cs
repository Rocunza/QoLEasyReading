using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace ZCN.QoL.EasyReading
{
    // S3-UI-TAB2: settings chrome only. No Scribe keys, Harmony patches, or game data writes.
    // Created once per EasyReadingMod instance; tab/scroll state stays transient.
    internal sealed class EasyReadingSettingsWindow
    {
        private EasyReadingSettingsTab activeTab = EasyReadingSettingsTab.Basic;
        private readonly List<TabRecord> tabs = new List<TabRecord>(2);
        private Vector2 basicScrollPosition;
        private Vector2 iconsScrollPosition;
        // Existing UI-TAB1 estimates, updated after the listing's Repaint.
        private float basicContentHeight = 720f;
        private float iconsContentHeight = 760f;
        private string lastActionMessage = string.Empty;

        internal void Draw(Rect inRect, EasyReadingSettings settings, Action writeSettings)
        {
            if (settings == null || writeSettings == null)
                return;

            const float footerHeight = 30f;
            const float footerGap = 38f;
            const float bodySafetyGap = 4f;

            // The original XP Customizer uses this same RimWorld menu/tabs pairing.
            // Tabs extend above the panel: reserve TabHeight inside the parent rect.
            Rect panelRect = new Rect(inRect.x, inRect.y + TabDrawer.TabHeight,
                inRect.width, Mathf.Max(1f, inRect.height - TabDrawer.TabHeight));
            Widgets.DrawMenuSection(panelRect);
            DrawTabs(panelRect);
            Rect innerRect = panelRect.ContractedBy(10f);

            // Existing listing and each tab's independent scroll position are preserved.
            float bodyY = innerRect.y + 8f;
            float bodyHeight = Mathf.Max(10f, innerRect.yMax - footerHeight - footerGap
                - bodyY - bodySafetyGap);
            Rect scrollRect = new Rect(innerRect.x, bodyY,
                innerRect.width, bodyHeight);
            Vector2 currentScroll = activeTab == EasyReadingSettingsTab.Basic
                ? basicScrollPosition : iconsScrollPosition;
            float storedContentHeight = activeTab == EasyReadingSettingsTab.Basic
                ? basicContentHeight : iconsContentHeight;
            Rect view = new Rect(0f, 0f, Mathf.Max(10f, scrollRect.width - 20f),
                Mathf.Max(scrollRect.height + 1f, storedContentHeight));

            Widgets.BeginScrollView(scrollRect, ref currentScroll, view);
            try
            {
                Listing_Standard listing = new Listing_Standard();
                listing.Begin(view);
                try
                {
                    EasyReadingSettingsUI.Draw(listing, settings, activeTab);
                    // Never issue a second draw; measure only in the Repaint event.
                    if (Event.current != null && Event.current.type == EventType.Repaint)
                    {
                        float measured = Mathf.Max(10f, listing.CurHeight + 18f);
                        if (activeTab == EasyReadingSettingsTab.Basic)
                            basicContentHeight = measured;
                        else
                            iconsContentHeight = measured;
                    }
                }
                finally { listing.End(); }
            }
            finally
            {
                Widgets.EndScrollView();
                Text.Font = GameFont.Small; // Existing UI-TAB1 cleanup behavior.
            }
            if (activeTab == EasyReadingSettingsTab.Basic)
                basicScrollPosition = currentScroll;
            else
                iconsScrollPosition = currentScroll;

            // Buttons remain fixed inside the menu section; callbacks unchanged.
            float buttonY = innerRect.yMax - footerHeight;
            float buttonWidth = (innerRect.width - 6f) / 2f;
            if (!string.IsNullOrEmpty(lastActionMessage))
                Widgets.Label(new Rect(innerRect.x + 5f, buttonY - 32f,
                    innerRect.width - 10f, 26f), lastActionMessage);
            Widgets.DrawLineHorizontal(innerRect.x, buttonY - 7f, innerRect.width);
            if (Widgets.ButtonText(new Rect(innerRect.x, buttonY, buttonWidth, footerHeight),
                EasyReadingI18n.Get("ER_RestoreDefaultsButton")))
                Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                    EasyReadingI18n.Get("ER_RestoreConfirm"),
                    delegate
                    {
                        EasyReadingSettingsPresets.RestoreReadDefaults(settings);
                        writeSettings();
                        lastActionMessage = EasyReadingI18n.Get("ER_RestoreDone");
                    }, false));
            if (Widgets.ButtonText(new Rect(innerRect.x + buttonWidth + 6f,
                buttonY, buttonWidth, footerHeight), EasyReadingI18n.Get("ER_DisableFeaturesButton")))
                Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                    EasyReadingI18n.Get("ER_DisableConfirm"),
                    delegate
                    {
                        EasyReadingSettingsPresets.DisableReadFeatures(settings);
                        writeSettings();
                        lastActionMessage = EasyReadingI18n.Get("ER_DisableDone");
                    }, false));
        }

        private void DrawTabs(Rect panelRect)
        {
            tabs.Clear();
            tabs.Add(new TabRecord(EasyReadingI18n.Get("ER_TabBasic"),
                delegate { activeTab = EasyReadingSettingsTab.Basic; },
                activeTab == EasyReadingSettingsTab.Basic));
            tabs.Add(new TabRecord(EasyReadingI18n.Get("ER_TabIcons"),
                delegate { activeTab = EasyReadingSettingsTab.Icons; },
                activeTab == EasyReadingSettingsTab.Icons));
            TabDrawer.DrawTabs(panelRect, tabs);
        }
    }
}
