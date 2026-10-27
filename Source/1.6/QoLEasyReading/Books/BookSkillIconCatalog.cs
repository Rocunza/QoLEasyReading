using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace ZCN.QoL.EasyReading
{
    /// <summary>
    /// BookUI-1: read the actual skill rewards from the book instance.
    /// Do not infer a skill from a generated title, untranslated text or quality.
    /// No caching by pawn/book identity: books can be regenerated or removed.
    /// </summary>
    internal static class BookSkillIconCatalog
    {
        private const string TextureRoot = "QoLEasyReading/BookSkills/";
        private static readonly Dictionary<string, Texture2D> Textures =
            new Dictionary<string, Texture2D>(StringComparer.Ordinal);
        private static readonly HashSet<string> Missing =
            new HashSet<string>(StringComparer.Ordinal);
        // Null or destroyed textures may occur while RimWorld reloads content
        // after a language switch. Bound retries so a missing asset cannot
        // cause a ContentFinder search on every GUI repaint.
        private static readonly Dictionary<string, int> FailedLoadAt =
            new Dictionary<string, int>(StringComparer.Ordinal);
        private const uint FailedRetryIntervalMs = 3000U;
        private static bool LanguageObserved;
        private static string LastLanguageFolder;
        private static bool AssetAuditComplete;
        private static readonly string[] BuiltInSkills =
        {
            "Shooting", "Melee", "Construction", "Mining", "Cooking", "Plants",
            "Animals", "Crafting", "Artistic", "Medicine", "Social", "Intellectual"
        };

        /// <summary>Returns at most 2 distinct skills. nulls mean no skill reward.</summary>
        internal static void GetBookSkills(Book book, out SkillDef first, out SkillDef second)
        {
            first = null;
            second = null;
            if (book == null || book.BookComp == null)
                return;

            foreach (BookOutcomeDoer doer in book.BookComp.Doers)
            {
                BookOutcomeDoerGainSkillExp skillReward = doer as BookOutcomeDoerGainSkillExp;
                if (skillReward == null || skillReward.Values == null)
                    continue;

                foreach (KeyValuePair<SkillDef, float> pair in skillReward.Values)
                {
                    if (pair.Key == null || pair.Value <= 0f)
                        continue;
                    if (first == null)
                        first = pair.Key;
                    else if (second == null && pair.Key != first)
                        second = pair.Key;
                    if (first != null && second != null)
                        return;
                }
            }
        }

        internal static Texture2D GetTexture(SkillDef skill)
        {
            if (skill == null)
                return null;
            string name = skill.defName;
            bool known = false;
            for (int i = 0; i < BuiltInSkills.Length; i++)
            {
                if (BuiltInSkills[i] == name)
                {
                    known = true;
                    break;
                }
            }
            // Custom skill defs use 'Other'; Medicine maps to Medical.png.
            string iconName = known ? (name == "Medicine" ? "Medical" : name) : "Other";
            Texture2D texture = GetAsset(iconName);
            if (texture == null && Missing.Add(iconName))
                Log.Warning("[QoL: Easy Reading][BookUI] Missing/unavailable icon: " +
                    TextureRoot + iconName + " (will retry; book row remains usable)");
            return texture;
        }

        // A language switch can rebuild mod resources while this class's
        // static dictionary outlives those resources. Detect the switch
        // lazily during UI draws; no Harmony hook or frame-wide scanning.
        private static void ObserveLanguage()
        {
            string current = LanguageDatabase.activeLanguage == null
                ? "" : LanguageDatabase.activeLanguage.folderName ?? "";
            if (!LanguageObserved)
            {
                LanguageObserved = true;
                LastLanguageFolder = current;
                return;
            }
            if (string.Equals(LastLanguageFolder, current, StringComparison.Ordinal))
                return;

            LastLanguageFolder = current;
            Textures.Clear();
            FailedLoadAt.Clear();
            Missing.Clear();
            AssetAuditComplete = false;
            if (EasyReadingDebug.Enabled)
                Log.Message("[QoL: Easy Reading][BookUI] Language changed; icon cache reset (" +
                    current + ").");
        }

        /// <summary>
        /// Explicit, opt-in boot/settings diagnostic; never scans all game books.
        /// Should not be called per-frame.
        /// </summary>
        internal static void AuditAssetsIfDebug()
        {
            if (!EasyReadingDebug.Enabled)
                return;
            ObserveLanguage();
            if (AssetAuditComplete)
                return;
            // Only call from a GUI event on Unity's main thread (settings / Contents).
            // Never call from EasyReadingMod's constructor: RimWorld loads mods async.
            if (Event.current == null)
                return;
            AssetAuditComplete = true;
            int found = 0;
            for (int i = 0; i < BuiltInSkills.Length; i++)
            {
                if (GetAsset(BuiltInSkills[i] == "Medicine" ? "Medical" : BuiltInSkills[i]) != null)
                    found++;
            }
            bool otherFound = GetAsset("Other") != null;
            // Check mapping, not just the presence of 12 PNGs: Medical.png
            // must map from RimWorld's real SkillDef "Medicine".
            bool medicineMapped = GetAsset("Medical") != null &&
                SkillDefOf.Medicine != null && SkillDefOf.Medicine.defName == "Medicine";
            Log.Message("[QoL: Easy Reading][BookUI] Asset audit: " + found +
                "/12 skill icons; custom-skill fallback=" +
                (otherFound ? "present" : "MISSING") +
                "; Medicine->Medical=" + (medicineMapped ? "OK" : "FAIL") +
                "; size=" + (EasyReadingMod.Settings == null ? -1 :
                    EasyReadingMod.Settings.bookSkillIconSizePercent) +
                "% ; position=" + (EasyReadingMod.Settings != null &&
                    EasyReadingMod.Settings.bookSkillIconPosition == 1 ? "Right" : "Left") +
                "; enabled=" + (EasyReadingMod.Settings != null &&
                EasyReadingMod.Settings.showBookSkillIcons) + ".");
        }

        private static Texture2D GetAsset(string iconName)
        {
            ObserveLanguage();
            Texture2D texture;
            bool wasCached = Textures.TryGetValue(iconName, out texture);
            // Unity's == null also detects native-destroyed Texture2D objects,
            // even when their managed reference still exists in our dictionary.
            if (wasCached && texture != null)
                return texture;

            // If the resource holder is temporarily unavailable during a
            // language/content reload, retry periodically without scanning
            // on every book row's GUI repaint.
            int now = Environment.TickCount;
            int failedAt;
            if (wasCached && FailedLoadAt.TryGetValue(iconName, out failedAt) &&
                unchecked((uint)(now - failedAt)) < FailedRetryIntervalMs)
                return null;

            texture = ContentFinder<Texture2D>.Get(TextureRoot + iconName, false);
            Textures[iconName] = texture;
            if (texture == null)
            {
                FailedLoadAt[iconName] = now;
            }
            else
            {
                FailedLoadAt.Remove(iconName);
                Missing.Remove(iconName);
            }
            return texture;
        }
    }
}
