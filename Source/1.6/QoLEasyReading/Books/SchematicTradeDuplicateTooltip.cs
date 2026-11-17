using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace ZCN.QoL.EasyReading
{
    /// <summary>
    /// S3-C2-T1: only the actual DuplicateOwned ! icon gets a short tooltip.
    /// RimWorld 1.6 registers vanilla DrawTransferableInfo's long TipRegion
    /// BEFORE the existing Harmony Postfix. ClearTooltipsFrom is guarded by
    /// Mouse.IsOver(iconRect) and Repaint; outside the icon it never executes.
    /// Never intercepts clicks, global tip rendering or any foreign UI region.
    /// </summary>
    internal static class SchematicTradeDuplicateTooltip
    {
        private const int MaxCacheKeys = 128;
        private static readonly Dictionary<string, SchematicDuplicateTooltipContent.Result> Cache =
            new Dictionary<string, SchematicDuplicateTooltipContent.Result>(StringComparer.Ordinal);
        private static SchematicOwnershipSnapshot cachedSnapshot;
        private static string cachedLanguage;
        private static int cachedSession = -1;
#if DEBUG
        private static readonly HashSet<string> LoggedReasons =
            new HashSet<string>(StringComparer.Ordinal);
#endif

        internal static void TryRegister(Rect iconRect, string fullKey,
            SchematicOwnershipSnapshot snapshot)
        {
            Event evt = Event.current;
            if (evt == null || evt.type != EventType.Repaint ||
                iconRect.width <= 0f || iconRect.height <= 0f ||
                string.IsNullOrEmpty(fullKey) || snapshot == null ||
                !TradeSession.Active ||
                EasyReadingMod.Settings == null ||
                !EasyReadingMod.Settings.showSchematicTradeIcons ||
                !Mouse.IsOver(iconRect))
                return;
            try
            {
                int sequence = SchematicTradeOwnershipSession.SessionSequence;
                string language = LanguageDatabase.activeLanguage == null
                    ? "" : (LanguageDatabase.activeLanguage.folderName ?? "");
                if (!object.ReferenceEquals(cachedSnapshot, snapshot) ||
                    cachedSession != sequence ||
                    !string.Equals(cachedLanguage, language, StringComparison.Ordinal))
                {
                    Cache.Clear();
#if DEBUG
                    LoggedReasons.Clear();
#endif
                    cachedSnapshot = snapshot;
                    cachedSession = sequence;
                    cachedLanguage = language;
                }
                SchematicDuplicateTooltipContent.Result value;
                if (!Cache.TryGetValue(fullKey, out value))
                {
                    value = SchematicDuplicateTooltipContent.Build(fullKey, snapshot);
                    if (Cache.Count < MaxCacheKeys)
                        Cache[fullKey] = value;
                }
                if (value == null || string.IsNullOrEmpty(value.Text))
                    return;
                // Verified in supplied RW1.6 DLL: the vanilla TipRegion for
                // DrawTransferableInfo is registered at the end of that method.
                // This Postfix executes afterward. ClearTooltipsFrom removes
                // current-frame tip registrations ONLY IF mouse is in iconRect.
                // Then our one icon-only signal is registered in its place.
                TooltipHandler.ClearTooltipsFrom(iconRect);
                TooltipHandler.TipRegion(iconRect,
                    new TipSignal(value.Text, StableSignalId(fullKey)));
#if DEBUG
                if (EasyReadingDebug.Enabled && LoggedReasons.Count < 12 &&
                    LoggedReasons.Add(value.Status + ":" + fullKey))
                    Log.Message("[QoL: Easy Reading][S3C2T1][TOOLTIP] seq=" + sequence +
                        "; status=" + value.Status +
                        "; distinctTitles=" + value.DistinctTitles +
                        "; verifiedBooks=" + value.VerifiedBooks +
                        "; scope=ICON_ONLY; vanillaTipAtIcon=REPLACED_THIS_FRAME; " +
                        "scansAdded=0; build=" + EasyReadingBuildIdentity.BuildId + ";");
#endif
            }
            catch (Exception ex)
            {
                // A tooltip failure cannot interfere with the original icon,
                // trade controls or any game's read/research code paths.
                Log.ErrorOnce("[QoL: Easy Reading][S3C2T1] Tooltip unavailable: " +
                    ex.GetType().Name, 1726194731);
            }
        }

        private static int StableSignalId(string key)
        {
            // Use one namespace-separated stable ID per project-set signature.
            // Never allocate a different ID on every GUI frame.
            unchecked
            {
                uint hash = 2166136261;
                for (int i = 0; i < key.Length; i++)
                    hash = (hash ^ key[i]) * 16777619;
                return (int)(hash ^ 0x6C32A195u);
            }
        }
    }
}
