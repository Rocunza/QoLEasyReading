using UnityEngine;
using Verse;

namespace ZCN.QoL.EasyReading
{
    internal enum RepeatReadingIntent
    {
        None,
        Preset,
        Dialog
    }

    /// <summary>
    /// Maps ONE configured held key + a vanilla Read-menu left click to an
    /// intent. Nothing is registered as a global hotkey. Other menu actions
    /// are never rewritten (ReadingRepeatProbe validates Book.Read separately).
    /// </summary>
    internal static class RepeatReadingShortcut
    {
        public const int DefaultPresetKey = (int)KeyCode.LeftControl;
        public const int DefaultDialogKey = (int)KeyCode.LeftAlt;

        public static void RestoreDefaults(EasyReadingSettings settings)
        {
            if (settings == null) return;
            settings.repeatReadingPresetKey = DefaultPresetKey;
            settings.repeatReadingDialogKey = DefaultDialogKey;
            settings.repeatReadingPresetShortcutEnabled = true;
            settings.repeatReadingDialogShortcutEnabled = true;
        }

        public static void NormalizeSettings(EasyReadingSettings settings)
        {
            if (settings == null) return;
            settings.repeatReadingPresetKey = NormalizeKey(settings.repeatReadingPresetKey, DefaultPresetKey);
            settings.repeatReadingDialogKey = NormalizeKey(settings.repeatReadingDialogKey, DefaultDialogKey);

            // An invalid or hand-edited config must not make the two commands
            // compete for the same keystroke. Preserve the preset choice.
            if (settings.repeatReadingPresetKey == settings.repeatReadingDialogKey)
            {
                settings.repeatReadingDialogKey = settings.repeatReadingPresetKey == DefaultDialogKey
                    ? DefaultPresetKey : DefaultDialogKey;
            }
        }

        public static bool TryAssign(EasyReadingSettings settings, bool preset,
            KeyCode rawKey, out string rejection)
        {
            rejection = null;
            if (settings == null)
            {
                rejection = EasyReadingI18n.Get("ER_KeyErrorSettings");
                return false;
            }
            KeyCode key = CanonicalKey(rawKey);
            if (!IsSupported(key))
            {
                rejection = EasyReadingI18n.Get("ER_KeyErrorAllowed");
                return false;
            }
            int other = preset ? settings.repeatReadingDialogKey : settings.repeatReadingPresetKey;
            if ((int)key == other)
            {
                rejection = EasyReadingI18n.Get("ER_KeyErrorDuplicate");
                return false;
            }
            if (preset)
                settings.repeatReadingPresetKey = (int)key;
            else
                settings.repeatReadingDialogKey = (int)key;
            return true;
        }

        public static string KeyLabel(int key)
        {
            KeyCode code = (KeyCode)NormalizeKey(key, DefaultPresetKey);
            if (code == KeyCode.LeftControl) return "Ctrl";
            if (code == KeyCode.LeftAlt) return "Alt";
            return code.ToString();
        }

        public static RepeatReadingIntent Detect(EasyReadingSettings settings)
        {
            if (settings == null) return RepeatReadingIntent.None;
            Event ev = Event.current;
            // FloatMenuOption.Chosen is called after Widgets.ButtonInvisible;
            // the GUI event may have already been marked Used. Do not depend
            // on ev.type == MouseUp here, but reject non-left mouse buttons.
            if (ev == null || (ev.type != EventType.Used && ev.button != 0))
                return RepeatReadingIntent.None;

            bool ctrl = ev.control || Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            bool alt = ev.alt || Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
            bool shift = ev.shift || Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            bool command = ev.command || Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand);

            // Shift remains exclusively vanilla QueueOrder, including
            // Ctrl+Shift+Read and Alt+Shift+Read. Mixed modifiers are not
            // equivalent to a single configured Ctrl/Alt binding.
            if (shift || command || (ctrl && alt))
                return RepeatReadingIntent.None;

            // Do not interpret Ctrl+X as Ctrl alone if X is assigned to
            // the other command, or vice versa. Two bound keys together
            // are ambiguous and should fall back to normal Read.
            if (RawPressed((KeyCode)settings.repeatReadingPresetKey, ctrl, alt) &&
                RawPressed((KeyCode)settings.repeatReadingDialogKey, ctrl, alt))
                return RepeatReadingIntent.None;

            bool presetHeld = settings.repeatReadingPresetShortcutEnabled &&
                Pressed((KeyCode)settings.repeatReadingPresetKey, ctrl, alt);
            bool dialogHeld = settings.repeatReadingDialogShortcutEnabled &&
                Pressed((KeyCode)settings.repeatReadingDialogKey, ctrl, alt);
            if (presetHeld == dialogHeld)
                return RepeatReadingIntent.None;

            return presetHeld ? RepeatReadingIntent.Preset : RepeatReadingIntent.Dialog;
        }

        private static bool RawPressed(KeyCode configured, bool ctrl, bool alt)
        {
            configured = CanonicalKey(configured);
            if (!IsSupported(configured)) return false;
            if (configured == KeyCode.LeftControl) return ctrl;
            if (configured == KeyCode.LeftAlt) return alt;
            return Input.GetKey(configured);
        }

        private static bool Pressed(KeyCode configured, bool ctrl, bool alt)
        {
            configured = CanonicalKey(configured);
            if (!IsSupported(configured)) return false;
            if (configured == KeyCode.LeftControl) return ctrl && !alt;
            if (configured == KeyCode.LeftAlt) return alt && !ctrl;
            // Letter/function-key bindings require zero modifiers. This also
            // avoids capturing Ctrl+C or Alt+X used by another mod.
            return !ctrl && !alt && Input.GetKey(configured);
        }

        private static int NormalizeKey(int raw, int fallback)
        {
            KeyCode canonical = CanonicalKey((KeyCode)raw);
            return IsSupported(canonical) ? (int)canonical : fallback;
        }

        private static KeyCode CanonicalKey(KeyCode code)
        {
            if (code == KeyCode.RightControl) return KeyCode.LeftControl;
            if (code == KeyCode.RightAlt) return KeyCode.LeftAlt;
            return code;
        }

        private static bool IsSupported(KeyCode code)
        {
            return code == KeyCode.LeftControl || code == KeyCode.LeftAlt ||
                (code >= KeyCode.A && code <= KeyCode.Z) ||
                (code >= KeyCode.F6 && code <= KeyCode.F12);
        }
    }
}
