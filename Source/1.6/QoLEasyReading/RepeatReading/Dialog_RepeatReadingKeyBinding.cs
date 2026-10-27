using UnityEngine;
using Verse;

namespace ZCN.QoL.EasyReading
{
    /// <summary>
    /// Captures a single key in a focused popup, rather than globally
    /// listening to every keyboard event during gameplay.
    /// </summary>
    internal sealed class Dialog_RepeatReadingKeyBinding : Window
    {
        private readonly EasyReadingSettings settings;
        private readonly bool preset;
        private string feedback;

        public override Vector2 InitialSize { get { return new Vector2(510f, 215f); } }

        public Dialog_RepeatReadingKeyBinding(EasyReadingSettings settings, bool preset)
        {
            this.settings = settings;
            this.preset = preset;
            doCloseX = true;
            closeOnCancel = true;
            closeOnAccept = false;
            closeOnClickedOutside = false;
            absorbInputAroundWindow = true;
        }

        public override void DoWindowContents(Rect rect)
        {
            Event ev = Event.current;
            if (ev != null && ev.type == EventType.KeyDown && ev.keyCode != KeyCode.None)
            {
                KeyCode pressed = ev.keyCode;
                ev.Use();
                if (pressed == KeyCode.Escape)
                {
                    Close();
                    return;
                }
                string error;
                if (RepeatReadingShortcut.TryAssign(settings, preset, pressed, out error))
                {
                    Close();
                    return;
                }
                feedback = error;
            }

            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, 6f, rect.width, 34f),
                preset ? EasyReadingI18n.Get("ER_PresetKeyTitle") : EasyReadingI18n.Get("ER_DialogKeyTitle"));
            Text.Font = GameFont.Small;
            Widgets.Label(new Rect(0f, 54f, rect.width, 30f),
                EasyReadingI18n.Get("ER_PressKey"));
            Widgets.Label(new Rect(0f, 87f, rect.width, 48f),
                feedback ?? EasyReadingI18n.Get("ER_KeyInstructions"));
            if (Widgets.ButtonText(new Rect(rect.width - 135f, rect.height - 41f,
                    135f, 34f), EasyReadingI18n.Get("ER_Cancel")))
                Close();
        }
    }
}
