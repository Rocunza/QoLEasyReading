using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace ZCN.QoL.EasyReading
{
    // These are PRESENTATION choices, not SchematicResearchClassification values.
    // "Unknown" is part of the legacy PNG filename, not an error state icon.
    internal enum SchematicIconKind
    {
        ResearchAvailable,
        ReviewRequired,
        AllFinished,
        DuplicateOwned
    }

    /// <summary>
    /// S2-B2: independent read-only loader for the three user-supplied icons.
    /// Shared by existing Schematic UI and S3-B1 trade GUI on the main thread.
    /// Never holds a Book or research-state cache.
    /// </summary>
    internal static class SchematicStatusIconCatalog
    {
        private const string TextureRoot = "QoLEasyReading/Schematic/";
        private const uint RetryAfterFailureMs = 3000U;
        private static readonly Dictionary<SchematicIconKind, Texture2D> Cache =
            new Dictionary<SchematicIconKind, Texture2D>();
        private static readonly Dictionary<SchematicIconKind, int> LastFailedAt =
            new Dictionary<SchematicIconKind, int>();
        private static bool languageSeen;
        private static string lastLanguageFolder;

        internal static string GetPath(SchematicIconKind kind)
        {
            switch (kind)
            {
                case SchematicIconKind.ResearchAvailable:
                    return TextureRoot + "Schematic_Unknown";
                case SchematicIconKind.ReviewRequired:
                    return TextureRoot + "Schematic_Review";
                case SchematicIconKind.AllFinished:
                    return TextureRoot + "Schematic_Complete";
                case SchematicIconKind.DuplicateOwned:
                    return TextureRoot + "Schematic_DuplicateWarning";
                default:
                    return null;
            }
        }

        internal static Texture2D GetTexture(SchematicIconKind kind)
        {
            string path = GetPath(kind);
            if (path == null)
                return null;

            ObserveLanguage();
            Texture2D cached;
            bool wasCached = Cache.TryGetValue(kind, out cached);
            // Unity's overloaded null detects a destroyed native texture.
            if (wasCached && cached != null)
                return cached;

            int now = Environment.TickCount;
            int failedAt;
            if (LastFailedAt.TryGetValue(kind, out failedAt) &&
                unchecked((uint)(now - failedAt)) < RetryAfterFailureMs)
                return null;

            try
            {
                Texture2D texture = ContentFinder<Texture2D>.Get(path, false);
                Cache[kind] = texture;
                if (texture == null)
                    LastFailedAt[kind] = now;
                else
                    LastFailedAt.Remove(kind);
                return texture;
            }
            catch (Exception)
            {
                // Missing/reloading resources must not break vanilla reading
                // or trade rendering; the explicit audit reports which failed.
                Cache.Remove(kind);
                LastFailedAt[kind] = now;
                return null;
            }
        }

        private static void ObserveLanguage()
        {
            string current = LanguageDatabase.activeLanguage == null
                ? "" : (LanguageDatabase.activeLanguage.folderName ?? "");
            if (!languageSeen)
            {
                languageSeen = true;
                lastLanguageFolder = current;
                return;
            }
            if (string.Equals(lastLanguageFolder, current, StringComparison.Ordinal))
                return;

            lastLanguageFolder = current;
            Cache.Clear();
            LastFailedAt.Clear();
            if (EasyReadingDebug.Enabled)
                Log.Message("[QoL: Easy Reading][SCHEMATIC-ASSETS] languageChanged=True; cacheReset=True; BUILD_ID=" +
                    EasyReadingBuildIdentity.BuildId + ";");
        }
    }
}
