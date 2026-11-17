using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace ZCN.QoL.EasyReading
{
    internal static partial class BookTradeSkillIcons
    {
        [HarmonyPrefix]
        private static void Prefix(Transferable trad, ref Rect idRect, out RowState __state)
        {
            __state = default(RowState);
            activeLeftTitleShift = 0f;
            // Production cache lifetime is always maintained. Diagnostic
            // observers never determine transfer policy or paint geometry.
            try
            {
                SchematicTradeOwnershipSession.ResetIfInactive();
            }
            catch (Exception ex)
            {
                Log.ErrorOnce("[QoL: Easy Reading][S3C2] Session reset skipped: " + ex,
                    1726194683);
            }
#if DEBUG
            try
            {
                bool auditActive = EasyReadingDebug.Enabled && TradeSession.Active;
                if (!auditActive)
                {
                    SchematicTradeGroupDiagnostics.ResetIfInactive(false);
                    SchematicOwnershipAudit.ResetIfInactive(false);
                }
                else if (Event.current != null && Event.current.type == EventType.Repaint &&
                    trad is Tradeable)
                {
                    SchematicTradeGroupDiagnostics.Observe((Tradeable)trad);
                    if (EasyReadingMod.Settings != null &&
                        EasyReadingMod.Settings.showSchematicTradeIcons)
                        SchematicOwnershipAudit.Observe((Tradeable)trad);
                    else
                        SchematicOwnershipAudit.ResetIfInactive(false);
                }
            }
            catch (Exception ex)
            {
                // Debug observations must never break the vanilla trade row.
                Log.ErrorOnce("[QoL: Easy Reading][S3B0] Audit skipped: " + ex,
                    1726194650);
            }
#endif
            EasyReadingSettings settings = EasyReadingMod.Settings;
            if (settings == null ||
                (!settings.showTradeSkillIcons && !settings.showSchematicTradeIcons) ||
                !TradeSession.Active || !(trad is Tradeable) ||
                Event.current == null)
                return;
            try
            {
                Tradeable tradeable = (Tradeable)trad;
                // Fast path: ordinary merchandise is never sent through the
                // expensive, read-only Schematic group inspector.
                if (!tradeable.IsThing || !tradeable.HasAnyThing)
                    return;
                Thing sampleThing = tradeable.AnyThing;
                Book sampleBook = sampleThing == null ? null :
                    sampleThing.GetInnerIfMinified() as Book;
                if (sampleBook == null)
                    return;

                bool isSchematic = SchematicStatusIconResolver.IsSchematic(sampleBook);
                SkillDef first = null;
                SkillDef second = null;
                Texture2D schematicTexture = null;
                string duplicateProjectKey = null;
                SchematicOwnershipSnapshot duplicateOwnershipSnapshot = null;
                SchematicTradeIconResolver.Decision schematicDecision =
                    default(SchematicTradeIconResolver.Decision);
                if (isSchematic)
                {
                    if (!settings.showSchematicTradeIcons)
                        return;
                    // Entire colony+trader row is inspected; no transfer sign
                    // is used to choose the research icon. Debug B0 retains
                    // its independent sign observation for comparison.
                    SchematicTradeGroupResolver.GroupSnapshot snapshot =
                        SchematicTradeGroupResolver.Inspect(tradeable, 0);
                    string suppressed;
                    if (!SchematicTradeIconResolver.TryResolve(snapshot,
                        out schematicDecision, out suppressed))
                    {
                        LogSchematicDecision("SUPPRESSED", suppressed, null);
                        return;
                    }
                    // C2: retain the proven research-group fail-closed check.
                    // ONLY a trader-only purchase row with a verified player
                    // ownership match may replace '?' with '!'.
                    if (schematicDecision.Kind == SchematicIconKind.ResearchAvailable)
                    {
                        string fullKey = snapshot.Colony.SchematicBooks > 0
                            ? snapshot.Colony.UniformProjectSignature
                            : snapshot.Trader.UniformProjectSignature;
                        SchematicOwnershipSnapshot owned;
                        bool sessionValid = SchematicTradeOwnershipSession.TryGetSnapshot(out owned);
                        SchematicOwnershipDecision ownership = sessionValid && owned != null
                            ? owned.GetDecision(fullKey) : SchematicOwnershipDecision.Unknown;
                        int selectedCount = 0;
                        try { selectedCount = tradeable.CountToTransfer; }
                        catch (Exception) { /* direction uncertain, policy stays conservative */ }
                        string reason;
                        schematicDecision.Kind = SchematicTradePurchaseIconPolicy.Select(
                            schematicDecision.Kind, snapshot.InventorySides, selectedCount,
                            ownership, sessionValid, out reason);
#if DEBUG
                        SchematicTradeOwnershipSession.LogIconDecisionOnce(snapshot.InventorySides,
                            fullKey, ownership, schematicDecision.Kind, reason);
#endif
                        if (schematicDecision.Kind == SchematicIconKind.DuplicateOwned &&
                            owned != null)
                        {
                            duplicateProjectKey = fullKey;
                            duplicateOwnershipSnapshot = owned;
                        }
                    }
                    schematicTexture = SchematicStatusIconCatalog.GetTexture(
                        schematicDecision.Kind);
                    if (schematicTexture == null &&
                        schematicDecision.Kind == SchematicIconKind.DuplicateOwned)
                    {
                        // Never make a missing temporary asset hide the
                        // existing, verified research-available '?' icon.
                        LogSchematicDecision("FALLBACK", "DUPLICATE_TEXTURE_MISSING", null);
                        schematicDecision.Kind = SchematicIconKind.ResearchAvailable;
                        schematicTexture = SchematicStatusIconCatalog.GetTexture(
                            schematicDecision.Kind);
                    }
                    if (schematicTexture == null)
                    {
                        LogSchematicDecision("SUPPRESSED", "TEXTURE_MISSING", null);
                        return; // never reserve empty space
                    }
                    LogSchematicDecision("DECISION", schematicDecision.Kind.ToString(),
                        schematicDecision);
                }
                else
                {
                    if (!settings.showTradeSkillIcons || !(tradeable.AnyThing is Book) ||
                        !TryGetUniformSkills(tradeable, out first, out second))
                        return;
                }

                float iconSize = BookIconMetrics.IconSize(settings.bookSkillIconSizePercent);
                // Fit even the largest (27px) icon within the current row.
                // Smaller UI scales may make the row shorter.
                iconSize = Mathf.Min(iconSize, idRect.height - 2f);
                if (iconSize < 8f)
                    return;

                // Reserve only the actual skill slots (one or two).
                // Two-skill layout reserves the same width as the previous
                // calculation; one-skill titles no longer waste a blank slot.
                float reserve = LeftGap + iconSize +
                    (!isSchematic && second != null ? iconSize + SlotGap : 0f) + EndPadding;
                if (idRect.width < LeftContentWidth + MinimumNameWidth + reserve)
                    return; // Tiny windows: vanilla label wins; never overlap.

                bool placeLeft = settings.tradeSkillIconPosition == 0;
                if (placeLeft && !LeftTitleHookReady)
                {
                    if (!LoggedLeftHookWarning)
                    {
                        LoggedLeftHookWarning = true;
                        Log.Warning("[QoL: Easy Reading][TradeUI] Left markers " +
                            "hidden because title-indent hook is unavailable.");
                    }
                    return;
                }

                // RIGHT: F1 placement at the end of the item's name column.
                // LEFT: after the vanilla 27px thing icon and info button,
                // before the title that the narrow transpiler shifts right.
                float startX = placeLeft
                    ? idRect.x + LeftContentWidth + LeftGap
                    : idRect.xMax - reserve + LeftGap;
                float y = idRect.y + (idRect.height - iconSize) * 0.5f;
                __state.First = first;
                __state.Second = second;
                __state.Schematic = isSchematic;
                __state.SchematicKind = schematicDecision.Kind;
                __state.SchematicTexture = schematicTexture;
                __state.DuplicateProjectKey = duplicateProjectKey;
                __state.DuplicateOwnershipSnapshot = duplicateOwnershipSnapshot;
                __state.Left = placeLeft;
                __state.FirstIcon = new Rect(startX, y, iconSize, iconSize);
                __state.SecondIcon = new Rect(startX + iconSize + SlotGap, y,
                    iconSize, iconSize);
                __state.Paint = true;
                // S3-B2: separately prove the formerly suppressed 1/N, 2/N
                // research case reached a drawable row.  One log per session
                // only, through the existing bounded Debug/Repaint logger.
                if (isSchematic && schematicDecision.Finished > 0 &&
                    schematicDecision.Finished < schematicDecision.Total)
                    LogSchematicDecision("PARTIAL_DECISION",
                        schematicDecision.Kind.ToString(), schematicDecision);

                if (placeLeft)
                {
                    // Only Widgets.Label's book title gets indented. The
                    // ordinary info button, icon and full-row tooltip remain.
                    activeLeftTitleShift = reserve;
                }
                else
                {
                    // Existing F1 path: clip label by narrowing idRect;
                    // do not shift the title or the trade controls.
                    idRect.width -= reserve;
                }
            }
            catch (Exception ex)
            {
                // Never impair an otherwise functional trade window.
                __state = default(RowState);
                activeLeftTitleShift = 0f;
                Log.ErrorOnce("[QoL: Easy Reading][TradeUI] Skill layout skipped: " + ex,
                    1726194620);
            }
        }

        [System.Diagnostics.Conditional("DEBUG")]
        private static void LogSchematicDecision(string type, string reason,
            SchematicTradeIconResolver.Decision? counts)
        {
#if DEBUG
            // GUI layout and repaint can call this many times per second.
            // Keep at most one log for each unique decision per running game.
            if (!EasyReadingDebug.Enabled || Event.current == null ||
                Event.current.type != EventType.Repaint ||
                LoggedSchematicDecisions.Count >= 24)
                return;
            string key = type + ":" + (reason ?? "NONE");
            if (!LoggedSchematicDecisions.Add(key)) return;
            string detail = "";
            if (counts.HasValue)
            {
                SchematicTradeIconResolver.Decision d = counts.Value;
                detail = "; finished=" + d.Finished + "; total=" + d.Total +
                    "; grantable=" + d.Grantable;
            }
            Log.Message("[QoL: Easy Reading][S3B2][TradeIcon][" + type +
                "] reason=" + reason + detail +
                "; group=UNIFORM_IF_DECIDED; directionUsedForDecision=False" +
                "; ownershipChecked=S3C2_ON_TRADE_RESEARCH_AVAILABLE; BUILD_ID=" +
                EasyReadingBuildIdentity.BuildId + ";");
#endif
        }

    }
}
