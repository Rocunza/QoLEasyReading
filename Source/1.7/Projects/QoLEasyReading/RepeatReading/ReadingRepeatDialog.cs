using System;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ZCN.QoL.EasyReading
{

    /// <summary>
    /// Stage 3F popup: queue the actual selected count (1..50).
    /// Stage 4S-2E3: show explicit textbook skills; hide the entire row for
    /// books with no BookOutcomeDoerGainSkillExp reward. Ordinary reading may
    /// still grant Intellectual XP; absence of this doer is NOT 'no XP'.
    /// Queue, shortcut, confirmation, and cancellation behavior are unchanged.
    /// The initial slider value comes from the saved preset.
    /// </summary>
    internal sealed class Dialog_RepeatReadingStage3F : Window
    {
        private readonly Pawn pawn;
        private readonly Book book;
        private int repeatCount;
        // Both mouse confirmation and keyboard acceptance share one guarded path.
        private bool confirmationStarted;
        private bool skillDisplayLogged;
        private readonly SkillDef firstBookSkill;
        private readonly SkillDef secondBookSkill;

        public override Vector2 InitialSize
        {
            get { return new Vector2(520f, firstBookSkill == null ? 290f : 342f); }
        }

        public Dialog_RepeatReadingStage3F(Pawn pawn, Book book, int startingCount)
        {
            this.pawn = pawn;
            this.book = book;
            repeatCount = Mathf.Clamp(startingCount, 1, 50);
            // Read the explicit textbook rewards once. A custom modded book
            // with broken reward data must not prevent opening this dialog.
            SkillDef first = null;
            SkillDef second = null;
            try
            {
                BookSkillIconCatalog.GetBookSkills(book, out first, out second);
            }
            catch (Exception ex)
            {
                Log.ErrorOnce("[QoL: Easy Reading][RepeatDialog] Could not read " +
                    "book skills; hiding the skill row: " + ex, 1726194523);
            }
            firstBookSkill = first;
            secondBookSkill = second;
            doCloseX = true;
            // WindowStack routes the Accept key to OnAcceptKeyPressed only when
            // closeOnAccept is enabled. Our override confirms instead of just closing.
            closeOnAccept = true;
            // Native RimWorld Escape handling closes the window without queueing.
            closeOnCancel = true;
            closeOnClickedOutside = false;
            absorbInputAroundWindow = true;
            forcePause = true;
        }

        public override void OnAcceptKeyPressed()
        {
            // WindowStack calls this when RimWorld's Accept key is pressed.
            // Use one guarded path for Return, numpad Enter, and mouse.
            string trigger = "accept-key";
            if (Event.current != null && Event.current.type == EventType.KeyDown)
            {
                if (Event.current.keyCode == KeyCode.KeypadEnter)
                    trigger = "keypad-enter";
                else if (Event.current.keyCode == KeyCode.Return)
                    trigger = "return-key";
                Event.current.Use();
            }
            ConfirmAndQueue(trigger);
        }

        public override void OnCancelKeyPressed()
        {
            // Escape must never invoke ConfirmAndQueue(). The native window
            // handler closes/consumes Cancel for us.
            if (EasyReadingDebug.Enabled)
                Log.Message("[QoL: Easy Reading] DEBUG Stage 3H DIALOG canceled: " +
                    "source=cancel-key, queuedJobsByDialog=0.");
            base.OnCancelKeyPressed();
        }

        private void ConfirmAndQueue(string trigger = "mouse-button")
        {
            if (confirmationStarted || !IsOpen)
            {
                return;
            }

            confirmationStarted = true;
            int selectedCount = Mathf.Clamp(repeatCount, 1, 50);
            if (EasyReadingDebug.Enabled)
                Log.Message("[QoL: Easy Reading] DEBUG repeat-reading Stage 3H " +
                    "DIALOG confirmed: pawn=" +
                (pawn != null ? pawn.LabelShort : "none") +
                ", book=" + (book != null ? book.LabelCap : "none") +
                ", requestedCount=" + selectedCount + ", trigger=" + trigger + ".");
            Close();
            RepeatReadingQueueStage3F.QueueRequested(pawn, book, "dialog", selectedCount);
        }

        public override void DoWindowContents(Rect inRect)
        {
            // Unity's keypad-enter can be distinct from the RimWorld Accept binding.
            // The guard in ConfirmAndQueue prevents duplicate enqueueing if the
            // native accept dispatcher also handles this key.
            Event currentEvent = Event.current;
            if (currentEvent != null && currentEvent.type == EventType.KeyDown &&
                currentEvent.keyCode == KeyCode.KeypadEnter)
            {
                currentEvent.Use();
                ConfirmAndQueue("keypad-enter");
                return;
            }

            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, 4f, inRect.width, 34f),
                EasyReadingI18n.Get("ER_RepeatTitle"));
            Text.Font = GameFont.Small;

            Widgets.Label(new Rect(0f, 44f, inRect.width, 26f),
                EasyReadingI18n.Get("ER_PawnLabel") + (pawn != null ? pawn.LabelShort : EasyReadingI18n.Get("ER_Unknown")));
            Widgets.Label(new Rect(0f, 72f, inRect.width, 46f),
                EasyReadingI18n.Get("ER_BookLabel") + (book != null ? book.LabelCap : EasyReadingI18n.Get("ER_Unknown")));
            // Textbook-specific skills only. Ordinary books still can award
            // reading-related Intellectual XP; do not display "None (no XP)".
            if (firstBookSkill != null)
            {
                // A broken custom icon must not block the dialog's actions.
                try
                {
                    DrawBookSkills(inRect.width);
                }
                catch (Exception ex)
                {
                    Log.ErrorOnce("[QoL: Easy Reading][RepeatDialog] Skill display skipped: " + ex,
                        1726194522);
                }
            }
            else if (!skillDisplayLogged && EasyReadingDebug.Enabled &&
                Event.current != null && Event.current.type == EventType.Repaint)
            {
                skillDisplayLogged = true;
                Log.Message("[QoL: Easy Reading][RepeatDialog] Explicit textbook " +
                    "skill row hidden; no BookOutcomeDoerGainSkillExp rewards; " +
                    "ordinary reading XP unaffected.");
            }

            // Preserve the original compact 4S-2E1 positions in ordinary books
            // so a two-line Book title does not overlap Repeat count.
            float countY = firstBookSkill == null ? 126f : 164f;
            float sliderY = firstBookSkill == null ? 156f : 194f;
            float descriptionY = firstBookSkill == null ? 190f : 230f;
            Widgets.Label(new Rect(0f, countY, inRect.width, 25f),
                EasyReadingI18n.Format("ER_RepeatCount", repeatCount));

            repeatCount = Mathf.RoundToInt(Widgets.HorizontalSlider(
                new Rect(10f, sliderY, inRect.width - 20f, 24f),
                repeatCount, 1f, 50f, true));

            Widgets.Label(new Rect(0f, descriptionY, inRect.width, 22f),
                EasyReadingI18n.Get("ER_QueueDesc"));

            if (Widgets.ButtonText(new Rect(0f, inRect.height - 40f,
                    (inRect.width - 12f) / 2f, 36f), EasyReadingI18n.Get("ER_Cancel")))
            {
                Close();
            }
            if (Widgets.ButtonText(new Rect((inRect.width + 12f) / 2f,
                    inRect.height - 40f, (inRect.width - 12f) / 2f, 36f),
                    EasyReadingI18n.Get("ER_QueueCountButton")))
            {
                ConfirmAndQueue();
            }
        }

        private void DrawBookSkills(float contentWidth)
        {
            const float rowY = 124f;
            const float textY = 128f;
            const float labelStartX = 67f;
            const float iconTextGap = 5f;
            const float betweenSkills = 10f;

            Widgets.Label(new Rect(0f, textY, 64f, 25f), EasyReadingI18n.Get("ER_SkillsLabel"));

            SkillDef first = firstBookSkill;
            SkillDef second = secondBookSkill;
            if (!skillDisplayLogged && EasyReadingDebug.Enabled &&
                Event.current != null && Event.current.type == EventType.Repaint)
            {
                skillDisplayLogged = true;
                Log.Message("[QoL: Easy Reading][RepeatDialog] Book skill row: " +
                    first.defName + (second == null ? "" : ", " + second.defName) +
                    "; job controls unchanged=True.");
            }

            EasyReadingSettings settings = EasyReadingMod.Settings;
            int percent = settings == null ? 125 :
                BookContentsSkillIcons.NormalizeSize(settings.bookSkillIconSizePercent);
            float iconSize = percent == 100 ? 18f : (percent == 150 ? 27f : 23f);
            float availableWidth = Mathf.Max(0f, contentWidth - labelStartX);
            if (second == null)
            {
                DrawSkill(first, labelStartX, rowY, availableWidth,
                    iconSize, iconTextGap);
            }
            else
            {
                float halfWidth = Mathf.Max(0f, (availableWidth - betweenSkills) / 2f);
                DrawSkill(first, labelStartX, rowY, halfWidth,
                    iconSize, iconTextGap);
                DrawSkill(second, labelStartX + halfWidth + betweenSkills,
                    rowY, halfWidth, iconSize, iconTextGap);
            }
        }

        private static void DrawSkill(SkillDef skill, float x, float y, float width,
            float size, float iconTextGap)
        {
            if (skill == null || width <= 0f)
                return;
            float iconY = y + (32f - size) / 2f;
            Rect iconRect = new Rect(x, iconY, size, size);
            Texture2D texture = BookSkillIconCatalog.GetTexture(skill);
            if (texture != null && Event.current != null &&
                Event.current.type == EventType.Repaint)
            {
                Color previousColor = GUI.color;
                try
                {
                    GUI.color = Color.white;
                    GUI.DrawTexture(iconRect, texture);
                }
                finally { GUI.color = previousColor; }
            }

            string skillName = skill.LabelCap.ToString();
            float nameX = x + size + iconTextGap;
            float nameWidth = Mathf.Max(0f, width - size - iconTextGap);
            if (nameWidth > 0f)
            {
                Color previousColor = GUI.color;
                try
                {
                    // One color for all labels: the icons, not twelve
                    // competing font colors, identify the disciplines.
                    GUI.color = new Color(0.70f, 0.87f, 1f, previousColor.a);
                    Widgets.Label(new Rect(nameX, y + 4f, nameWidth, 25f),
                        skillName.Truncate(nameWidth));
                }
                finally { GUI.color = previousColor; }
            }
            if (Event.current != null)
                TooltipHandler.TipRegion(new Rect(x, y, width, 32f), skillName);
        }
    }
}
