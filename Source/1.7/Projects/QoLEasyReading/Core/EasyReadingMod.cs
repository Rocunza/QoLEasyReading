using HarmonyLib;
using UnityEngine;
using Verse;

namespace ZCN.QoL.EasyReading
{
    public sealed class EasyReadingMod : Mod
    {
        public static EasyReadingSettings Settings { get; private set; }
        private Vector2 scrollPosition;
        private string lastActionMessage = string.Empty;

        public EasyReadingMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<EasyReadingSettings>();
            EasyReadingBuildIdentity.LogStartup();
            Harmony harmony = new Harmony("zcn.qol.easyreading");
            harmony.PatchAll();
            EasyReadingStartup.InstallReadingInput(harmony);
            Log.Message("[QoL: Easy Reading] Book icon and repeat-reading patches installed.");
        }

        public override string SettingsCategory() => "QoL: Easy Reading";

        public override void DoSettingsWindowContents(Rect inRect)
        {
            const float footerHeight = 30f;
            const float footerGap = 38f;
            Rect scrollRect = new Rect(inRect.x, inRect.y, inRect.width,
                Mathf.Max(10f, inRect.height - footerHeight - footerGap));
            Rect view = new Rect(0f, 0f, Mathf.Max(10f, scrollRect.width - 20f), 1380f);
            Widgets.BeginScrollView(scrollRect, ref scrollPosition, view);
            try
            {
                Listing_Standard listing = new Listing_Standard();
                listing.Begin(view);
                try { EasyReadingSettingsUI.Draw(listing, Settings); }
                finally { listing.End(); }
            }
            finally
            {
                Widgets.EndScrollView();
                Text.Font = GameFont.Small;
            }
            float buttonY = inRect.yMax - footerHeight;
            float buttonWidth = (inRect.width - 6f) / 2f;
            if (!string.IsNullOrEmpty(lastActionMessage))
                Widgets.Label(new Rect(inRect.x + 5f, buttonY - 32f,
                    inRect.width - 10f, 26f), lastActionMessage);
            Widgets.DrawLineHorizontal(inRect.x, buttonY - 7f, inRect.width);
            if (Widgets.ButtonText(new Rect(inRect.x, buttonY, buttonWidth, footerHeight),
                EasyReadingI18n.Get("ER_RestoreDefaultsButton")))
                Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                    EasyReadingI18n.Get("ER_RestoreConfirm"),
                    delegate
                    {
                        EasyReadingSettingsPresets.RestoreReadDefaults(Settings);
                        WriteSettings();
                        lastActionMessage = EasyReadingI18n.Get("ER_RestoreDone");
                    }, false));
            if (Widgets.ButtonText(new Rect(inRect.x + buttonWidth + 6f,
                buttonY, buttonWidth, footerHeight), EasyReadingI18n.Get("ER_DisableFeaturesButton")))
                Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                    EasyReadingI18n.Get("ER_DisableConfirm"),
                    delegate
                    {
                        EasyReadingSettingsPresets.DisableReadFeatures(Settings);
                        WriteSettings();
                        lastActionMessage = EasyReadingI18n.Get("ER_DisableDone");
                    }, false));
            base.DoSettingsWindowContents(inRect);
        }
    }
}
