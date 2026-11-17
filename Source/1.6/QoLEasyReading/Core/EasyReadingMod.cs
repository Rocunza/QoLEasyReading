using HarmonyLib;
using UnityEngine;
using Verse;

namespace ZCN.QoL.EasyReading
{
    public sealed class EasyReadingMod : Mod
    {
        public static EasyReadingSettings Settings { get; private set; }
        // S3-UI-TAB2: visual settings state is owned by a dedicated per-mod UI instance.
        private readonly EasyReadingSettingsWindow settingsWindow = new EasyReadingSettingsWindow();

        public EasyReadingMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<EasyReadingSettings>();
            EasyReadingBuildIdentity.LogStartup();
            Harmony harmony = new Harmony("zcn.qol.easyreading");
            harmony.PatchAll();
            EasyReadingStartup.InstallReadingInput(harmony);
            EasyReadingRuntimeHealth.Initialize();
#if DEBUG
            SchematicResearchAudit.Initialize();
            SchematicOwnershipAudit.RunPureSelfTests();
            SchematicTradePurchaseIconPolicy.RunPureSelfTests();
            SchematicDuplicateTooltipContent.RunPureSelfTests();
#endif
            Log.Message("[QoL: Easy Reading] Book icon and repeat-reading patches installed.");
        }

        public override string SettingsCategory() => "QoL: Easy Reading";

        public override void DoSettingsWindowContents(Rect inRect)
        {
            settingsWindow.Draw(inRect, Settings, WriteSettings);
            base.DoSettingsWindowContents(inRect);
        }
    }
}
