using Verse;

namespace ZCN.QoL.EasyReading
{
    /// <summary>Settings stored independently from Child Learning.
    /// Old Read & Learn settings are NOT automatically migrated.</summary>
    public sealed class EasyReadingSettings : ModSettings
    {
        // Serialized field key retained for future explicit settings migration.
        public bool showRepeatReadingDialog = true;

        // Deprecated (persisted for backward-compatibility only): the old
        // Ctrl+Shift choice. The two new shortcuts have separate roles.
        public bool repeatReadingAskEveryTime = true;
        public int repeatReadingPresetCount = 17;

        // Stage Key1: two independently enabled single-key+left-click bindings.
        // Controls are intercepted only for verified Book.Read menu actions.
        public bool repeatReadingPresetShortcutEnabled = true;
        public bool repeatReadingDialogShortcutEnabled = true;
        public int repeatReadingPresetKey = RepeatReadingShortcut.DefaultPresetKey;
        public int repeatReadingDialogKey = RepeatReadingShortcut.DefaultDialogKey;

        // BookUI-2: textbook skill markers in Bookcase Contents default ON.
        // No book, pawn, XP or save-game modifications.
        public bool showBookSkillIcons = true;

        // 4S-2E1: independent, opt-in textbook markers in Book.Read menu.
        // Existing saved OFF values are preserved when updating.
        public bool showReadMenuSkillIcons = true;

        // 4S-2F1: independently enabled trade-window skill markers.
        // Defaults ON for new configs; OFF still preserves vanilla trade rows.
        public bool showTradeSkillIcons = true;

        // 4S-2F2: Trade row location, independent of bookcase positioning.
        // 0 = before the book title, after vanilla item/info icons.
        // 1 = right side of the name column (legacy previous default).
        public int tradeSkillIconPosition = 0;

        // BookUI-2-Fix2: saved independently of enable state.
        // 100=18px, 125=23px, 150=27px; rows remain vanilla 28px.
        public int bookSkillIconSizePercent = 125;

        // 0 = left of book title (default); 1 = right of title.
        public int bookSkillIconPosition = 0;

        // WORK-FIX1: explicitly ordered reading ignores only Work joy-time limits.
        // Default ON for a newly created Easy Reading configuration.
        // Existing explicitly saved values are read through Scribe.
        public bool allowManualReadingDuringWork = true;

        public bool enableDebugLogging = false;

        public override void ExposeData()
        {
            base.ExposeData();
            EasyReadingSettingsData.Expose(this);
            Scribe_Values.Look(ref enableDebugLogging, "enableDebugLogging", false);
        }
    }
}
